namespace TiempoBiblia.Api.Features.Pedidos
{
    /// <summary>
    /// DTO de solo lectura para el panel de administración.
    /// Resumen rápido para la tabla de ventas.
    /// </summary>
    public class PedidoAdminDto
    {
        public int Id { get; set; }
        public string TransaccionGatewayId { get; set; } = string.Empty;
        public string Pasarela { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public decimal TotalCobrado { get; set; }
        public string Moneda { get; set; } = string.Empty;
        public string? CorreoCliente { get; set; }
        public DateTime FechaCreacion { get; set; }
        
        // Un resumen de cuántos productos compró en esta factura
        public int CantidadProductos { get; set; } 
    }
}