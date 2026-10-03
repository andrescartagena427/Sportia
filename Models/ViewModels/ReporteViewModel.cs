using System;
using System.Collections.Generic;

namespace Sportia.Models.ViewModels
{
    // =============================================================
    // PÁGINA DE REPORTES DEL ADMINISTRADOR (AdminReporte/Index)
    // Todo se calcula en AdminReporteController con datos reales.
    // =============================================================
    public class ReporteViewModel
    {
        // ---------- Período ----------
        public string Periodo { get; set; } = "mes";
        public DateOnly Desde { get; set; }
        public DateOnly Hasta { get; set; }
        public string TextoRango { get; set; } = "";
        public string TextoComparacion { get; set; } = "vs. período anterior";

        // ---------- Tarjetas ----------
        public int TotalClientes { get; set; }
        public int ClientesNuevos { get; set; }
        public double? VariacionClientes { get; set; }
        public List<double> SerieClientes { get; set; } = new();

        public int TotalEmpresas { get; set; }
        public int EmpresasActivas { get; set; }

        public int ReservasPeriodo { get; set; }
        public double? VariacionReservas { get; set; }
        public List<double> SerieReservasMini { get; set; } = new();

        public int ReservasHoy { get; set; }
        public double? VariacionHoy { get; set; }
        public List<double> SerieHoy { get; set; } = new();

        public decimal Ingresos { get; set; }
        public double? VariacionIngresos { get; set; }

        public decimal SaldoPendiente { get; set; }
        public int ReservasConSaldo { get; set; }

        // ---------- Resumen general (todo el sistema) ----------
        public int TotalUsuarios { get; set; }
        public int TotalEscenarios { get; set; }
        public int TotalPagos { get; set; }
        public int TotalReservas { get; set; }

        // ---------- Gráficas (una por pestaña) ----------
        public SerieGrafica GraficaReservas { get; set; } = new();
        public SerieGrafica GraficaIngresos { get; set; } = new();
        public SerieGrafica GraficaClientes { get; set; } = new();

        // ---------- Detalles por pestaña ----------
        public List<ItemReporte> ReservasPorEstado { get; set; } = new();
        public List<ItemReporte> TopEscenarios { get; set; } = new();
        public List<ItemReporte> IngresosPorMetodo { get; set; } = new();
        public List<ItemReporte> UsuariosPorRol { get; set; } = new();
    }

    public class SerieGrafica
    {
        public List<string> Etiquetas { get; set; } = new();
        public List<double> Valores { get; set; } = new();
        public bool EsDinero { get; set; }
    }

    public class ItemReporte
    {
        public string Nombre { get; set; } = "";
        public double Valor { get; set; }
        public string? Extra { get; set; }
    }
}
