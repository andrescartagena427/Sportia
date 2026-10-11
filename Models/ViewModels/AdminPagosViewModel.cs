using System;
using System.Collections.Generic;

namespace Sportia.Models.ViewModels
{
    // =============================================================
    // PÁGINA DE PAGOS DEL ADMINISTRADOR (AdminPago/Index)
    // =============================================================
    public class AdminPagosViewModel
    {
        // ---------- Filtros ----------
        public string? Buscar { get; set; }
        public int? IdMetodo { get; set; }
        public DateOnly? Desde { get; set; }
        public DateOnly? Hasta { get; set; }
        public List<MetodosPago> Metodos { get; set; } = new();

        // ---------- Tarjetas (según los filtros) ----------
        public int TotalPagos { get; set; }
        public decimal DineroRecibido { get; set; }
        public decimal SaldoPendiente { get; set; }
        public int PorConfirmar { get; set; }

        // Comparación de este mes contra el mes anterior (todos los pagos)
        public double? VariacionPagosMes { get; set; }
        public double? VariacionDineroMes { get; set; }

        // ---------- Tabla ----------
        public List<PagoFilaViewModel> Pagos { get; set; } = new();
        public int Pagina { get; set; } = 1;
        public int TamanoPagina { get; set; } = 10;
        public int TotalFiltrados { get; set; }
        public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(TotalFiltrados / (double)TamanoPagina));
        public int MostrandoDesde => TotalFiltrados == 0 ? 0 : (Pagina - 1) * TamanoPagina + 1;
        public int MostrandoHasta => Math.Min(Pagina * TamanoPagina, TotalFiltrados);
    }

    public class PagoFilaViewModel
    {
        public int IdPago { get; set; }
        public int IdCliente { get; set; }
        public string Cliente { get; set; } = "";
        public string CodigoReserva { get; set; } = "";
        public string Escenario { get; set; } = "";
        public string Metodo { get; set; } = "";
        public decimal MontoPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public bool ReservaCancelada { get; set; }
        public DateTime? FechaPago { get; set; }
    }
}
