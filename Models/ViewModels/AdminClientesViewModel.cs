using System;
using System.Collections.Generic;

namespace Sportia.Models.ViewModels
{
    // =============================================================
    // PÁGINA DE CLIENTES DEL ADMINISTRADOR (AdminCliente/Index)
    // =============================================================
    public class AdminClientesViewModel
    {
        // ---------- Filtros ----------
        public string? Buscar { get; set; }
        public string Estado { get; set; } = "";   // "" | "activo" | "inactivo"

        // ---------- Tarjetas ----------
        public int Total { get; set; }
        public double? VariacionTotal { get; set; }
        public int ConCorreo { get; set; }
        public double? VariacionCorreo { get; set; }
        public int ConTelefono { get; set; }
        public double? VariacionTelefono { get; set; }
        public int Activos { get; set; }
        public double? VariacionActivos { get; set; }

        // Un cliente es "activo" si tiene reservas en los últimos X meses
        // o se registró hace menos de 1 mes.
        public int MesesActividad { get; set; }

        // ---------- Tabla ----------
        public List<ClienteFilaViewModel> Clientes { get; set; } = new();
        public int Pagina { get; set; } = 1;
        public int TamanoPagina { get; set; } = 10;
        public int TotalFiltrados { get; set; }
        public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(TotalFiltrados / (double)TamanoPagina));
        public int MostrandoDesde => TotalFiltrados == 0 ? 0 : (Pagina - 1) * TamanoPagina + 1;
        public int MostrandoHasta => Math.Min(Pagina * TamanoPagina, TotalFiltrados);
    }

    public class ClienteFilaViewModel
    {
        public int Numero { get; set; }
        public int IdCliente { get; set; }
        public string Nombre { get; set; } = "";
        public string? Documento { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public DateTime? FechaRegistro { get; set; }
        public bool Activo { get; set; }
        public int Reservas { get; set; }
        public DateOnly? UltimaReserva { get; set; }
    }
}
