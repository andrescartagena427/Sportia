using System;
using System.Collections.Generic;

namespace Sportia.Models;

public partial class Pago
{
    public int IdPago { get; set; }

    public int IdReserva { get; set; }

    public int IdMetodo { get; set; }

    public decimal MontoPagado { get; set; }

    public decimal SaldoPendiente { get; set; }

    public DateTime? FechaPago { get; set; }

    public string? Comprobante { get; set; }

    public virtual MetodosPago IdMetodoNavigation { get; set; } = null!;

    public virtual Reserva IdReservaNavigation { get; set; } = null!;
}
