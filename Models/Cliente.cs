using System;
using System.Collections.Generic;

namespace Sportia.Models;

public partial class Cliente
{
    public int IdCliente { get; set; }

    public string? Documento { get; set; }

    public string Nombres { get; set; } = null!;

    public string Apellidos { get; set; } = null!;

    public string Telefono { get; set; } = null!;

    public string? Correo { get; set; }

    public string? Password { get; set; }

    public DateTime? FechaRegistro { get; set; }

    public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
