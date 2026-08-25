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
        public async Task ActualizarCorreoPedidoYTokensAsync(int pedidoId, string correoAntiguo, string correoNuevo, List<int> productosIds)
        {
            // 1. Actualizamos el pedido
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            if (pedido != null) pedido.CorreoCliente = correoNuevo;

            // 2. Actualizamos los tokens mágicos para que el nuevo correo tenga permiso
            var tokens = await _context.TokensDescarga
                .Where(t => t.CorreoCliente == correoAntiguo && productosIds.Contains(t.ProductoId))
                .ToListAsync();
            
            foreach (var t in tokens) t.CorreoCliente = correoNuevo;

            await _context.SaveChangesAsync();
        }
        /// <summary>
        /// Valida que los productos de tipo 'presencial' no excedan su límite de cupos.
        /// HACK MÁESTRO: Utiliza la propiedad 'PrecioUsd' como el límite máximo de asistentes.
        /// </summary>
        public async Task<string?> ValidarDisponibilidadCuposAsync(List<int> productosIds)
        {
            // 1. Obtenemos los IDs y cuántas veces intentan comprar el mismo taller en esta transacción
            var carritoAgrupado = productosIds.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());

            foreach (var item in carritoAgrupado)
            {
                int productoId = item.Key;
                int cantidadDeseada = item.Value;

                // 2. Buscamos el producto
                var producto = await _context.Productos.FindAsync(productoId);

                // 3. Si no existe o NO es presencial, lo ignoramos (pase libre)
                if (producto == null || !producto.Tipo.Equals("presencial", StringComparison.OrdinalIgnoreCase))
                    continue;

                // 4. Si es presencial, el PrecioUsd es nuestro límite de cupos
                int limiteCupos = (int)producto.PrecioUsd;

                // 5. Contamos cuántas veces se ha vendido en la tabla de Detalles de Pedido
                int ventasActuales = await _context.PedidoDetalles.CountAsync(d => d.ProductoId == productoId);

                // 6. Validamos si hay espacio suficiente
                if (ventasActuales + cantidadDeseada > limiteCupos)
                {
                    int cuposDisponibles = Math.Max(0, limiteCupos - ventasActuales);
                    return $"El '{producto.Nombre}' está agotado.";
                }
            }

            // Si sobrevive al ciclo, hay cupos para todo.
            return null; 
        }
    }
}