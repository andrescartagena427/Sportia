using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sportia.Helpers;
using Sportia.Models;

namespace Sportia.Controllers
{
    public class ClienteController : Controller
    {
        private readonly SportiaDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<ClienteController> _logger;

        public ClienteController(SportiaDbContext context, IConfiguration config, ILogger<ClienteController> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        // =========================================================
        // PANEL PRINCIPAL DEL CLIENTE
        // =========================================================

        public IActionResult Index()
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

            var cliente = ObtenerClienteLogueado(idUsuario.Value);
            var reservasCliente = cliente == null
                ? new List<Reserva>()
                : _context.Reservas
                    .Include(r => r.IdEstadoNavigation)
                    .Where(r => r.IdCliente == cliente.IdCliente)
                    .ToList();
            var hoy = DateOnly.FromDateTime(DateTime.Now);

            ViewBag.TotalReservas = reservasCliente.Count;
            ViewBag.ReservasProximas = reservasCliente.Count(r =>
                r.FechaUso >= hoy &&
                !(r.IdEstadoNavigation?.Nombre ?? "").ToLower().Contains("cancel"));

            var escenarios = _context.Escenarios
                .Include(e => e.IdTipoNavigation)
                .Where(e => e.Estado == true)
                .OrderByDescending(e => e.IdEscenario)
                .ToList();

            ViewBag.TotalEscenarios = escenarios.Count;
            ViewBag.Escenarios = escenarios;

            return View();
        }


        // =========================================================
        // PANTALLA DE RESERVA (GET)
        // =========================================================

        [HttpGet]
        public IActionResult Reservar(int idEscenario)
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

            var escenario = _context.Escenarios
                .Include(e => e.IdTipoNavigation)
                .FirstOrDefault(e => e.IdEscenario == idEscenario && e.Estado == true);

            if (escenario == null)
            {
                TempData["ErrorReserva"] = "El escenario seleccionado no está disponible.";
                return RedirectToAction(nameof(Index));
            }

            var fechaUso = DateOnly.FromDateTime(DateTime.Today);
            var horaInicio = new TimeOnly(8, 0);
            var horaFin = new TimeOnly(9, 0);

            var vm = new ReservaClienteViewModel
            {
                IdEscenario = escenario.IdEscenario,
                NombreEscenario = escenario.Nombre,
                Descripcion = escenario.Descripcion,
                Imagen = escenario.Imagen,
                Categoria = escenario.IdTipoNavigation?.Nombre ?? "Deporte",
                Capacidad = escenario.Capacidad,

                FechaUso = fechaUso,
                HoraInicio = horaInicio,
                HoraFin = horaFin,

                MetodosPagoDisponibles = _context.MetodosPagos
                    .OrderBy(m => m.Nombre)
                    .ToList()
            };

            // Intenta calcular un precio inicial para la franja por defecto.
            // Si no hay tarifa configurada para hoy a esa hora, simplemente
            // queda en 0 y se muestra el mensaje correspondiente.
            var (ok, precioHora, total, mensaje) = CalcularPrecioInterno(idEscenario, fechaUso, horaInicio, horaFin);
            vm.PrecioHora = precioHora;
            vm.Total = total;
            vm.MensajePrecio = ok ? null : mensaje;

            return View(vm);
        }


        // =========================================================
        // PANTALLA DE RESERVA (POST) - CREA RESERVA + PAGO
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reservar(ReservaClienteViewModel vm)
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
            // RECARGAR DATOS DEL ESCENARIO
            // ---------------------------------------------------
            var escenario = await _context.Escenarios
                .Include(e => e.IdTipoNavigation)
                .FirstOrDefaultAsync(e => e.IdEscenario == vm.IdEscenario && e.Estado == true);

            if (escenario == null)
            {
                TempData["ErrorReserva"] = "El escenario seleccionado no está disponible.";
                return RedirectToAction(nameof(Index));
            }

            vm.NombreEscenario = escenario.Nombre;
            vm.Descripcion = escenario.Descripcion;
            vm.Imagen = escenario.Imagen;
            vm.Categoria = escenario.IdTipoNavigation?.Nombre ?? "Deporte";
            vm.Capacidad = escenario.Capacidad;
            vm.MetodosPagoDisponibles = await _context.MetodosPagos.OrderBy(m => m.Nombre).ToListAsync();

            // ---------------------------------------------------
            // VALIDAR FECHA / HORA
            // ---------------------------------------------------
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            if (vm.FechaUso < hoy)
            {
                ModelState.AddModelError("", "La fecha no puede ser anterior a hoy.");
            }

            if (vm.HoraFin <= vm.HoraInicio)
            {
                ModelState.AddModelError("", "La hora de fin debe ser mayor que la de inicio.");
            }

            // ---------------------------------------------------
            // VALIDAR MÉTODO DE PAGO Y SUS CAMPOS EXTRA
            // ---------------------------------------------------
            var metodoSeleccionado = await _context.MetodosPagos
                .FirstOrDefaultAsync(m => m.IdMetodo == vm.IdMetodoPago);

            string? comprobante = null;

            if (metodoSeleccionado == null)
            {
                ModelState.AddModelError("", "Debes seleccionar un método de pago válido.");
            }
            else
            {
                string tipoMetodo = ClasificarMetodo(metodoSeleccionado.Nombre);

                if (tipoMetodo == "tarjeta")
                {
                    string numeroLimpio = (vm.NumeroTarjeta ?? "").Replace(" ", "").Trim();

                    if (numeroLimpio.Length < 12 || !numeroLimpio.All(char.IsDigit))
                    {
                        ModelState.AddModelError("", "Ingresa un número de tarjeta válido.");
                    }

                    if (string.IsNullOrWhiteSpace(vm.CvvTarjeta) || vm.CvvTarjeta.Trim().Length < 3)
                    {
                        ModelState.AddModelError("", "Ingresa un CVV válido.");
                    }

                    if (string.IsNullOrWhiteSpace(vm.NombreTitular))
                    {
                        ModelState.AddModelError("", "Ingresa el nombre del titular de la tarjeta.");
                    }

                    if (ModelState.IsValid && numeroLimpio.Length >= 4)
                    {
                        string ultimos4 = numeroLimpio[^4..];
                        comprobante = $"Tarjeta terminada en {ultimos4} - Titular: {vm.NombreTitular}";
                    }
                }
                else if (tipoMetodo == "transferencia")
                {
                    if (string.IsNullOrWhiteSpace(vm.ReferenciaTransferencia))
                    {
                        ModelState.AddModelError("", "Ingresa el número de referencia de la transferencia.");
                    }
                    else
                    {
                        comprobante = $"Transferencia - Referencia: {vm.ReferenciaTransferencia.Trim()}";
                    }
                }
                else if (tipoMetodo == "efectivo")
                {
                    comprobante = "Pago en efectivo pendiente por confirmar presencialmente.";
                }
                else
                {
                    comprobante = $"Pago con {metodoSeleccionado.Nombre} pendiente de confirmación.";
                }
            }

            // IMPORTANTE: nunca guardamos el número completo ni el CVV.
            // Se descartan aquí, solo queda la variable "comprobante".
            vm.NumeroTarjeta = null;
            vm.CvvTarjeta = null;

            if (!ModelState.IsValid)
            {
                var (_, ph, tot, msg) = CalcularPrecioInterno(vm.IdEscenario, vm.FechaUso, vm.HoraInicio, vm.HoraFin);
                vm.PrecioHora = ph;
                vm.Total = tot;
                vm.MensajePrecio = msg;
                return View(vm);
            }

            // ---------------------------------------------------
            // CALCULAR PRECIO REAL EN EL SERVIDOR
            // ---------------------------------------------------
            var (ok, precioHora, total, mensaje) = CalcularPrecioInterno(vm.IdEscenario, vm.FechaUso, vm.HoraInicio, vm.HoraFin);

            if (!ok)
            {
                vm.PrecioHora = precioHora;
                vm.Total = total;
                vm.MensajePrecio = mensaje;
                ModelState.AddModelError("", mensaje ?? "No se pudo calcular el precio para ese horario.");
                return View(vm);
            }

            // ---------------------------------------------------
            // VERIFICAR QUE EL HORARIO NO ESTÉ OCUPADO
            // ---------------------------------------------------
            // (las reservas canceladas ya no ocupan el horario)
            bool ocupado = await _context.Reservas.AnyAsync(r =>
                r.IdEscenario == vm.IdEscenario &&
                r.FechaUso == vm.FechaUso &&
                r.HoraInicio < vm.HoraFin &&
                r.HoraFin > vm.HoraInicio &&
                !r.IdEstadoNavigation.Nombre.ToLower().Contains("cancel"));

            if (ocupado)
            {
                vm.PrecioHora = precioHora;
                vm.Total = total;
                ModelState.AddModelError("", "Ese horario ya fue reservado por otra persona. Elige otro.");
                return View(vm);
            }

            // ---------------------------------------------------
            // UBICAR AL CLIENTE LOGUEADO
            // ---------------------------------------------------
            var cliente = ObtenerClienteLogueado(idUsuario.Value);

            if (cliente == null)
            {
                TempData["ErrorReserva"] = "No se pudo identificar tu cuenta de cliente.";
                return RedirectToAction(nameof(Index));
            }



            // ---------------------------------------------------
            // ESTADO INICIAL DE LA RESERVA: "PENDIENTE"
            // ---------------------------------------------------
            var estadoPendiente = await _context.EstadosReservas
                .FirstOrDefaultAsync(e => e.Nombre.ToLower().Contains("pendiente"));

            if (estadoPendiente == null)
            {
                estadoPendiente = await _context.EstadosReservas.OrderBy(e => e.IdEstado).FirstOrDefaultAsync();
            }

            if (estadoPendiente == null)
            {
                TempData["ErrorReserva"] = "No hay estados de reserva configurados en el sistema.";
                return RedirectToAction(nameof(Index));
            }

            // ---------------------------------------------------
            // PAGO CON TARJETA: queda pagado y la reserva confirmada.
            // Efectivo / transferencia: queda pendiente hasta que el
            // administrador confirme el pago (AdminPago > Confirmar pago).
            // ---------------------------------------------------
            bool pagadoConTarjeta = metodoSeleccionado != null &&
                ClasificarMetodo(metodoSeleccionado.Nombre) == "tarjeta";

            var estadoInicial = estadoPendiente;

            if (pagadoConTarjeta)
            {
                var estadoConfirmada = await _context.EstadosReservas
                    .FirstOrDefaultAsync(e => e.Nombre.ToLower().Contains("confirm"));

                if (estadoConfirmada != null)
                {
                    estadoInicial = estadoConfirmada;
                }
            }

            var reserva = new Reserva
            {
                Codigo = "RES-" + DateTime.Now.ToString("yyyyMMddHHmmssfff"),
                IdEscenario = vm.IdEscenario,
                IdCliente = cliente.IdCliente,
                IdUsuario = idUsuario.Value,
                IdEstado = estadoInicial.IdEstado,
                FechaUso = vm.FechaUso,
                HoraInicio = vm.HoraInicio,
                HoraFin = vm.HoraFin,
                ValorTotal = total,
                Observaciones = vm.Observaciones,
                FechaReserva = DateTime.Now
            };

            try
            {
                _context.Reservas.Add(reserva);
                await _context.SaveChangesAsync();

                // -----------------------------------------------
                // CREAR EL PAGO ASOCIADO
                // (tarjeta = pagado; efectivo/transferencia = pendiente)
                // -----------------------------------------------
                var pago = new Pago
                {
                    IdReserva = reserva.IdReserva,
                    IdMetodo = vm.IdMetodoPago,
                    MontoPagado = pagadoConTarjeta ? total : 0,
                    SaldoPendiente = pagadoConTarjeta ? 0 : total,
                    FechaPago = DateTime.Now,
                    Comprobante = comprobante
                };

                _context.Pagos.Add(pago);
                await _context.SaveChangesAsync();

                string totalTexto = total.ToString("N0", new System.Globalization.CultureInfo("es-CO"));

                TempData["MensajeReserva"] = pagadoConTarjeta
                    ? $"¡Pago aprobado! Tu reserva {reserva.Codigo} quedó confirmada · Total pagado: ${totalTexto}."
                    : $"¡Reserva creada! Código {reserva.Codigo} · Total: ${totalTexto}. Queda pendiente hasta que se confirme tu pago.";
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorReserva"] = "No se pudo guardar la reserva: " + (ex.InnerException?.Message ?? ex.Message);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorReserva"] = "Ocurrió un error al guardar la reserva: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }

            // ---------------------------------------------------
            // CORREO DE CONFIRMACIÓN (si falla, la reserva sigue)
            // ---------------------------------------------------
            bool correoEnviado = await CorreoReserva.EnviarConfirmacionAsync(
                _config, _logger, reserva, cliente, escenario.Nombre, metodoSeleccionado?.Nombre ?? "", pagadoConTarjeta);

            if (correoEnviado)
            {
                TempData["MensajeReserva"] += $" Te enviamos la confirmación a {cliente.Correo}.";
            }

            return RedirectToAction("Mias", "Reservas", new { nueva = reserva.IdReserva });
        }

        // Clasifica el método de pago según su nombre, igual que en la vista,
        // para decidir qué validaciones aplicar y qué comprobante construir.
        private static string ClasificarMetodo(string nombre)
        {
            string n = (nombre ?? "").ToLower();
            if (n.Contains("tarjeta")) return "tarjeta";
            if (n.Contains("transferencia")) return "transferencia";
            if (n.Contains("efectivo")) return "efectivo";
            return "otro";
        }


        // =========================================================
        // CALCULAR PRECIO EN VIVO (AJAX, mientras el cliente
        // cambia fecha/hora en la pantalla de Reservar)
        // =========================================================

        [HttpGet]
        public IActionResult CalcularPrecio(int idEscenario, DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin)
        {
            var (ok, precioHora, total, mensaje) = CalcularPrecioInterno(idEscenario, fecha, horaInicio, horaFin);

            double horas = ok ? (horaFin.ToTimeSpan() - horaInicio.ToTimeSpan()).TotalHours : 0;

            return Json(new
            {
                ok,
                mensaje = mensaje ?? "",
                precioHora,
                horas,
                total
            });
        }


        // =========================================================
        // HELPERS PRIVADOS
        // =========================================================

        // Busca el cliente por el correo/documento del usuario
        // (ver Helpers/ClienteActual.cs)
        private Cliente? ObtenerClienteLogueado(int idUsuario)
        {
            return ClienteActual.Obtener(_context, idUsuario, incluirReservas: true);
        }

        // Calcula precio/hora y total usando el precio fijo del escenario
        // (Escenario.Precio), sin depender de tarifas por día de la semana.
        private (bool ok, decimal precioHora, decimal total, string? mensaje) CalcularPrecioInterno(
            int idEscenario, DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin)
        {
            if (horaFin <= horaInicio)
            {
                return (false, 0, 0, "La hora de fin debe ser mayor que la de inicio.");
            }

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            if (fecha < hoy)
            {
                return (false, 0, 0, "La fecha no puede ser anterior a hoy.");
            }

            var escenario = _context.Escenarios.FirstOrDefault(e => e.IdEscenario == idEscenario);

            if (escenario == null)
            {
                return (false, 0, 0, "El escenario seleccionado no existe.");
            }

            decimal precioHora = escenario.Precio ?? 0;

            // (las reservas canceladas ya no ocupan el horario)
            bool ocupado = _context.Reservas.Any(r =>
                r.IdEscenario == idEscenario &&
                r.FechaUso == fecha &&
                r.HoraInicio < horaFin &&
                r.HoraFin > horaInicio &&
                !r.IdEstadoNavigation.Nombre.ToLower().Contains("cancel"));

            if (ocupado)
            {
                return (false, precioHora, 0, "Ese horario ya está reservado.");
            }

            double horas = (horaFin.ToTimeSpan() - horaInicio.ToTimeSpan()).TotalHours;
            decimal total = precioHora * (decimal)horas;

            return (true, precioHora, total, null);
        }

        // Ajusta estos textos si en tu tabla Tarifa los días están
        // guardados sin tilde o en otro formato/idioma.
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
