using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sportia.Models;

namespace Sportia.Controllers
{
    public class PagoController : Controller
    {
        private readonly SportiaDbContext _context;

        public PagoController(SportiaDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // VERIFICAR QUE SEA ADMINISTRADOR
        // =====================================================
        private bool EsAdministrador()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            int? idRol = HttpContext.Session.GetInt32("IdRol");

            return idUsuario != null && idRol == 1;
        }


        // =====================================================
        // LISTADO DE PAGOS / RESERVAS
        // GET: /Pago
        // =====================================================
        public async Task<IActionResult> Index()
        {
            // Verificar sesión
            if (!EsAdministrador())
            {
                return RedirectToAction("Index", "Login");
            }

            // Traer todas las reservas con:
            // Cliente
            // Escenario
            // Pagos
            var reservas = await _context.Reservas
                .Include(r => r.IdClienteNavigation)
                .Include(r => r.IdEscenarioNavigation)
                .Include(r => r.Pagos)
                    .ThenInclude(p => p.IdMetodoNavigation)
                .OrderByDescending(r => r.FechaReserva)
                .ToListAsync();

            return View(reservas);
        }


        // =====================================================
        // DETALLE DE PAGOS DE UNA RESERVA
        // GET: /Pago/Details/5
        // =====================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (!EsAdministrador())
            {
                return RedirectToAction("Index", "Login");
            }

            if (id == null)
            {
                return NotFound();
            }

            var reserva = await _context.Reservas
                .Include(r => r.IdClienteNavigation)
                .Include(r => r.IdEscenarioNavigation)
                .Include(r => r.Pagos)
                    .ThenInclude(p => p.IdMetodoNavigation)
                .FirstOrDefaultAsync(r => r.IdReserva == id);

            if (reserva == null)
            {
                return NotFound();
            }

            // Calcular cuánto se ha pagado
            decimal totalPagado = reserva.Pagos
                .Sum(p => p.MontoPagado);

            // Calcular saldo
            decimal saldoPendiente = reserva.ValorTotal - totalPagado;

            // Evitar valores negativos por seguridad
            if (saldoPendiente < 0)
            {
                saldoPendiente = 0;
            }

            ViewBag.TotalPagado = totalPagado;
            ViewBag.SaldoPendiente = saldoPendiente;

            return View(reserva);
        }


        // =====================================================
        // FORMULARIO PARA REGISTRAR PAGO
        // GET: /Pago/Create/5
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Create(int? id)
        {
            if (!EsAdministrador())
            {
                return RedirectToAction("Index", "Login");
            }

            if (id == null)
            {
                return NotFound();
            }

            var reserva = await _context.Reservas
                .Include(r => r.IdClienteNavigation)
                .Include(r => r.IdEscenarioNavigation)
                .Include(r => r.Pagos)
                .FirstOrDefaultAsync(r => r.IdReserva == id);

            if (reserva == null)
            {
                return NotFound();
            }

            // Calcular total pagado anteriormente
            decimal totalPagado = reserva.Pagos
                .Sum(p => p.MontoPagado);

            // Calcular saldo actual
            decimal saldoPendiente = reserva.ValorTotal - totalPagado;

            if (saldoPendiente < 0)
            {
                saldoPendiente = 0;
            }

            // Cargar métodos de pago
            var metodosPago = await _context.MetodosPagos
                .OrderBy(m => m.Nombre)
                .ToListAsync();

            ViewBag.TotalPagado = totalPagado;
            ViewBag.SaldoPendiente = saldoPendiente;
            ViewBag.MetodosPago = metodosPago;

            return View(reserva);
        }


        // =====================================================
        // GUARDAR PAGO
        // POST: /Pago/Create
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int IdReserva,
            int IdMetodo,
            decimal MontoPagado,
            string? Comprobante)
        {
            if (!EsAdministrador())
            {
                return RedirectToAction("Index", "Login");
            }

            // Buscar la reserva
            var reserva = await _context.Reservas
                .Include(r => r.Pagos)
                .FirstOrDefaultAsync(r => r.IdReserva == IdReserva);

            if (reserva == null)
            {
                return NotFound();
            }

            // =============================================
            // VALIDAR MONTO
            // =============================================

            if (MontoPagado <= 0)
            {
                TempData["Error"] = "El monto del pago debe ser mayor a $0.";

                return RedirectToAction(
                    nameof(Create),
                    new { id = IdReserva }
                );
            }

            // =============================================
            // CALCULAR PAGOS ANTERIORES
            // =============================================

            decimal totalPagadoAnterior = reserva.Pagos
                .Sum(p => p.MontoPagado);

            // =============================================
            // CALCULAR SALDO ACTUAL
            // =============================================

            decimal saldoActual = reserva.ValorTotal - totalPagadoAnterior;

            if (saldoActual < 0)
            {
                saldoActual = 0;
            }

            // =============================================
            // VALIDAR QUE NO PAGUE DE MÁS
            // =============================================

            if (MontoPagado > saldoActual)
            {
                TempData["Error"] =
                    $"El monto ingresado (${MontoPagado:N0}) " +
                    $"supera el saldo pendiente (${saldoActual:N0}).";

                return RedirectToAction(
                    nameof(Create),
                    new { id = IdReserva }
                );
            }

            // =============================================
            // CALCULAR NUEVO SALDO
            // =============================================

            decimal nuevoSaldo = saldoActual - MontoPagado;

            if (nuevoSaldo < 0)
            {
                nuevoSaldo = 0;
            }

            // =============================================
            // CREAR PAGO
            // =============================================

            var pago = new Pago
            {
                IdReserva = IdReserva,
                IdMetodo = IdMetodo,
                MontoPagado = MontoPagado,
                SaldoPendiente = nuevoSaldo,
                FechaPago = DateTime.Now,
                Comprobante = Comprobante
            };

            _context.Pagos.Add(pago);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Pago registrado correctamente. " +
                $"Saldo pendiente: ${nuevoSaldo:N0}.";

            return RedirectToAction(
                nameof(Details),
                new { id = IdReserva }
            );
        }
    }
}