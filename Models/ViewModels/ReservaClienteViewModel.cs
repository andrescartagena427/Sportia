using System;
using System.Collections.Generic;

namespace Sportia.Models
{
    // =============================================================
    // VIEWMODEL PARA EL FLUJO DE RESERVA DEL CLIENTE
    // =============================================================
    public class ReservaClienteViewModel
    {
        // ---------------------------------------------------------
        // DATOS DEL ESCENARIO (solo para mostrar en la pantalla)
        // ---------------------------------------------------------
        public int IdEscenario { get; set; }

        public string? NombreEscenario { get; set; }

        public string? Descripcion { get; set; }

        public string? Imagen { get; set; }

        public string? Categoria { get; set; }

        public int? Capacidad { get; set; }

        // ---------------------------------------------------------
        // DATOS QUE EL CLIENTE ELIGE EN EL FORMULARIO
        // ---------------------------------------------------------
        public DateOnly FechaUso { get; set; }

        public TimeOnly HoraInicio { get; set; }

        public TimeOnly HoraFin { get; set; }

        public int IdMetodoPago { get; set; }

        public string? Observaciones { get; set; }

        // ---------------------------------------------------------
        // CAMPOS DE PAGO SIMULADO
        // ---------------------------------------------------------
        // IMPORTANTE: estos datos NUNCA se guardan completos en la
        // base de datos. Solo se usan en el servidor para construir
        // un texto de comprobante (ej: "Tarjeta terminada en 4532").
        // El número completo y el CVV se descartan tras procesarlos.

        public string? NumeroTarjeta { get; set; }

        public string? VencimientoTarjeta { get; set; }

        public string? CvvTarjeta { get; set; }

        public string? NombreTitular { get; set; }

        public string? ReferenciaTransferencia { get; set; }

        // ---------------------------------------------------------
        // PRECIO (se recalcula siempre en el servidor con el precio
        // del escenario; estos valores son solo para mostrarlos)
        // ---------------------------------------------------------
        public decimal PrecioHora { get; set; }

        public decimal Total { get; set; }

        public string? MensajePrecio { get; set; }

        // ---------------------------------------------------------
        // LISTAS PARA EL FORMULARIO
        // ---------------------------------------------------------
        public List<MetodosPago> MetodosPagoDisponibles { get; set; } = new();
    }
}