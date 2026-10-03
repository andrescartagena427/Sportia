using System;
using System.Collections.Generic;

namespace Sportia.Models.ViewModels
{
    // =============================================================
    // DATOS DEL DASHBOARD DEL ADMINISTRADOR
    // (todo se calcula en DashboardController con datos reales)
    // =============================================================
    public class DashboardAdminViewModel
    {
        public string NombreAdmin { get; set; } = "Admin";
        public DateOnly Hoy { get; set; }
        public DateOnly FechaCalendario { get; set; }

        // ---------- Tarjetas superiores ----------
        public int ReservasHoy { get; set; }
        public double? VariacionReservas { get; set; }
        public List<double> SerieReservas { get; set; } = new();

        public decimal IngresosHoy { get; set; }
        public double? VariacionIngresos { get; set; }
        public List<double> SerieIngresos { get; set; } = new();

        public int OcupacionHoy { get; set; }
        public double? VariacionOcupacion { get; set; }

        public int ClientesActivos { get; set; }
        public double? VariacionClientes { get; set; }
        public List<double> SerieClientes { get; set; } = new();

        // ---------- Calendario de canchas (día) ----------
        public int HoraInicioCalendario { get; set; }
        public int HoraFinCalendario { get; set; }
        public List<FilaCalendarioViewModel> Calendario { get; set; } = new();

        // ---------- Listas ----------
        public List<ReservaResumenViewModel> ProximasReservas { get; set; } = new();
        public List<ReservaResumenViewModel> ReservasRecientes { get; set; } = new();
        public List<EstadoCanchaViewModel> EstadoCanchas { get; set; } = new();

        // ---------- Ingresos de la semana ----------
        public List<IngresoDiaViewModel> IngresosSemana { get; set; } = new();
        public decimal TotalIngresosSemana { get; set; }
        public double? VariacionIngresosSemana { get; set; }
    }

    public class FilaCalendarioViewModel
    {
        public int IdEscenario { get; set; }
        public string Nombre { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string? Imagen { get; set; }
        public List<BloqueCalendarioViewModel> Bloques { get; set; } = new();
    }

    public class BloqueCalendarioViewModel
    {
        // "reserva", "libre" o "cerrado"
        public string Tipo { get; set; } = "libre";
        public int Columna { get; set; }   // empieza en 1 (cada columna = 30 minutos)
        public int Span { get; set; }
        public string? Cliente { get; set; }
        public string? Horario { get; set; }
        public string? Estado { get; set; }
    }

    public class ReservaResumenViewModel
    {
        public int IdReserva { get; set; }
        public string Cliente { get; set; } = "";
        public string Escenario { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string? Imagen { get; set; }
        public DateOnly Fecha { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
        public string Estado { get; set; } = "";
        public decimal Valor { get; set; }
    }

    public class EstadoCanchaViewModel
    {
        public string Nombre { get; set; } = "";
        public string Tipo { get; set; } = "";
        public int Porcentaje { get; set; }
        public double HorasReservadas { get; set; }
        public double HorasDisponibles { get; set; }
    }

    public class IngresoDiaViewModel
    {
        public string Etiqueta { get; set; } = "";
        public DateOnly Fecha { get; set; }
        public decimal Total { get; set; }
        public bool EsHoy { get; set; }
    }
}
