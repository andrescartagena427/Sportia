using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sportia.Models;
using System.Security.Cryptography;
using System.Text;

namespace Sportia.Controllers
{
    public class LoginController : Controller
    {
        private readonly SportiaDbContext _context;

        public LoginController(SportiaDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // MOSTRAR LOGIN
        // =========================================================
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }


        // =========================================================
        // MOSTRAR REGISTRO
        // =========================================================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }


        // =========================================================
        // PROCESAR REGISTRO
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> Register(
            string nombres,
            string apellidos,
            string documento,
            string telefono,
            string correo,
            string contrasena,
            string confirmarContrasena)
        {
            // Verificar contraseñas
            if (contrasena != confirmarContrasena)
            {
                ViewBag.Error = "Las contraseñas no coinciden.";
                return View();
            }

            // Verificar correo
            var correoExiste = await _context.Usuarios
                .AnyAsync(u => u.Correo == correo);

            if (correoExiste)
            {
                ViewBag.Error = "Ya existe una cuenta con ese correo.";
                return View();
            }

            // Verificar documento
            var documentoExiste = await _context.Usuarios
                .AnyAsync(u => u.Documento == documento);

            if (documentoExiste)
            {
                ViewBag.Error = "Ya existe una cuenta con ese documento.";
                return View();
            }

            // =====================================================
            // CREAR USUARIO
            // =====================================================

            var usuario = new Usuario
            {
                // 2 = Cliente
                // Todos los usuarios que se registren desde aquí
                // serán clientes.
                IdRol = 2,

                Nombres = nombres,
                Apellidos = apellidos,
                Documento = documento,
                Telefono = telefono,
                Correo = correo,

                // Guardar contraseña encriptada
                Password = HashPassword(contrasena),

                Estado = true,
                FechaRegistro = DateTime.Now
            };

            _context.Usuarios.Add(usuario);

            await _context.SaveChangesAsync();

            // Mensaje para el login
            TempData["Mensaje"] =
                "Cuenta creada correctamente. Ahora puedes iniciar sesión.";

            // Después de registrarse va al login
            return RedirectToAction("Index", "Login");
        }


        // =========================================================
        // PROCESAR LOGIN
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> Index(
            string correo,
            string contrasena)
        {
            // Verificar campos vacíos
            if (string.IsNullOrWhiteSpace(correo) ||
                string.IsNullOrWhiteSpace(contrasena))
            {
                ViewBag.Error =
                    "Debe ingresar el correo y la contraseña.";

                return View();
            }

            // Buscar usuario activo
            var usuario = await _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .FirstOrDefaultAsync(u =>
                    u.Correo == correo &&
                    u.Estado == true);

            // Usuario no encontrado
            if (usuario == null)
            {
                ViewBag.Error =
                    "El correo no está registrado.";

                return View();
            }

            // =====================================================
            // VERIFICAR CONTRASEÑA
            // =====================================================

            if (!VerifyPassword(
                    contrasena,
                    usuario.Password))
            {
                ViewBag.Error =
                    "La contraseña es incorrecta.";

                return View();
            }


            // =====================================================
            // GUARDAR INFORMACIÓN EN SESIÓN
            // =====================================================

            HttpContext.Session.SetInt32(
                "IdUsuario",
                usuario.IdUsuario);

            HttpContext.Session.SetInt32(
                "IdRol",
                usuario.IdRol);

            HttpContext.Session.SetString(
                "NombreUsuario",
                usuario.Nombres + " " + usuario.Apellidos);

            HttpContext.Session.SetString(
                "Rol",
                usuario.IdRolNavigation.Nombre);


            // =====================================================
            // REDIRECCIÓN SEGÚN EL ROL
            // =====================================================

            // -----------------------------------------------------
            // ROL 1 = ADMINISTRADOR
            // -----------------------------------------------------
            if (usuario.IdRol == 1)
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }


            // -----------------------------------------------------
            // ROL 2 = CLIENTE
            // -----------------------------------------------------
            if (usuario.IdRol == 2)
            {
                return RedirectToAction(
                    "Index",
                    "Cliente");
            }


            // =====================================================
            // SI EL ROL NO ES VÁLIDO
            // =====================================================

            HttpContext.Session.Clear();

            ViewBag.Error =
                "El usuario no tiene un rol válido.";

            return View();
        }


        // =========================================================
        // CERRAR SESIÓN
        // =========================================================
        public IActionResult Logout()
        {
            // Eliminar toda la información de sesión
            HttpContext.Session.Clear();

            // Regresar a la página principal
            return RedirectToAction(
                "Index",
                "Home");
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
            string hash =
                HashPassword(password);

            return hash == hashedPassword;
        }
    }
}