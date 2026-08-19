using Microsoft.AspNetCore.Mvc;
using Sportia.Models;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Sportia.Controllers
{
    public class ContactoController : Controller
    {
        private readonly IConfiguration _config;
        private readonly SportiaDbContext _context;

        public ContactoController(IConfiguration config, SportiaDbContext context)
        {
            _config = config;
            _context = context;
        }

        // GET: /Contacto
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // POST: /Contacto/Enviar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enviar(string Nombre, string Correo, string Asunto, string Mensaje)
        {
            if (string.IsNullOrWhiteSpace(Nombre) ||
                string.IsNullOrWhiteSpace(Correo) ||
                string.IsNullOrWhiteSpace(Mensaje))
            {
                return Json(new { ok = false, error = "Completa todos los campos obligatorios." });
            }

            // =========================================================
            // 1) GUARDAR EL MENSAJE EN LA BASE DE DATOS
            // =========================================================
            try
            {
                var nuevoMensaje = new ContactoMensaje
                {
                    Nombre = Nombre,
                    Correo = Correo,
                    Asunto = Asunto,
                    Mensaje = Mensaje,
                    FechaEnvio = DateTime.Now,
                    Respondido = false
                };

                _context.ContactoMensajes.Add(nuevoMensaje);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                return Json(new { ok = false, error = "No se pudo guardar tu mensaje. Intenta de nuevo." });
            }

            // =========================================================
            // 2) ENVIAR EL CORREO (si esto falla, el mensaje ya quedó
            //    guardado en la base de datos, así que no se pierde)
            // =========================================================
            try
            {
                var smtpHost = _config["Smtp:Host"];
                var smtpPort = int.Parse(_config["Smtp:Port"]);
                var smtpUser = _config["Smtp:User"];
                var smtpPass = _config["Smtp:Password"];
                var correoDestino = _config["Smtp:CorreoDestino"];

                var mensajeCorreo = new MailMessage
                {
                    From = new MailAddress(smtpUser, "Sportia - Formulario de contacto"),
                    Subject = $"[Sportia] {Asunto}",
                    Body = $"Nombre: {Nombre}\nCorreo: {Correo}\n\nMensaje:\n{Mensaje}",
                    IsBodyHtml = false
                };

                mensajeCorreo.To.Add(correoDestino);
                mensajeCorreo.ReplyToList.Add(new MailAddress(Correo, Nombre));

                using var cliente = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                await cliente.SendMailAsync(mensajeCorreo);
            }
            catch (Exception)
            {
                return Json(new { ok = true, aviso = "Tu mensaje quedó registrado, pero hubo un problema enviando la notificación por correo." });
            }

            return Json(new { ok = true });
        }
    }
}
