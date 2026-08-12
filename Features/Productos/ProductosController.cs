using Microsoft.AspNetCore.Mvc;

namespace TiempoBiblia.Api.Features.Productos
{
    /// <summary>
    /// Puntos de entrada HTTP para la gestión del catálogo de productos.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private readonly ProductoService _service;

        public ProductosController(ProductoService service)
        {
            _service = service;
        }
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Producto>>> GetPublico() => Ok(await _service.ObtenerActivosPublicoAsync());

        [HttpGet("admin")]
        public async Task<ActionResult<IEnumerable<Producto>>> GetAdmin() => Ok(await _service.ObtenerTodosAdminAsync());

        [HttpPost]
        public async Task<ActionResult<Producto>> Post(Producto producto)
        {
            try
            {
                var nuevoProducto = await _service.CrearAsync(producto);
                return CreatedAtAction(nameof(GetById), new { id = nuevoProducto.Id }, nuevoProducto);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        // 🔥 NUEVO: Endpoint para traer un solo producto
        [HttpGet("{id}")]
        public async Task<ActionResult<Producto>> GetById(int id)
        {
            var producto = await _service.ObtenerPorIdAsync(id);
            if (producto == null) return NotFound(new { mensaje = "Producto no encontrado." });
            return Ok(producto);
        }

        // 🔥 NUEVO: Endpoint para modificar (PUT)
        [HttpPut("{id}")]
        public async Task<ActionResult<Producto>> Put(int id, Producto producto)
        {
            try
            {
                var productoActualizado = await _service.ActualizarAsync(id, producto);
                return Ok(productoActualizado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
        }

        // 🔥 NUEVO: Endpoint para eliminar (DELETE)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var eliminado = await _service.EliminarAsync(id);
            if (!eliminado) return NotFound(new { mensaje = "Producto no encontrado o ya fue eliminado." });
            
            return NoContent(); // HTTP 204: Indica que se borró con éxito y no hay contenido que devolver.
        }
    }
}