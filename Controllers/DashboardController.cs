using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sportia.Models;

namespace Sportia.Controllers
{
    public class DashboardController : Controller
    {
        private readonly SportiaDbContext _context;

        public DashboardController(SportiaDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // DASHBOARD ADMINISTRADOR
        // =====================================================
        public async Task<IActionResult> Index()
        {
            // Verificar que haya una sesión iniciada
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            int? idRol = HttpContext.Session.GetInt32("IdRol");

            // Si no ha iniciado sesión
            if (idUsuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // Si no es administrador
            if (idRol != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // =================================================
            // DATOS DEL DASHBOARD
            // =================================================

            ViewBag.TotalUsuarios = await _context.Usuarios.CountAsync();

            ViewBag.TotalClientes = await _context.Clientes.CountAsync();

            ViewBag.TotalEscenarios = await _context.Escenarios.CountAsync();

            ViewBag.TotalReservas = await _context.Reservas.CountAsync();

            ViewBag.TotalEmpresas = await _context.Empresas.CountAsync();

            ViewBag.TotalPagos = await _context.Pagos.CountAsync();

            return View();
        }
    }
}