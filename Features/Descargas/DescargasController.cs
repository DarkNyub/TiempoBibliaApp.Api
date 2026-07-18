using Microsoft.AspNetCore.Mvc;

namespace TiempoBiblia.Api.Features.Descargas
{
    [Route("api/[controller]")]
    [ApiController]
    public class DescargasController : ControllerBase
    {
        private readonly DescargaService _service;
        private readonly HttpClient _httpClient; // Usado para actuar de puente (Proxy) con Drive

        public DescargasController(DescargaService service, HttpClient httpClient)
        {
            _service = service;
            _httpClient = httpClient;
        }

        /// <summary>
        /// POST: api/descargas/generar
        /// (Solo para el Admin) Crea un link de 24 horas para un cliente.
        /// </summary>
        [HttpPost("generar")]
        public async Task<IActionResult> GenerarLink([FromBody] GenerarLinkRequest request)
        {
            var token = await _service.GenerarLinkDescargaAsync(request.ProductoId, request.CorreoCliente);
            
            // Retornamos la URL que le vas a enviar por WhatsApp
            var urlDescarga = $"{Request.Scheme}://{Request.Host}/api/descargas/{token.Id}";
            
            return Ok(new { UrlSegura = urlDescarga, ExpiraEn = token.FechaExpiracion });
        }

        /// <summary>
        /// GET: api/descargas/{tokenId}
        /// (Para el Cliente) Valida el token y descarga el PDF desde Drive por detrás.
        /// </summary>
        [HttpGet("{tokenId}")]
        public async Task<IActionResult> DescargarArchivo(Guid tokenId)
        {
            var token = await _service.ValidarYConsumirTokenAsync(tokenId);

            if (token == null)
            {
                return BadRequest("El link de descarga no existe, ha caducado, o ya superó el límite de descargas.");
            }

            if (string.IsNullOrEmpty(token.Producto.PdfUrl))
            {
                return BadRequest("Este producto no tiene un archivo configurado.");
            }

            try
            {
                // 1. Creamos la petición manualmente
                var requestDrive = new HttpRequestMessage(HttpMethod.Get, token.Producto.PdfUrl);
                
                // 2. EL TRUCO DE MAGIA: Le decimos a Google que somos un navegador real (Chrome en Windows)
                requestDrive.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                // 3. Enviamos la petición disfrazada
                var responseDrive = await _httpClient.SendAsync(requestDrive);
                responseDrive.EnsureSuccessStatusCode();

                // 4. Extraemos el archivo real
                var streamArchivo = await responseDrive.Content.ReadAsStreamAsync();

                // 5. Se lo entregamos al cliente con su extensión correcta
                return File(streamArchivo, "application/pdf", $"{token.Producto.Nombre}.pdf");
            }
            catch (Exception ex)
            {
                // Ahora si falla, te dirá exactamente por qué falló
                return StatusCode(500, $"Error al descargar desde Drive: {ex.Message}");
            }
        }
    }

    // DTO auxiliar para recibir los datos del admin
    public class GenerarLinkRequest
    {
        public int ProductoId { get; set; }
        public string CorreoCliente { get; set; } = string.Empty;
    }
}