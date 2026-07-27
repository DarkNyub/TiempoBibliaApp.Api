using Microsoft.AspNetCore.Mvc;

namespace TiempoBiblia.Api.Features.Descargas
{
    [Route("api/[controller]")]
    [ApiController]
    public class DescargasController : ControllerBase
    {
        private readonly DescargaService _service;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public DescargasController(
            DescargaService service, 
            HttpClient httpClient, 
            IConfiguration configuration)
        {
            _service = service;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        // ============================================================
        // 1. ENDPOINT ADMINISTRADOR
        // ============================================================

        /// <summary>
        /// POST: api/descargas/generar
        /// Genera el enlace que apunta al Frontend en el nuevo dominio.
        /// </summary>
        [HttpPost("generar")]
        public async Task<IActionResult> GenerarLink([FromBody] GenerarLinkRequest request)
        {
            var token = await _service.GenerarLinkDescargaAsync(request.ProductoId, request.CorreoCliente);
            
            // 🔥 LEEMOS LA URL DESDE APPSETTINGS CON UN FALLBACK SEGURO
            var baseUrl = _configuration["FrontendSettings:BaseUrl"]?.TrimEnd('/') 
                          ?? "https://tiempobiblia-luzy.online";

            var urlDescarga = $"{baseUrl}/descargar/{token.Id}";
            
            return Ok(new { UrlSegura = urlDescarga, ExpiraEn = token.FechaExpiracion });
        }

        // ============================================================
        // 2. ENDPOINTS CLIENTE (PASO A PASO PARA BLAZOR)
        // ============================================================

        /// <summary>
        /// GET: api/descargas/validar/{tokenId}
        /// Paso 1: Valida el token y devuelve la URL directa de Google Drive.
        /// </summary>
        [HttpGet("validar/{tokenId:guid}")]
        public async Task<IActionResult> ValidarToken(Guid tokenId)
        {
            var token = await _service.ObtenerDatosArchivoAsync(tokenId);

            if (token == null || string.IsNullOrEmpty(token.Producto?.PdfUrl))
            {
                return BadRequest(new { valido = false, mensaje = "El link no existe, caducó o ya fue usado." });
            }

            // Retornamos el estado válido y la URL directa de Google Drive
            return Ok(new { 
                valido = true, 
                urlDirecta = token.Producto.PdfUrl 
            });
        }

        /// <summary>
        /// GET: api/descargas/obtener-archivo/{tokenId}
        /// Paso 2 (AJAX): Actúa como Proxy descargando los bytes de Google Drive en memoria.
        /// </summary>
        [HttpGet("obtener-archivo/{tokenId:guid}")]
        public async Task<IActionResult> ObtenerArchivo(Guid tokenId)
        {
            var token = await _service.ObtenerDatosArchivoAsync(tokenId);

            if (token == null || string.IsNullOrEmpty(token.Producto.PdfUrl))
            {
                return BadRequest("El archivo no está disponible o el token expiró.");
            }

            try
            {
                // Disfrazamos la petición para que Google Drive responda limpiamente
                var requestDrive = new HttpRequestMessage(HttpMethod.Get, token.Producto.PdfUrl);
                requestDrive.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                var responseDrive = await _httpClient.SendAsync(requestDrive);
                responseDrive.EnsureSuccessStatusCode();

                var bytes = await responseDrive.Content.ReadAsByteArrayAsync();

                // Retornamos el arreglo de bytes para que Blazor lo convierta en Blob
                return File(bytes, "application/pdf", $"{token.Producto.Nombre}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al procesar el archivo desde Drive: {ex.Message}");
            }
        }

        /// <summary>
        /// POST: api/descargas/marcar-usado/{tokenId}
        /// Paso 3 (AJAX): Invalida un uso en la Base de Datos una vez completado el flujo.
        /// </summary>
        [HttpPost("marcar-usado/{tokenId:guid}")]
        public async Task<IActionResult> MarcarUsado(Guid tokenId)
        {
            var exito = await _service.ConsumirTokenAsync(tokenId);

            if (!exito)
            {
                return BadRequest(new { mensaje = "No se pudo actualizar el token." });
            }

            return Ok(new { mensaje = "Descarga registrada con éxito." });
        }
    }

    // ============================================================
    // DTOs Y MODELOS DE SOLICITUD
    // ============================================================

    public class GenerarLinkRequest
    {
        public int ProductoId { get; set; }
        public string CorreoCliente { get; set; } = string.Empty;
    }
}