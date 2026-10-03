using System.Collections.Generic;

namespace Sportia.Models.ViewModels
{
    // =============================================================
    // DATOS DE LA PÁGINA PRINCIPAL (pública)
    // Se llenan en HomeController.Index con datos reales.
    // =============================================================
    public class InicioViewModel
    {
        // Franja de estadísticas
        public int TotalCanchas { get; set; }
        public int TotalUsuarios { get; set; }
        public int TotalReservas { get; set; }

        // Escenarios activos para las tarjetas
        public List<EscenarioInicioViewModel> Escenarios { get; set; } = new();

        // Opciones de los filtros
        public List<string> Tipos { get; set; } = new();
        public List<string> Empresas { get; set; } = new();

        // Sesión: para saber a dónde lleva el botón "Reservar"
        public bool EsCliente { get; set; }
        public bool HaySesion { get; set; }
    }

    public class EscenarioInicioViewModel
    {
        public int IdEscenario { get; set; }
        public string Nombre { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string Empresa { get; set; } = "";
        public string Ubicacion { get; set; } = "";
        public int? Capacidad { get; set; }
        public decimal? Precio { get; set; }
        public string? Imagen { get; set; }
    }
}
