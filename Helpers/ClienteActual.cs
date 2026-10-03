using Microsoft.EntityFrameworkCore;
using Sportia.Models;

namespace Sportia.Helpers
{
    // =============================================================
    // CLIENTE DE LA PERSONA QUE INICIÓ SESIÓN
    // -------------------------------------------------------------
    // Los usuarios (tabla usuarios) y los clientes (tabla clientes)
    // tienen números de ID distintos, así que se relacionan por el
    // correo (y si no, por el documento). Solo como último recurso
    // se usa el mismo número de ID, como se hacía antes.
    // =============================================================
    public static class ClienteActual
    {
        public static Cliente? Obtener(SportiaDbContext context, int idUsuario, bool incluirReservas = false)
        {
            IQueryable<Cliente> clientes = context.Clientes;

            if (incluirReservas)
            {
                clientes = clientes.Include(c => c.Reservas);
            }

            var usuario = context.Usuarios
                .AsNoTracking()
                .FirstOrDefault(u => u.IdUsuario == idUsuario);

            if (usuario != null)
            {
                if (!string.IsNullOrWhiteSpace(usuario.Correo))
                {
                    var porCorreo = clientes.FirstOrDefault(c => c.Correo == usuario.Correo);
                    if (porCorreo != null) return porCorreo;
                }

                if (!string.IsNullOrWhiteSpace(usuario.Documento))
                {
                    var porDocumento = clientes.FirstOrDefault(c => c.Documento == usuario.Documento);
                    if (porDocumento != null) return porDocumento;
                }
            }

            return clientes.FirstOrDefault(c => c.IdCliente == idUsuario);
        }
    }
}
