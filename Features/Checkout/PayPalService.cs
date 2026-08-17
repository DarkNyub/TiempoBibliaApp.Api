using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TiempoBiblia.Api.Features.Checkout
{
    public class PayPalService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;

        public PayPalService(IConfiguration config, HttpClient http)
        {
            _config = config;
            _http = http;
        }

        private string GetBaseUrl() => _config["PayPal:Mode"] == "Live" 
            ? "https://api-m.paypal.com" 
            : "https://api-m.sandbox.paypal.com";

        private async Task<string> GetAccessTokenAsync()
        {
            var clientId = _config["PayPal:ClientId"];
            var secret = _config["PayPal:Secret"];
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{secret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, $"{GetBaseUrl()}/v1/oauth2/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
            request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("access_token").GetString()!;
        }

        public async Task<string> CrearPedidoAsync(decimal totalCop, string returnUrl, string cancelUrl)
        {
            var token = await GetAccessTokenAsync();
            
            // 🔥 Tasa de cambio simulada (Puedes ajustar este número)
            decimal tasaCambio = 4000m; 
            decimal totalUsd = Math.Round(totalCop / tasaCambio, 2);
            if (totalUsd <= 0) totalUsd = 1.00m; // PayPal exige un mínimo de $1 USD

            var request = new HttpRequestMessage(HttpMethod.Post, $"{GetBaseUrl()}/v2/checkout/orders");
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
                },
                application_context = new
                {
                    return_url = returnUrl,
                    cancel_url = cancelUrl,
                    user_action = "PAY_NOW"
                }
            };

            request.Content = new StringContent(JsonSerializer.Serialize(order), Encoding.UTF8, "application/json");
            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var links = json.GetProperty("links").EnumerateArray();
            
            // Retorna la URL oficial de PayPal para redirigir al cliente
            return links.FirstOrDefault(l => l.GetProperty("rel").GetString() == "approve").GetProperty("href").GetString()!;
        }

        public async Task<string?> CapturarPedidoAsync(string orderId)
        {
            var token = await GetAccessTokenAsync();
            var request = new HttpRequestMessage(HttpMethod.Post, $"{GetBaseUrl()}/v2/checkout/orders/{orderId}/capture");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent("", Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (json.GetProperty("status").GetString() == "COMPLETED")
                {
                    // Extraemos el ID real de la transacción exitosa de PayPal
                    var captureId = json.GetProperty("purchase_units")[0]
                                        .GetProperty("payments")
                                        .GetProperty("captures")[0]
                                        .GetProperty("id").GetString();
                    return captureId;
                }
            }
            return null;
        }
    }
}