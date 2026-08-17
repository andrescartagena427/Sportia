using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sportia.Models;

namespace Sportia.Controllers
{
    public class ReservasController : Controller
    {
        private readonly SportiaDbContext _context;

        public ReservasController(SportiaDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // LISTADO DE RESERVAS
        // =====================================================

        public async Task<IActionResult> Index()
        {
            // Verificar sesión
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            int? idRol = HttpContext.Session.GetInt32("IdRol");

            if (idUsuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // Solo administrador
            if (idRol != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            var reservas = await _context.Reservas
                .Include(r => r.IdClienteNavigation)
                .Include(r => r.IdEscenarioNavigation)
                .Include(r => r.IdEstadoNavigation)
                .OrderByDescending(r => r.FechaReserva)
                .ToListAsync();

            return View(reservas);
        }


        // =====================================================
        // CREAR RESERVA - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Verificar sesión
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

            // Cargar datos para comprobar IDs
            ViewBag.Clientes = await _context.Clientes
                .OrderBy(c => c.IdCliente)
                .ToListAsync();

            ViewBag.Escenarios = await _context.Escenarios
                .OrderBy(e => e.IdEscenario)
                .ToListAsync();

            ViewBag.Estados = await _context.EstadosReservas
                .OrderBy(e => e.IdEstado)
                .ToListAsync();

            return View();
        }


        // =====================================================
        // CREAR RESERVA - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reserva reserva)
        {
            // Usuario de la sesión
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
            // VALIDACIONES
            // =================================================

            if (reserva.IdCliente <= 0)
            {
                ModelState.AddModelError(
                    "IdCliente",
                    "Debes seleccionar un cliente."
                );
            }

            if (reserva.IdEscenario <= 0)
            {
                ModelState.AddModelError(
                    "IdEscenario",
                    "Debes seleccionar un escenario."
                );
            }

            if (reserva.IdEstado <= 0)
            {
                ModelState.AddModelError(
                    "IdEstado",
                    "Debes seleccionar un estado."
                );
            }

            if (reserva.HoraFin <= reserva.HoraInicio)
            {
                ModelState.AddModelError(
                    "HoraFin",
                    "La hora de finalización debe ser mayor que la hora de inicio."
                );
            }

            if (reserva.FechaUso < DateOnly.FromDateTime(DateTime.Today))
            {
                ModelState.AddModelError(
                    "FechaUso",
                    "La fecha de uso no puede ser anterior a hoy."
                );
            }

            // =================================================
            // COMPROBAR CLIENTE
            // =================================================

            var clienteExiste = await _context.Clientes
                .AnyAsync(c => c.IdCliente == reserva.IdCliente);

            if (!clienteExiste)
            {
                ModelState.AddModelError(
                    "IdCliente",
                    "El cliente seleccionado no existe."
                );
            }

            // =================================================
            // COMPROBAR ESCENARIO
            // =================================================

            var escenarioExiste = await _context.Escenarios
                .AnyAsync(e => e.IdEscenario == reserva.IdEscenario);

            if (!escenarioExiste)
            {
                ModelState.AddModelError(
                    "IdEscenario",
                    "El escenario seleccionado no existe."
                );
            }

            // =================================================
            // COMPROBAR ESTADO
            // =================================================

            var estadoExiste = await _context.EstadosReservas
                .AnyAsync(e => e.IdEstado == reserva.IdEstado);

            if (!estadoExiste)
            {
                ModelState.AddModelError(
                    "IdEstado",
                    "El estado seleccionado no existe."
                );
            }

            // =================================================
            // SI HAY ERRORES
            // =================================================

            if (!ModelState.IsValid)
            {
                ViewBag.Clientes = await _context.Clientes
                    .OrderBy(c => c.IdCliente)
                    .ToListAsync();

                ViewBag.Escenarios = await _context.Escenarios
                    .OrderBy(e => e.IdEscenario)
                    .ToListAsync();

                ViewBag.Estados = await _context.EstadosReservas
                    .OrderBy(e => e.IdEstado)
                    .ToListAsync();

                return View(reserva);
            }

            // =================================================
            // DATOS AUTOMÁTICOS
            // =================================================

            reserva.IdUsuario = idUsuario.Value;

            reserva.FechaReserva = DateTime.Now;

            // Generar código de reserva
            reserva.Codigo = "RES-" +
                             DateTime.Now.ToString("yyyyMMddHHmmss");

            // =================================================
            // GUARDAR
            // =================================================

            _context.Reservas.Add(reserva);

            await _context.SaveChangesAsync();

            // =================================================
            // VOLVER AL DASHBOARD
            // =================================================

            return RedirectToAction("Index", "Dashboard");
        }
    }
}