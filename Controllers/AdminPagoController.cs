using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sportia.Models;

namespace Sportia.Controllers
{
    public class AdminPagoController : Controller
    {
        private readonly SportiaDbContext _context;

        public AdminPagoController(SportiaDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // LISTAR PAGOS
        // =====================================================
        public async Task<IActionResult> Index(string? buscar)
        {
            if (!EsAdmin())
            {
                return RedirectToAction("Index", "Login");
            }

            IQueryable<Pago> pagos = _context.Pagos
                .Include(p => p.IdReservaNavigation)
                    .ThenInclude(r => r.IdClienteNavigation)
                .Include(p => p.IdReservaNavigation)
                    .ThenInclude(r => r.IdEscenarioNavigation)
                .Include(p => p.IdReservaNavigation)
                    .ThenInclude(r => r.IdEstadoNavigation)
                .Include(p => p.IdMetodoNavigation);

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.Trim();

                pagos = pagos.Where(p =>
                    p.IdReservaNavigation.Codigo.Contains(buscar) ||
                    p.IdReservaNavigation.IdClienteNavigation.Nombres.Contains(buscar) ||
                    p.IdReservaNavigation.IdClienteNavigation.Apellidos.Contains(buscar) ||
                    p.IdReservaNavigation.IdEscenarioNavigation.Nombre.Contains(buscar) ||
                    p.IdMetodoNavigation.Nombre.Contains(buscar)
                );
            }

            pagos = pagos.OrderByDescending(p => p.FechaPago);

            ViewBag.Buscar = buscar;

            return View(await pagos.ToListAsync());
        }


        // =====================================================
        // CREAR PAGO - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!EsAdmin())
            {
                return RedirectToAction("Index", "Login");
            }

            await CargarReservas();
            await CargarMetodosPago();

            return View();
        }


        // =====================================================
        // CREAR PAGO - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Pago pago)
        {
            if (!EsAdmin())
            {
                return RedirectToAction("Index", "Login");
            }

            // No validar propiedades de navegación
            ModelState.Remove(nameof(Pago.IdReservaNavigation));
            ModelState.Remove(nameof(Pago.IdMetodoNavigation));

            if (!ModelState.IsValid)
            {
                await CargarReservas();
                await CargarMetodosPago();

                return View(pago);
            }

            var reserva = await _context.Reservas
                .FirstOrDefaultAsync(r => r.IdReserva == pago.IdReserva);

            if (reserva == null)
            {
                ModelState.AddModelError(
                    nameof(Pago.IdReserva),
                    "La reserva seleccionada no existe."
                );

                await CargarReservas();
                await CargarMetodosPago();

                return View(pago);
            }


            // =================================================
            // CALCULAR TOTAL PAGADO ANTERIORMENTE
            // =================================================

            var totalPagadoAnterior = await _context.Pagos
                .Where(p => p.IdReserva == pago.IdReserva)
                .SumAsync(p => (decimal?)p.MontoPagado) ?? 0;


            // Saldo antes del nuevo pago
            var saldoActual = reserva.ValorTotal - totalPagadoAnterior;


            // =================================================
            // VALIDAR MONTO
            // =================================================

            if (pago.MontoPagado <= 0)
            {
                ModelState.AddModelError(
                    nameof(Pago.MontoPagado),
                    "El monto debe ser mayor a cero."
                );
            }

            if (saldoActual <= 0)
            {
                ModelState.AddModelError(
                    nameof(Pago.IdReserva),
                    "Esta reserva ya está completamente pagada."
                );
            }

            if (pago.MontoPagado > saldoActual)
            {
                ModelState.AddModelError(
                    nameof(Pago.MontoPagado),
                    $"El monto supera el saldo pendiente de {saldoActual:N0}."
                );
            }


            if (!ModelState.IsValid)
            {
                await CargarReservas();
                await CargarMetodosPago();

                return View(pago);
            }


            // =================================================
            // CALCULAR NUEVO SALDO
            // =================================================

            pago.SaldoPendiente =
                saldoActual - pago.MontoPagado;

            pago.FechaPago ??= DateTime.Now;


            // =================================================
            // GUARDAR
            // =================================================

            try
            {
                _context.Pagos.Add(pago);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Pago registrado correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    "",
                    "No se pudo registrar el pago."
                );

                await CargarReservas();
                await CargarMetodosPago();

                return View(pago);
            }
        }


        // =====================================================
        // DETALLES DEL PAGO
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (!EsAdmin())
            {
                return RedirectToAction("Index", "Login");
            }

            if (id == null)
            {
                return NotFound();
            }

            var pago = await _context.Pagos
                .Include(p => p.IdMetodoNavigation)
                .Include(p => p.IdReservaNavigation)
                    .ThenInclude(r => r.IdEstadoNavigation)
                .Include(p => p.IdReservaNavigation)
                    .ThenInclude(r => r.IdClienteNavigation)
                .Include(p => p.IdReservaNavigation)
                    .ThenInclude(r => r.IdEscenarioNavigation)
                .FirstOrDefaultAsync(p => p.IdPago == id);

            if (pago == null)
            {
                return NotFound();
            }

            return View(pago);
        }


        // =====================================================
        // CONFIRMAR PAGO (efectivo / transferencia recibidos)
        // Deja el saldo en $0 y la reserva como "Confirmada".
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarPago(int id, string? volverA)
        {
            if (!EsAdmin())
            {
                return RedirectToAction("Index", "Login");
            }

            var pago = await _context.Pagos
                .Include(p => p.IdReservaNavigation)
                    .ThenInclude(r => r.IdEstadoNavigation)
                .FirstOrDefaultAsync(p => p.IdPago == id);

            if (pago == null)
            {
                return NotFound();
            }

            var reserva = pago.IdReservaNavigation;

            if ((reserva.IdEstadoNavigation?.Nombre ?? "").ToLower().Contains("cancel"))
            {
                TempData["Error"] = $"La reserva {reserva.Codigo} está cancelada; no se puede confirmar su pago.";
                return VolverDespuesDeConfirmar(volverA, id);
            }

            var pagosReserva = await _context.Pagos
                .Where(p => p.IdReserva == reserva.IdReserva)
                .ToListAsync();

            decimal faltante = reserva.ValorTotal - pagosReserva.Sum(p => p.MontoPagado);

            if (faltante <= 0 && pago.SaldoPendiente <= 0)
            {
                TempData["Error"] = $"El pago de la reserva {reserva.Codigo} ya estaba completo.";
                return VolverDespuesDeConfirmar(volverA, id);
            }

            if (faltante > 0)
            {
                pago.MontoPagado += faltante;
            }

            // La reserva queda sin saldo pendiente
            foreach (var p in pagosReserva)
            {
                p.SaldoPendiente = 0;
            }

            pago.FechaPago = DateTime.Now;

            string nota = $"Pago recibido y confirmado por el administrador el {DateTime.Now:dd/MM/yyyy HH:mm}.";
            pago.Comprobante = string.IsNullOrWhiteSpace(pago.Comprobante) || pago.Comprobante.Contains("pendiente")
                ? nota
                : (pago.Comprobante + " · " + nota);

            if (pago.Comprobante.Length > 255)
            {
                pago.Comprobante = pago.Comprobante[..255];
            }

            var estadoConfirmada = await _context.EstadosReservas
                .FirstOrDefaultAsync(e => e.Nombre.ToLower().Contains("confirm"));

            if (estadoConfirmada != null && (reserva.IdEstadoNavigation?.Nombre ?? "").ToLower().Contains("pend"))
            {
                reserva.IdEstado = estadoConfirmada.IdEstado;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Pago de la reserva {reserva.Codigo} confirmado. La reserva quedó pagada.";
            return VolverDespuesDeConfirmar(volverA, id);
        }

        private IActionResult VolverDespuesDeConfirmar(string? volverA, int id) =>
            volverA == "detalle"
                ? RedirectToAction(nameof(Details), new { id })
                : RedirectToAction(nameof(Index));


        // =====================================================
        // SOLO ADMINISTRADORES
        // =====================================================
        private bool EsAdmin() =>
            HttpContext.Session.GetInt32("IdUsuario") != null &&
            HttpContext.Session.GetInt32("IdRol") == 1;


        // =====================================================
        // CARGAR RESERVAS
        // =====================================================
        private async Task CargarReservas()
        {
            var reservas = await _context.Reservas
                .Include(r => r.IdClienteNavigation)
                .Include(r => r.IdEscenarioNavigation)
                .OrderByDescending(r => r.FechaReserva)
                .ToListAsync();

            ViewBag.Reservas = reservas
                .Select(r => new SelectListItem
                {
                    Value = r.IdReserva.ToString(),

                    Text =
                        $"{r.Codigo} - " +
                        $"{r.IdClienteNavigation.Nombres} " +
                        $"{r.IdClienteNavigation.Apellidos} - " +
                        $"{r.IdEscenarioNavigation.Nombre} - " +
                        $"${r.ValorTotal:N0}"
                })
                .ToList();
        }


        // =====================================================
        // CARGAR MÉTODOS DE PAGO
        // =====================================================
        private async Task CargarMetodosPago()
        {
            ViewBag.MetodosPago = await _context.MetodosPagos
                .OrderBy(m => m.Nombre)
                .Select(m => new SelectListItem
                {
                    Value = m.IdMetodo.ToString(),
                    Text = m.Nombre
                })
                .ToListAsync();
        }
    }
}