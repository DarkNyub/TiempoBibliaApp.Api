using MercadoPago.Client.Payment;
using MercadoPago.Client.Common; 
using MercadoPago.Config;
using MercadoPago.Resource.Payment;
using TiempoBiblia.Api.Features.Descargas;
using TiempoBiblia.Api.Features.Correos;
using TiempoBiblia.Api.Features.Pedidos;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore; // 🔥 Para poder usar el .AnyAsync()
using TiempoBiblia.Api.Data;       // 🔥 Para que reconozca tu AppDbContext

namespace TiempoBiblia.Api.Features.Checkout
{
    public class CheckoutService
    {
        private readonly IConfiguration _config;
        private readonly DescargaService _descargaService;
        private readonly EmailService _emailService;
        private readonly PedidoRepository _pedidoRepository; // 🔥 NUEVO: Para validar cupos de talleres
        private readonly HttpClient _http; // 🔥 NUEVO: Para comunicarnos directamente con PayPal

        public CheckoutService(
            IConfiguration config, 
            DescargaService descargaService,
            PedidoRepository pedidoRepository,
            EmailService emailService, 
            HttpClient http) // 🔥 Lo inyectamos en el constructor
        {
            _config = config;
            _descargaService = descargaService;
            _pedidoRepository = pedidoRepository;
            _emailService = emailService; 
            _http = http;
            
            MercadoPagoConfig.AccessToken = _config["MercadoPago:AccessToken"];
        }

        // ==============================================================================
        // 🔥 ENRUTADOR PRINCIPAL
        // ==============================================================================
        // 🔥 Agregamos el parámetro ipCliente (con un valor por defecto por si acaso)
        public async Task<RespuestaPagoBrickDto> ProcesarPagoGenericoAsync(string pasarela, BrickPayloadDto payload, string ipCliente = "127.0.0.1")
        {
            if (payload == null || string.IsNullOrWhiteSpace(payload.CorreoCliente) || !payload.ProductosIds.Any())
                throw new ArgumentException("Datos incompletos. Faltan productos o el correo del cliente.");

            if (pasarela.Equals("MercadoPago", StringComparison.OrdinalIgnoreCase))
                return await ProcesarConMercadoPagoAsync(payload, ipCliente); // 🔥 Le pasamos la IP aquí
            
            throw new ArgumentException($"La pasarela '{pasarela}' no está soportada en este enrutador.");
        }

        // ==============================================================================
        // 🔥 LÓGICA DE MERCADO PAGO
        // ==============================================================================
        private async Task<RespuestaPagoBrickDto> ProcesarConMercadoPagoAsync(BrickPayloadDto payload, string ipCliente)
        {
            var request = payload.FormData?.formData;
            var paymentType = payload.FormData?.paymentType;

            if (request == null)
                return new RespuestaPagoBrickDto { Aprobado = false, Mensaje = "Datos de pago vacíos." };

            // Verificamos si es tarjeta para exigir o no el token
            bool esTarjeta = paymentType == "credit_card" || paymentType == "debit_card";
            if (esTarjeta && string.IsNullOrEmpty(request.token))
                return new RespuestaPagoBrickDto { Aprobado = false, Mensaje = "El token de la tarjeta es inválido." };

            var baseUrl = _config["FrontendSettings:BaseUrl"] ?? "https://tiempobiblia-luzy.online";

            // ==============================================================================
            // 🔥 VALIDACIÓN DE CUPOS PARA TALLERES PRESENCIALES
            // ==============================================================================
            string? errorCupos = await _pedidoRepository.ValidarDisponibilidadCuposAsync(payload.ProductosIds);
            
            if (!string.IsNullOrEmpty(errorCupos))
            {
                // Devolvemos el rechazo inmediato para que el frontend lo muestre como error
                return new RespuestaPagoBrickDto 
                { 
                    Aprobado = false, 
                    Mensaje = errorCupos 
                };
            }

            // ==============================================================================
            // 🔥 CONSTRUCCIÓN DEL REQUEST A MERCADO PAGO
            // ==============================================================================
            var paymentRequest = new PaymentCreateRequest
            {
                TransactionAmount = request.transaction_amount,
                Token = esTarjeta ? request.token : null,
                Description = "Recursos Digitales - Tiempo Biblia",
                Installments = request.installments > 0 ? request.installments : 1,
                PaymentMethodId = request.payment_method_id,
                IssuerId = string.IsNullOrWhiteSpace(request.issuer_id) ? null : request.issuer_id,
                
                // 🔥 EL EQUIPAJE SECRETO: Guardamos qué compró y su correo
                Metadata = new Dictionary<string, object>
                {
                    { "correo", payload.CorreoCliente },
                    { "productos", string.Join(",", payload.ProductosIds) }
                },
                // Redirección post-pago PSE
                CallbackUrl = $"{baseUrl}/resultado-pago", 
                
                // IP Obligatoria para transferencias bancarias (PSE)
                AdditionalInfo = new PaymentAdditionalInfoRequest
                {
                    IpAddress = ipCliente 
                },
                

                // 🔥 CONFIGURACIÓN DEL PAGADOR (PAYER)
                Payer = new PaymentPayerRequest
                {
                    // 1. Datos REALES (Vienen del formulario del usuario)
                    Email = request.payer.email,
                    EntityType = string.IsNullOrWhiteSpace(request.payer.entity_type) ? "individual" : request.payer.entity_type,
                    Identification = new IdentificationRequest 
                    {
                        Type = request.payer.identification.type,
                        Number = request.payer.identification.number
                    },
                    
                    // 2. Datos QUEMADOS (Para satisfacer las estrictas reglas de PSE sin molestar al cliente)
                    FirstName = "Lector",
                    LastName = "Tiempo Biblia",
                    Phone = new PaymentPayerPhoneRequest
                    {
                        AreaCode = "57",
                        Number = string.IsNullOrWhiteSpace(payload.CelularCliente) ? "3000000000" : payload.CelularCliente // Número genérico de 10 dígitos
                    },
                    Address = new PaymentPayerAddressRequest
                    {
                        ZipCode = "111156",          // Código postal genérico (Bogotá)
                        StreetName = "Calle Virtual",// Requerido por PSE
                        StreetNumber = 123,        // Requerido por PSE
                        Neighborhood = "Centro",     // Requerido por PSE
                        City = "Bogotá",             // Requerido por PSE
                        FederalUnit = "Bogotá D.C."  // Departamento/Estado
                    }
                }
            };

            // 🔥 Si es PSE, agregamos el código del banco (Institución Financiera)
            if (request.transaction_details != null && !string.IsNullOrWhiteSpace(request.transaction_details.financial_institution))
            {
                paymentRequest.TransactionDetails = new PaymentTransactionDetailsRequest
                {
                    FinancialInstitution = request.transaction_details.financial_institution
                };
            }

            var client = new PaymentClient();
            Payment payment = await client.CreateAsync(paymentRequest);

            // ==============================================================================
            // 🔥 MANEJO DE LA RESPUESTA DE MERCADO PAGO
            // ==============================================================================
            
            // 1. SI ES TARJETA (Se aprueba al instante)
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
                    TutorialUrl: t.Producto?.VideoUrl ?? "",
                    Tipo: t.Producto?.Tipo ?? ""
                )).ToList();

                await _emailService.EnviarCorreoCompraAsync(payload.CorreoCliente, idPago, itemsDescarga);
                
                return new RespuestaPagoBrickDto { Aprobado = true, Estado = payment.Status, IdPago = idPago, Mensaje = "¡Pago procesado con éxito!" };
            }
            // 2. SI ES PSE (El estado es "pending" y requiere redirección al banco)
            else if (payment.Status == "pending" && payment.TransactionDetails?.ExternalResourceUrl != null)
            {
                return new RespuestaPagoBrickDto 
                { 
                    Aprobado = true, 
                    Estado = "pending", 
                    IdPago = payment.Id?.ToString() ?? "", 
                    UrlRedireccion = payment.TransactionDetails.ExternalResourceUrl 
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
                        TutorialUrl: t.Producto?.VideoUrl ?? "",
                        Tipo: t.Producto?.Tipo ?? ""
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
                TutorialUrl: t.Producto?.VideoUrl ?? "",
                Tipo: t.Producto?.Tipo ?? ""
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
        // ==============================================================================
        // 🔥 WEBHOOK: ESCUCHA DE PAGOS ASÍNCRONOS (PSE)
        // ==============================================================================
        public async Task ProcesarWebhookMercadoPagoAsync(WebhookMpDto webhook, AppDbContext _context)
        {
            // MercadoPago envía avisos de pagos nuevos o actualizados
            if (webhook.type == "payment" || webhook.action == "payment.updated" || webhook.action == "payment.created")
            {
                if (long.TryParse(webhook.data.id, out long paymentId))
                {
                    var client = new PaymentClient();
                    Payment payment = await client.GetAsync(paymentId); // Preguntamos a MP el estado real

                    // Si ya está aprobado, procedemos a despachar
                    if (payment.Status == "approved")
                    {
                        string idPago = payment.Id.ToString()!;

                        // 🔥 REGLA DE ORO (Idempotencia): Verificamos que no hayamos despachado este pedido antes.
                        // Esto evita que mandes correos dobles por Tarjetas de Crédito.
                        bool yaProcesado = await _context.Pedidos.AnyAsync(p => p.TransaccionGatewayId == idPago);
                        if (yaProcesado) return; // Si ya existe, ignoramos el webhook

                        // Rescatamos el equipaje secreto
                        if (payment.Metadata != null && payment.Metadata.ContainsKey("correo"))
                        {
                            string correo = payment.Metadata["correo"].ToString()!;
                            string productosString = payment.Metadata["productos"].ToString()!;
                            List<int> productosIds = productosString.Split(',').Select(int.Parse).ToList();

                            string franquicia = payment.PaymentMethodId ?? "PSE";
                            decimal montoPagado = payment.TransactionAmount ?? 0;

                            // 1. Guardamos en Base de Datos
                            var tokens = await _descargaService.ProcesarPedidoAsync(
                                correo, idPago, "MercadoPago", franquicia, null, productosIds, montoPagado, "COP");

                            // 2. Preparamos el correo
                            var baseUrl = _config["FrontendSettings:BaseUrl"] ?? "https://tiempobiblia-luzy.online";
                            var itemsDescarga = tokens.Select(t => (
                                NombreProducto: t.Producto?.Nombre ?? "Recurso Digital",
                                LinkDescarga: $"{baseUrl}/descargar/{t.Id}",
                                ImagenUrl: string.IsNullOrEmpty(t.Producto?.ImagenUrl) ? $"{baseUrl}/images/default.jpg" : t.Producto.ImagenUrl,
                                TutorialUrl: t.Producto?.VideoUrl ?? "",
                                Tipo: t.Producto?.Tipo ?? ""
                            )).ToList();

                            // 3. Despachamos el correo en silencio (background)
                            await _emailService.EnviarCorreoCompraAsync(correo, idPago, itemsDescarga);
                        }
                    }
                }
            }
        }
    }
}