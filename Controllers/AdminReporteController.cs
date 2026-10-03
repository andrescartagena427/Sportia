using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sportia.Models;
using Sportia.Models.ViewModels;

namespace Sportia.Controllers
{
    public class AdminReporteController : Controller
    {
        private readonly SportiaDbContext _context;
        private static readonly CultureInfo Co = new("es-CO");

        public AdminReporteController(SportiaDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // REPORTES
        // periodo: mes | mes-anterior | 7d | 30d | anio | todo
        // =====================================================
        public async Task<IActionResult> Index(string? periodo)
        {
            if (!EsAdmin())
            {
                return RedirectToAction("Index", "Login");
            }

            var hoy = DateOnly.FromDateTime(DateTime.Today);

            // ---------- Datos base (solo las columnas necesarias) ----------
            var reservas = await _context.Reservas
                .AsNoTracking()
                .Select(r => new
                {
                    r.IdReserva,
                    r.FechaUso,
                    r.ValorTotal,
                    Estado = r.IdEstadoNavigation.Nombre,
                    Escenario = r.IdEscenarioNavigation.Nombre
                })
                .ToListAsync();

            var pagos = await _context.Pagos
                .AsNoTracking()
                .Select(p => new
                {
                    p.IdReserva,
                    p.MontoPagado,
                    p.FechaPago,
                    Metodo = p.IdMetodoNavigation.Nombre
                })
                .ToListAsync();

            var fechasClientes = await _context.Clientes
                .AsNoTracking()
                .Select(c => c.FechaRegistro)
                .ToListAsync();

            // ---------- Período elegido y período anterior ----------
            var fechasDatos = reservas.Select(r => r.FechaUso)
                .Concat(fechasClientes.Where(f => f.HasValue).Select(f => DateOnly.FromDateTime(f!.Value)))
                .DefaultIfEmpty(hoy)
                .ToList();

            var (clave, desde, hasta, desdeAnt, hastaAnt, textoComparacion) = CalcularPeriodo(
                periodo, hoy, fechasDatos.Min(), fechasDatos.Max());

            bool Dentro(DateOnly f, DateOnly a, DateOnly b) => f >= a && f <= b;
            bool EsCancelada(string? e) => (e ?? "").ToLower().Contains("cancel");

            var validas = reservas.Where(r => !EsCancelada(r.Estado)).ToList();
            bool hayComparacion = clave != "todo";

            var vm = new ReporteViewModel
            {
                Periodo = clave,
                Desde = desde,
                Hasta = hasta,
                TextoRango = $"{Fecha(desde)} - {Fecha(hasta)}",
                TextoComparacion = textoComparacion
            };

            // ---------- Clientes ----------
            vm.TotalClientes = fechasClientes.Count;
            vm.ClientesNuevos = fechasClientes.Count(f => f.HasValue && Dentro(DateOnly.FromDateTime(f.Value), desde, hasta));
            int clientesAnt = fechasClientes.Count(f => f.HasValue && Dentro(DateOnly.FromDateTime(f.Value), desdeAnt, hastaAnt));
            vm.VariacionClientes = hayComparacion ? Variacion(vm.ClientesNuevos, clientesAnt) : null;

            // ---------- Empresas ----------
            vm.TotalEmpresas = await _context.Empresas.CountAsync();
            vm.EmpresasActivas = await _context.Empresas.CountAsync(e => e.Estado == true);

            // ---------- Reservas del período (sin canceladas) ----------
            vm.ReservasPeriodo = validas.Count(r => Dentro(r.FechaUso, desde, hasta));
            int reservasAnt = validas.Count(r => Dentro(r.FechaUso, desdeAnt, hastaAnt));
            vm.VariacionReservas = hayComparacion ? Variacion(vm.ReservasPeriodo, reservasAnt) : null;

            // ---------- Reservas de hoy (vs ayer) ----------
            vm.ReservasHoy = validas.Count(r => r.FechaUso == hoy);
            vm.VariacionHoy = Variacion(vm.ReservasHoy, validas.Count(r => r.FechaUso == hoy.AddDays(-1)));
            vm.SerieHoy = Enumerable.Range(0, 7)
                .Select(i => (double)validas.Count(r => r.FechaUso == hoy.AddDays(i - 6)))
                .ToList();

            // ---------- Ingresos recibidos en el período ----------
            DateOnly? FechaPago(DateTime? f) => f.HasValue ? DateOnly.FromDateTime(f.Value) : null;

            vm.Ingresos = pagos
                .Where(p => FechaPago(p.FechaPago) is DateOnly f && Dentro(f, desde, hasta))
                .Sum(p => p.MontoPagado);
            decimal ingresosAnt = pagos
                .Where(p => FechaPago(p.FechaPago) is DateOnly f && Dentro(f, desdeAnt, hastaAnt))
                .Sum(p => p.MontoPagado);
            vm.VariacionIngresos = hayComparacion ? Variacion((double)vm.Ingresos, (double)ingresosAnt) : null;

            // ---------- Saldo pendiente (todas las reservas activas) ----------
            var pagadoPorReserva = pagos
                .GroupBy(p => p.IdReserva)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.MontoPagado));

            var saldos = validas
                .Select(r => r.ValorTotal - (pagadoPorReserva.TryGetValue(r.IdReserva, out var pagado) ? pagado : 0))
                .Where(s => s > 0)
                .ToList();

            vm.SaldoPendiente = saldos.Sum();
            vm.ReservasConSaldo = saldos.Count;

            // ---------- Totales del sistema ----------
            vm.TotalUsuarios = await _context.Usuarios.CountAsync();
            vm.TotalEscenarios = await _context.Escenarios.CountAsync();
            vm.TotalPagos = pagos.Count;
            vm.TotalReservas = reservas.Count;

            // ---------- Gráficas (por día o por mes si el rango es largo) ----------
            var grupos = Agrupaciones(desde, hasta);

            vm.GraficaReservas = new SerieGrafica
            {
                Etiquetas = grupos.Select(g => g.etiqueta).ToList(),
                Valores = grupos.Select(g => (double)validas.Count(r => Dentro(r.FechaUso, g.ini, g.fin))).ToList()
            };

            vm.GraficaIngresos = new SerieGrafica
            {
                EsDinero = true,
                Etiquetas = grupos.Select(g => g.etiqueta).ToList(),
                Valores = grupos.Select(g => (double)pagos
                    .Where(p => FechaPago(p.FechaPago) is DateOnly f && Dentro(f, g.ini, g.fin))
                    .Sum(p => p.MontoPagado)).ToList()
            };

            vm.GraficaClientes = new SerieGrafica
            {
                Etiquetas = grupos.Select(g => g.etiqueta).ToList(),
                Valores = grupos.Select(g => (double)fechasClientes
                    .Count(f => f.HasValue && Dentro(DateOnly.FromDateTime(f.Value), g.ini, g.fin))).ToList()
            };

            vm.SerieReservasMini = vm.GraficaReservas.Valores;
            vm.SerieClientes = vm.GraficaClientes.Valores;

            // ---------- Detalles de cada pestaña ----------
            vm.ReservasPorEstado = reservas
                .Where(r => Dentro(r.FechaUso, desde, hasta))
                .GroupBy(r => string.IsNullOrWhiteSpace(r.Estado) ? "Sin estado" : r.Estado)
                .Select(g => new ItemReporte { Nombre = g.Key, Valor = g.Count() })
                .OrderByDescending(i => i.Valor)
                .ToList();

            vm.TopEscenarios = validas
                .Where(r => Dentro(r.FechaUso, desde, hasta))
                .GroupBy(r => r.Escenario ?? "Escenario")
                .Select(g => new ItemReporte
                {
                    Nombre = g.Key,
                    Valor = g.Count(),
                    Extra = "$" + g.Sum(r => r.ValorTotal).ToString("N0", Co)
                })
                .OrderByDescending(i => i.Valor)
                .Take(5)
                .ToList();

            vm.IngresosPorMetodo = pagos
                .Where(p => FechaPago(p.FechaPago) is DateOnly f && Dentro(f, desde, hasta) && p.MontoPagado > 0)
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Metodo) ? "Otro" : p.Metodo)
                .Select(g => new ItemReporte { Nombre = g.Key, Valor = (double)g.Sum(p => p.MontoPagado), Extra = $"{g.Count()} pago(s)" })
                .OrderByDescending(i => i.Valor)
                .ToList();

            vm.UsuariosPorRol = await _context.Usuarios
                .GroupBy(u => u.IdRolNavigation.Nombre)
                .Select(g => new ItemReporte { Nombre = g.Key, Valor = g.Count() })
                .ToListAsync();

            return View(vm);
        }


        // =====================================================
        // EXPORTAR LAS RESERVAS DEL PERÍODO (CSV para Excel)
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Exportar(string? periodo)
        {
            if (!EsAdmin())
            {
                return RedirectToAction("Index", "Login");
            }

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var primera = await _context.Reservas.Select(r => (DateOnly?)r.FechaUso).MinAsync() ?? hoy;
            var ultima = await _context.Reservas.Select(r => (DateOnly?)r.FechaUso).MaxAsync() ?? hoy;
            var (_, desde, hasta, _, _, _) = CalcularPeriodo(periodo, hoy, primera, ultima);

            var filas = await _context.Reservas
                .AsNoTracking()
                .Where(r => r.FechaUso >= desde && r.FechaUso <= hasta)
                .OrderBy(r => r.FechaUso).ThenBy(r => r.HoraInicio)
                .Select(r => new
                {
                    r.Codigo,
                    r.FechaUso,
                    r.HoraInicio,
                    r.HoraFin,
                    Cliente = r.IdClienteNavigation.Nombres + " " + r.IdClienteNavigation.Apellidos,
                    Escenario = r.IdEscenarioNavigation.Nombre,
                    Estado = r.IdEstadoNavigation.Nombre,
                    r.ValorTotal,
                    Pagado = r.Pagos.Sum(p => (decimal?)p.MontoPagado) ?? 0
                })
                .ToListAsync();

            string Celda(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"") + "\"";

            var csv = new StringBuilder();
            csv.AppendLine("Código;Fecha;Hora inicio;Hora fin;Cliente;Escenario;Estado;Valor total;Pagado;Saldo");

            foreach (var f in filas)
            {
                decimal saldo = (f.Estado ?? "").ToLower().Contains("cancel") ? 0 : Math.Max(0, f.ValorTotal - f.Pagado);

                csv.AppendLine(string.Join(";",
                    Celda(f.Codigo),
                    f.FechaUso.ToString("dd/MM/yyyy"),
                    f.HoraInicio.ToString("HH:mm"),
                    f.HoraFin.ToString("HH:mm"),
                    Celda(f.Cliente),
                    Celda(f.Escenario),
                    Celda(f.Estado),
                    f.ValorTotal.ToString("0", CultureInfo.InvariantCulture),
                    f.Pagado.ToString("0", CultureInfo.InvariantCulture),
                    saldo.ToString("0", CultureInfo.InvariantCulture)));
            }

            // Con BOM para que Excel muestre bien las tildes
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return File(bytes, "text/csv", $"reservas-sportia-{desde:yyyyMMdd}-{hasta:yyyyMMdd}.csv");
        }


        // =====================================================
        // AYUDAS
        // =====================================================

        private bool EsAdmin() =>
            HttpContext.Session.GetInt32("IdUsuario") != null &&
            HttpContext.Session.GetInt32("IdRol") == 1;

        private static (string clave, DateOnly desde, DateOnly hasta, DateOnly desdeAnt, DateOnly hastaAnt, string comparacion)
            CalcularPeriodo(string? periodo, DateOnly hoy, DateOnly primeraFecha, DateOnly ultimaFecha)
        {
            var inicioMes = new DateOnly(hoy.Year, hoy.Month, 1);

            switch (periodo)
            {
                case "mes-anterior":
                {
                    var ini = inicioMes.AddMonths(-1);
                    return ("mes-anterior", ini, inicioMes.AddDays(-1), ini.AddMonths(-1), ini.AddDays(-1), "vs. mes anterior");
                }
                case "7d":
                    return ("7d", hoy.AddDays(-6), hoy, hoy.AddDays(-13), hoy.AddDays(-7), "vs. 7 días anteriores");
                case "30d":
                    return ("30d", hoy.AddDays(-29), hoy, hoy.AddDays(-59), hoy.AddDays(-30), "vs. 30 días anteriores");
                case "anio":
                {
                    var ini = new DateOnly(hoy.Year, 1, 1);
                    return ("anio", ini, new DateOnly(hoy.Year, 12, 31), ini.AddYears(-1), ini.AddDays(-1), "vs. año anterior");
                }
                case "todo":
                {
                    var ini = primeraFecha < hoy ? primeraFecha : hoy;
                    var fin = ultimaFecha > hoy ? ultimaFecha : hoy;
                    return ("todo", ini, fin, ini, ini.AddDays(-1), "histórico completo");
                }
                default:
                    return ("mes", inicioMes, inicioMes.AddMonths(1).AddDays(-1), inicioMes.AddMonths(-1), inicioMes.AddDays(-1), "vs. mes anterior");
            }
        }

        // Grupos de la gráfica: por día si el rango es corto, por mes si es largo
        private static List<(string etiqueta, DateOnly ini, DateOnly fin)> Agrupaciones(DateOnly desde, DateOnly hasta)
        {
            var lista = new List<(string, DateOnly, DateOnly)>();
            if (hasta < desde) hasta = desde;

            if (hasta.DayNumber - desde.DayNumber <= 62)
            {
                for (var d = desde; d <= hasta; d = d.AddDays(1))
                {
                    lista.Add((d.ToString("d MMM", Co).Replace(".", ""), d, d));
                }
            }
            else
            {
                var mes = new DateOnly(desde.Year, desde.Month, 1);
                while (mes <= hasta)
                {
                    var fin = mes.AddMonths(1).AddDays(-1);
                    var etiqueta = mes.ToString("MMM yy", Co).Replace(".", "");
                    lista.Add((char.ToUpper(etiqueta[0]) + etiqueta[1..], mes < desde ? desde : mes, fin > hasta ? hasta : fin));
                    mes = mes.AddMonths(1);
                }
            }

            return lista;
        }

        private static double? Variacion(double actual, double anterior)
        {
            if (anterior == 0) return actual == 0 ? 0 : null;
            return Math.Round((actual - anterior) / anterior * 100, 1);
        }

        private static string Fecha(DateOnly f)
        {
            var texto = f.ToString("dd MMM yyyy", Co).Replace(".", "");
            return texto.Length > 3 ? texto[..3] + char.ToUpper(texto[3]) + texto[4..] : texto;
        }
    }
}
