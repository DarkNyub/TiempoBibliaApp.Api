using Microsoft.AspNetCore.Mvc;

namespace TiempoBiblia.Api.Features.Paquetes
{
    /// <summary>
    /// Endpoints para administrar y consumir los paquetes de productos.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PaquetesController : ControllerBase
    {
        private readonly PaqueteService _service;

        public PaquetesController(PaqueteService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Paquete>>> GetPublico()
        {
            var paquetes = await _service.ObtenerActivosPublicoAsync();
            return Ok(paquetes);
        }

        [HttpGet("admin")]
        public async Task<ActionResult<IEnumerable<Paquete>>> GetAdmin()
        {
            var paquetes = await _service.ObtenerTodosAdminAsync();
            return Ok(paquetes);
        }

        [HttpPost]
        public async Task<ActionResult<Paquete>> Post(Paquete paquete)
        {
            try
            {
                var nuevoPaquete = await _service.CrearAsync(paquete);
                return CreatedAtAction(nameof(GetAdmin), new { id = nuevoPaquete.Id }, nuevoPaquete);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}