using System;
using System.Collections.Generic;

namespace Sportia.Models.ViewModels
{
    // =============================================================
    // PÁGINA DE EMPRESAS DEL ADMINISTRADOR (AdminEmpresa/Index)
    // =============================================================
    public class AdminEmpresasViewModel
    {
        public string? Buscar { get; set; }

        // ---------- Tarjetas ----------
        public int Total { get; set; }
        public int ConEscenarios { get; set; }
        public int Activas { get; set; }
        public int Inactivas { get; set; }
        public int Escenarios { get; set; }
        public int EscenariosActivos { get; set; }

        public int PorcentajeActivas => Total == 0 ? 0 : (int)Math.Round(Activas * 100.0 / Total);
        public int PorcentajeInactivas => Total == 0 ? 0 : 100 - PorcentajeActivas;

        // ---------- Tabla ----------
        public List<EmpresaFilaViewModel> Empresas { get; set; } = new();
        public int Pagina { get; set; } = 1;
        public int TamanoPagina { get; set; } = 10;
        public int TotalFiltradas { get; set; }
        public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(TotalFiltradas / (double)TamanoPagina));
        public int MostrandoDesde => TotalFiltradas == 0 ? 0 : (Pagina - 1) * TamanoPagina + 1;
        public int MostrandoHasta => Math.Min(Pagina * TamanoPagina, TotalFiltradas);
    }

    public class EmpresaFilaViewModel
    {
        public int IdEmpresa { get; set; }
        public string Nombre { get; set; } = "";
        public string? Nit { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string? Direccion { get; set; }
        public string? Logo { get; set; }
        public string Responsable { get; set; } = "";
        public string RolResponsable { get; set; } = "";
        public int Escenarios { get; set; }
        public bool Activa { get; set; }
    }
}
