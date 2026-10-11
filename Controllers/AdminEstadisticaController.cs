using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sportia.Models;
using Sportia.Models.ViewModels;

namespace Sportia.Controllers
{
    public class AdminEstadisticaController : Controller
    {
        private readonly SportiaDbContext _context;

        public AdminEstadisticaController(SportiaDbContext context)
        {
            _context = context;
        }

        // dias: 7, 30, 90, 365 o 0 (todo el tiempo)
        // actividad: días que muestra la gráfica de actividad (7, 14 o 30)
        public async Task<IActionResult> Index(int dias = 30, int actividad = 7)
        {
            // ---------- Solo el administrador ----------
            if (HttpContext.Session.GetInt32("IdUsuario") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            if (HttpContext.Session.GetInt32("IdRol") != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            if (dias is not (0 or 7 or 30 or 90 or 365)) dias = 30;
            if (actividad is not (7 or 14 or 30)) actividad = 7;

            var ahora = DateTime.Now;
            var hoy = DateTime.Today;

            // ---------- Datos (pocas columnas, se calcula en memoria) ----------
            var clientes = await _context.Clientes.AsNoTracking()
                .Select(c => c.FechaRegistro)
                .ToListAsync();

            var empresas = await _context.Empresas.AsNoTracking()
                .Select(e => e.Estado)
                .ToListAsync();

            var escenarios = await _context.Escenarios.AsNoTracking()
                .Select(e => new { e.IdEscenario, e.Nombre, e.Estado })
                .ToListAsync();

            var reservas = await _context.Reservas.AsNoTracking()
                .Select(r => new
                {
                    r.IdReserva,
                    r.IdEscenario,
                    r.FechaReserva,
                    r.FechaUso,
                    Estado = r.IdEstadoNavigation.Nombre
                })
                .ToListAsync();

            var pagos = await _context.Pagos.AsNoTracking()
                .Select(p => new
                {
                    p.IdPago,
                    p.IdReserva,
                    p.MontoPagado,
                    p.SaldoPendiente,
                    p.FechaPago,
                    Cliente = p.IdReservaNavigation.IdClienteNavigation.Nombres + " " + p.IdReservaNavigation.IdClienteNavigation.Apellidos,
                    Escenario = p.IdReservaNavigation.IdEscenarioNavigation.Nombre,
                    Estado = p.IdReservaNavigation.IdEstadoNavigation.Nombre
                })
                .ToListAsync();

            // Fecha en que se hizo cada reserva (si no tiene, la fecha de uso)
            DateTime FechaDe(DateTime? creada, DateOnly uso) => creada ?? uso.ToDateTime(TimeOnly.MinValue);
            bool Cancelada(string? estado) => (estado ?? "").ToLower().Contains("cancel");

            // ---------- Período actual y anterior ----------
            DateTime inicio;
            if (dias == 0)
            {
                var fechas = reservas.Select(r => FechaDe(r.FechaReserva, r.FechaUso))
                    .Concat(clientes.Where(f => f.HasValue).Select(f => f!.Value))
                    .Concat(pagos.Where(p => p.FechaPago.HasValue).Select(p => p.FechaPago!.Value))
                    .ToList();
                inicio = fechas.Any() ? fechas.Min().Date : hoy.AddDays(-30);
            }
            else
            {
                inicio = hoy.AddDays(-(dias - 1));
            }

            var largo = ahora - inicio;
            var inicioAnterior = inicio - largo;

            bool EnPeriodo(DateTime? f) => f.HasValue && f.Value >= inicio && f.Value <= ahora;
            bool EnAnterior(DateTime? f) => f.HasValue && f.Value >= inicioAnterior && f.Value < inicio;

            // Fechas de corte para las mini gráficas (8 puntos)
            const int puntos = 8;
            var cortes = Enumerable.Range(1, puntos)
                .Select(i => inicio + TimeSpan.FromTicks(largo.Ticks * i / puntos))
                .ToList();

            List<double> PorTramo(IEnumerable<DateTime> fechas)
            {
                var lista = fechas.ToList();
                var serie = new List<double>();
                DateTime desde = inicio;
                foreach (var hasta in cortes)
                {
                    serie.Add(lista.Count(f => f >= desde && f < hasta));
                    desde = hasta;
                }
                return serie;
            }

            var fechasReservas = reservas.Select(r => FechaDe(r.FechaReserva, r.FechaUso)).ToList();
            var escenariosActivos = escenarios.Where(e => e.Estado == true).Select(e => e.IdEscenario).ToHashSet();

            // ---------- Tarjetas ----------
            int clientesAntes = clientes.Count(f => f == null || f < inicio);

            var vm = new EstadisticasViewModel
            {
                Dias = dias,
                DiasActividad = actividad,
                TextoPeriodo = dias switch
                {
                    0 => "Todo el tiempo",
                    7 => "Últimos 7 días",
                    90 => "Últimos 90 días",
                    365 => "Último año",
                    _ => "Últimos 30 días"
                },

                Clientes = clientes.Count,
                VariacionClientes = Variacion(clientes.Count, clientesAntes),
                SerieClientes = cortes.Select(c => (double)clientes.Count(f => f == null || f < c)).ToList(),

                Empresas = empresas.Count,
                EmpresasActivas = empresas.Count(e => e == true),
                SerieEmpresas = PorTramo(fechasReservas),

                Escenarios = escenarios.Count,
                EscenariosActivos = escenariosActivos.Count,
                SerieEscenarios = PorTramo(reservas
                    .Where(r => escenariosActivos.Contains(r.IdEscenario) && !Cancelada(r.Estado))
                    .Select(r => FechaDe(r.FechaReserva, r.FechaUso))),

                Reservas = fechasReservas.Count(f => EnPeriodo(f)),
                VariacionReservas = dias == 0 ? null : Variacion(fechasReservas.Count(f => EnPeriodo(f)), fechasReservas.Count(f => EnAnterior(f))),
                SerieReservas = PorTramo(fechasReservas)
            };

            // ---------- Actividad de reservas (una por día) ----------
            for (int i = actividad - 1; i >= 0; i--)
            {
                var dia = hoy.AddDays(-i);
                vm.Actividad.Add(new PuntoGrafica
                {
                    Etiqueta = dia.ToString("d MMM", new System.Globalization.CultureInfo("es-CO")).Replace(".", ""),
                    Valor = fechasReservas.Count(f => f.Date == dia)
                });
            }

            // ---------- Estado financiero del período ----------
            var pagosPeriodo = pagos.Where(p => dias == 0 || EnPeriodo(p.FechaPago)).ToList();
            vm.Ingresos = pagosPeriodo.Sum(p => p.MontoPagado);

            // Lo pendiente es el saldo del último pago de cada reserva (sin canceladas)
            vm.Pendiente = pagosPeriodo
                .Where(p => !Cancelada(p.Estado))
                .GroupBy(p => p.IdReserva)
                .Sum(g => Math.Max(0, g.OrderByDescending(p => p.IdPago).First().SaldoPendiente));

            vm.VariacionIngresos = dias == 0
                ? null
                : Variacion((double)vm.Ingresos, (double)pagos.Where(p => EnAnterior(p.FechaPago)).Sum(p => p.MontoPagado));

            // ---------- Reservas por escenario (top 5 del período) ----------
            var reservasPeriodo = reservas
                .Where(r => (dias == 0 || EnPeriodo(FechaDe(r.FechaReserva, r.FechaUso))) && !Cancelada(r.Estado))
                .ToList();

            int totalPeriodo = reservasPeriodo.Count;
            vm.PorEscenario = reservasPeriodo
                .GroupBy(r => r.IdEscenario)
                .Select(g => new BarraEscenario
                {
                    Nombre = escenarios.FirstOrDefault(e => e.IdEscenario == g.Key)?.Nombre ?? "Escenario",
                    Cantidad = g.Count(),
                    Porcentaje = totalPeriodo == 0 ? 0 : (int)Math.Round(g.Count() * 100.0 / totalPeriodo)
                })
                .OrderByDescending(b => b.Cantidad)
                .ThenBy(b => b.Nombre)
                .Take(5)
                .ToList();

            // ---------- Pagos recientes ----------
            vm.PagosRecientes = pagos
                .OrderByDescending(p => p.FechaPago ?? DateTime.MinValue)
                .ThenByDescending(p => p.IdPago)
                .Take(5)
                .Select(p => new PagoReciente
                {
                    Cliente = p.Cliente.Trim(),
                    Escenario = p.Escenario,
                    Monto = p.MontoPagado,
                    Fecha = p.FechaPago,
                    Estado = Cancelada(p.Estado) ? "Cancelada" : p.SaldoPendiente > 0 ? "Pendiente" : "Pagado"
                })
                .ToList();

            return View(vm);
        }

        // null = no había datos antes para comparar
        private static double? Variacion(double actual, double anterior)
        {
            if (anterior == 0)
            {
                return actual == 0 ? 0 : null;
            }

            return Math.Round((actual - anterior) / anterior * 100, 0);
        }
    }
}
