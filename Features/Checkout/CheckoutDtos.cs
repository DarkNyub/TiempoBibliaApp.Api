namespace TiempoBiblia.Api.Features.Checkout
{
    /// <summary>
    /// 🔥 NUEVO: Envoltorio principal que recibe el Backend desde el Carrito.
    /// Ahora trae la tarjeta, el correo validado y los productos a facturar.
    /// Envoltorio principal para atrapar el JSON enviado por el frontend (Mercado Pago Bricks).
    /// Bricks encapsula los datos de pago dentro de un objeto llamado 'formData'.
    /// </summary>
    public class BrickPayloadDto
    {
        // Ahora apunta a una clase que contiene la capa extra
        public MpOuterFormDataDto FormData { get; set; } = new(); 
        public string CorreoCliente { get; set; } = string.Empty;
        public List<int> ProductosIds { get; set; } = new();
    }

    // 🔥 NUEVO: Atrapa la capa extra que envía Mercado Pago Bricks
    public class MpOuterFormDataDto
    {
        public string paymentType { get; set; } = string.Empty;
        public PagoBrickDto formData { get; set; } = new(); // Aquí viene el token real
    }

    /// <summary>
    /// DTO que mapea exactamente los datos de la tarjeta y la transacción enviados por Mercado Pago.
    /// NOTA: Las propiedades están en 'snake_case' (minúsculas y guiones bajos) 
    /// para garantizar que el serializador JSON de .NET no pierda los datos en la conversión.
    /// </summary>
    public class PagoBrickDto
    {
        /// <summary>Token encriptado de un solo uso que representa la tarjeta de crédito/débito.</summary>
        public string token { get; set; } = string.Empty;
        
        /// <summary>Método de pago detectado (ej. 'visa', 'master').</summary>
        public string payment_method_id { get; set; } = string.Empty;
        
        /// <summary>ID del banco emisor de la tarjeta.</summary>
        public string? issuer_id { get; set; }
        
        /// <summary>Monto total exacto a cobrar al cliente.</summary>
        public decimal transaction_amount { get; set; }
        
        /// <summary>Cantidad de cuotas seleccionadas por el cliente.</summary>
        public int installments { get; set; }
        
        /// <summary>Información personal del pagador (correo y documento).</summary>
        public PayerDto payer { get; set; } = new();
        
        // 🔥 2. Atrapa los detalles de PSE (ej. código del banco)
        public TransactionDetailsDto? transaction_details { get; set; }
    }

    /// <summary>
    /// Contiene la información de contacto y validación del cliente que está pagando.
    /// </summary>
    public class PayerDto
    {
        public string email { get; set; } = string.Empty;
        // 🔥 3. Atrapa el tipo de persona (individual o jurídica), requerido por PSE
        public string entity_type { get; set; } = string.Empty;
        public IdentificationDto identification { get; set; } = new();
    }

    /// <summary>
    /// Representa el documento de identidad del pagador (Ej. CC, CE) y su número.
    /// </summary>
    public class IdentificationDto
    {
        public string type { get; set; } = string.Empty;
        public string number { get; set; } = string.Empty;
    }
    public class TransactionDetailsDto
    {
        public string financial_institution { get; set; } = string.Empty;
    }

    /// <summary>
    /// Objeto estructurado que el Backend le devuelve al Frontend de Blazor 
    /// para notificarle el resultado de la transacción (Aprobado o Rechazado).
    /// </summary>
    public class RespuestaPagoBrickDto
    {
        public bool Aprobado { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string IdPago { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string UrlRedireccion { get; set; } = string.Empty; // 🔥 NUEVO PARA PSE
    }
    // 🔥 NUEVOS DTOs PARA PAYPAL
    public class SolicitudPagoDto
    {
        public string Titulo { get; set; } = string.Empty;
        public decimal TotalAPagar { get; set; }
        public string CorreoCliente { get; set; } = string.Empty;
        public List<int> ProductosIds { get; set; } = new();
    }

    public class RespuestaPagoDto
    {
        public string UrlPago { get; set; } = string.Empty;
    }

    public class CapturaPayPalRequestDto
    {
        public string OrderId { get; set; } = string.Empty;
        public string CorreoCliente { get; set; } = string.Empty;
        public List<int> ProductosIds { get; set; } = new();
    }

    public class SolicitudPedidoGratisDto 
    {
        public string CorreoCliente { get; set; } = string.Empty;
        public List<int> ProductosIds { get; set; } = new();
    }
    // 🔥 NUEVOS DTOs PARA EL WEBHOOK DE MERCADO PAGO
    public class WebhookMpDto
    {
        public string action { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
        public DataMp data { get; set; } = new();
    }

    public class DataMp
    {
        public string id { get; set; } = string.Empty;
    }
}