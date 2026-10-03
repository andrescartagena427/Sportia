using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace Sportia.Helpers
{
    // =============================================================
    // CONTRASEÑAS SEGURAS
    // -------------------------------------------------------------
    // Usa el PasswordHasher de ASP.NET (PBKDF2 con "sal" aleatoria
    // y miles de iteraciones). Cada contraseña queda distinta en la
    // base de datos aunque dos personas usen la misma.
    //
    // Compatibilidad: las cuentas antiguas (SHA256 sin sal o texto
    // plano) siguen funcionando. Cuando la persona inicia sesión,
    // "Verificar" avisa con necesitaActualizar = true para que su
    // contraseña se guarde con el formato nuevo.
    // =============================================================
    public static class SeguridadPassword
    {
        private static readonly PasswordHasher<object> Hasher = new();
        private static readonly object Usuario = new();

        public static string Hash(string password)
        {
            return Hasher.HashPassword(Usuario, password);
        }

        public static bool Verificar(string password, string? guardado, out bool necesitaActualizar)
        {
            necesitaActualizar = false;

            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(guardado))
            {
                return false;
            }

            byte[]? bytes = null;
            try
            {
                bytes = Convert.FromBase64String(guardado);
            }
            catch (FormatException)
            {
                // No es Base64: puede ser una contraseña guardada en texto plano
            }

            // 1) Formato nuevo (PasswordHasher v3 empieza con el byte 0x01)
            if (bytes != null && bytes.Length > 32 && bytes[0] == 0x01)
            {
                var resultado = Hasher.VerifyHashedPassword(Usuario, guardado, password);

                if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    necesitaActualizar = true;
                }

                return resultado != PasswordVerificationResult.Failed;
            }

            // 2) Formato antiguo: SHA256 sin sal en Base64 (32 bytes)
            if (bytes != null && bytes.Length == 32)
            {
                byte[] calculado = SHA256.HashData(Encoding.UTF8.GetBytes(password));
                bool ok = CryptographicOperations.FixedTimeEquals(calculado, bytes);
                necesitaActualizar = ok;
                return ok;
            }

            // 3) Texto plano (cuentas creadas sin encriptar)
            bool igual = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(password),
                Encoding.UTF8.GetBytes(guardado));

            necesitaActualizar = igual;
            return igual;
        }
    }
}
