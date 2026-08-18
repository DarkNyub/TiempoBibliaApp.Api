using MercadoPago.Client.Payment;
using MercadoPago.Client.Common; // 🔥 SOLUCIÓN: Agregamos esta librería
using MercadoPago.Config;
using MercadoPago.Resource.Payment;
using TiempoBiblia.Api.Features.Descargas;
using TiempoBiblia.Api.Features.Correos; // 🔥 SOLUCIÓN: Importamos el namespace del correo

namespace TiempoBiblia.Api.Features.Checkout
{
    public class CheckoutService
    {
        private readonly IConfiguration _config;
        private readonly DescargaService _descargaService;
        private readonly EmailService _emailService; // 🔥 SOLUCIÓN CS0103: Declaramos el servicio

        public CheckoutService(IConfiguration config, DescargaService descargaService, EmailService emailService)
        {
            _config = config;
            _descargaService = descargaService;
            _emailService = emailService; // 🔥 Lo inyectamos
            
            MercadoPagoConfig.AccessToken = _config["MercadoPago:AccessToken"];
        }

        public async Task<RespuestaPagoBrickDto> ProcesarPagoGenericoAsync(string pasarela, BrickPayloadDto payload)
        {
            if (payload == null || string.IsNullOrWhiteSpace(payload.CorreoCliente) || !payload.ProductosIds.Any())
            {
                throw new ArgumentException("Datos incompletos. Faltan productos o el correo del cliente.");
            }

            if (pasarela.Equals("MercadoPago", StringComparison.OrdinalIgnoreCase))
            {
                return await ProcesarConMercadoPagoAsync(payload);
            }
            else if (pasarela.Equals("PayPal", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotImplementedException("La pasarela PayPal se integrará en el siguiente paso.");
            }
            else
            {
                throw new ArgumentException($"La pasarela '{pasarela}' no está soportada en el sistema.");
            }
        }

        private async Task<RespuestaPagoBrickDto> ProcesarConMercadoPagoAsync(BrickPayloadDto payload)
        {
            // 🔥 SOLUCIÓN: Desempaquetamos la capa extra agregando ".formData"
            var request = payload.FormData?.formData;

            if (request == null || string.IsNullOrEmpty(request.token))
            {
                return new RespuestaPagoBrickDto { Aprobado = false, Mensaje = "El token de la tarjeta es inválido." };
            }

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
                    // 🔥 SOLUCIÓN CS0117: Usamos IdentificationRequest (del SDK de MP), NO IdentificationDto
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
                
                // 🔥 NUEVO: Ahora empaquetamos Nombre, Link, Imagen y Tutorial (VideoUrl)
                var itemsDescarga = tokens.Select(t => (
                    NombreProducto: t.Producto?.Nombre ?? "Recurso Digital",
                    LinkDescarga: $"{baseUrl}/descargar/{t.Id}",
                    ImagenUrl: string.IsNullOrEmpty(t.Producto?.ImagenUrl) ? $"{baseUrl}/images/default.jpg" : t.Producto.ImagenUrl,
                    TutorialUrl: t.Producto?.VideoUrl ?? ""
                )).ToList();

                // Disparamos el correo en silencio enviando la lista completa
                await _emailService.EnviarCorreoCompraAsync(payload.CorreoCliente, idPago, itemsDescarga);
                
                return new RespuestaPagoBrickDto 
                { 
                    Aprobado = true, 
                    Estado = payment.Status, 
                    IdPago = idPago, 
                    Mensaje = "¡Pago procesado y registrado con éxito!"
                };
            }
            
            return new RespuestaPagoBrickDto 
            { 
                Aprobado = false, 
                Estado = payment.Status ?? "Rechazado",
                Mensaje = "El pago fue rechazado por el banco."
            };
        }
    }
}