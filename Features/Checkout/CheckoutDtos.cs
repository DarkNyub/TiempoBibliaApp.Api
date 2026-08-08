using System.Text.Json.Serialization;

namespace TiempoBiblia.Api.Features.Checkout
{
    // 🔥 1. EL ENVOLTORIO: Esto atrapa la cajita exterior que manda JavaScript
    public class BrickPayloadDto
    {
        [JsonPropertyName("formData")]
        public PagoBrickDto FormData { get; set; } = new();
    }
    // =========================================================
    // DTOs PARA RECIBIR EL PAGO DESDE BRICKS (FRONTEND)
    // =========================================================
    public class PagoBrickDto
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("payment_method_id")]
        public string PaymentMethodId { get; set; } = string.Empty;

        [JsonPropertyName("transaction_amount")]
        public decimal TransactionAmount { get; set; }

        [JsonPropertyName("installments")]
        public int Installments { get; set; }

        [JsonPropertyName("payer")]
        public PayerDto Payer { get; set; } = new();
    }

    public class PayerDto
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("identification")]
        public IdentificationDto Identification { get; set; } = new();
    }

    public class IdentificationDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("number")]
        public string Number { get; set; } = string.Empty;
    }

    // =========================================================
    // DTO PARA RESPONDER AL FRONTEND
    // =========================================================
    public class RespuestaPagoBrickDto
    {
        public bool Aprobado { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string IdPago { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }
}