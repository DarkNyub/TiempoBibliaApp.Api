using Microsoft.AspNetCore.Mvc;
using MercadoPago.Config;
using MercadoPago.Client.Preference;
using MercadoPago.Resource.Preference;

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
            
            // Leemos el Token de forma segura desde el JSON
            MercadoPagoConfig.AccessToken = _config["MercadoPago:AccessToken"];
        }

        // =========================================================
        // 1. ENDPOINT PARA PRODUCCIÓN (DINERO REAL)
        // =========================================================
        [HttpPost("crear-preferencia")]
        public async Task<IActionResult> CrearPreferenciaPago([FromBody] SolicitudPagoDto request)
        {
            try
            {
                var preference = await GenerarPreferenciaMercadoPago(request);
                
                // Devuelve la URL REAL (InitPoint)
                return Ok(new RespuestaPagoDto { UrlPago = preference.InitPoint });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = "Error al conectar con Mercado Pago: " + ex.Message });
            }
        }

        // =========================================================
        // 2. ENDPOINT PARA PRUEBAS (SANDBOX)
        // =========================================================
        [HttpPost("crear-preferencia-sandbox")]
        public async Task<IActionResult> CrearPreferenciaPagoSandbox([FromBody] SolicitudPagoDto request)
        {
            try
            {
                var preference = await GenerarPreferenciaMercadoPago(request);
                
                // Devuelve la URL DE PRUEBAS (SandboxInitPoint)
                return Ok(new RespuestaPagoDto { UrlPago = preference.SandboxInitPoint });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = "Error al conectar con Mercado Pago: " + ex.Message });
            }
        }

        // =========================================================
        // MÉTODO AUXILIAR PARA NO REPETIR CÓDIGO
        // =========================================================
        private async Task<Preference> GenerarPreferenciaMercadoPago(SolicitudPagoDto request)
        {
            var frontendUrl = _config["FrontendSettings:BaseUrl"];

            var requestPreferencia = new PreferenceRequest
            {
                Items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Title = request.Titulo,
                        Quantity = 1,
                        CurrencyId = "COP", // Moneda en Pesos Colombianos
                        UnitPrice = request.TotalAPagar,
                    }
                },
                BackUrls = new PreferenceBackUrlsRequest
                {
                    Success = $"{frontendUrl}/pago-exitoso", 
                    Failure = $"{frontendUrl}/carrito",
                    Pending = $"{frontendUrl}/carrito"
                },
                AutoReturn = "approved"
            };

            var client = new PreferenceClient();
            return await client.CreateAsync(requestPreferencia);
        }
    }
}