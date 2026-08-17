using Microsoft.EntityFrameworkCore;
using TiempoBiblia.Api.Data;
using TiempoBiblia.Api.shared;

namespace TiempoBiblia.Api.Features.Descargas
{
    public class DescargaRepository
    {
        private readonly AppDbContext _context;

        public DescargaRepository(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // 1. MÉTODOS DE CREACIÓN Y LECTURA
        // ============================================================

        public async Task<TokenDescarga> CrearTokenAsync(TokenDescarga token)
        {
            _context.TokensDescarga.Add(token);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<TokenDescarga?> ObtenerTokenConProductoAsync(Guid tokenId)
        {
            return await _context.TokensDescarga
                .Include(t => t.Producto)
                .FirstOrDefaultAsync(t => t.Id == tokenId);
        }

        // ============================================================
        // 2. MÉTODOS DE ESCRITURA Y ACTUALIZACIÓN
        // ============================================================

        public async Task ActualizarTokenAsync(TokenDescarga token)
        {
            _context.TokensDescarga.Update(token);
            await _context.SaveChangesAsync();
        }
        // ============================================================
        // 3. MÉTODOS DE PEDIDOS Y CONTABILIDAD (NÚCLEO BLINDADO)
        // ============================================================
        public async Task<List<TokenDescarga>> GuardarPedidoYTokensAsync(
            string correo, 
            string pagoId, 
            string pasarela, 
            string? franquicia, 
            string? ultimos4Digitos, 
            List<int> productosIds)
        {
            // 1. Buscamos los productos originales para obtener sus precios y nombres actuales
            var productos = await _context.Productos
                .Where(p => productosIds.Contains(p.Id))
                .ToListAsync();

            if (!productos.Any()) throw new Exception("No se encontraron productos para facturar.");

            // 2. Creamos la "Factura" (El Pedido Maestro) con toda la auditoría
            var pedido = new TiempoBiblia.Api.Features.Pedidos.Pedido
            {
                TransaccionGatewayId = pagoId,
                Pasarela = pasarela, 
                Estado = "approved",
                TotalCobrado = productos.Sum(p => p.Precio),
                Moneda = "COP",
                CorreoCliente = correo,
                Franquicia = franquicia,
                Ultimos4Digitos = ultimos4Digitos,
                FechaCreacion = DateTime.UtcNow,
                
                // Agregamos las líneas de la factura inmutables
                Detalles = productos.Select(p => new TiempoBiblia.Api.Features.Pedidos.PedidoDetalle
                {
                    ProductoId = p.Id,
                    NombreProductoHistorico = p.Nombre,
                    PrecioUnitarioPagado = p.Precio
                }).ToList()
            };

            _context.Pedidos.Add(pedido);

            // 3. Generamos los tokens mágicos de descarga
            var tokens = new List<TokenDescarga>();
            foreach (var prod in productos)
            {
                var token = new TokenDescarga
                {
                    ProductoId = prod.Id,
                    CorreoCliente = correo,
                    FechaCreacion = DateTime.UtcNow,
                    FechaExpiracion = DateTime.UtcNow.AddDays(7), // 7 días de acceso
                    DescargasRealizadas = 0,
                    LimiteDescargas = 3 // 3 intentos permitidos
                };
                
                _context.TokensDescarga.Add(token);
                tokens.Add(token);
            }

            // 4. Guardamos TODO en una sola transacción SQL
            // Si hay un microcorte aquí, la base de datos hace Rollback automático.
            await _context.SaveChangesAsync();

            return tokens;
        }
    }
}