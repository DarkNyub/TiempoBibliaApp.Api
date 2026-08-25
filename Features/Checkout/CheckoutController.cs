using Microsoft.AspNetCore.Mvc;
using TiempoBiblia.Api.Features.Checkout;
using TiempoBiblia.Api.Data;       // 🔥 Para que reconozca tu AppDbContext

namespace TiempoBiblia.Api.Features.Checkout
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckoutController : ControllerBase
    {
        private readonly CheckoutService _checkoutService;

        public CheckoutController(CheckoutService checkoutService)
        {
            _checkoutService = checkoutService;
        }

        [HttpPost("procesar-pago-brick")]
        public async Task<IActionResult> ProcesarPagoBrick([FromBody] BrickPayloadDto payloadWrapper)
        {
            try
            {
                // 🔥 NUEVO: Capturamos la IP real del cliente que está haciendo la petición
                string ipCliente = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                
                // Si estás probando en localhost, a veces llega en formato IPv6 (::1). Lo pasamos a IPv4.
                if (ipCliente == "::1") ipCliente = "127.0.0.1";

                // Le pasamos la IP al servicio
                var resultado = await _checkoutService.ProcesarPagoGenericoAsync("MercadoPago", payloadWrapper, ipCliente);
                if (resultado.Aprobado)
                {
                    return Ok(resultado);
                }
                
                return BadRequest(resultado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }
        [HttpPost("paypal/crear")]
        public async Task<IActionResult> CrearOrdenPayPal([FromBody] SolicitudPagoDto solicitud)
        {
            try
            {
                // El servicio crea la orden en PayPal y nos devuelve el ID
                var orderId = await _checkoutService.CrearOrdenPayPalAsync(solicitud.TotalAPagar, solicitud.ProductosIds);
                return Ok(new RespuestaPagoDto { UrlPago = orderId }); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al conectar con PayPal", detalle = ex.Message });
            }
        }

        [HttpPost("procesar-pago-paypal")]
        public async Task<IActionResult> ProcesarPagoPayPal([FromBody] CapturaPayPalRequestDto payload)
        {
            try
            {
                // El servicio captura el dinero y envía los correos
                var resultado = await _checkoutService.ProcesarConPayPalAsync(payload.OrderId, payload.CorreoCliente, payload.ProductosIds);
                
                if (resultado.Aprobado) return Ok(resultado);
                return BadRequest(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        [HttpPost("procesar-gratis")]
        public async Task<IActionResult> ProcesarPedidoGratis([FromBody] SolicitudPedidoGratisDto solicitud)
        {
            try
            {
                var resultado = await _checkoutService.ProcesarPedidoGratisAsync(solicitud);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al procesar pedido gratuito", detalle = ex.Message });
            }
        }
        [HttpPost("webhook")]
        public async Task<IActionResult> WebhookMercadoPago([FromBody] WebhookMpDto payload, [FromServices] AppDbContext context)
        {
            try
            {
                // Disparamos la lógica en segundo plano sin hacer esperar a Mercado Pago
                _ = _checkoutService.ProcesarWebhookMercadoPagoAsync(payload, context);
                
                // Mercado Pago exige que le respondas "200 OK" rápidamente
                return Ok(new { recibido = true }); 
            }
            catch (Exception ex)
            {
                // Aún si hay error nuestro, le devolvemos OK a MP para que no reintente locamente
                return Ok(new { error = ex.Message }); 
            }
        }
    }
}