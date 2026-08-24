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
            var paymentType = payload.FormData?.paymentType; // 🔥 Extraemos el tipo de pago

            if (request == null)
                return new RespuestaPagoBrickDto { Aprobado = false, Mensaje = "Datos de pago vacíos." };

            // 🔥 1. VALIDACIÓN INTELIGENTE: Solo exigimos token si es Tarjeta. PSE no usa token.
            bool esTarjeta = paymentType == "credit_card" || paymentType == "debit_card";
            if (esTarjeta && string.IsNullOrEmpty(request.token))
                return new RespuestaPagoBrickDto { Aprobado = false, Mensaje = "El token de la tarjeta es inválido." };

            var baseUrl = _config["FrontendSettings:BaseUrl"] ?? "https://tiempobiblia-luzy.online";

            var paymentRequest = new PaymentCreateRequest
            {
                TransactionAmount = request.transaction_amount,
                Token = esTarjeta ? request.token : null, // Solo mandamos token para tarjetas
                Description = "Recursos Digitales - Tiempo Biblia",
                Installments = request.installments > 0 ? request.installments : 1, // PSE no usa cuotas
                PaymentMethodId = request.payment_method_id,
                IssuerId = string.IsNullOrWhiteSpace(request.issuer_id) ? null : request.issuer_id,
                Payer = new PaymentPayerRequest
                {
                    Email = request.payer.email,
                    EntityType = string.IsNullOrWhiteSpace(request.payer.entity_type) ? null : request.payer.entity_type,
                    Identification = new IdentificationRequest 
                    {
                        Type = request.payer.identification.type,
                        Number = request.payer.identification.number
                    }
                },
                // 🔥 2. CLAVE PARA PSE: A dónde regresa el cliente después de ir al banco
                CallbackUrl = $"{baseUrl}/resultado-pago"
            };

            // 🔥 NUEVO: Si trae datos del banco (PSE), se los agregamos al request
            if (request.transaction_details != null && !string.IsNullOrWhiteSpace(request.transaction_details.financial_institution))
            {
                paymentRequest.TransactionDetails = new PaymentTransactionDetailsRequest
                {
                    FinancialInstitution = request.transaction_details.financial_institution
                };
            }

            var client = new PaymentClient();
            Payment payment = await client.CreateAsync(paymentRequest);

            // 🔥 3. SI ES TARJETA (Se aprueba al instante)
            if (payment.Status == "approved")
            {
                string franquicia = payment.PaymentMethodId;
                string? ultimos4 = payment.Card?.LastFourDigits;
                string idPago = payment.Id?.ToString() ?? Guid.NewGuid().ToString();

                var tokens = await _descargaService.ProcesarPedidoAsync(
                    payload.CorreoCliente, idPago, "MercadoPago", franquicia, ultimos4, payload.ProductosIds,
                    request.transaction_amount, "COP");

                var itemsDescarga = tokens.Select(t => (
                    NombreProducto: t.Producto?.Nombre ?? "Recurso Digital",
                    LinkDescarga: $"{baseUrl}/descargar/{t.Id}",
                    ImagenUrl: string.IsNullOrEmpty(t.Producto?.ImagenUrl) ? $"{baseUrl}/images/default.jpg" : t.Producto.ImagenUrl,
                    TutorialUrl: t.Producto?.VideoUrl ?? ""
                )).ToList();

                await _emailService.EnviarCorreoCompraAsync(payload.CorreoCliente, idPago, itemsDescarga);
                
                return new RespuestaPagoBrickDto { Aprobado = true, Estado = payment.Status, IdPago = idPago, Mensaje = "¡Pago procesado con éxito!" };
            }
            // 🔥 4. SI ES PSE (El estado inicial en MP siempre es "pending")
            else if (payment.Status == "pending" && payment.TransactionDetails?.ExternalResourceUrl != null)
            {
                return new RespuestaPagoBrickDto 
                { 
                    Aprobado = true, // Lo marcamos "true" para que no salte el error rojo en el Carrito
                    Estado = "pending", 
                    IdPago = payment.Id?.ToString() ?? "", 
                    UrlRedireccion = payment.TransactionDetails.ExternalResourceUrl // La URL para abrir PSE/Nequi
                };
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
        public async Task<string> CrearOrdenPayPalAsync(decimal totalUsd) // 🔥 Ahora recibe USD directamente
        {
            var token = await GetPayPalAccessTokenAsync();
            
            // 🔥 FIX: Eliminamos la tasa de cambio quemada. Usamos el valor real que mandó el Frontend.
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
                        description = "Recursos Digitales - TiempoBiblia-Luzy"
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
                    // 🔥 Extraemos el ID y el monto real cobrado por PayPal
                    var captureNode = json.GetProperty("purchase_units")[0].GetProperty("payments").GetProperty("captures")[0];
                    var captureId = captureNode.GetProperty("id").GetString()!;
                    
                    var amountString = captureNode.GetProperty("amount").GetProperty("value").GetString()!;
                    decimal totalCobradoUsd = decimal.Parse(amountString, System.Globalization.CultureInfo.InvariantCulture);

                    // 🔥 GUARDADO ATÓMICO Y ENVÍO DE CORREO
                    var tokens = await _descargaService.ProcesarPedidoAsync(
                        correoCliente, captureId, "PayPal", "paypal_balance", null, productosIds,
                        totalCobradoUsd, // 🔥 Le pasamos el monto en Dólares
                        "USD");          // 🔥 Le decimos que la moneda es USD

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
        // ==============================================================================
        // 🔥 LÓGICA DE PEDIDOS GRATUITOS (TOTAL $0)
        // ==============================================================================
        public async Task<RespuestaPagoBrickDto> ProcesarPedidoGratisAsync(SolicitudPedidoGratisDto request)
        {
            if (string.IsNullOrWhiteSpace(request.CorreoCliente) || !request.ProductosIds.Any())
                throw new ArgumentException("Faltan datos para procesar el pedido gratuito.");

            // 1. Generamos un ID de transacción único para auditoría
            string idPago = $"FREE-{Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper()}";

            // 2. Registramos en BD reutilizando tu flujo blindado (Monto: 0, Pasarela: "Gratuito")
            var tokens = await _descargaService.ProcesarPedidoAsync(
                request.CorreoCliente, 
                idPago, 
                "Gratuito", 
                "N/A", 
                null, 
                request.ProductosIds,
                0m,     // 🔥 Total cobrado es 0
                "COP"   // Divisa base
            );

            // 3. Preparamos y enviamos el correo exactamente igual que en compras reales
            var baseUrl = _config["FrontendSettings:BaseUrl"] ?? "https://tiempobiblia-luzy.online";
            var itemsDescarga = tokens.Select(t => (
                NombreProducto: t.Producto?.Nombre ?? "Recurso Digital",
                LinkDescarga: $"{baseUrl}/descargar/{t.Id}",
                ImagenUrl: string.IsNullOrEmpty(t.Producto?.ImagenUrl) ? $"{baseUrl}/images/default.jpg" : t.Producto.ImagenUrl,
                TutorialUrl: t.Producto?.VideoUrl ?? ""
            )).ToList();

            await _emailService.EnviarCorreoCompraAsync(request.CorreoCliente, idPago, itemsDescarga);

            return new RespuestaPagoBrickDto 
            { 
                Aprobado = true, 
                Estado = "approved", 
                IdPago = idPago, 
                Mensaje = "¡Pedido gratuito procesado y enviado con éxito!" 
            };
        }
    }
}