using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sportia.Helpers;
using Sportia.Models;
using Sportia.Models.ViewModels;

namespace Sportia.Controllers
{
    public class ReservasController : Controller
    {
        private readonly SportiaDbContext _context;

        public ReservasController(SportiaDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // LISTADO DE RESERVAS (ADMIN)
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
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

            var reservas = await _context.Reservas
                .Include(r => r.IdClienteNavigation)
                .Include(r => r.IdEscenarioNavigation)
                .Include(r => r.IdEstadoNavigation)
                .OrderByDescending(r => r.FechaReserva)
                .ToListAsync();

            return View(reservas);
        }


        // =====================================================
        // CREAR RESERVA - GET (ADMIN)
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Create()
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

            await CargarDatosFormulario();

            var reserva = new Reserva
            {
                FechaUso = DateOnly.FromDateTime(DateTime.Today),
                HoraInicio = new TimeOnly(8, 0),
                HoraFin = new TimeOnly(9, 0),
                ValorTotal = 0
            };

            return View(reserva);
        }


        // =====================================================
        // CREAR RESERVA - POST (ADMIN)
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reserva reserva)
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

            ModelState.Remove(nameof(Reserva.IdUsuario));
            ModelState.Remove(nameof(Reserva.FechaReserva));
            ModelState.Remove(nameof(Reserva.Codigo));
            ModelState.Remove(nameof(Reserva.IdClienteNavigation));
            ModelState.Remove(nameof(Reserva.IdEscenarioNavigation));
            ModelState.Remove(nameof(Reserva.IdEstadoNavigation));
            ModelState.Remove(nameof(Reserva.IdUsuarioNavigation));
            ModelState.Remove(nameof(Reserva.Pagos));

            if (reserva.IdCliente <= 0)
            {
                ModelState.AddModelError(nameof(Reserva.IdCliente), "Debes seleccionar un cliente.");
            }
            else
            {
                bool clienteExiste = await _context.Clientes.AnyAsync(c => c.IdCliente == reserva.IdCliente);
                if (!clienteExiste)
                {
                    ModelState.AddModelError(nameof(Reserva.IdCliente), "El cliente seleccionado no existe.");
                }
            }

            if (reserva.IdEscenario <= 0)
            {
                ModelState.AddModelError(nameof(Reserva.IdEscenario), "Debes seleccionar un escenario.");
            }
            else
            {
                var escenario = await _context.Escenarios.FirstOrDefaultAsync(e => e.IdEscenario == reserva.IdEscenario);

                if (escenario == null)
                {
                    ModelState.AddModelError(nameof(Reserva.IdEscenario), "El escenario seleccionado no existe.");
                }
                else if (escenario.Estado != true)
                {
                    ModelState.AddModelError(nameof(Reserva.IdEscenario), "El escenario seleccionado no está disponible.");
                }
            }

            if (reserva.IdEstado <= 0)
            {
                ModelState.AddModelError(nameof(Reserva.IdEstado), "Debes seleccionar un estado.");
            }
            else
            {
                bool estadoExiste = await _context.EstadosReservas.AnyAsync(e => e.IdEstado == reserva.IdEstado);
                if (!estadoExiste)
                {
                    ModelState.AddModelError(nameof(Reserva.IdEstado), "El estado seleccionado no existe.");
                }
            }

            DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);

            if (reserva.FechaUso < hoy)
            {
                ModelState.AddModelError(nameof(Reserva.FechaUso), "La fecha de uso no puede ser anterior a hoy.");
            }

            if (reserva.HoraFin <= reserva.HoraInicio)
            {
                ModelState.AddModelError(nameof(Reserva.HoraFin), "La hora de finalización debe ser mayor que la hora de inicio.");
            }

            if (reserva.ValorTotal < 0)
            {
                ModelState.AddModelError(nameof(Reserva.ValorTotal), "El valor total no puede ser negativo.");
            }

            if (reserva.IdEscenario > 0 && reserva.HoraFin > reserva.HoraInicio && reserva.FechaUso >= hoy)
            {
                bool horarioOcupado = await _context.Reservas.AnyAsync(r =>
                    r.IdEscenario == reserva.IdEscenario &&
                    r.FechaUso == reserva.FechaUso &&
                    r.HoraInicio < reserva.HoraFin &&
                    r.HoraFin > reserva.HoraInicio &&
                    !r.IdEstadoNavigation.Nombre.ToLower().Contains("cancel"));

                if (horarioOcupado)
                {
                    ModelState.AddModelError("", "El escenario ya tiene una reserva en ese horario.");
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarDatosFormulario();
                return View(reserva);
            }

            reserva.IdUsuario = idUsuario.Value;
            reserva.FechaReserva = DateTime.Now;
            reserva.Codigo = "RES-" + DateTime.Now.ToString("yyyyMMddHHmmssfff");

            try
            {
                _context.Reservas.Add(reserva);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                await CargarDatosFormulario();
                string mensaje = ex.InnerException?.Message ?? ex.Message;
                ModelState.AddModelError("", "Error al guardar la reserva: " + mensaje);
                return View(reserva);
            }
            catch (Exception ex)
            {
                await CargarDatosFormulario();
                ModelState.AddModelError("", "Ocurrió un error al guardar la reserva: " + ex.Message);
                return View(reserva);
            }

            TempData["MensajeReserva"] = $"La reserva {reserva.Codigo} fue creada correctamente.";

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // CLIENTE: CALCULAR PRECIO Y DISPONIBILIDAD (AJAX)
        // =====================================================
        // Se llama desde el modal de "Reservar" en Cliente/Index
        // cada vez que el cliente elige fecha/hora, para mostrarle
        // el precio real (según Tarifa) antes de confirmar.

        [HttpGet]
        public async Task<IActionResult> CalcularPrecio(int idEscenario, DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin)
        {
            if (horaFin <= horaInicio)
            {
                return Json(new { ok = false, mensaje = "La hora de fin debe ser mayor que la de inicio." });
            }

            DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);
            if (fecha < hoy)
            {
                return Json(new { ok = false, mensaje = "La fecha no puede ser anterior a hoy." });
            }

            string diaSemana = ObtenerNombreDiaSemana(fecha.DayOfWeek);

            var tarifa = await _context.Tarifas
                .Where(t => t.IdEscenario == idEscenario
                    && t.DiaSemana == diaSemana
                    && t.HoraInicio <= horaInicio
                    && t.HoraFin >= horaFin)
                .FirstOrDefaultAsync();

            if (tarifa == null)
            {
                return Json(new { ok = false, mensaje = $"No hay una tarifa configurada para {diaSemana} en ese horario." });
            }

            bool ocupado = await _context.Reservas.AnyAsync(r =>
                r.IdEscenario == idEscenario &&
                r.FechaUso == fecha &&
                r.HoraInicio < horaFin &&
                r.HoraFin > horaInicio &&
                !r.IdEstadoNavigation.Nombre.ToLower().Contains("cancel"));

            if (ocupado)
            {
                return Json(new { ok = false, mensaje = "Ese horario ya está reservado." });
            }

            double horas = (horaFin.ToTimeSpan() - horaInicio.ToTimeSpan()).TotalHours;
            decimal total = tarifa.Precio * (decimal)horas;

            return Json(new
            {
                ok = true,
                mensaje = "",
                precioHora = tarifa.Precio,
                horas = horas,
                total = total
            });
        }


        // =====================================================
        // CLIENTE: CREAR SU PROPIA RESERVA
        // =====================================================
        // A diferencia de Create() (admin), aquí el cliente NO elige
        // a qué cliente pertenece la reserva: se toma de su sesión.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearCliente(int IdEscenario, DateOnly FechaUso, TimeOnly HoraInicio, TimeOnly HoraFin, string? Observaciones)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            int? idRol = HttpContext.Session.GetInt32("IdRol");

            if (idUsuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            if (idRol != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            // ---------------------------------------------------
            // Ubicar al cliente logueado (ver Helpers/ClienteActual.cs)
            // ---------------------------------------------------
            var cliente = ClienteActual.Obtener(_context, idUsuario.Value);

            if (cliente == null)
            {
                TempData["ErrorReserva"] = "No se pudo identificar tu cuenta de cliente.";
                return RedirectToAction("Index", "Cliente");
            }

            var escenario = await _context.Escenarios
                .FirstOrDefaultAsync(e => e.IdEscenario == IdEscenario && e.Estado == true);

            if (escenario == null)
            {
                TempData["ErrorReserva"] = "El escenario seleccionado no está disponible.";
                return RedirectToAction("Index", "Cliente");
            }

            DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);

            if (FechaUso < hoy)
            {
                TempData["ErrorReserva"] = "La fecha no puede ser anterior a hoy.";
                return RedirectToAction("Index", "Cliente");
            }

            if (HoraFin <= HoraInicio)
            {
                TempData["ErrorReserva"] = "La hora de fin debe ser mayor que la de inicio.";
                return RedirectToAction("Index", "Cliente");
            }

            string diaSemana = ObtenerNombreDiaSemana(FechaUso.DayOfWeek);

            var tarifa = await _context.Tarifas
                .Where(t => t.IdEscenario == IdEscenario
                    && t.DiaSemana == diaSemana
                    && t.HoraInicio <= HoraInicio
                    && t.HoraFin >= HoraFin)
                .FirstOrDefaultAsync();

            if (tarifa == null)
            {
                TempData["ErrorReserva"] = $"No hay una tarifa configurada para {diaSemana} en ese horario.";
                return RedirectToAction("Index", "Cliente");
            }

            bool ocupado = await _context.Reservas.AnyAsync(r =>
                r.IdEscenario == IdEscenario &&
                r.FechaUso == FechaUso &&
                r.HoraInicio < HoraFin &&
                r.HoraFin > HoraInicio &&
                !r.IdEstadoNavigation.Nombre.ToLower().Contains("cancel"));

            if (ocupado)
            {
                TempData["ErrorReserva"] = "Ese horario ya fue reservado por otra persona. Elige otro horario.";
                return RedirectToAction("Index", "Cliente");
            }

            double horas = (HoraFin.ToTimeSpan() - HoraInicio.ToTimeSpan()).TotalHours;
            decimal valorTotal = tarifa.Precio * (decimal)horas;

            // Estado inicial: "Pendiente" (por confirmar/pagar).
            // Se busca por nombre para no depender de un Id fijo;
            // si no existe ese nombre exacto en tu tabla, ajusta el texto.
            var estadoPendiente = await _context.EstadosReservas
                .FirstOrDefaultAsync(e => e.Nombre.ToLower().Contains("pendiente"));

            if (estadoPendiente == null)
            {
                estadoPendiente = await _context.EstadosReservas
                    .OrderBy(e => e.IdEstado)
                    .FirstOrDefaultAsync();
            }

            if (estadoPendiente == null)
            {
                TempData["ErrorReserva"] = "No hay estados de reserva configurados en el sistema.";
                return RedirectToAction("Index", "Cliente");
            }

            var reserva = new Reserva
            {
                IdCliente = cliente.IdCliente,
                IdEscenario = IdEscenario,
                IdEstado = estadoPendiente.IdEstado,
                IdUsuario = idUsuario.Value,
                FechaUso = FechaUso,
                HoraInicio = HoraInicio,
                HoraFin = HoraFin,
                ValorTotal = valorTotal,
                Observaciones = Observaciones,
                FechaReserva = DateTime.Now,
                Codigo = "RES-" + DateTime.Now.ToString("yyyyMMddHHmmssfff")
            };

            try
            {
                _context.Reservas.Add(reserva);
                await _context.SaveChangesAsync();

                TempData["MensajeReserva"] =
                    $"¡Reserva creada! Código {reserva.Codigo} · Total: ${valorTotal:N0}. Queda pendiente de confirmación.";
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorReserva"] = "No se pudo guardar la reserva: " + (ex.InnerException?.Message ?? ex.Message);
            }
            catch (Exception ex)
            {
                TempData["ErrorReserva"] = "Ocurrió un error al guardar la reserva: " + ex.Message;
            }

            return RedirectToAction("Index", "Cliente");
        }


        // =====================================================
        // MIS RESERVAS (CLIENTE)
        // =====================================================

        // Horas mínimas de anticipación para que el cliente pueda cancelar
        private const int HorasMinimasParaCancelar = 2;

        [HttpGet]
        public async Task<IActionResult> Mias(int? nueva)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            int? idRol = HttpContext.Session.GetInt32("IdRol");

            if (idUsuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            if (idRol != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            var cliente = ClienteActual.Obtener(_context, idUsuario.Value);

            var vm = new MisReservasViewModel
            {
                IdReservaNueva = nueva,
                HorasMinimasParaCancelar = HorasMinimasParaCancelar
            };

            if (cliente == null)
            {
                return View(vm);
            }

            var reservas = await _context.Reservas
                .AsNoTracking()
                .Include(r => r.IdEscenarioNavigation).ThenInclude(e => e.IdTipoNavigation)
                .Include(r => r.IdEscenarioNavigation).ThenInclude(e => e.IdEmpresaNavigation)
                .Include(r => r.IdEstadoNavigation)
                .Include(r => r.Pagos).ThenInclude(p => p.IdMetodoNavigation)
                .Where(r => r.IdCliente == cliente.IdCliente)
                .ToListAsync();

            var ahora = DateTime.Now;

            foreach (var r in reservas)
            {
                var inicio = r.FechaUso.ToDateTime(r.HoraInicio);
                var fin = r.FechaUso.ToDateTime(r.HoraFin);
                string estado = r.IdEstadoNavigation?.Nombre ?? "Pendiente";
                bool cancelada = EsCancelada(estado);

                decimal pagado = r.Pagos.Sum(p => p.MontoPagado);
                decimal saldo = r.Pagos.Any() ? r.Pagos.Sum(p => p.SaldoPendiente) : r.ValorTotal;

                var item = new MiReservaViewModel
                {
                    IdReserva = r.IdReserva,
                    Codigo = r.Codigo,
                    IdEscenario = r.IdEscenario,
                    Escenario = r.IdEscenarioNavigation?.Nombre ?? "Escenario",
                    Tipo = r.IdEscenarioNavigation?.IdTipoNavigation?.Nombre ?? "",
                    Ubicacion = r.IdEscenarioNavigation?.IdEmpresaNavigation?.Direccion
                        ?? r.IdEscenarioNavigation?.IdEmpresaNavigation?.Nombre ?? "",
                    Imagen = r.IdEscenarioNavigation?.Imagen,
                    FechaUso = r.FechaUso,
                    HoraInicio = r.HoraInicio,
                    HoraFin = r.HoraFin,
                    Estado = estado,
                    Cancelada = cancelada,
                    ValorTotal = r.ValorTotal,
                    Pagado = pagado,
                    Saldo = saldo,
                    MetodoPago = r.Pagos.Select(p => p.IdMetodoNavigation?.Nombre).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)),
                    Observaciones = r.Observaciones,
                    PuedeCancelar = !cancelada && inicio > ahora.AddHours(HorasMinimasParaCancelar)
                };

                if (cancelada)
                {
                    vm.Canceladas.Add(item);
                }
                else if (fin >= ahora)
                {
                    vm.Proximas.Add(item);
                }
                else
                {
                    vm.Pasadas.Add(item);
                }
            }

            vm.Proximas = vm.Proximas.OrderBy(x => x.FechaUso).ThenBy(x => x.HoraInicio).ToList();
            vm.Pasadas = vm.Pasadas.OrderByDescending(x => x.FechaUso).ThenByDescending(x => x.HoraInicio).ToList();
            vm.Canceladas = vm.Canceladas.OrderByDescending(x => x.FechaUso).ThenByDescending(x => x.HoraInicio).ToList();
            vm.TotalInvertido = vm.Proximas.Concat(vm.Pasadas).Sum(x => x.ValorTotal);

            return View(vm);
        }


        // =====================================================
        // CANCELAR UNA RESERVA PROPIA (CLIENTE)
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarMia(int id)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            int? idRol = HttpContext.Session.GetInt32("IdRol");

            if (idUsuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            if (idRol != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            var cliente = ClienteActual.Obtener(_context, idUsuario.Value);

            var reserva = cliente == null
                ? null
                : await _context.Reservas
                    .Include(r => r.IdEstadoNavigation)
                    .FirstOrDefaultAsync(r => r.IdReserva == id && r.IdCliente == cliente.IdCliente);

            // Solo puede cancelar sus propias reservas
            if (reserva == null)
            {
                TempData["ErrorReserva"] = "No encontramos esa reserva en tu cuenta.";
                return RedirectToAction(nameof(Mias));
            }

            if (EsCancelada(reserva.IdEstadoNavigation?.Nombre))
            {
                TempData["ErrorReserva"] = $"La reserva {reserva.Codigo} ya estaba cancelada.";
                return RedirectToAction(nameof(Mias));
            }

            var inicio = reserva.FechaUso.ToDateTime(reserva.HoraInicio);

            if (inicio <= DateTime.Now.AddHours(HorasMinimasParaCancelar))
            {
                TempData["ErrorReserva"] =
                    $"Solo puedes cancelar con al menos {HorasMinimasParaCancelar} horas de anticipación. Comunícate con nosotros desde Contacto.";
                return RedirectToAction(nameof(Mias));
            }

            var estadoCancelada = await _context.EstadosReservas
                .FirstOrDefaultAsync(e => e.Nombre.ToLower().Contains("cancel"));

            if (estadoCancelada == null)
            {
                TempData["ErrorReserva"] = "No hay un estado \"Cancelada\" configurado. Pide al administrador que lo cree.";
                return RedirectToAction(nameof(Mias));
            }

            reserva.IdEstado = estadoCancelada.IdEstado;
            await _context.SaveChangesAsync();

            TempData["MensajeReserva"] = $"Cancelaste la reserva {reserva.Codigo}. El horario quedó libre para otras personas.";
            return RedirectToAction(nameof(Mias));
        }

        private static bool EsCancelada(string? estado) =>
            (estado ?? "").ToLower().Contains("cancel");


        // =====================================================
        // CARGAR DATOS DEL FORMULARIO (ADMIN)
        // =====================================================

        private async Task CargarDatosFormulario()
        {
            ViewBag.Clientes = await _context.Clientes
                .OrderBy(c => c.Nombres)
                .ThenBy(c => c.Apellidos)
                .ToListAsync();

            ViewBag.Escenarios = await _context.Escenarios
                .Where(e => e.Estado == true)
                .OrderBy(e => e.Nombre)
                .ToListAsync();

            ViewBag.Estados = await _context.EstadosReservas
                .OrderBy(e => e.IdEstado)
                .ToListAsync();
        }


        // =====================================================
        // NOMBRE DEL DÍA DE LA SEMANA EN ESPAÑOL
        // =====================================================
        // Ajusta los textos si en tu tabla Tarifa los días
        // están guardados sin tildes o en otro formato.

        private static string ObtenerNombreDiaSemana(DayOfWeek dia)
        {
            return dia switch
            {
                DayOfWeek.Monday => "Lunes",
                DayOfWeek.Tuesday => "Martes",
                DayOfWeek.Wednesday => "Miércoles",
                DayOfWeek.Thursday => "Jueves",
                DayOfWeek.Friday => "Viernes",
                DayOfWeek.Saturday => "Sábado",
                DayOfWeek.Sunday => "Domingo",
                _ => ""
            };
        }
    }
}