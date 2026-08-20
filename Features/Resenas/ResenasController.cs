using Microsoft.AspNetCore.Mvc;

namespace TiempoBiblia.Api.Features.Resenas
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResenasController : ControllerBase
    {
        private readonly ResenaService _service;

        public ResenasController(ResenaService service)
        {
            _service = service;
        }

        [HttpGet("producto/{productoId}")]
        public async Task<IActionResult> GetResenasPorProducto(int productoId) => 
            Ok(await _service.ObtenerPorProductoAsync(productoId));

        [HttpGet("admin/producto/{productoId}")]
        public async Task<IActionResult> GetResenasAdmin(int productoId) => 
            Ok(await _service.ObtenerTodasAdminAsync(productoId));

        [HttpPut("{id}")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] ResenaDto dto)
        {
            await _service.ActualizarEstadoAsync(id, dto.Aprobada);
            return Ok();
        }

        [HttpGet("aprobar/{id}")]
        public async Task<ContentResult> AprobarDesdeCorreo(int id)
        {
            await _service.ActualizarEstadoAsync(id, true);
            string html = "<div style='text-align:center; padding: 50px; font-family: Arial;'><h1 style='color: #4CAF50;'>¡Reseña Aprobada!</h1><p>Ya es visible en la tienda de Luzy.</p></div>";
            return new ContentResult { ContentType = "text/html", StatusCode = 200, Content = html };
        }

        [HttpPost]
        public async Task<IActionResult> CrearResena([FromBody] CrearResenaDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _service.CrearResenaAsync(dto);
            return Ok(new { mensaje = "¡Reseña enviada y en espera de moderación!" });
        }

        [HttpPost("global")]
        public async Task<IActionResult> CrearResenaGlobal([FromBody] CrearResenaGlobalDto dto)
        {
            if (dto.Calificacion < 1 || dto.Calificacion > 5)
                return BadRequest(new { mensaje = "Calificación inválida." });

            await _service.CrearResenaGlobalAsync(dto);
            return Ok(new { mensaje = "¡Tus reseñas han sido publicadas con éxito!" });
        }
    }
}