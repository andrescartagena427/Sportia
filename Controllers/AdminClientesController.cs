using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sportia.Models;
using Sportia.Models.ViewModels;

namespace Sportia.Controllers
{
    public class AdminClienteController : Controller
    {
        private readonly SportiaDbContext _context;

        // Un cliente es "activo" si tuvo reservas en estos últimos meses
        // (o si se registró hace menos de un mes).
        private const int MesesActividad = 6;

        public AdminClienteController(SportiaDbContext context)
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

        // =========================================================
        // LISTAR CLIENTES
        // =========================================================

        public async Task<IActionResult> Index(string? buscar, string? estado, int pagina = 1)
        {
            var bloqueo = ValidarAdmin();
            if (bloqueo != null) return bloqueo;

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var inicioMes = new DateOnly(hoy.Year, hoy.Month, 1);
            var inicioMesDT = inicioMes.ToDateTime(TimeOnly.MinValue);

            // Datos de todos los clientes con un resumen de sus reservas
            var datos = await _context.Clientes
                .AsNoTracking()
                .Select(c => new
                {
                    c.IdCliente,
                    c.Nombres,
                    c.Apellidos,
                    c.Documento,
                    c.Telefono,
                    c.Correo,
                    c.FechaRegistro,
                    Reservas = c.Reservas.Count(),
                    Ultima = c.Reservas
                        .Where(r => !r.IdEstadoNavigation.Nombre.ToLower().Contains("cancel"))
                        .Max(r => (DateOnly?)r.FechaUso),
                    UltimaAntesDelMes = c.Reservas
                        .Where(r => !r.IdEstadoNavigation.Nombre.ToLower().Contains("cancel") && r.FechaUso < inicioMes)
                        .Max(r => (DateOnly?)r.FechaUso)
                })
                .ToListAsync();

            bool EsActivo(DateOnly? ultima, DateTime? registro, DateOnly corte) =>
                (ultima.HasValue && ultima.Value >= corte.AddMonths(-MesesActividad)) ||
                (registro.HasValue && DateOnly.FromDateTime(registro.Value) >= corte.AddMonths(-1));

            // ---------- Tarjetas (hoy vs. inicio del mes) ----------
            var antes = datos.Where(d => d.FechaRegistro == null || d.FechaRegistro < inicioMesDT).ToList();

            int total = datos.Count;
            int conCorreo = datos.Count(d => !string.IsNullOrWhiteSpace(d.Correo));
            int conTelefono = datos.Count(d => !string.IsNullOrWhiteSpace(d.Telefono));
            int activos = datos.Count(d => EsActivo(d.Ultima, d.FechaRegistro, hoy));

            int totalAntes = antes.Count;
            int correoAntes = antes.Count(d => !string.IsNullOrWhiteSpace(d.Correo));
            int telefonoAntes = antes.Count(d => !string.IsNullOrWhiteSpace(d.Telefono));
            int activosAntes = antes.Count(d => EsActivo(d.UltimaAntesDelMes, d.FechaRegistro, inicioMes));

            // ---------- Filtros ----------
            string texto = (buscar ?? "").Trim();
            string filtroEstado = (estado ?? "").Trim().ToLower();

            var filas = datos
                .OrderByDescending(d => d.FechaRegistro ?? DateTime.MinValue)
                .ThenByDescending(d => d.IdCliente)
                .Select(d => new ClienteFilaViewModel
                {
                    IdCliente = d.IdCliente,
                    Nombre = $"{d.Nombres} {d.Apellidos}".Trim(),
                    Documento = d.Documento,
                    Telefono = d.Telefono,
                    Correo = d.Correo,
                    FechaRegistro = d.FechaRegistro,
                    Activo = EsActivo(d.Ultima, d.FechaRegistro, hoy),
                    Reservas = d.Reservas,
                    UltimaReserva = d.Ultima
                });

            if (texto.Length > 0)
            {
                filas = filas.Where(f =>
                    Contiene(f.Nombre, texto) ||
                    Contiene(f.Documento, texto) ||
                    Contiene(f.Telefono, texto) ||
                    Contiene(f.Correo, texto));
            }

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

            var vm = new AdminClientesViewModel
            {
                Buscar = texto,
                Estado = filtroEstado,
                Total = total,
                VariacionTotal = Variacion(total, totalAntes),
                ConCorreo = conCorreo,
                VariacionCorreo = Variacion(conCorreo, correoAntes),
                ConTelefono = conTelefono,
                VariacionTelefono = Variacion(conTelefono, telefonoAntes),
                Activos = activos,
                VariacionActivos = Variacion(activos, activosAntes),
                MesesActividad = MesesActividad,
                Clientes = paginaActual,
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

        // =========================================================
        // DETALLE DEL CLIENTE (la vista Details ya existía)
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            var bloqueo = ValidarAdmin();
            if (bloqueo != null) return bloqueo;

            if (id == null)
            {
                return NotFound();
            }

            var cliente = await _context.Clientes
                .AsNoTracking()
                .Include(c => c.Reservas.OrderByDescending(r => r.FechaUso))
                .FirstOrDefaultAsync(c => c.IdCliente == id);

            if (cliente == null)
            {
                return NotFound();
            }

            return View(cliente);
        }

        // =========================================================
        // CREAR CLIENTE - GET
        // =========================================================

        [HttpGet]
        public IActionResult Create()
        {
            var bloqueo = ValidarAdmin();
            if (bloqueo != null) return bloqueo;

            return View();
        }

        // =========================================================
        // CREAR CLIENTE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Cliente cliente)
        {
            var bloqueo = ValidarAdmin();
            if (bloqueo != null) return bloqueo;

            if (!ModelState.IsValid)
            {
                return View(cliente);
            }

            try
            {
                cliente.FechaRegistro ??= DateTime.Now;

                // La contraseña se guarda encriptada (antes quedaba en texto plano)
                if (!string.IsNullOrWhiteSpace(cliente.Password))
                {
                    cliente.Password = Sportia.Helpers.SeguridadPassword.Hash(cliente.Password);
                }

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
            var bloqueo = ValidarAdmin();
            if (bloqueo != null) return bloqueo;

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
            var bloqueo = ValidarAdmin();
            if (bloqueo != null) return bloqueo;

            if (id != cliente.IdCliente)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(cliente);
            }

            // Se actualizan solo los datos del formulario.
            // (Antes se usaba _context.Update(cliente) y eso borraba la
            //  contraseña y la fecha de registro, porque no vienen en el formulario.)
            var existente = await _context.Clientes.FirstOrDefaultAsync(c => c.IdCliente == id);

            if (existente == null)
            {
                return NotFound();
            }

            existente.Nombres = cliente.Nombres;
            existente.Apellidos = cliente.Apellidos;
            existente.Documento = cliente.Documento;
            existente.Telefono = cliente.Telefono;
            existente.Correo = cliente.Correo;

            try
            {
                await _context.SaveChangesAsync();

                TempData["Success"] = "Cliente actualizado correctamente.";

                return RedirectToAction(nameof(Index));
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
            var bloqueo = ValidarAdmin();
            if (bloqueo != null) return bloqueo;

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
    }
}
