using System;
using System.Collections.Generic;

namespace Sportia.Models;

public partial class EstadosReserva
{
    public int IdEstado { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
