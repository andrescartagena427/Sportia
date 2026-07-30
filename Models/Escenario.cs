using System;
using System.Collections.Generic;

namespace Sportia.Models;

public partial class Escenario
{
    public int IdEscenario { get; set; }

    public int IdEmpresa { get; set; }

    public int IdTipo { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public int? Capacidad { get; set; }

    public bool? Estado { get; set; }

    public virtual ICollection<DisponibilidadCancha> DisponibilidadCanchas { get; set; } = new List<DisponibilidadCancha>();

    public virtual Empresa IdEmpresaNavigation { get; set; } = null!;

    public virtual TiposEscenario IdTipoNavigation { get; set; } = null!;

    public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();

    public virtual ICollection<Tarifa> Tarifas { get; set; } = new List<Tarifa>();
}
