using Microsoft.AspNetCore.Mvc;

namespace TiempoBiblia.Api.Features.Pedidos
{
    [Route("api/[controller]")]
    [ApiController]
    public class PedidosController : ControllerBase
    {
        private readonly PedidoRepository _repository;
        private readonly PedidoService _pedidoService; // 🔥 Inyectamos el nuevo servicio

        public PedidosController(PedidoRepository repository, PedidoService pedidoService)
        {
            _repository = repository;
            _pedidoService = pedidoService;
        }

        [HttpGet("admin")]
        public async Task<ActionResult<IEnumerable<PedidoAdminDto>>> GetHistorialAdmin()
        {
            var ventas = await _repository.ObtenerHistorialVentasAsync();
            return Ok(ventas);
        }

        // 🔥 NUEVO: Endpoint para reenviar el correo
        [HttpPost("{id}/reenviar-correo")]
        public async Task<IActionResult> ReenviarCorreoCompra(int id)
        {
            try
            {
                await _pedidoService.ReenviarCorreoPedidoAsync(id);
                return Ok(new { mensaje = "Correo reenviado exitosamente al cliente." });
            }
            catch (Exception ex)
            {
                // Si el servicio lanza una excepción (ej. "Pedido no encontrado"), la atrapamos aquí
                return BadRequest(new { mensaje = "Error al reenviar el correo.", detalle = ex.Message });
            }
        }
    }
}