namespace TiempoBiblia.Api.Features.Checkout
{
    // =========================================================
    // DTOs PARA RECIBIR EL PAGO DESDE BRICKS (FRONTEND)
    // =========================================================
    public class PagoBrickDto
    {
        public string Token { get; set; } = string.Empty;
        public string PaymentMethodId { get; set; } = string.Empty;
        public decimal TransactionAmount { get; set; }
        public int Installments { get; set; }
        public PayerDto Payer { get; set; } = new();
    }

    public class PayerDto
    {
        public string Email { get; set; } = string.Empty;
        public IdentificationDto Identification { get; set; } = new();
    }

    public class IdentificationDto
    {
        public string Type { get; set; } = string.Empty;
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