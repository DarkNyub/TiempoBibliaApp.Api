namespace TiempoBiblia.Api.Features.Checkout
{
    // DTO para enviar el total a cobrar desde Blazor
    public class SolicitudPagoDto
    {
        public string Titulo { get; set; } = "Recursos de Tiempo Biblia";
        public decimal TotalAPagar { get; set; }
    }

    // DTO para devolverle el link generado a Blazor
    public class RespuestaPagoDto
    {
        public string UrlPago { get; set; } = string.Empty;
    }
}