using Microsoft.AspNetCore.Mvc;
using MercadoPago.Config;
using MercadoPago.Client.Payment;
using MercadoPago.Resource.Payment;
using MercadoPago.Client.Common;

namespace TiempoBiblia.Api.Features.Checkout
{
    /// <summary>
    /// Controlador encargado de procesar los pagos directos generados a través de Mercado Pago Bricks.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CheckoutController : ControllerBase
    {
        private readonly IConfiguration _config;

        public CheckoutController(IConfiguration config)
        {
            _config = config;
            
            // 1. CONFIGURACIÓN INICIAL: Leemos el Access Token (Llave Privada) desde appsettings.json.
            // Asegura que las peticiones vayan al entorno correcto (Pruebas TEST- o Producción APP_USR-).
            MercadoPagoConfig.AccessToken = _config["MercadoPago:AccessToken"];
        }

        /// <summary>
        /// Endpoint que recibe los datos encriptados del formulario de la tarjeta (Bricks) 
        /// y ejecuta el cobro inmediato comunicándose de servidor a servidor con Mercado Pago.
        /// </summary>
        /// <param name="payloadWrapper">Objeto JSON que contiene el token de la tarjeta y el monto.</param>
        /// <returns>Retorna 200 OK si el banco aprueba el pago, o 400 BadRequest si es rechazado.</returns>
        [HttpPost("procesar-pago-brick")]
        public async Task<IActionResult> ProcesarPagoBrick([FromBody] BrickPayloadDto payloadWrapper)
        {
            try
            {
                // 2. EXTRACCIÓN DE DATOS: Sacamos la información real que viene dentro de 'formData'.
                var request = payloadWrapper.formData;

                // 3. TRAMPA DE SEGURIDAD (Validación de Mapeo): 
                // Verificamos que el serializador JSON haya logrado extraer los datos correctamente.
                // Si el token viene vacío o el monto es 0, evitamos enviar la petición a Mercado Pago para ahorrar errores 400.
                if (request == null || string.IsNullOrEmpty(request.token) || request.transaction_amount <= 0)
                {
                    return BadRequest(new { 
                        mensaje = "Error de Mapeo en el Backend.", 
                        detalle = $"Token recibido: {request?.token}, Monto recibido: {request?.transaction_amount}" 
                    });
                }

                // 4. CONSTRUCCIÓN DEL PAGO: Armamos el objeto oficial que exige el SDK de Mercado Pago.
                var paymentRequest = new PaymentCreateRequest
                {
                    TransactionAmount = request.transaction_amount,
                    Token = request.token,
                    Description = "Recursos Digitales - Tiempo Biblia",
                    Installments = request.installments,
                    PaymentMethodId = request.payment_method_id,
                    IssuerId = request.issuer_id, // Vital para que el banco identifique la procedencia de la tarjeta.
                    Payer = new PaymentPayerRequest
                    {
                        Email = request.payer.email,
                        Identification = new IdentificationRequest
                        {
                            Type = request.payer.identification.type,
                            Number = request.payer.identification.number
                        }
                    }
                };

                // 5. EJECUCIÓN DEL COBRO: Instanciamos el cliente y enviamos la orden de cobro al banco.
                var client = new PaymentClient();
                Payment payment = await client.CreateAsync(paymentRequest);

                // 6. EVALUACIÓN DE RESPUESTA: Comprobamos si el banco (Visa, Mastercard, etc.) aprobó la transacción.
                if (payment.Status == "approved")
                {
                    return Ok(new RespuestaPagoBrickDto 
                    { 
                        Aprobado = true, 
                        Estado = payment.Status, 
                        // Uso de operador nulo para evitar excepciones si la API de MP no retorna un ID por algún fallo raro.
                        IdPago = payment.Id?.ToString() ?? string.Empty, 
                        Mensaje = "¡Pago procesado con éxito!"
                    });
                }
                else
                {
                    // Si el estado es "rejected" (fondos insuficientes) o "in_process" (requiere revisión manual).
                    return BadRequest(new RespuestaPagoBrickDto 
                    { 
                        Aprobado = false, 
                        Estado = payment.Status ?? "Rechazado",
                        Mensaje = "El pago fue rechazado por el banco o está pendiente de revisión."
                    });
                }
            }
            catch (Exception ex)
            {
                // 7. MANEJO DE EXCEPCIONES: Atrapa caídas del servidor de Mercado Pago, timeouts o errores de código.
                return StatusCode(500, new { mensaje = "Error interno al conectar con Mercado Pago", detalle = ex.Message });
            }
        }
    }
}