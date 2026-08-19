using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sportia.Models
{
    [Table("ContactoMensajes")]
    public class ContactoMensaje
    {
        [Key]
        public int IdContacto { get; set; }

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; }

        [Required]
        [MaxLength(150)]
        public string Correo { get; set; }

        [Required]
        [MaxLength(200)]
        public string Asunto { get; set; }

        [Required]
        public string Mensaje { get; set; }

        public DateTime FechaEnvio { get; set; } = DateTime.Now;

        public bool Respondido { get; set; } = false;
    }
}
