using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sportia.Models;
using System.Security.Cryptography;
using System.Text;

namespace Sportia.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly SportiaDbContext _context;

        public UsuarioController(SportiaDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // MI PERFIL
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Perfil()
        {
            // Obtener usuario de la sesión
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            // Si no ha iniciado sesión
            if (idUsuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // Buscar usuario
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

            // Si no existe
            if (usuario == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction("Index", "Login");
            }

            return View(usuario);
        }


        // =========================================================
        // EDITAR PERFIL - MOSTRAR
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> EditarPerfil()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

            if (usuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            return View(usuario);
        }


        // =========================================================
        // EDITAR PERFIL - GUARDAR
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPerfil(
            string nombres,
            string apellidos,
            string correo,
            string telefono)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

            if (usuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // Verificar si el correo ya pertenece a otro usuario
            bool correoExiste = await _context.Usuarios
                .AnyAsync(u =>
                    u.Correo == correo &&
                    u.IdUsuario != idUsuario);

            if (correoExiste)
            {
                ViewBag.Error = "Ese correo ya está registrado por otro usuario.";

                return View(usuario);
            }

            // Actualizar datos
            usuario.Nombres = nombres;
            usuario.Apellidos = apellidos;
            usuario.Correo = correo;
            usuario.Telefono = telefono;

            await _context.SaveChangesAsync();

            // Actualizar nombre en sesión
            HttpContext.Session.SetString(
                "NombreUsuario",
                usuario.Nombres + " " + usuario.Apellidos);

            TempData["Mensaje"] = "Perfil actualizado correctamente.";

            return RedirectToAction("Perfil");
        }


        // =========================================================
        // CAMBIAR CONTRASEÑA - MOSTRAR
        // =========================================================

        [HttpGet]
        public IActionResult CambiarPassword()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            return View();
        }


        // =========================================================
        // CAMBIAR CONTRASEÑA - GUARDAR
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarPassword(
            string contrasenaActual,
            string nuevaContrasena,
            string confirmarContrasena)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // Buscar usuario
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

            if (usuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // Verificar contraseña actual
            if (!VerifyPassword(
                contrasenaActual,
                usuario.Password))
            {
                ViewBag.Error = "La contraseña actual es incorrecta.";

                return View();
            }

            // Verificar nueva contraseña
            if (string.IsNullOrWhiteSpace(nuevaContrasena))
            {
                ViewBag.Error = "Debe ingresar una nueva contraseña.";

                return View();
            }

            // Verificar que coincidan
            if (nuevaContrasena != confirmarContrasena)
            {
                ViewBag.Error = "Las nuevas contraseñas no coinciden.";

                return View();
            }

            // Guardar nueva contraseña
            usuario.Password = HashPassword(nuevaContrasena);

            await _context.SaveChangesAsync();

            TempData["Mensaje"] =
                "Contraseña cambiada correctamente.";

            return RedirectToAction("Perfil");
        }


        // =========================================================
        // ENCRIPTAR CONTRASEÑA
        // =========================================================

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes =
                    Encoding.UTF8.GetBytes(password);

                byte[] hash =
                    sha256.ComputeHash(bytes);

                return Convert.ToBase64String(hash);
            }
        }


        // =========================================================
        // VERIFICAR CONTRASEÑA
        // =========================================================

        private bool VerifyPassword(
            string password,
            string hashedPassword)
        {
            string hash = HashPassword(password);

            return hash == hashedPassword;
        }
    }
}