using MercadoPago.Client.Payment;
using MercadoPago.Client.Common; 
using MercadoPago.Config;
using MercadoPago.Resource.Payment;
using TiempoBiblia.Api.Features.Descargas;
using TiempoBiblia.Api.Features.Correos;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TiempoBiblia.Api.Features.Checkout
{
    public class CheckoutService
    {
        private readonly IConfiguration _config;
        private readonly DescargaService _descargaService;
        private readonly EmailService _emailService;
        private readonly HttpClient _http; // 🔥 NUEVO: Para comunicarnos directamente con PayPal

        public CheckoutService(
            IConfiguration config, 
            DescargaService descargaService, 
            EmailService emailService, 
            HttpClient http) // 🔥 Lo inyectamos en el constructor
        {
            _config = config;
            _descargaService = descargaService;
            _emailService = emailService; 
            _http = http;
            
            MercadoPagoConfig.AccessToken = _config["MercadoPago:AccessToken"];
        }

        // ==============================================================================
        // 🔥 ENRUTADOR PRINCIPAL
        // ==============================================================================
        public async Task<RespuestaPagoBrickDto> ProcesarPagoGenericoAsync(string pasarela, BrickPayloadDto payload)
        {
            if (payload == null || string.IsNullOrWhiteSpace(payload.CorreoCliente) || !payload.ProductosIds.Any())
                throw new ArgumentException("Datos incompletos. Faltan productos o el correo del cliente.");

            if (pasarela.Equals("MercadoPago", StringComparison.OrdinalIgnoreCase))
                return await ProcesarConMercadoPagoAsync(payload);
            
            throw new ArgumentException($"La pasarela '{pasarela}' no está soportada en este enrutador.");
        }

        // ==============================================================================
        // 🔥 LÓGICA DE MERCADO PAGO
        // ==============================================================================
        private async Task<RespuestaPagoBrickDto> ProcesarConMercadoPagoAsync(BrickPayloadDto payload)
        {
            var request = payload.FormData?.formData;

            if (request == null || string.IsNullOrEmpty(request.token))
                return new RespuestaPagoBrickDto { Aprobado = false, Mensaje = "El token de la tarjeta es inválido." };

            var paymentRequest = new PaymentCreateRequest
            {
                TransactionAmount = request.transaction_amount,
                Token = request.token,
                Description = "Recursos Digitales - Tiempo Biblia",
                Installments = request.installments,
                PaymentMethodId = request.payment_method_id,
                IssuerId = request.issuer_id,
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

            var client = new PaymentClient();
            Payment payment = await client.CreateAsync(paymentRequest);

            if (payment.Status == "approved")
            {
                string franquicia = payment.PaymentMethodId;
                string? ultimos4 = payment.Card?.LastFourDigits;
                string idPago = payment.Id?.ToString() ?? Guid.NewGuid().ToString();

                var tokens = await _descargaService.ProcesarPedidoAsync(
                    payload.CorreoCliente, idPago, "MercadoPago", franquicia, ultimos4, payload.ProductosIds);

                var baseUrl = _config["FrontendSettings:BaseUrl"] ?? "https://tiempobiblia-luzy.online";
                
                var itemsDescarga = tokens.Select(t => (
                    NombreProducto: t.Producto?.Nombre ?? "Recurso Digital",
                    LinkDescarga: $"{baseUrl}/descargar/{t.Id}",
                    ImagenUrl: string.IsNullOrEmpty(t.Producto?.ImagenUrl) ? $"{baseUrl}/images/default.jpg" : t.Producto.ImagenUrl,
                    TutorialUrl: t.Producto?.VideoUrl ?? ""
                )).ToList();

                await _emailService.EnviarCorreoCompraAsync(payload.CorreoCliente, idPago, itemsDescarga);
                
                return new RespuestaPagoBrickDto { Aprobado = true, Estado = payment.Status, IdPago = idPago, Mensaje = "¡Pago procesado con éxito!" };
            }
            
            return new RespuestaPagoBrickDto { Aprobado = false, Estado = payment.Status ?? "Rechazado", Mensaje = "El pago fue rechazado." };
        }

        // ==============================================================================
        // 🔥 LÓGICA DE PAYPAL (SMART BUTTONS)
        // ==============================================================================
        
        private string GetPayPalBaseUrl() => _config["PayPal:Mode"] == "Live" 
            ? "https://api-m.paypal.com" 
            : "https://api-m.sandbox.paypal.com";

        /// <summary>
        /// Genera el token de acceso temporal necesario para hablar con la API de PayPal.
        /// </summary>
        private async Task<string> GetPayPalAccessTokenAsync()
        {
            var clientId = _config["PayPal:ClientId"];
            var secret = _config["PayPal:Secret"];
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{secret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, $"{GetPayPalBaseUrl()}/v1/oauth2/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
            request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("access_token").GetString()!;
        }

        /// <summary>
        /// Paso 1: Crea la orden en PayPal (solo retorna el ID de la orden).
        /// </summary>
        public async Task<string> CrearOrdenPayPalAsync(decimal totalCop)
        {
            var token = await GetPayPalAccessTokenAsync();
            
            // ⚠️ Ajusta tu tasa de cambio real aquí
            decimal tasaCambio = 3000m; 
            decimal totalUsd = Math.Round(totalCop / tasaCambio, 2);
            if (totalUsd <= 0) totalUsd = 1.00m; // PayPal exige un mínimo de $1 USD

            var request = new HttpRequestMessage(HttpMethod.Post, $"{GetPayPalBaseUrl()}/v2/checkout/orders");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var order = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new {
                        amount = new { currency_code = "USD", value = totalUsd.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) },
                        description = "Recursos Digitales - Tiempo Biblia"
                    }
                }
            };

            request.Content = new StringContent(JsonSerializer.Serialize(order), Encoding.UTF8, "application/json");
            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("id").GetString()!; // Retorna el OrderID al Frontend
        }

        /// <summary>
        /// Paso 2: El cliente aprobó en la ventanita. Capturamos el dinero y despachamos el pedido.
        /// </summary>
        public async Task<RespuestaPagoBrickDto> ProcesarConPayPalAsync(string orderId, string correoCliente, List<int> productosIds)
        {
            var token = await GetPayPalAccessTokenAsync();
            
            var request = new HttpRequestMessage(HttpMethod.Post, $"{GetPayPalBaseUrl()}/v2/checkout/orders/{orderId}/capture");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent("", Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (json.GetProperty("status").GetString() == "COMPLETED")
                {
                    // Extraemos el ID de transacción real de PayPal
                    var captureId = json.GetProperty("purchase_units")[0]
                                        .GetProperty("payments")
                                        .GetProperty("captures")[0]
                                        .GetProperty("id").GetString()!;

                    // 🔥 GUARDADO ATÓMICO Y ENVÍO DE CORREO
                    var tokens = await _descargaService.ProcesarPedidoAsync(
                        correoCliente, captureId, "PayPal", "paypal_balance", null, productosIds);

                    var baseUrl = _config["FrontendSettings:BaseUrl"] ?? "https://tiempobiblia-luzy.online";
                    
                    var itemsDescarga = tokens.Select(t => (
                        NombreProducto: t.Producto?.Nombre ?? "Recurso Digital",
                        LinkDescarga: $"{baseUrl}/descargar/{t.Id}",
                        ImagenUrl: string.IsNullOrEmpty(t.Producto?.ImagenUrl) ? $"{baseUrl}/images/default.jpg" : t.Producto.ImagenUrl,
                        TutorialUrl: t.Producto?.VideoUrl ?? ""
                    )).ToList();

                    await _emailService.EnviarCorreoCompraAsync(correoCliente, captureId, itemsDescarga);

                    return new RespuestaPagoBrickDto 
                    { 
                        Aprobado = true, 
                        Estado = "approved", 
                        IdPago = captureId, 
                        Mensaje = "¡Pago procesado con éxito vía PayPal!" 
                    };
                }
            }

            return new RespuestaPagoBrickDto { Aprobado = false, Mensaje = "El pago no pudo ser capturado por PayPal." };
        }
    }
}