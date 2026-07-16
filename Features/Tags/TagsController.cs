using Microsoft.AspNetCore.Mvc;

namespace TiempoBiblia.Api.Features.Tags
{
    /// <summary>
    /// Puntos de entrada HTTP para administrar las Etiquetas.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TagsController : ControllerBase
    {
        private readonly TagService _service;

        public TagsController(TagService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tag>>> GetPublico()
        {
            var tags = await _service.ObtenerActivosPublicoAsync();
            return Ok(tags);
        }

        [HttpGet("admin")]
        public async Task<ActionResult<IEnumerable<Tag>>> GetAdmin()
        {
            var tags = await _service.ObtenerTodosAdminAsync();
            return Ok(tags);
        }

        [HttpPost]
        public async Task<ActionResult<Tag>> Post(Tag tag)
        {
            try
            {
                var nuevoTag = await _service.CrearAsync(tag);
                return CreatedAtAction(nameof(GetAdmin), new { id = nuevoTag.Id }, nuevoTag);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}