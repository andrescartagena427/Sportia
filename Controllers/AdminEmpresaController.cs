using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sportia.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Sportia.Controllers
{
    public class AdminEmpresaController : Controller
    {
        private readonly SportiaDbContext _context;

        public AdminEmpresaController(SportiaDbContext context)
        {
            _context = context;
        }

        // GET: /AdminEmpresa
        public async Task<IActionResult> Index(string? buscar)
        {
            IQueryable<Empresa> empresas = _context.Empresas
                .Include(e => e.IdUsuarioNavigation)
                .Include(e => e.Escenarios);

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.Trim();

                empresas = empresas.Where(e =>
                    e.Nombre.Contains(buscar) ||
                    (e.Nit != null && e.Nit.Contains(buscar)) ||
                    (e.Telefono != null && e.Telefono.Contains(buscar)) ||
                    (e.Correo != null && e.Correo.Contains(buscar)) ||
                    (e.Direccion != null && e.Direccion.Contains(buscar))
                );
            }

            empresas = empresas
                .OrderByDescending(e => e.IdEmpresa);

            ViewBag.Buscar = buscar;

            return View(await empresas.ToListAsync());
        }


        // GET: /AdminEmpresa/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var empresa = await _context.Empresas
                .Include(e => e.IdUsuarioNavigation)
                .Include(e => e.Escenarios)
                .FirstOrDefaultAsync(e => e.IdEmpresa == id);

            if (empresa == null)
            {
                return NotFound();
            }

            return View(empresa);
        }


        // GET: /AdminEmpresa/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await CargarUsuarios();

            return View();
        }


        // POST: /AdminEmpresa/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Empresa empresa)
        {
            ModelState.Remove(nameof(Empresa.IdUsuarioNavigation));
            ModelState.Remove(nameof(Empresa.Escenarios));

            if (!ModelState.IsValid)
            {
                await CargarUsuarios();
                return View(empresa);
            }

            try
            {
                empresa.Estado ??= true;

                _context.Empresas.Add(empresa);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Empresa creada correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    "",
                    "No se pudo guardar la empresa. Verifique los datos ingresados."
                );

                await CargarUsuarios();

                return View(empresa);
            }
        }


        // GET: /AdminEmpresa/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var empresa = await _context.Empresas
                .FirstOrDefaultAsync(e => e.IdEmpresa == id);

            if (empresa == null)
            {
                return NotFound();
            }

            await CargarUsuarios();

            return View(empresa);
        }


        // POST: /AdminEmpresa/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Empresa empresa)
        {
            if (id != empresa.IdEmpresa)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(Empresa.IdUsuarioNavigation));
            ModelState.Remove(nameof(Empresa.Escenarios));

            if (!ModelState.IsValid)
            {
                await CargarUsuarios();
                return View(empresa);
            }

            try
            {
                var empresaBD = await _context.Empresas
                    .FirstOrDefaultAsync(e => e.IdEmpresa == id);

                if (empresaBD == null)
                {
                    return NotFound();
                }

                empresaBD.IdUsuario = empresa.IdUsuario;
                empresaBD.Nombre = empresa.Nombre;
                empresaBD.Nit = empresa.Nit;
                empresaBD.Telefono = empresa.Telefono;
                empresaBD.Correo = empresa.Correo;
                empresaBD.Direccion = empresa.Direccion;
                empresaBD.Descripcion = empresa.Descripcion;
                empresaBD.Logo = empresa.Logo;
                empresaBD.Estado = empresa.Estado;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Empresa actualizada correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    "",
                    "No se pudo actualizar la empresa. Verifique los datos ingresados."
                );

                await CargarUsuarios();

                return View(empresa);
            }
        }


        // POST: /AdminEmpresa/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var empresa = await _context.Empresas
                .Include(e => e.Escenarios)
                .FirstOrDefaultAsync(e => e.IdEmpresa == id);

            if (empresa == null)
            {
                return NotFound();
            }

            if (empresa.Escenarios.Any())
            {
                TempData["Error"] =
                    "No se puede eliminar esta empresa porque tiene escenarios asociados.";

                return RedirectToAction(nameof(Index));
            }

            _context.Empresas.Remove(empresa);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Empresa eliminada correctamente.";

            return RedirectToAction(nameof(Index));
        }


        // Cargar usuarios para el selector
        private async Task CargarUsuarios()
        {
            ViewBag.Usuarios = await _context.Usuarios
                .Where(u => u.Estado == true)
                .OrderBy(u => u.Nombres)
                .ThenBy(u => u.Apellidos)
                .Select(u => new SelectListItem
                {
                    Value = u.IdUsuario.ToString(),
                    Text = u.Nombres + " " + u.Apellidos
                })
                .ToListAsync();
        }
    }
}