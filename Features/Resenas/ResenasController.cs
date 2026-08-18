using Microsoft.AspNetCore.Mvc;

namespace TiempoBiblia.Api.Features.Resenas
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResenasController : ControllerBase
    {
        private readonly ResenaRepository _repository;

        public ResenasController(ResenaRepository repository)
        {
            _repository = repository;
        }

        // GET: api/resenas/producto/5
        [HttpGet("producto/{productoId}")]
        public async Task<IActionResult> GetResenasPorProducto(int productoId)
        {
            var resenas = await _repository.ObtenerPorProductoAsync(productoId);
            return Ok(resenas);
        }

        // POST: api/resenas
        [HttpPost]
        public async Task<IActionResult> CrearResena([FromBody] CrearResenaDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var nuevaResena = new Resena
            {
                ProductoId = dto.ProductoId,
                NombreCliente = dto.NombreCliente,
                Calificacion = dto.Calificacion,
                Comentario = dto.Comentario,
                FechaCreacion = DateTime.UtcNow,
                Aprobada = true 
            };

            await _repository.GuardarAsync(nuevaResena);
            
            return Ok(new { mensaje = "¡Gracias por tu reseña! Ayudará a muchas personas." });
        }
    }
}