using Microsoft.AspNetCore.Mvc;

namespace TiempoBiblia.Api.Features.Checkout
{
    [Route("api/[controller]")]
    [ApiController]
    public class PagosController : ControllerBase
    {
        private readonly PayPalService _payPalService;
        private readonly IConfiguration _config;

        public PagosController(PayPalService payPalService, IConfiguration config)
        {
            _payPalService = payPalService;
            _config = config;
        }

        [HttpPost("paypal")]
        public async Task<IActionResult> CrearPagoPayPal([FromBody] SolicitudPagoDto solicitud)
        {
            try
            {
                var baseUrl = _config["FrontendSettings:BaseUrl"] ?? "https://localhost:5001"; // Pon aquí tu puerto local mientras pruebas
                
                // Le decimos a PayPal a dónde devolver al cliente
                var returnUrl = $"{baseUrl}/procesando-paypal";
                var cancelUrl = $"{baseUrl}/carrito";

                var urlAprobacion = await _payPalService.CrearPedidoAsync(solicitud.TotalAPagar, returnUrl, cancelUrl);
                
                return Ok(new RespuestaPagoDto { UrlPago = urlAprobacion });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al conectar con PayPal", detalle = ex.Message });
            }
        }

        [HttpPost("paypal/capturar")]
        public async Task<IActionResult> CapturarPagoPayPal([FromBody] CapturaPayPalRequest request)
        {
            try
            {
                var captureId = await _payPalService.CapturarPedidoAsync(request.TokenId);
                
                if (!string.IsNullOrEmpty(captureId))
                {
                    // ¡El pago fue exitoso! Devolvemos el ID de PayPal para la auditoría
                    return Ok(new { aprobado = true, pagoId = $"paypal-{captureId}" });
                }
                
                return BadRequest(new { aprobado = false, mensaje = "El pago no se pudo completar o fue cancelado." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al capturar el pago", detalle = ex.Message });
            }
        }
    }

    public class SolicitudPagoDto
    {
        public string Titulo { get; set; } = string.Empty;
        public decimal TotalAPagar { get; set; }
        public string CorreoCliente { get; set; } = string.Empty;
        public List<int> ProductosIds { get; set; } = new();
    }

    public class RespuestaPagoDto
    {
        public string UrlPago { get; set; } = string.Empty;
    }

    public class CapturaPayPalRequest
    {
        public string TokenId { get; set; } = string.Empty;
    }
}