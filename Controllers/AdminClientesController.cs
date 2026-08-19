using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sportia.Models;

namespace Sportia.Controllers
{
    public class AdminClienteController : Controller
    {
        private readonly SportiaDbContext _context;

        public AdminClienteController(SportiaDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // LISTAR CLIENTES
        // =========================================================

        public async Task<IActionResult> Index(string? buscar)
        {
            IQueryable<Cliente> clientes = _context.Clientes;

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.Trim();

                clientes = clientes.Where(c =>
                    c.Nombres.Contains(buscar) ||
                    c.Apellidos.Contains(buscar) ||
                    (c.Documento != null && c.Documento.Contains(buscar)) ||
                    c.Telefono.Contains(buscar) ||
                    (c.Correo != null && c.Correo.Contains(buscar))
                );
            }

            clientes = clientes.OrderByDescending(c => c.FechaRegistro);

            ViewBag.Buscar = buscar;

            return View(await clientes.ToListAsync());
        }

        // =========================================================
        // CREAR CLIENTE - GET
        // =========================================================

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // =========================================================
        // CREAR CLIENTE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Cliente cliente)
        {
            if (!ModelState.IsValid)
            {
                return View(cliente);
            }

            try
            {
                cliente.FechaRegistro ??= DateTime.Now;

                _context.Clientes.Add(cliente);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Cliente creado correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    "Documento",
                    "No se pudo guardar el cliente. Verifique que el documento no esté repetido."
                );

                return View(cliente);
            }
        }

        // =========================================================
        // EDITAR CLIENTE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.IdCliente == id);

            if (cliente == null)
            {
                return NotFound();
            }

            return View(cliente);
        }

        // =========================================================
        // EDITAR CLIENTE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Cliente cliente)
        {
            if (id != cliente.IdCliente)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(cliente);
            }

            try
            {
                _context.Update(cliente);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Cliente actualizado correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClienteExiste(cliente.IdCliente))
                {
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    "Documento",
                    "No se pudo actualizar el cliente. Verifique que el documento no esté repetido."
                );

                return View(cliente);
            }
        }

        // =========================================================
        // ELIMINAR CLIENTE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Reservas)
                .FirstOrDefaultAsync(c => c.IdCliente == id);

            if (cliente == null)
            {
                return NotFound();
            }

            // No permitir eliminar clientes que tengan reservas
            if (cliente.Reservas.Any())
            {
                TempData["Error"] =
                    "No se puede eliminar este cliente porque tiene reservas asociadas.";

                return RedirectToAction(nameof(Index));
            }

            _context.Clientes.Remove(cliente);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cliente eliminado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EXISTENCIA
        // =========================================================

        private bool ClienteExiste(int id)
        {
            return _context.Clientes.Any(c => c.IdCliente == id);
        }
    }
}