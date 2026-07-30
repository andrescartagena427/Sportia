using System;
using System.Collections.Generic;

namespace Sportia.Models;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public int IdRol { get; set; }

    public string Nombres { get; set; } = null!;

    public string Apellidos { get; set; } = null!;

    public string Documento { get; set; } = null!;

    public string? Telefono { get; set; }

    public string Correo { get; set; } = null!;

    public string Password { get; set; } = null!;

    public bool? Estado { get; set; }

    public DateTime? FechaRegistro { get; set; }

    public virtual ICollection<Empresa> Empresas { get; set; } = new List<Empresa>();

    public virtual Role IdRolNavigation { get; set; } = null!;

    public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
