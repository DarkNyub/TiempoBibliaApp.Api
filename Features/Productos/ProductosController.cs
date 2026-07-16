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
        public async Task<ActionResult<IEnumerable<Producto>>> GetPublico()
        {
            var productos = await _service.ObtenerActivosPublicoAsync();
            return Ok(productos);
        }

        [HttpGet("admin")]
        public async Task<ActionResult<IEnumerable<Producto>>> GetAdmin()
        {
            var productos = await _service.ObtenerTodosAdminAsync();
            return Ok(productos);
        }

        [HttpPost]
        public async Task<ActionResult<Producto>> Post(Producto producto)
        {
            try
            {
                var nuevoProducto = await _service.CrearAsync(producto);
                return CreatedAtAction(nameof(GetAdmin), new { id = nuevoProducto.Id }, nuevoProducto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}