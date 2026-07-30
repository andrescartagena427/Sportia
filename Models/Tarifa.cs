using System;
using System.Collections.Generic;

namespace Sportia.Models;

public partial class Tarifa
{
    public int IdTarifa { get; set; }

    public int IdEscenario { get; set; }

    public string DiaSemana { get; set; } = null!;

    public TimeOnly HoraInicio { get; set; }

    public TimeOnly HoraFin { get; set; }

    public decimal Precio { get; set; }

    public virtual Escenario IdEscenarioNavigation { get; set; } = null!;
}
