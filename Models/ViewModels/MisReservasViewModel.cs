using System;
using System.Collections.Generic;

namespace Sportia.Models.ViewModels
{
    // =============================================================
    // PÁGINA "MIS RESERVAS" DEL CLIENTE (Reservas/Mias)
    // =============================================================
    public class MisReservasViewModel
    {
        public List<MiReservaViewModel> Proximas { get; set; } = new();
        public List<MiReservaViewModel> Pasadas { get; set; } = new();
        public List<MiReservaViewModel> Canceladas { get; set; } = new();

        public decimal TotalInvertido { get; set; }

        // Reserva recién creada (para resaltarla)
        public int? IdReservaNueva { get; set; }

        public int HorasMinimasParaCancelar { get; set; }
    }

    public class MiReservaViewModel
    {
        public int IdReserva { get; set; }
        public string Codigo { get; set; } = "";
        public int IdEscenario { get; set; }
        public string Escenario { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string Ubicacion { get; set; } = "";
        public string? Imagen { get; set; }

        public DateOnly FechaUso { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }

        public string Estado { get; set; } = "";
        public bool Cancelada { get; set; }

        public decimal ValorTotal { get; set; }
        public decimal Pagado { get; set; }
        public decimal Saldo { get; set; }
        public string? MetodoPago { get; set; }
        public string? Observaciones { get; set; }

        public bool PuedeCancelar { get; set; }
    }
}
