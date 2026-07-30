using System;
using System.Collections.Generic;

namespace Sportia.Models;

public partial class DisponibilidadCancha
{
    public int IdDisponibilidad { get; set; }

    public int IdEscenario { get; set; }

    public string DiaSemana { get; set; } = null!;

    public TimeOnly HoraApertura { get; set; }

    public TimeOnly HoraCierre { get; set; }

    public virtual Escenario IdEscenarioNavigation { get; set; } = null!;
}
