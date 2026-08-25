using Microsoft.EntityFrameworkCore;
using TiempoBiblia.Api.Data;
using TiempoBiblia.Api.Features.Descargas;
using TiempoBiblia.Api.shared;

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
        /// Obtiene todo el historial de ventas con sus detalles anidados (para el acordeón).
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
                    Franquicia = p.Franquicia,
                    Ultimos4Digitos = p.Ultimos4Digitos,
                    // 🔥 Mapeamos los detalles internos para que viajen en el mismo paquete
                    Detalles = p.Detalles.Select(d => new PedidoDetalleAdminDto
                    {
                        ProductoId = d.ProductoId,
                        NombreProductoHistorico = d.NombreProductoHistorico,
                        PrecioUnitarioPagado = d.PrecioUnitarioPagado
                    }).ToList()
                })
                .AsNoTracking()
                .ToListAsync();
        }
        /// <summary>
        /// 🔥 NUEVO: Busca un pedido específico con sus detalles por su ID interno.
        /// </summary>
        public async Task<Pedido?> ObtenerPedidoConDetallesPorIdAsync(int pedidoId)
        {
            return await _context.Pedidos
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);
        }

        /// <summary>
        /// 🔥 NUEVO: Busca los tokens de descarga generados para un correo y unos productos específicos.
        /// </summary>
        public async Task<List<TokenDescarga>> ObtenerTokensParaReenvioAsync(string correo, List<int> productosIds)
        {
            return await _context.TokensDescarga
                .Include(t => t.Producto)
                .Where(t => t.CorreoCliente == correo && productosIds.Contains(t.ProductoId))
                .ToListAsync();
        }
    }
}