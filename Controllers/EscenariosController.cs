using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sportia.Models;

namespace Sportia.Controllers
{
    public class EscenariosController : Controller
    {
        private readonly SportiaDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<EscenariosController> _logger;

        public EscenariosController(
            SportiaDbContext context,
            IWebHostEnvironment environment,
            ILogger<EscenariosController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // =====================================================
        // INDEX
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!ValidarSesion())
            {
                return RedirectToAction("Index", "Login");
            }

            var escenarios = await _context.Escenarios
                .Include(e => e.IdTipoNavigation)
                .Include(e => e.IdEmpresaNavigation)
                .OrderBy(e => e.Nombre)
                .ToListAsync();

            // Datos extra para las tarjetas y el detalle
            ViewBag.TotalEmpresas = await _context.Empresas.CountAsync(e => e.Estado == true);

            ViewBag.ReservasPorEscenario = await _context.Reservas
                .GroupBy(r => r.IdEscenario)
                .Select(g => new { IdEscenario = g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.IdEscenario, x => x.Total);

            return View(escenarios);
        }

        // =====================================================
        // ACTIVAR / DESACTIVAR (menú "..." de la tarjeta)
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            if (!ValidarSesion())
            {
                return RedirectToAction("Index", "Login");
            }

            var escenario = await _context.Escenarios.FirstOrDefaultAsync(e => e.IdEscenario == id);

            if (escenario == null)
            {
                return NotFound();
            }

            escenario.Estado = escenario.Estado != true;
            await _context.SaveChangesAsync();

            TempData["MensajeEscenario"] = escenario.Estado == true
                ? $"El escenario {escenario.Nombre} fue activado."
                : $"El escenario {escenario.Nombre} fue desactivado.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // CREATE - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!ValidarSesion())
            {
                return RedirectToAction("Index", "Login");
            }

            await CargarDatosFormulario();

            var escenario = new Escenario
            {
                Estado = true,
                Capacidad = 1
            };

            return View(escenario);
        }

        // =====================================================
        // CREATE - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("IdEmpresa,IdTipo,Nombre,Descripcion,Capacidad,Precio,Estado")]
            Escenario escenario,
            IFormFile? ImagenArchivo)
        {

            if (!ValidarSesion())
            {
                return RedirectToAction("Index", "Login");
            }

            ModelState.Clear();

            // =================================================
            // VALIDACIONES
            // =================================================

            if (escenario.IdTipo <= 0)
            {
                ModelState.AddModelError(nameof(Escenario.IdTipo), "Debes seleccionar un tipo de escenario.");
            }
            else if (!await _context.TiposEscenarios.AnyAsync(t => t.IdTipo == escenario.IdTipo))
            {
                ModelState.AddModelError(nameof(Escenario.IdTipo), "El tipo de escenario seleccionado no existe.");
            }

            if (escenario.IdEmpresa <= 0)
            {
                ModelState.AddModelError(nameof(Escenario.IdEmpresa), "Debes seleccionar una empresa.");
            }
            else if (!await _context.Empresas.AnyAsync(e => e.IdEmpresa == escenario.IdEmpresa))
            {
                ModelState.AddModelError(nameof(Escenario.IdEmpresa), "La empresa seleccionada no existe.");
            }

            if (string.IsNullOrWhiteSpace(escenario.Nombre))
            {
                ModelState.AddModelError(nameof(Escenario.Nombre), "Debes ingresar el nombre del escenario.");
            }

            if (escenario.Capacidad == null || escenario.Capacidad <= 0)
            {
                ModelState.AddModelError(nameof(Escenario.Capacidad), "La cantidad de canchas debe ser mayor que 0.");
            }

            if (escenario.Precio == null || escenario.Precio < 0)
            {
                ModelState.AddModelError(nameof(Escenario.Precio), "El precio debe ser mayor o igual a 0.");
            }

            if (ImagenArchivo != null && ImagenArchivo.Length > 0)
            {
                if (!EsImagenValida(ImagenArchivo))
                {
                    ModelState.AddModelError("ImagenArchivo", "La imagen debe ser JPG, JPEG, PNG o WEBP.");
                }

                if (ImagenArchivo.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ImagenArchivo", "La imagen no puede superar los 5 MB.");
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarDatosFormulario();
                return View(escenario);
            }


            string? imagenGuardada = null;

            try
            {
                escenario.Estado = true;

                if (ImagenArchivo != null && ImagenArchivo.Length > 0)
                {
                    imagenGuardada = GuardarImagenSincrono(ImagenArchivo);
                    escenario.Imagen = imagenGuardada;
                }
                else
                {
                    escenario.Imagen = null;
                }


                _context.Escenarios.Add(escenario);
                await _context.SaveChangesAsync();

            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "No se pudo guardar el escenario {Nombre}.", escenario.Nombre);

                if (!string.IsNullOrWhiteSpace(imagenGuardada))
                {
                    EliminarImagen(imagenGuardada);
                }

                await CargarDatosFormulario();
                ModelState.AddModelError("", "No se pudo guardar el escenario: " + (ex.InnerException?.Message ?? ex.Message));
                return View(escenario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al guardar el escenario {Nombre}.", escenario.Nombre);

                if (!string.IsNullOrWhiteSpace(imagenGuardada))
                {
                    EliminarImagen(imagenGuardada);
                }

                await CargarDatosFormulario();
                ModelState.AddModelError("", "Ocurrió un error al guardar el escenario: " + ex.Message);
                return View(escenario);
            }


            TempData["MensajeEscenario"] = $"El escenario {escenario.Nombre} fue creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // EDIT - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!ValidarSesion())
            {
                return RedirectToAction("Index", "Login");
            }

            var escenario = await _context.Escenarios.FirstOrDefaultAsync(e => e.IdEscenario == id);

            if (escenario == null)
            {
                return NotFound();
            }

            await CargarDatosFormulario();

            return View(escenario);
        }

        // =====================================================
        // EDIT - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("IdEscenario,IdEmpresa,IdTipo,Nombre,Descripcion,Capacidad,Precio,Estado")]
            Escenario escenario,
            IFormFile? ImagenArchivo)
        {
            if (!ValidarSesion())
            {
                return RedirectToAction("Index", "Login");
            }

            if (id != escenario.IdEscenario)
            {
                return NotFound();
            }

            ModelState.Clear();

            var escenarioOriginal = await _context.Escenarios
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEscenario == id);

            if (escenarioOriginal == null)
            {
                return NotFound();
            }

            if (escenario.IdTipo <= 0)
            {
                ModelState.AddModelError(nameof(Escenario.IdTipo), "Debes seleccionar un tipo de escenario.");
            }
            else if (!await _context.TiposEscenarios.AnyAsync(t => t.IdTipo == escenario.IdTipo))
            {
                ModelState.AddModelError(nameof(Escenario.IdTipo), "El tipo de escenario seleccionado no existe.");
            }

            if (escenario.IdEmpresa <= 0)
            {
                ModelState.AddModelError(nameof(Escenario.IdEmpresa), "Debes seleccionar una empresa.");
            }
            else if (!await _context.Empresas.AnyAsync(e => e.IdEmpresa == escenario.IdEmpresa))
            {
                ModelState.AddModelError(nameof(Escenario.IdEmpresa), "La empresa seleccionada no existe.");
            }

            if (string.IsNullOrWhiteSpace(escenario.Nombre))
            {
                ModelState.AddModelError(nameof(Escenario.Nombre), "Debes ingresar el nombre del escenario.");
            }

            if (escenario.Capacidad == null || escenario.Capacidad <= 0)
            {
                ModelState.AddModelError(nameof(Escenario.Capacidad), "La cantidad de canchas debe ser mayor que 0.");
            }

            if (escenario.Precio == null || escenario.Precio < 0)
            {
                ModelState.AddModelError(nameof(Escenario.Precio), "El precio debe ser mayor o igual a 0.");
            }

            if (ImagenArchivo != null && ImagenArchivo.Length > 0)
            {
                if (!EsImagenValida(ImagenArchivo))
                {
                    ModelState.AddModelError("ImagenArchivo", "La imagen debe ser JPG, JPEG, PNG o WEBP.");
                }

                if (ImagenArchivo.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ImagenArchivo", "La imagen no puede superar los 5 MB.");
                }
            }

            if (!ModelState.IsValid)
            {
                escenario.Imagen = escenarioOriginal.Imagen;
                await CargarDatosFormulario();
                return View(escenario);
            }

            string? nuevaImagen = null;
            string? imagenAnterior = escenarioOriginal.Imagen;

            try
            {
                escenario.Imagen = escenarioOriginal.Imagen;

                if (ImagenArchivo != null && ImagenArchivo.Length > 0)
                {
                    nuevaImagen = GuardarImagenSincrono(ImagenArchivo);
                    escenario.Imagen = nuevaImagen;
                }

                var escenarioDb = await _context.Escenarios.FirstOrDefaultAsync(e => e.IdEscenario == id);

                if (escenarioDb == null)
                {
                    if (!string.IsNullOrWhiteSpace(nuevaImagen))
                    {
                        EliminarImagen(nuevaImagen);
                    }

                    return NotFound();
                }

                escenarioDb.IdEmpresa = escenario.IdEmpresa;
                escenarioDb.IdTipo = escenario.IdTipo;
                escenarioDb.Nombre = escenario.Nombre;
                escenarioDb.Descripcion = escenario.Descripcion;
                escenarioDb.Capacidad = escenario.Capacidad;
                escenarioDb.Precio = escenario.Precio;
                escenarioDb.Estado = escenario.Estado;
                escenarioDb.Imagen = escenario.Imagen;

                await _context.SaveChangesAsync();

                if (!string.IsNullOrWhiteSpace(nuevaImagen) &&
                    !string.IsNullOrWhiteSpace(imagenAnterior) &&
                    imagenAnterior != nuevaImagen)
                {
                    EliminarImagen(imagenAnterior);
                }
            }
            catch (DbUpdateException ex)
            {
                if (!string.IsNullOrWhiteSpace(nuevaImagen))
                {
                    EliminarImagen(nuevaImagen);
                }

                escenario.Imagen = escenarioOriginal.Imagen;
                await CargarDatosFormulario();
                ModelState.AddModelError("", "No se pudo actualizar el escenario: " + (ex.InnerException?.Message ?? ex.Message));
                return View(escenario);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(nuevaImagen))
                {
                    EliminarImagen(nuevaImagen);
                }

                escenario.Imagen = escenarioOriginal.Imagen;
                await CargarDatosFormulario();
                ModelState.AddModelError("", "Ocurrió un error al actualizar el escenario: " + ex.Message);
                return View(escenario);
            }

            TempData["MensajeEscenario"] = $"El escenario {escenario.Nombre} fue actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // CARGAR TIPOS Y EMPRESAS
        // =====================================================

        private async Task CargarDatosFormulario()
        {
            ViewBag.Tipos = await _context.TiposEscenarios.OrderBy(t => t.Nombre).ToListAsync();
            ViewBag.Empresas = await _context.Empresas.Where(e => e.Estado == true).OrderBy(e => e.Nombre).ToListAsync();
        }

        // =====================================================
        // VALIDAR SESIÓN
        // =====================================================

        private bool ValidarSesion()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            int? idRol = HttpContext.Session.GetInt32("IdRol");

            if (idUsuario == null) return false;
            if (idRol != 1) return false;

            return true;
        }

        // =====================================================
        // VALIDAR IMAGEN
        // =====================================================

        private bool EsImagenValida(IFormFile archivo)
        {
            string[] extensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };
            string extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            return extensionesPermitidas.Contains(extension);
        }

        // =====================================================
        // GUARDAR IMAGEN - VERSIÓN SÍNCRONA (SIN async/await)
        // =====================================================
        // Lee el archivo completo a un arreglo de bytes en memoria
        // y lo escribe de una sola vez con File.WriteAllBytes,
        // en vez de usar un FileStream asíncrono con CopyToAsync.

        private string GuardarImagenSincrono(IFormFile archivo)
        {

            string carpeta = Path.Combine(_environment.WebRootPath, "uploads", "escenarios");


            if (!Directory.Exists(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }

            string extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            string nombreArchivo = $"{Guid.NewGuid()}{extension}";
            string rutaCompleta = Path.Combine(carpeta, nombreArchivo);

            byte[] bytes;
            using (var memoryStream = new MemoryStream())
            {
                archivo.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();
            }


            System.IO.File.WriteAllBytes(rutaCompleta, bytes);


            return $"/uploads/escenarios/{nombreArchivo}";
        }

        // =====================================================
        // ELIMINAR IMAGEN
        // =====================================================

        private void EliminarImagen(string rutaImagen)
        {
            string nombreArchivo = Path.GetFileName(rutaImagen);

            if (string.IsNullOrWhiteSpace(nombreArchivo))
            {
                return;
            }

            string rutaCompleta = Path.Combine(_environment.WebRootPath, "uploads", "escenarios", nombreArchivo);

            if (System.IO.File.Exists(rutaCompleta))
            {
                System.IO.File.Delete(rutaCompleta);
            }
        }
    }
}
