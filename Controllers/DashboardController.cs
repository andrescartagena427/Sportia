using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sportia.Models;
using Sportia.Models.ViewModels;

namespace Sportia.Controllers
{
    public class DashboardController : Controller
    {
        private readonly SportiaDbContext _context;

        // Horario que se usa cuando un escenario no tiene
        // disponibilidad registrada para ese día de la semana.
        private const int HoraAperturaPorDefecto = 8;
        private const int HoraCierrePorDefecto = 22;

        public DashboardController(SportiaDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // DASHBOARD ADMINISTRADOR
        // /Dashboard?fecha=2026-10-03  (la fecha mueve el calendario)
        // =====================================================
        public async Task<IActionResult> Index(string? fecha)
        {
            // =================================================
            // VERIFICAR SESIÓN
            // =================================================
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            int? idRol = HttpContext.Session.GetInt32("IdRol");

            if (idUsuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            if (idRol != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // =================================================
            // FECHAS DE REFERENCIA
            // =================================================
            var ahora = DateTime.Now;
            var hoy = DateOnly.FromDateTime(ahora);
            var ayer = hoy.AddDays(-1);

            var fechaCalendario = hoy;
            if (!string.IsNullOrWhiteSpace(fecha) &&
                DateOnly.TryParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaElegida))
            {
                fechaCalendario = fechaElegida;
            }

            // Semana actual (lunes a domingo) y la anterior
            int diasDesdeLunes = ((int)hoy.DayOfWeek + 6) % 7;
            var inicioSemana = hoy.AddDays(-diasDesdeLunes);
            var inicioSemanaAnterior = inicioSemana.AddDays(-7);

            // =================================================
            // DATOS BASE (se cargan una sola vez)
            // =================================================
            var escenariosActivos = await _context.Escenarios
                .Include(e => e.IdTipoNavigation)
                .Where(e => e.Estado == true)
                .OrderBy(e => e.Nombre)
                .ToListAsync();

            var disponibilidades = await _context.DisponibilidadCanchas.ToListAsync();

            var desde = new[] { hoy.AddDays(-60), inicioSemanaAnterior, fechaCalendario }.Min();
            var hasta = new[] { hoy.AddDays(60), fechaCalendario }.Max();

            var reservas = await _context.Reservas
                .Include(r => r.IdClienteNavigation)
                .Include(r => r.IdEscenarioNavigation).ThenInclude(e => e.IdTipoNavigation)
                .Include(r => r.IdEstadoNavigation)
                .Where(r => r.FechaUso >= desde && r.FechaUso <= hasta)
                .ToListAsync();

            var reservasValidas = reservas
                .Where(r => !EsCancelada(r.IdEstadoNavigation?.Nombre))
                .ToList();

            var inicioPagos = new[] { hoy.AddDays(-7), inicioSemanaAnterior }.Min()
                .ToDateTime(TimeOnly.MinValue);

            var pagos = await _context.Pagos
                .Where(p => p.FechaPago != null && p.FechaPago >= inicioPagos)
                .Select(p => new { p.FechaPago, p.MontoPagado })
                .ToListAsync();

            decimal IngresosDel(DateOnly dia) => pagos
                .Where(p => DateOnly.FromDateTime(p.FechaPago!.Value) == dia)
                .Sum(p => p.MontoPagado);

            var vm = new DashboardAdminViewModel
            {
                NombreAdmin = HttpContext.Session.GetString("NombreUsuario") ?? "Admin",
                Hoy = hoy,
                FechaCalendario = fechaCalendario
            };

            var ultimos7 = Enumerable.Range(0, 7).Select(i => hoy.AddDays(-6 + i)).ToList();

            // =================================================
            // 1) RESERVAS DE HOY
            // =================================================
            vm.ReservasHoy = reservasValidas.Count(r => r.FechaUso == hoy);
            vm.VariacionReservas = Variacion(vm.ReservasHoy, reservasValidas.Count(r => r.FechaUso == ayer));
            vm.SerieReservas = ultimos7.Select(d => (double)reservasValidas.Count(r => r.FechaUso == d)).ToList();

            // =================================================
            // 2) INGRESOS DE HOY (pagos registrados hoy)
            // =================================================
            vm.IngresosHoy = IngresosDel(hoy);
            vm.VariacionIngresos = Variacion((double)vm.IngresosHoy, (double)IngresosDel(ayer));
            vm.SerieIngresos = ultimos7.Select(d => (double)IngresosDel(d)).ToList();

            // =================================================
            // 3) OCUPACIÓN DE CANCHAS (horas reservadas / horas abiertas)
            // =================================================
            vm.OcupacionHoy = (int)Math.Round(OcupacionTotal(hoy, escenariosActivos, disponibilidades, reservasValidas));
            var ocupacionAyer = OcupacionTotal(ayer, escenariosActivos, disponibilidades, reservasValidas);
            vm.VariacionOcupacion = Variacion(vm.OcupacionHoy, ocupacionAyer);

            // =================================================
            // 4) CLIENTES ACTIVOS (con reservas en los últimos 30 días)
            // =================================================
            vm.ClientesActivos = reservasValidas
                .Where(r => r.FechaUso > hoy.AddDays(-30) && r.FechaUso <= hoy)
                .Select(r => r.IdCliente).Distinct().Count();
            var clientesPeriodoAnterior = reservasValidas
                .Where(r => r.FechaUso > hoy.AddDays(-60) && r.FechaUso <= hoy.AddDays(-30))
                .Select(r => r.IdCliente).Distinct().Count();
            vm.VariacionClientes = Variacion(vm.ClientesActivos, clientesPeriodoAnterior);
            vm.SerieClientes = ultimos7
                .Select(d => (double)reservasValidas.Where(r => r.FechaUso == d).Select(r => r.IdCliente).Distinct().Count())
                .ToList();

            // =================================================
            // 5) CALENDARIO DE CANCHAS DEL DÍA ELEGIDO
            // =================================================
            var horarios = escenariosActivos
                .ToDictionary(e => e.IdEscenario, e => HorarioDe(e.IdEscenario, fechaCalendario, disponibilidades));

            int horaInicio = horarios.Any() ? horarios.Values.Min(h => h.apertura.Hour) : HoraAperturaPorDefecto;
            int horaFin = horarios.Any()
                ? horarios.Values.Max(h => h.cierre.Minute > 0 ? h.cierre.Hour + 1 : h.cierre.Hour)
                : HoraCierrePorDefecto;
            if (horaFin <= horaInicio) horaFin = horaInicio + 1;

            vm.HoraInicioCalendario = horaInicio;
            vm.HoraFinCalendario = horaFin;

            int totalSlots = (horaFin - horaInicio) * 2;   // cada slot = 30 minutos

            foreach (var esc in escenariosActivos)
            {
                var fila = new FilaCalendarioViewModel
                {
                    IdEscenario = esc.IdEscenario,
                    Nombre = esc.Nombre,
                    Tipo = esc.IdTipoNavigation?.Nombre ?? "Deporte",
                    Imagen = esc.Imagen
                };

                var (apertura, cierre) = horarios[esc.IdEscenario];

                // Estado de cada slot: null = libre, reserva = ocupado, "cerrado"
                var slots = new object?[totalSlots];
                for (int i = 0; i < totalSlots; i++)
                {
                    var inicioSlot = new TimeOnly(horaInicio, 0).AddMinutes(i * 30);
                    var finSlot = inicioSlot.AddMinutes(30);
                    bool abierto = inicioSlot >= apertura && (finSlot <= cierre || cierre == TimeOnly.MinValue);
                    if (!abierto) slots[i] = "cerrado";
                }

                var reservasDia = reservasValidas
                    .Where(r => r.IdEscenario == esc.IdEscenario && r.FechaUso == fechaCalendario)
                    .OrderBy(r => r.HoraInicio)
                    .ToList();

                foreach (var r in reservasDia)
                {
                    int desdeSlot = (int)Math.Floor((r.HoraInicio.ToTimeSpan().TotalMinutes - horaInicio * 60) / 30.0);
                    int hastaSlot = (int)Math.Ceiling((r.HoraFin.ToTimeSpan().TotalMinutes - horaInicio * 60) / 30.0);
                    desdeSlot = Math.Clamp(desdeSlot, 0, totalSlots);
                    hastaSlot = Math.Clamp(hastaSlot, 0, totalSlots);
                    for (int i = desdeSlot; i < hastaSlot; i++)
                    {
                        if (slots[i] is not Reserva) slots[i] = r;
                    }
                }

                // Convertir los slots en bloques visuales
                int s = 0;
                while (s < totalSlots)
                {
                    if (slots[s] is Reserva reserva)
                    {
                        int fin = s;
                        while (fin < totalSlots && ReferenceEquals(slots[fin], reserva)) fin++;
                        fila.Bloques.Add(new BloqueCalendarioViewModel
                        {
                            Tipo = "reserva",
                            Columna = s + 1,
                            Span = fin - s,
                            Cliente = NombreCorto(reserva.IdClienteNavigation),
                            Horario = $"{reserva.HoraInicio:HH\\:mm} - {reserva.HoraFin:HH\\:mm}",
                            Estado = reserva.IdEstadoNavigation?.Nombre ?? "Pendiente"
                        });
                        s = fin;
                    }
                    else
                    {
                        string tipo = (slots[s] as string) == "cerrado" ? "cerrado" : "libre";
                        int fin = s + 1;
                        // Los bloques libres se parten en cada hora en punto
                        while (fin < totalSlots && fin % 2 != 0 && !(slots[fin] is Reserva) && (((slots[fin] as string) == "cerrado") == (tipo == "cerrado")))
                        {
                            fin++;
                        }
                        fila.Bloques.Add(new BloqueCalendarioViewModel { Tipo = tipo, Columna = s + 1, Span = fin - s });
                        s = fin;
                    }
                }

                vm.Calendario.Add(fila);
            }

            // =================================================
            // 6) PRÓXIMAS RESERVAS
            // =================================================
            var horaActual = TimeOnly.FromDateTime(ahora);
            vm.ProximasReservas = reservasValidas
                .Where(r => r.FechaUso > hoy || (r.FechaUso == hoy && r.HoraFin > horaActual))
                .OrderBy(r => r.FechaUso).ThenBy(r => r.HoraInicio)
                .Take(5)
                .Select(Resumen)
                .ToList();

            // =================================================
            // 7) ESTADO DE LAS CANCHAS (ocupación de hoy)
            // =================================================
            vm.EstadoCanchas = escenariosActivos
                .Select(e =>
                {
                    var (horasReservadas, horasAbiertas) = HorasDelDia(e.IdEscenario, hoy, disponibilidades, reservasValidas);
                    return new EstadoCanchaViewModel
                    {
                        Nombre = e.Nombre,
                        Tipo = e.IdTipoNavigation?.Nombre ?? "Deporte",
                        HorasReservadas = Math.Round(horasReservadas, 1),
                        HorasDisponibles = Math.Round(horasAbiertas, 1),
                        Porcentaje = horasAbiertas > 0 ? (int)Math.Round(horasReservadas * 100 / horasAbiertas) : 0
                    };
                })
                .OrderByDescending(e => e.Porcentaje)
                .ThenBy(e => e.Nombre)
                .Take(4)
                .ToList();

            // =================================================
            // 8) INGRESOS DE LA SEMANA (lunes a domingo)
            // =================================================
            string[] etiquetas = { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };
            for (int i = 0; i < 7; i++)
            {
                var dia = inicioSemana.AddDays(i);
                vm.IngresosSemana.Add(new IngresoDiaViewModel
                {
                    Etiqueta = etiquetas[i],
                    Fecha = dia,
                    Total = IngresosDel(dia),
                    EsHoy = dia == hoy
                });
            }
            vm.TotalIngresosSemana = vm.IngresosSemana.Sum(d => d.Total);
            decimal semanaAnterior = Enumerable.Range(0, 7).Sum(i => IngresosDel(inicioSemanaAnterior.AddDays(i)));
            vm.VariacionIngresosSemana = Variacion((double)vm.TotalIngresosSemana, (double)semanaAnterior);

            // =================================================
            // 9) RESERVAS RECIENTES (las últimas registradas)
            // =================================================
            var recientes = await _context.Reservas
                .Include(r => r.IdClienteNavigation)
                .Include(r => r.IdEscenarioNavigation).ThenInclude(e => e.IdTipoNavigation)
                .Include(r => r.IdEstadoNavigation)
                .OrderByDescending(r => r.IdReserva)
                .Take(5)
                .ToListAsync();
            vm.ReservasRecientes = recientes.Select(Resumen).ToList();

            return View(vm);
        }

        // =====================================================
        // AYUDANTES
        // =====================================================

        // % de cambio entre dos valores. null = no hay base para comparar.
        private static double? Variacion(double actual, double anterior)
        {
            if (anterior == 0) return actual == 0 ? 0 : null;
            return Math.Round((actual - anterior) / anterior * 100, 1);
        }

        private static bool EsCancelada(string? estado) =>
            (estado ?? "").ToLowerInvariant().Contains("cancel");

        private static string NombreCorto(Cliente? c)
        {
            if (c == null) return "Cliente";
            var nombre = (c.Nombres ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            var inicial = string.IsNullOrWhiteSpace(c.Apellidos) ? "" : " " + c.Apellidos.Trim()[0] + ".";
            return (nombre + inicial).Trim();
        }

        private static ReservaResumenViewModel Resumen(Reserva r) => new()
        {
            IdReserva = r.IdReserva,
            Cliente = $"{r.IdClienteNavigation?.Nombres} {r.IdClienteNavigation?.Apellidos}".Trim(),
            Escenario = r.IdEscenarioNavigation?.Nombre ?? "",
            Tipo = r.IdEscenarioNavigation?.IdTipoNavigation?.Nombre ?? "",
            Imagen = r.IdEscenarioNavigation?.Imagen,
            Fecha = r.FechaUso,
            HoraInicio = r.HoraInicio,
            HoraFin = r.HoraFin,
            Estado = r.IdEstadoNavigation?.Nombre ?? "Pendiente",
            Valor = r.ValorTotal
        };

        private static string QuitarTildes(string texto)
        {
            var normalizado = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalizado)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            }
            return sb.ToString().ToLowerInvariant().Trim();
        }

        private static string NombreDia(DateOnly fecha) => fecha.DayOfWeek switch
        {
            DayOfWeek.Monday => "lunes",
            DayOfWeek.Tuesday => "martes",
            DayOfWeek.Wednesday => "miercoles",
            DayOfWeek.Thursday => "jueves",
            DayOfWeek.Friday => "viernes",
            DayOfWeek.Saturday => "sabado",
            _ => "domingo"
        };

        private static (TimeOnly apertura, TimeOnly cierre) HorarioDe(int idEscenario, DateOnly fecha, List<DisponibilidadCancha> disponibilidades)
        {
            var dia = NombreDia(fecha);
            var registro = disponibilidades.FirstOrDefault(d =>
                d.IdEscenario == idEscenario && QuitarTildes(d.DiaSemana) == dia);

            return registro != null
                ? (registro.HoraApertura, registro.HoraCierre)
                : (new TimeOnly(HoraAperturaPorDefecto, 0), new TimeOnly(HoraCierrePorDefecto, 0));
        }

        private static (double reservadas, double abiertas) HorasDelDia(int idEscenario, DateOnly fecha,
            List<DisponibilidadCancha> disponibilidades, List<Reserva> reservas)
        {
            var (apertura, cierre) = HorarioDe(idEscenario, fecha, disponibilidades);
            double abiertas = Math.Max(0, (cierre.ToTimeSpan() - apertura.ToTimeSpan()).TotalHours);

            double reservadas = reservas
                .Where(r => r.IdEscenario == idEscenario && r.FechaUso == fecha)
                .Sum(r =>
                {
                    var ini = r.HoraInicio < apertura ? apertura : r.HoraInicio;
                    var fin = r.HoraFin > cierre ? cierre : r.HoraFin;
                    return Math.Max(0, (fin.ToTimeSpan() - ini.ToTimeSpan()).TotalHours);
                });

            return (Math.Min(reservadas, abiertas), abiertas);
        }

        private static double OcupacionTotal(DateOnly fecha, List<Escenario> escenarios,
            List<DisponibilidadCancha> disponibilidades, List<Reserva> reservas)
        {
            double totalReservadas = 0, totalAbiertas = 0;
            foreach (var e in escenarios)
            {
                var (r, a) = HorasDelDia(e.IdEscenario, fecha, disponibilidades, reservas);
                totalReservadas += r;
                totalAbiertas += a;
            }
            return totalAbiertas > 0 ? totalReservadas * 100 / totalAbiertas : 0;
        }
    }
}
