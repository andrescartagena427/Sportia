namespace Sportia.Models
{
    public class Configuracion
    {
        public int IdConfiguracion { get; set; }

        public string NombrePlataforma { get; set; } = "Sportia";

        public string? Correo { get; set; }

        public string? Telefono { get; set; }

        public string? Pais { get; set; }

        public string? Logo { get; set; }

        public bool PermitirRegistros { get; set; }

        public bool Notificaciones { get; set; }

        public bool ModoMantenimiento { get; set; }
    }
}