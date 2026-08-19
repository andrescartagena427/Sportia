using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sportia.Models;

namespace Sportia.Controllers
{
    public class AdminConfiguracionController : Controller
    {
        private readonly SportiaDbContext _context;

        public AdminConfiguracionController(
            SportiaDbContext context)
        {
            _context = context;
        }


        // ==========================================
        // MOSTRAR CONFIGURACIÓN
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var configuracion = await _context.Configuraciones
                .FirstOrDefaultAsync();

            // Si todavía no existe configuración
            if (configuracion == null)
            {
                configuracion = new Configuracion
                {
                    NombrePlataforma = "Sportia",
                    Pais = "Colombia",
                    PermitirRegistros = true,
                    Notificaciones = true,
                    ModoMantenimiento = false
                };

                _context.Configuraciones.Add(configuracion);

                await _context.SaveChangesAsync();
            }

            return View(configuracion);
        }


        // ==========================================
        // GUARDAR CONFIGURACIÓN
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(
            Configuracion configuracion)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", configuracion);
            }


            var configuracionBD = await _context.Configuraciones
                .FirstOrDefaultAsync();


            // Si no existe, crearla
            if (configuracionBD == null)
            {
                _context.Configuraciones.Add(configuracion);
            }
            else
            {
                configuracionBD.NombrePlataforma =
                    configuracion.NombrePlataforma;

                configuracionBD.Correo =
                    configuracion.Correo;

                configuracionBD.Telefono =
                    configuracion.Telefono;

                configuracionBD.Pais =
                    configuracion.Pais;

                configuracionBD.Logo =
                    configuracion.Logo;

                configuracionBD.PermitirRegistros =
                    configuracion.PermitirRegistros;

                configuracionBD.Notificaciones =
                    configuracion.Notificaciones;

                configuracionBD.ModoMantenimiento =
                    configuracion.ModoMantenimiento;
            }


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Configuración guardada correctamente.";


            return RedirectToAction(nameof(Index));
        }
    }
}