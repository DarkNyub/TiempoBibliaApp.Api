using Microsoft.AspNetCore.Mvc;

namespace TiempoBiblia.Api.Features.Categorias
{
    /// <summary>
    /// Puntos de entrada HTTP para la entidad Categoría.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriasController : ControllerBase
    {
        private readonly CategoriaService _service;

        public CategoriasController(CategoriaService service)
        {
            _service = service;
        }

        /// <summary>
        /// GET: api/categorias
        /// Devuelve solo las categorías activas (Para el Frontend del cliente)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Categoria>>> GetPublico()
        {
            var categorias = await _service.ObtenerActivasPublicoAsync();
            return Ok(categorias);
        }

        /// <summary>
        /// GET: api/categorias/admin
        /// Devuelve absolutamente todas las categorías (Para el Frontend del administrador)
        /// </summary>
        [HttpGet("admin")]
        public async Task<ActionResult<IEnumerable<Categoria>>> GetAdmin()
        {
            var categorias = await _service.ObtenerTodasAdminAsync();
            return Ok(categorias);
        }

        /// <summary>
        /// POST: api/categorias
        /// Crea una nueva categoría
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Categoria>> Post(Categoria categoria)
        {
            try
            {
                var nuevaCategoria = await _service.CrearAsync(categoria);
                return CreatedAtAction(nameof(GetAdmin), new { id = nuevaCategoria.Id }, nuevaCategoria);
            }
            catch (ArgumentException ex)
            {
                // Si la regla de negocio falla (ej. nombre vacío), devuelve un Error 400
                return BadRequest(ex.Message);
            }
        }
    }
}