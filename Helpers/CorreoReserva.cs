using System.Globalization;
using System.Net;
using System.Net.Mail;
using Sportia.Models;

namespace Sportia.Helpers
{
    // =============================================================
    // CORREO DE CONFIRMACIÓN DE RESERVA
    // -------------------------------------------------------------
    // Usa la misma configuración "Smtp" de appsettings.json que el
    // formulario de Contacto. Si el correo falla, la reserva NO se
    // pierde: solo se devuelve false.
    // =============================================================
    public static class CorreoReserva
    {
        public static async Task<bool> EnviarConfirmacionAsync(
            IConfiguration config,
            ILogger logger,
            Reserva reserva,
            Cliente cliente,
            string nombreEscenario,
            string metodoPago,
            bool pagada = false)
        {
            if (string.IsNullOrWhiteSpace(cliente.Correo))
            {
                return false;
            }

            try
            {
                var host = config["Smtp:Host"];
                var usuario = config["Smtp:User"];
                var password = config["Smtp:Password"];

                if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(usuario) ||
                    string.IsNullOrWhiteSpace(password) || !int.TryParse(config["Smtp:Port"], out int puerto))
                {
                    logger.LogWarning("No hay configuración SMTP completa; no se envió el correo de la reserva {Codigo}.", reserva.Codigo);
                    return false;
                }

                var co = new CultureInfo("es-CO");
                string fecha = co.TextInfo.ToTitleCase(reserva.FechaUso.ToString("dddd d 'de' MMMM 'de' yyyy", co));
                string horario = $"{reserva.HoraInicio:HH\\:mm} - {reserva.HoraFin:HH\\:mm}";
                string total = "$" + reserva.ValorTotal.ToString("N0", co);
                string nombre = WebUtility.HtmlEncode(cliente.Nombres);

                string Fila(string etiqueta, string valor) =>
                    $"<tr><td style=\"padding:8px 0;color:#64748b;font-size:14px\">{etiqueta}</td>" +
                    $"<td style=\"padding:8px 0;color:#0f172a;font-size:14px;font-weight:600;text-align:right\">{WebUtility.HtmlEncode(valor)}</td></tr>";

                string titulo = pagada ? "¡Tu reserva está confirmada" : "¡Tu reserva quedó registrada";

                string aviso = pagada
                    ? "<p style=\"margin:20px 0 0;padding:12px;background:#d1fae5;border-radius:10px;color:#065f46;font-size:13px\">" +
                      "Tu pago fue <b>aprobado</b>. ¡Te esperamos en la cancha! Puedes ver tu reserva en <b>Mis reservas</b> en Sportia.</p>"
                    : "<p style=\"margin:20px 0 0;padding:12px;background:#fef3c7;border-radius:10px;color:#92400e;font-size:13px\">" +
                      "Tu reserva queda <b>pendiente</b> hasta que se confirme el pago. Puedes verla o cancelarla desde <b>Mis reservas</b> en Sportia.</p>";

                string cuerpo = $@"
<div style=""background:#f1f5f9;padding:24px;font-family:Arial,Helvetica,sans-serif"">
  <div style=""max-width:520px;margin:0 auto;background:#ffffff;border-radius:16px;overflow:hidden"">
    <div style=""background:#0B0F19;padding:24px;text-align:center"">
      <span style=""display:inline-block;background:#10b981;color:#03150d;font-weight:900;border-radius:10px;padding:6px 12px"">S</span>
      <span style=""color:#ffffff;font-weight:800;font-size:20px;margin-left:8px"">SPORTIA</span>
    </div>
    <div style=""padding:28px"">
      <h1 style=""margin:0 0 8px;color:#0f172a;font-size:22px"">{titulo}, {nombre}!</h1>
      <p style=""margin:0 0 20px;color:#475569;font-size:15px"">Estos son los datos de tu reserva:</p>
      <table style=""width:100%;border-collapse:collapse"">
        {Fila("Código", reserva.Codigo)}
        {Fila("Escenario", nombreEscenario)}
        {Fila("Fecha", fecha)}
        {Fila("Horario", horario)}
        {Fila("Método de pago", metodoPago)}
        {Fila("Total", total)}
      </table>
      {aviso}
    </div>
    <div style=""padding:16px;text-align:center;color:#94a3b8;font-size:12px;border-top:1px solid #e2e8f0"">
      Sportia · Tu cancha, tu momento
    </div>
  </div>
</div>";

                using var mensaje = new MailMessage
                {
                    From = new MailAddress(usuario, "Sportia"),
                    Subject = pagada
                        ? $"Reserva {reserva.Codigo} confirmada - {nombreEscenario}"
                        : $"Reserva {reserva.Codigo} registrada - {nombreEscenario}",
                    Body = cuerpo,
                    IsBodyHtml = true
                };

                mensaje.To.Add(new MailAddress(cliente.Correo, $"{cliente.Nombres} {cliente.Apellidos}".Trim()));

                using var smtp = new SmtpClient(host, puerto)
                {
                    Credentials = new NetworkCredential(usuario, password),
                    EnableSsl = true,
                    Timeout = 15000
                };

                await smtp.SendMailAsync(mensaje);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "No se pudo enviar el correo de confirmación de la reserva {Codigo}.", reserva.Codigo);
                return false;
            }
        }
    }
}
