using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sportia.Models;

namespace Sportia.Controllers
{
    public class AdminUsuarioController : Controller
    {
        private readonly SportiaDbContext _context;

        public AdminUsuarioController(SportiaDbContext context)
        {
            _context = context;
        }

        // GET: /AdminUsuario
        public async Task<IActionResult> Index(string? buscar)
        {
            IQueryable<Usuario> usuarios = _context.Usuarios
                .Include(u => u.IdRolNavigation);

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.Trim();

                usuarios = usuarios.Where(u =>
                    u.Nombres.Contains(buscar) ||
                    u.Apellidos.Contains(buscar) ||
                    u.Documento.Contains(buscar) ||
                    u.Correo.Contains(buscar) ||
                    (u.Telefono != null && u.Telefono.Contains(buscar))
                );
            }

            usuarios = usuarios
                .OrderByDescending(u => u.FechaRegistro);

            ViewBag.Buscar = buscar;

            return View(await usuarios.ToListAsync());
        }

        // GET: /AdminUsuario/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
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
            if (id != usuario.IdUsuario)
            {
                return NotFound();
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
                var usuarioBD = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.IdUsuario == id);

                if (usuarioBD == null)
                {
                    return NotFound();
                }

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

        // POST: /AdminUsuario/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Reservas)
                .FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuario == null)
            {
                return NotFound();
            }

            if (usuario.Reservas.Any())
            {
                TempData["Error"] =
                    "No se puede eliminar este usuario porque tiene reservas asociadas.";

                return RedirectToAction(nameof(Index));
            }

            _context.Usuarios.Remove(usuario);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Usuario eliminado correctamente.";

            return RedirectToAction(nameof(Index));
        }
    }
}