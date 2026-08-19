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
            IQueryable<Pago> pagos = _context.Pagos
                .Include(p => p.IdReservaNavigation)
                    .ThenInclude(r => r.IdClienteNavigation)
                .Include(p => p.IdReservaNavigation)
                    .ThenInclude(r => r.IdEscenarioNavigation)
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
            if (id == null)
            {
                return NotFound();
            }

            var pago = await _context.Pagos
                .Include(p => p.IdMetodoNavigation)
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