using Microsoft.EntityFrameworkCore;
using TiempoBiblia.Api.Data;

namespace TiempoBiblia.Api.Features.Pedidos
{
    public class PedidoRepository
    {
        private readonly AppDbContext _context;

        public PedidoRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todo el historial de ventas ordenado desde el más reciente.
        /// </summary>
        public async Task<List<PedidoAdminDto>> ObtenerHistorialVentasAsync()
        {
            return await _context.Pedidos
                .Include(p => p.Detalles)
                .OrderByDescending(p => p.FechaCreacion)
                .Select(p => new PedidoAdminDto
                {
                    Id = p.Id,
                    TransaccionGatewayId = p.TransaccionGatewayId,
                    Pasarela = p.Pasarela,
                    Estado = p.Estado,
                    TotalCobrado = p.TotalCobrado,
                    Moneda = p.Moneda,
                    CorreoCliente = p.CorreoCliente,
                    FechaCreacion = p.FechaCreacion,
                    CantidadProductos = p.Detalles.Count
                })
                .AsNoTracking()
                .ToListAsync();
        }
    }
}