using System;
using System.Collections.Generic;

namespace Sportia.Models;

public partial class Reserva
{
    public int IdReserva { get; set; }

    public string Codigo { get; set; } = null!;

    public int IdEscenario { get; set; }

    public int IdCliente { get; set; }

    public int? IdUsuario { get; set; }

    public int IdEstado { get; set; }

    public DateOnly FechaUso { get; set; }

    public TimeOnly HoraInicio { get; set; }

    public TimeOnly HoraFin { get; set; }

    public decimal ValorTotal { get; set; }

    public string? Observaciones { get; set; }

    public DateTime? FechaReserva { get; set; }

    public virtual Cliente IdClienteNavigation { get; set; } = null!;

    public virtual Escenario IdEscenarioNavigation { get; set; } = null!;

    public virtual EstadosReserva IdEstadoNavigation { get; set; } = null!;

    public virtual Usuario? IdUsuarioNavigation { get; set; }

    public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}
