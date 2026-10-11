using System;
using System.Collections.Generic;

namespace Sportia.Models.ViewModels
{
    // =============================================================
    // PÁGINA DE RESERVAS DEL ADMINISTRADOR (Reservas/Index)
    // Todo se calcula en ReservasController.Index con datos reales.
    // =============================================================
    public class AdminReservasViewModel
    {
        // ---------- Filtros ----------
        public string? Buscar { get; set; }
        public DateOnly Desde { get; set; }
        public DateOnly Hasta { get; set; }
        public int? IdEstado { get; set; }
        public int? IdEscenario { get; set; }

        // ---------- Listas para los filtros ----------
        public List<EstadosReserva> Estados { get; set; } = new();
        public List<Escenario> Escenarios { get; set; } = new();

        // ---------- Tarjetas ----------
        public int TotalReservas { get; set; }
        public double? VariacionReservas { get; set; }

        public int ClientesUnicos { get; set; }
        public double? VariacionClientes { get; set; }

        public decimal ValorTotal { get; set; }
        public double? VariacionValor { get; set; }

        public int Confirmadas { get; set; }
        public double? VariacionConfirmadas { get; set; }

        // "que el mes anterior" o "que el período anterior"
        public string TextoComparacion { get; set; } = "que el período anterior";

        // ---------- Tabla y paginación ----------
        public List<ReservaFilaViewModel> Reservas { get; set; } = new();
        public int Pagina { get; set; } = 1;
        public int TamanoPagina { get; set; } = 9;
        public int TotalFiltradas { get; set; }
        public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(TotalFiltradas / (double)TamanoPagina));
        public int MostrandoDesde => TotalFiltradas == 0 ? 0 : (Pagina - 1) * TamanoPagina + 1;
        public int MostrandoHasta => Math.Min(Pagina * TamanoPagina, TotalFiltradas);
    }

    public class ReservaFilaViewModel
    {
        public int IdReserva { get; set; }
        public string Codigo { get; set; } = "";
        public string Cliente { get; set; } = "";
        public string Iniciales { get; set; } = "";
        public string? Documento { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string Escenario { get; set; } = "";
        public string TipoEscenario { get; set; } = "";
        public DateOnly FechaUso { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
        public decimal ValorTotal { get; set; }
        public decimal Pagado { get; set; }
        public string? MetodoPago { get; set; }
        public string Estado { get; set; } = "";
        public string? Observaciones { get; set; }
        public DateTime? FechaReserva { get; set; }
    }
}
