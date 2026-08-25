namespace TiempoBiblia.Api.Features.Pedidos
{
    /// <summary>
    /// DTO de solo lectura para el panel de administración.
    /// Contiene el resumen de la factura y la lista de productos comprados.
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
        
        // 🔥 NUEVOS CAMPOS: Para auditoría visual en el panel
        public string? Franquicia { get; set; }
        public string? Ultimos4Digitos { get; set; }

        public int CantidadProductos { get; set; } 
        
        // 🔥 NUEVO: La lista de productos que va a alimentar el Acordeón en el Frontend
        public List<PedidoDetalleAdminDto> Detalles { get; set; } = new();
    }

    /// <summary>
    /// 🔥 NUEVO: DTO para el detalle histórico de cada producto comprado en esa transacción.
    /// </summary>
    public class PedidoDetalleAdminDto
    {
        public int ProductoId { get; set; }
        public string NombreProductoHistorico { get; set; } = string.Empty;
        public decimal PrecioUnitarioPagado { get; set; }
    }
    public class ReenviarCorreoRequestDto
    {
        public string NuevoCorreo { get; set; } = string.Empty;
    }
}