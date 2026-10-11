using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sportia.Models;
using Sportia.Models.ViewModels;

namespace Sportia.Controllers
{
    public class AdminUsuarioController : Controller
    {
        private readonly SportiaDbContext _context;

        // Id del rol Administrador (el mismo que se valida en todo el proyecto)
        private const int RolAdministrador = 1;

        public AdminUsuarioController(SportiaDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // SOLO EL ADMINISTRADOR PUEDE ENTRAR A ESTAS PÁGINAS
        // =========================================================

        private IActionResult? ValidarAdmin()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            int? idRol = HttpContext.Session.GetInt32("IdRol");

            if (idUsuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            if (idRol != RolAdministrador)
            {
                return RedirectToAction("Index", "Home");
            }

            return null;
        }

        private int IdUsuarioActual => HttpContext.Session.GetInt32("IdUsuario") ?? 0;

        // ¿Quedaría el sistema sin ningún administrador activo?
        private async Task<bool> EsUltimoAdminActivo(int idUsuario)
        {
            return !await _context.Usuarios.AnyAsync(u =>
                u.IdUsuario != idUsuario &&
                u.IdRol == RolAdministrador &&
                u.Estado == true);
        }

        // GET: /AdminUsuario
        public async Task<IActionResult> Index(string? buscar, int? rol, string? estado, int pagina = 1)
        {
            var bloqueo = ValidarAdmin();
            if (bloqueo != null) return bloqueo;

            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            int idActual = IdUsuarioActual;

            var datos = await _context.Usuarios
                .AsNoTracking()
                .Select(u => new
                {
                    u.IdUsuario,
                    u.Nombres,
                    u.Apellidos,
                    u.Documento,
                    u.Telefono,
                    u.Correo,
                    u.IdRol,
                    Rol = u.IdRolNavigation.Nombre,
                    u.Estado,
                    u.FechaRegistro,
                    Reservas = u.Reservas.Count()
                })
                .ToListAsync();

            // ---------- Tarjetas (hoy vs. inicio del mes) ----------
            var antes = datos.Where(d => d.FechaRegistro == null || d.FechaRegistro < inicioMes).ToList();

            int total = datos.Count;
            int admins = datos.Count(d => d.IdRol == RolAdministrador);
            int clientes = datos.Count(d => d.IdRol != RolAdministrador);
            int activos = datos.Count(d => d.Estado == true);

            // ---------- Filtros ----------
            string texto = (buscar ?? "").Trim();
            string filtroEstado = (estado ?? "").Trim().ToLower();

            var filas = datos
                .OrderByDescending(d => d.FechaRegistro ?? DateTime.MinValue)
                .ThenByDescending(d => d.IdUsuario)
                .Select(d => new UsuarioFilaViewModel
                {
                    IdUsuario = d.IdUsuario,
                    Nombre = $"{d.Nombres} {d.Apellidos}".Trim(),
                    Documento = d.Documento,
                    Telefono = d.Telefono,
                    Correo = d.Correo,
                    IdRol = d.IdRol,
                    Rol = d.Rol,
                    Activo = d.Estado == true,
                    FechaRegistro = d.FechaRegistro,
                    Reservas = d.Reservas,
                    EsUsuarioActual = d.IdUsuario == idActual
                });

            if (texto.Length > 0)
            {
                filas = filas.Where(f =>
                    Contiene(f.Nombre, texto) ||
                    Contiene(f.Documento, texto) ||
                    Contiene(f.Telefono, texto) ||
                    Contiene(f.Correo, texto));
            }

            if (rol.HasValue && rol > 0) filas = filas.Where(f => f.IdRol == rol);
            if (filtroEstado == "activo") filas = filas.Where(f => f.Activo);
            if (filtroEstado == "inactivo") filas = filas.Where(f => !f.Activo);

            var lista = filas.ToList();

            const int tamano = 10;
            int paginas = Math.Max(1, (int)Math.Ceiling(lista.Count / (double)tamano));
            pagina = Math.Clamp(pagina, 1, paginas);

            var paginaActual = lista.Skip((pagina - 1) * tamano).Take(tamano).ToList();
            for (int i = 0; i < paginaActual.Count; i++)
            {
                paginaActual[i].Numero = (pagina - 1) * tamano + i + 1;
            }

            var vm = new AdminUsuariosViewModel
            {
                Buscar = texto,
                IdRol = rol,
                Estado = filtroEstado,
                Roles = await _context.Roles.AsNoTracking().OrderBy(r => r.IdRol).ToListAsync(),

                Total = total,
                VariacionTotal = Variacion(total, antes.Count),
                Administradores = admins,
                VariacionAdministradores = Variacion(admins, antes.Count(d => d.IdRol == RolAdministrador)),
                Clientes = clientes,
                VariacionClientes = Variacion(clientes, antes.Count(d => d.IdRol != RolAdministrador)),
                Activos = activos,
                Inactivos = total - activos,
                IdUsuarioActual = idActual,

                Usuarios = paginaActual,
                Pagina = pagina,
                TamanoPagina = tamano,
                TotalFiltrados = lista.Count
            };

            return View(vm);
        }

        private static bool Contiene(string? valor, string texto) =>
            !string.IsNullOrEmpty(valor) && valor.Contains(texto, StringComparison.OrdinalIgnoreCase);

        // null = no había datos antes para comparar
        private static double? Variacion(int actual, int anterior)
        {
            if (anterior == 0)
            {
                return actual == 0 ? 0 : null;
            }

            return Math.Round((actual - anterior) * 100.0 / anterior, 0);
        }

        // GET: /AdminUsuario/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            var bloqueo = ValidarAdmin();
            if (bloqueo != null) return bloqueo;

            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .Include(u => u.Reservas)
                .FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }

        // GET: /AdminUsuario/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var bloqueo = ValidarAdmin();
            if (bloqueo != null) return bloqueo;

            ViewBag.Roles = await _context.Roles
                .OrderBy(r => r.Nombre)
                .ToListAsync();

            return View();
        }

        // POST: /AdminUsuario/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Usuario usuario)
        {
            var bloqueo = ValidarAdmin();
            if (bloqueo != null) return bloqueo;

            // Estas propiedades no vienen del formulario.
            // Evitamos que interfieran con la validación.
            ModelState.Remove(nameof(Usuario.IdRolNavigation));
            ModelState.Remove(nameof(Usuario.Empresas));
            ModelState.Remove(nameof(Usuario.Reservas));

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await _context.Roles
                    .OrderBy(r => r.Nombre)
                    .ToListAsync();

                return View(usuario);
            }

            try
            {
                // Valores automáticos
                usuario.FechaRegistro = DateTime.Now;
                usuario.Estado = true;

                // La contraseña se guarda encriptada (antes quedaba en texto plano)
                if (!string.IsNullOrWhiteSpace(usuario.Password))
                {
                    usuario.Password = Sportia.Helpers.SeguridadPassword.Hash(usuario.Password);
                }

                // Guardar usuario
                _context.Usuarios.Add(usuario);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Usuario creado correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    "",
                    "No se pudo guardar el usuario. Verifique que el documento o correo no estén repetidos."
                );

                ViewBag.Roles = await _context.Roles
                    .OrderBy(r => r.Nombre)
                    .ToListAsync();

                return View(usuario);
            }
        }

        // GET: /AdminUsuario/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            var bloqueo = ValidarAdmin();
            if (bloqueo != null) return bloqueo;

            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuario == null)
            {
                return NotFound();
            }

            ViewBag.Roles = await _context.Roles
                .OrderBy(r => r.Nombre)
                .ToListAsync();

            return View(usuario);
        }

        // POST: /AdminUsuario/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Usuario usuario)
        {
            var bloqueo = ValidarAdmin();
            if (bloqueo != null) return bloqueo;

            if (id != usuario.IdUsuario)
            {
                return NotFound();
            }

            // La contraseña ya no viaja en el formulario: se conserva la actual
            ModelState.Remove(nameof(Usuario.Password));
            ModelState.Remove(nameof(Usuario.IdRolNavigation));
            ModelState.Remove(nameof(Usuario.Empresas));
            ModelState.Remove(nameof(Usuario.Reservas));

            var usuarioBD = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuarioBD == null)
            {
                return NotFound();
            }

            // No dejar al sistema sin administradores
            bool dejaDeSerAdminActivo =
                usuarioBD.IdRol == RolAdministrador && usuarioBD.Estado == true &&
                (usuario.IdRol != RolAdministrador || usuario.Estado != true);

            if (dejaDeSerAdminActivo && id == IdUsuarioActual)
            {
                ModelState.AddModelError("", "No puedes quitarte el rol de administrador ni desactivar tu propia cuenta.");
            }
            else if (dejaDeSerAdminActivo && await EsUltimoAdminActivo(id))
            {
                ModelState.AddModelError("", "Debe quedar al menos un administrador activo en el sistema.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await _context.Roles
                    .OrderBy(r => r.Nombre)
                    .ToListAsync();

                return View(usuario);
            }

            try
            {
                usuarioBD.IdRol = usuario.IdRol;
                usuarioBD.Nombres = usuario.Nombres;
                usuarioBD.Apellidos = usuario.Apellidos;
                usuarioBD.Documento = usuario.Documento;
                usuarioBD.Telefono = usuario.Telefono;
                usuarioBD.Correo = usuario.Correo;
                usuarioBD.Estado = usuario.Estado;

                // No modificamos la contraseña desde Edit.
                // La contraseña tendrá su propio proceso.

                await _context.SaveChangesAsync();

                TempData["Success"] = "Usuario actualizado correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    "",
                    "No se pudo actualizar el usuario. Verifique los datos ingresados."
                );

                ViewBag.Roles = await _context.Roles
                    .OrderBy(r => r.Nombre)
                    .ToListAsync();

                return View(usuario);
            }
        }

        // POST: /AdminUsuario/CambiarEstado/5  (activar / desactivar desde la lista)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id, string? volver)
        {
            var bloqueo = ValidarAdmin();
            if (bloqueo != null) return bloqueo;

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuario == null)
            {
                return NotFound();
            }

            bool activar = usuario.Estado != true;

            if (!activar && id == IdUsuarioActual)
            {
                TempData["Error"] = "No puedes desactivar tu propia cuenta.";
            }
            else if (!activar && usuario.IdRol == RolAdministrador && await EsUltimoAdminActivo(id))
            {
                TempData["Error"] = "Debe quedar al menos un administrador activo en el sistema.";
            }
            else
            {
                usuario.Estado = activar;
                await _context.SaveChangesAsync();

                TempData["Success"] = activar
                    ? $"{usuario.Nombres} {usuario.Apellidos} fue activado y ya puede iniciar sesión."
                    : $"{usuario.Nombres} {usuario.Apellidos} fue desactivado y no podrá iniciar sesión.";
            }

            if (!string.IsNullOrEmpty(volver) && Url.IsLocalUrl(volver))
            {
                return LocalRedirect(volver);
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /AdminUsuario/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var bloqueo = ValidarAdmin();
            if (bloqueo != null) return bloqueo;

            var usuario = await _context.Usuarios
                .Include(u => u.Reservas)
                .Include(u => u.Empresas)
                .FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuario == null)
            {
                return NotFound();
            }

            if (id == IdUsuarioActual)
            {
                TempData["Error"] = "No puedes eliminar tu propia cuenta.";
                return RedirectToAction(nameof(Index));
            }

            if (usuario.IdRol == RolAdministrador && usuario.Estado == true && await EsUltimoAdminActivo(id))
            {
                TempData["Error"] = "Debe quedar al menos un administrador activo en el sistema.";
                return RedirectToAction(nameof(Index));
            }

            if (usuario.Reservas.Any())
            {
                TempData["Error"] =
                    "No se puede eliminar este usuario porque tiene reservas asociadas. Puedes desactivarlo.";

                return RedirectToAction(nameof(Index));
            }

            if (usuario.Empresas.Any())
            {
                TempData["Error"] =
                    "No se puede eliminar este usuario porque tiene empresas asociadas. Puedes desactivarlo.";

                return RedirectToAction(nameof(Index));
            }

            _context.Usuarios.Remove(usuario);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Usuario eliminado correctamente.";

            return RedirectToAction(nameof(Index));
        }
    }
}
