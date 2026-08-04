namespace TiempoBiblia.Api.Features.Descargas
{
    // ============================================================
    // DTOs Y MODELOS DE SOLICITUD
    // ============================================================

    public class GenerarLinkRequest
    {
        public int ProductoId { get; set; }
        public string CorreoCliente { get; set; } = string.Empty;
    }
    public class DespacharPedidoRequestDto
    {
        public string Correo { get; set; } = string.Empty;
        public string PagoId { get; set; } = string.Empty;
        public List<int> ProductosIds { get; set; } = new();
    }
}