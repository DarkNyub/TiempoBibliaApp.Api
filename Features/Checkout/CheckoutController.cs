using Microsoft.AspNetCore.Mvc;

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
                // Le pasamos la data genérica al servicio indicándole que use "MercadoPago"
                var resultado = await _checkoutService.ProcesarPagoGenericoAsync("MercadoPago", payloadWrapper);

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
    }
}