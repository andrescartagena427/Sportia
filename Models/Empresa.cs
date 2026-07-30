using System;
using System.Collections.Generic;

namespace Sportia.Models;

public partial class Empresa
{
    public int IdEmpresa { get; set; }

    public int IdUsuario { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Nit { get; set; }

    public string? Telefono { get; set; }

    public string? Correo { get; set; }

    public string? Direccion { get; set; }

    public string? Descripcion { get; set; }

    public string? Logo { get; set; }

    public bool? Estado { get; set; }

    public virtual ICollection<Escenario> Escenarios { get; set; } = new List<Escenario>();

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
