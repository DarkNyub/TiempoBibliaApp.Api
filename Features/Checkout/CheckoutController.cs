using Microsoft.AspNetCore.Mvc;
using MercadoPago.Config;
using MercadoPago.Client.Payment;
using MercadoPago.Resource.Payment;
using MercadoPago.Client.Common;

namespace TiempoBiblia.Api.Features.Checkout
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckoutController : ControllerBase
    {
        private readonly IConfiguration _config;

        public CheckoutController(IConfiguration config)
        {
            _config = config;
            
            // Leemos el Token de forma segura desde el appsettings.json (Cero Hardcode)
            MercadoPagoConfig.AccessToken = _config["MercadoPago:AccessToken"];
        }

        /// <summary>
        /// POST: api/checkout/procesar-pago-brick
        /// Recibe el token de la tarjeta desde el frontend y ejecuta el cobro directamente.
        /// </summary>
        [HttpPost("procesar-pago-brick")]
        public async Task<IActionResult> ProcesarPagoBrick([FromBody] PagoBrickDto request)
        {
            try
            {
                // 1. Armamos la solicitud de cobro para Mercado Pago
                var paymentRequest = new PaymentCreateRequest
                {
                    TransactionAmount = request.TransactionAmount,
                    Token = request.Token,
                    Description = "Recursos Digitales - Tiempo Biblia",
                    Installments = request.Installments,
                    PaymentMethodId = request.PaymentMethodId,
                    Payer = new PaymentPayerRequest
                    {
                        Email = request.Payer.Email,
                        Identification = new IdentificationRequest
                        {
                            Type = request.Payer.Identification.Type,
                            Number = request.Payer.Identification.Number
                        }
                    }
                };

                // 2. Ejecutamos el cobro
                var client = new PaymentClient();
                Payment payment = await client.CreateAsync(paymentRequest);

                // 3. Evaluamos el resultado del banco
                if (payment.Status == "approved")
                {
                    return Ok(new RespuestaPagoBrickDto 
                    { 
                        Aprobado = true, 
                        Estado = payment.Status, 
                        IdPago = payment.Id.ToString(),
                        Mensaje = "¡Pago procesado con éxito!"
                    });
                }
                else
                {
                    // Si el banco rechazó (fondos insuficientes, etc.)
                    return BadRequest(new RespuestaPagoBrickDto 
                    { 
                        Aprobado = false, 
                        Estado = payment.Status,
                        Mensaje = "El pago fue rechazado por el banco o está pendiente de revisión."
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno al conectar con Mercado Pago", detalle = ex.Message });
            }
        }
    }
}