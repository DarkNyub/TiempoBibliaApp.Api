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
                // Descargamos de Drive a la memoria de nuestro servidor
                var streamArchivo = await _httpClient.GetStreamAsync(token.Producto.PdfUrl);

                // Y se lo pasamos al cliente (el cliente NUNCA ve la URL de Drive)
                return File(streamArchivo, "application/pdf", $"{token.Producto.Nombre}.pdf");
            }
            catch (Exception)
            {
                return StatusCode(500, "Hubo un error al obtener el archivo desde el servidor de almacenamiento.");
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