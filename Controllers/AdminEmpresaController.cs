using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sportia.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sportia.Models.ViewModels;

namespace Sportia.Controllers
{
    public class AdminEmpresaController : Controller
    {
        private readonly SportiaDbContext _context;

        public AdminEmpresaController(SportiaDbContext context)
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

            if (idRol != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            return null;
        }

        // GET: /AdminEmpresa
        public async Task<IActionResult> Index(string? buscar, int pagina = 1)
        {
            var bloqueo = ValidarAdmin();
            if (bloqueo != null) return bloqueo;

            var datos = await _context.Empresas
                .AsNoTracking()
                .Select(e => new EmpresaFilaViewModel
                {
                    IdEmpresa = e.IdEmpresa,
                    Nombre = e.Nombre,
                    Nit = e.Nit,
                    Telefono = e.Telefono,
                    Correo = e.Correo,
                    Direccion = e.Direccion,
                    Logo = e.Logo,
                    Responsable = e.IdUsuarioNavigation.Nombres + " " + e.IdUsuarioNavigation.Apellidos,
                    RolResponsable = e.IdUsuarioNavigation.IdRolNavigation.Nombre,
                    Escenarios = e.Escenarios.Count(),
                    Activa = e.Estado == true
                })
                .OrderByDescending(e => e.IdEmpresa)
                .ToListAsync();

            // ---------- Tarjetas ----------
            var escenarios = await _context.Escenarios
                .AsNoTracking()
                .Select(e => e.Estado)
                .ToListAsync();

            // ---------- Búsqueda ----------
            string texto = (buscar ?? "").Trim();
            var lista = texto.Length == 0
                ? datos
                : datos.Where(e =>
                    Contiene(e.Nombre, texto) ||
                    Contiene(e.Nit, texto) ||
                    Contiene(e.Telefono, texto) ||
                    Contiene(e.Correo, texto) ||
                    Contiene(e.Direccion, texto) ||
                    Contiene(e.Responsable, texto)).ToList();

            const int tamano = 10;
            int paginas = Math.Max(1, (int)Math.Ceiling(lista.Count / (double)tamano));
            pagina = Math.Clamp(pagina, 1, paginas);

            var vm = new AdminEmpresasViewModel
            {
                Buscar = texto,
                Total = datos.Count,
                ConEscenarios = datos.Count(e => e.Escenarios > 0),
                Activas = datos.Count(e => e.Activa),
                Inactivas = datos.Count(e => !e.Activa),
                Escenarios = escenarios.Count,
                EscenariosActivos = escenarios.Count(e => e == true),
                Empresas = lista.Skip((pagina - 1) * tamano).Take(tamano).ToList(),
                Pagina = pagina,
                TamanoPagina = tamano,
                TotalFiltradas = lista.Count
            };

            return View(vm);
        }

        private static bool Contiene(string? valor, string texto) =>
            !string.IsNullOrEmpty(valor) && valor.Contains(texto, StringComparison.OrdinalIgnoreCase);


        // GET: /AdminEmpresa/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            var bloqueo = ValidarAdmin();
            if (bloqueo != null) return bloqueo;

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
            var bloqueo = ValidarAdmin();
            if (bloqueo != null) return bloqueo;

            await CargarUsuarios();

            return View();
        }


        // POST: /AdminEmpresa/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Empresa empresa)
        {
            var bloqueo = ValidarAdmin();
            if (bloqueo != null) return bloqueo;

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
            var bloqueo = ValidarAdmin();
            if (bloqueo != null) return bloqueo;

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

            await CargarUsuarios(empresa.IdUsuario);

            return View(empresa);
        }


        // POST: /AdminEmpresa/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Empresa empresa)
        {
            var bloqueo = ValidarAdmin();
            if (bloqueo != null) return bloqueo;

            if (id != empresa.IdEmpresa)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(Empresa.IdUsuarioNavigation));
            ModelState.Remove(nameof(Empresa.Escenarios));

            if (!ModelState.IsValid)
            {
                await CargarUsuarios(empresa.IdUsuario);
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

                await CargarUsuarios(empresa.IdUsuario);

                return View(empresa);
            }
        }


        // POST: /AdminEmpresa/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var bloqueo = ValidarAdmin();
            if (bloqueo != null) return bloqueo;

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
        // (incluir = el responsable actual, para que aparezca aunque esté inactivo)
        private async Task CargarUsuarios(int? incluir = null)
        {
            ViewBag.Usuarios = await _context.Usuarios
                .Where(u => u.Estado == true || u.IdUsuario == incluir)
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