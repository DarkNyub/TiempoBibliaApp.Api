using Microsoft.AspNetCore.Mvc;

namespace TiempoBiblia.Api.Features.Upload
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly ImagenService _imagenService;

        public UploadController(ImagenService imagenService)
        {
            _imagenService = imagenService;
        }

        /// <summary>
        /// Endpoint que recibe el archivo multipart/form-data y retorna la URL pública.
        /// </summary>
        [HttpPost("imagen")]
        public async Task<IActionResult> SubirImagen(IFormFile archivo)
        {
            try
            {
                // Solo permitimos imágenes por seguridad
                var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

                if (!extensionesPermitidas.Contains(extension))
                {
                    return BadRequest(new { mensaje = "El archivo debe ser una imagen válida (.jpg, .png, .webp)." });
                }

                // Subimos la imagen usando el servicio
                var urlPublica = await _imagenService.SubirImagenAGitHubAsync(archivo);

                // Devolvemos la URL para que el Frontend la asigne a la caja de texto
                return Ok(new { Url = urlPublica });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error interno: {ex.Message}" });
            }
        }
    }
}