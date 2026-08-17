using Microsoft.AspNetCore.Mvc;

namespace TiempoBiblia.Api.Features.Pedidos
{
    [Route("api/[controller]")]
    [ApiController]
    public class PedidosController : ControllerBase
    {
        private readonly PedidoRepository _repository;

        public PedidosController(PedidoRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("admin")]
        public async Task<ActionResult<IEnumerable<PedidoAdminDto>>> GetHistorialAdmin()
        {
            var ventas = await _repository.ObtenerHistorialVentasAsync();
            return Ok(ventas);
        }
    }
}