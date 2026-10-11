using System;
using System.Collections.Generic;

namespace Sportia.Models.ViewModels
{
    // =============================================================
    // PÁGINA DE ESTADÍSTICAS DEL ADMINISTRADOR (AdminEstadistica/Index)
    // Todo se calcula en AdminEstadisticaController con datos reales.
    // =============================================================
    public class EstadisticasViewModel
    {
        // ---------- Período elegido ----------
        public int Dias { get; set; } = 30;          // 0 = todo el tiempo
        public int DiasActividad { get; set; } = 7;  // para la gráfica de actividad
        public string TextoPeriodo { get; set; } = "Últimos 30 días";

        // ---------- Tarjetas ----------
        public int Clientes { get; set; }
        public double? VariacionClientes { get; set; }
        public List<double> SerieClientes { get; set; } = new();

        public int Empresas { get; set; }
        public int EmpresasActivas { get; set; }
        public List<double> SerieEmpresas { get; set; } = new();

        public int Escenarios { get; set; }
        public int EscenariosActivos { get; set; }
        public List<double> SerieEscenarios { get; set; } = new();

        public int Reservas { get; set; }
        public double? VariacionReservas { get; set; }
        public List<double> SerieReservas { get; set; } = new();

        // ---------- Actividad de reservas (por día) ----------
        public List<PuntoGrafica> Actividad { get; set; } = new();

        // ---------- Estado financiero ----------
        public decimal Ingresos { get; set; }
        public decimal Pendiente { get; set; }
        public decimal TotalFinanciero => Ingresos + Pendiente;
        public double? VariacionIngresos { get; set; }

        // ---------- Reservas por escenario ----------
        public List<BarraEscenario> PorEscenario { get; set; } = new();

        // ---------- Pagos recientes ----------
        public List<PagoReciente> PagosRecientes { get; set; } = new();
    }

    public class PuntoGrafica
    {
        public string Etiqueta { get; set; } = "";
        public double Valor { get; set; }
    }

    public class BarraEscenario
    {
        public string Nombre { get; set; } = "";
        public int Cantidad { get; set; }
        public int Porcentaje { get; set; }
    }

    public class PagoReciente
    {
        public string Cliente { get; set; } = "";
        public string Escenario { get; set; } = "";
        public decimal Monto { get; set; }
        public DateTime? Fecha { get; set; }
        public string Estado { get; set; } = "";   // Pagado | Pendiente | Cancelada
    }
}
