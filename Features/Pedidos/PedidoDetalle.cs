using System.ComponentModel.DataAnnotations;

namespace TiempoBiblia.Api.Features.Pedidos
{
    /// <summary>
    /// Representa la línea de factura (el detalle). Es intocable históricamente.
    /// Garantiza que la auditoría no se rompa incluso si el producto original es eliminado.
    /// </summary>
    public class PedidoDetalle
    {
        public int Id { get; set; }
        
        // Relación con el Pedido Padre
        public int PedidoId { get; set; }
        public Pedido Pedido { get; set; } = null!;

        /// <summary>
        /// ID referencial del producto comprado.
        /// </summary>
        public int ProductoId { get; set; }
        
        /// <summary>
        /// Se guarda el texto exacto del nombre del producto en el momento de la compra.
        /// Si en 3 meses le cambias el nombre al producto o lo borras, la factura antigua mantiene el original.
        /// </summary>
        [Required, MaxLength(300)]
        public string NombreProductoHistorico { get; set; } = string.Empty;
        
        /// <summary>
        /// El precio unitario exacto al que se vendió este ítem en esta transacción específica,
        /// ignorando cambios de precio o descuentos futuros.
        /// </summary>
        public decimal PrecioUnitarioPagado { get; set; }
    }
}