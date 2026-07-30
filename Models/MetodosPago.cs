using System;
using System.Collections.Generic;

namespace Sportia.Models;

public partial class MetodosPago
{
    public int IdMetodo { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}
