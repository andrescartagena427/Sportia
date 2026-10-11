using System;
using System.Collections.Generic;

namespace Sportia.Models.ViewModels
{
    // =============================================================
    // PÁGINA DE USUARIOS DEL ADMINISTRADOR (AdminUsuario/Index)
    // =============================================================
    public class AdminUsuariosViewModel
    {
        // ---------- Filtros ----------
        public string? Buscar { get; set; }
        public int? IdRol { get; set; }
        public string Estado { get; set; } = "";   // "" | "activo" | "inactivo"
        public List<Role> Roles { get; set; } = new();

        // ---------- Tarjetas ----------
        public int Total { get; set; }
        public double? VariacionTotal { get; set; }
        public int Administradores { get; set; }
        public double? VariacionAdministradores { get; set; }
        public int Clientes { get; set; }
        public double? VariacionClientes { get; set; }
        public int Activos { get; set; }
        public int Inactivos { get; set; }

        // El usuario que tiene la sesión abierta (no puede borrarse ni desactivarse)
        public int IdUsuarioActual { get; set; }

        // ---------- Tabla ----------
        public List<UsuarioFilaViewModel> Usuarios { get; set; } = new();
        public int Pagina { get; set; } = 1;
        public int TamanoPagina { get; set; } = 10;
        public int TotalFiltrados { get; set; }
        public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(TotalFiltrados / (double)TamanoPagina));
        public int MostrandoDesde => TotalFiltrados == 0 ? 0 : (Pagina - 1) * TamanoPagina + 1;
        public int MostrandoHasta => Math.Min(Pagina * TamanoPagina, TotalFiltrados);
    }

    public class UsuarioFilaViewModel
    {
        public int Numero { get; set; }
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = "";
        public string Documento { get; set; } = "";
        public string? Telefono { get; set; }
        public string Correo { get; set; } = "";
        public int IdRol { get; set; }
        public string Rol { get; set; } = "";
        public bool Activo { get; set; }
        public DateTime? FechaRegistro { get; set; }
        public int Reservas { get; set; }
        public bool EsUsuarioActual { get; set; }
    }
}
