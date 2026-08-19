namespace Sportia.Models.ViewModels
{
    public class DashboardViewModel
    {
        // ==========================
        // TARJETAS SUPERIORES
        // ==========================

        public int TotalReservas { get; set; }

        public int TotalEscenariosActivos { get; set; }

        public int TotalClientes { get; set; }

        public decimal IngresosMes { get; set; }

        public int ReservasHoy { get; set; }


        // ==========================
        // GRÁFICA ÚLTIMOS 7 DÍAS
        // ==========================

        public List<ReservaPorDiaViewModel> ReservasPorDia { get; set; }
            = new();


        // ==========================
        // DISTRIBUCIÓN POR ESCENARIO
        // ==========================

        public List<DistribucionEscenarioViewModel> DistribucionEscenarios { get; set; }
            = new();


        // ==========================
        // RESERVAS POR ESTADO
        // ==========================

        public List<ReservaPorEstadoViewModel> ReservasPorEstado { get; set; }
            = new();


        // ==========================
        // RESERVAS RECIENTES
        // ==========================

        public List<ReservaRecienteViewModel> ReservasRecientes { get; set; }
            = new();


        // ==========================
        // ESCENARIOS MÁS RESERVADOS
        // ==========================

        public List<EscenarioPopularViewModel> EscenariosPopulares { get; set; }
            = new();
    }


    // ======================================
    // RESERVAS POR DÍA
    // ======================================

    public class ReservaPorDiaViewModel
    {
        public string Dia { get; set; } = "";

        public int Cantidad { get; set; }
    }


    // ======================================
    // DISTRIBUCIÓN ESCENARIOS
    // ======================================

    public class DistribucionEscenarioViewModel
    {
        public string Nombre { get; set; } = "";

        public int Cantidad { get; set; }

        public decimal Porcentaje { get; set; }
    }


    // ======================================
    // RESERVAS POR ESTADO
    // ======================================

    public class ReservaPorEstadoViewModel
    {
        public string Nombre { get; set; } = "";

        public int Cantidad { get; set; }

        public decimal Porcentaje { get; set; }
    }


    // ======================================
    // RESERVAS RECIENTES
    // ======================================

    public class ReservaRecienteViewModel
    {
        public int IdReserva { get; set; }

        public string Cliente { get; set; } = "";

        public string Escenario { get; set; } = "";

        public DateOnly Fecha { get; set; }

        public TimeOnly HoraInicio { get; set; }

        public string Estado { get; set; } = "";

        public decimal ValorTotal { get; set; }

        public decimal TotalPagado { get; set; }

        public string EstadoPago { get; set; } = "";
    }


    // ======================================
    // ESCENARIOS POPULARES
    // ======================================

    public class EscenarioPopularViewModel
    {
        public int IdEscenario { get; set; }

        public string Nombre { get; set; } = "";

        public int CantidadReservas { get; set; }

        public decimal Porcentaje { get; set; }
    }
}