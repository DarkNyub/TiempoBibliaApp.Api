using System.ComponentModel.DataAnnotations;

namespace TiempoBiblia.Api.Features.Pedidos
{
    /// <summary>
    /// Representa el encabezado de una transacción o compra en el sistema.
    /// Funciona como el registro de auditoría principal de ingresos.
    /// </summary>
    public class Pedido
    {
        public int Id { get; set; }
        
        /// <summary>
        /// El ID de transacción que devuelve la pasarela (Ej. el número larguísimo de Mercado Pago).
        /// Fundamental para buscar reclamos o devoluciones en el panel del banco.
        /// </summary>
        [Required, MaxLength(100)]
        public string TransaccionGatewayId { get; set; } = string.Empty; 
        
        /// <summary>
        /// Define quién procesó el pago. (Ej. "MercadoPago", "PayPal").
        /// Se deja como string quemado para no sobrecargar la BD con tablas innecesarias.
        /// </summary>
        [Required, MaxLength(50)]
        public string Pasarela { get; set; } = string.Empty; 
        
        /// <summary>
        /// El estado final devuelto por el banco: "approved", "rejected", "in_process".
        /// </summary>
        [Required, MaxLength(50)]
        public string Estado { get; set; } = string.Empty; 
        
        /// <summary>
        /// El monto total exacto que se le debitó a la tarjeta del cliente.
        /// </summary>
        public decimal TotalCobrado { get; set; }
        
        /// <summary>
        /// Divisa en la que se realizó el cobro (Ej. "COP" para Mercado Pago, "USD" para PayPal).
        /// </summary>
        [Required, MaxLength(10)]
        public string Moneda { get; set; } = "COP"; 
        
        /// <summary>
        /// Correo al que se enviaron los enlaces. Permite nulos al inicio porque se llena 
        /// en la pantalla de éxito después de que el pago ya fue aprobado.
        /// </summary>
        [MaxLength(150)]
        public string? CorreoCliente { get; set; } 
        
        /// <summary>
        /// Fecha y hora exacta (UTC) en la que el banco aprobó la transacción.
        /// </summary>
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Relación 1 a Muchos: Un pedido tiene varios detalles (productos comprados)
        public ICollection<PedidoDetalle> Detalles { get; set; } = new List<PedidoDetalle>();
    }
}