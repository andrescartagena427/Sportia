using System;
using System.Collections.Generic;

namespace Sportia.Models;

public partial class TiposEscenario
{
    public int IdTipo { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Escenario> Escenarios { get; set; } = new List<Escenario>();
}
