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

        [HttpPost("crear-preferencia")]
        public async Task<IActionResult> CrearPreferenciaPago([FromBody] SolicitudPagoDto request)
        {
            try
            {
                // Leemos la URL de tu frontend desde tu appsettings para no quemarla en el código
                var frontendUrl = _config["FrontendSettings:BaseUrl"];

                // 1. Armamos el carrito para Mercado Pago
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
                    // 2. ¿A dónde enviamos al cliente cuando termine de pagar?
                    BackUrls = new PreferenceBackUrlsRequest
                    {
                        Success = $"{frontendUrl}/pago-exitoso", 
                        Failure = $"{frontendUrl}/carrito",
                        Pending = $"{frontendUrl}/carrito"
                    },
                    AutoReturn = "approved"
                };

                // 3. Hablamos con los servidores de Mercado Pago
                var client = new PreferenceClient();
                Preference preference = await client.CreateAsync(requestPreferencia);

                // 4. Devolvemos la URL del Checkout a Blazor
                return Ok(new RespuestaPagoDto { UrlPago = preference.InitPoint });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = "Error al conectar con Mercado Pago: " + ex.Message });
            }
        }
    }
}