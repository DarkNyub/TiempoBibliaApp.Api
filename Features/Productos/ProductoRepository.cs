using Microsoft.EntityFrameworkCore;
using TiempoBiblia.Api.Data;

namespace TiempoBiblia.Api.Features.Productos
{
    /// <summary>
    /// Capa de acceso a datos para Productos.
    /// </summary>
    public class ProductoRepository
    {
        private readonly AppDbContext _context;

        public ProductoRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Trae TODOS los productos para el panel de administración INCLUYENDO sus relaciones.
        /// </summary>
        public async Task<List<Producto>> ObtenerTodosAdminAsync()
        {
            return await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.CategoriasSecundarias)
                .Include(p => p.ProductoTags)
                .Include(p => p.ProductosRelacionadosOrigen)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Trae los productos activos e INCLUYE sus relaciones principales para la tienda web.
        /// </summary>
        public async Task<List<Producto>> ObtenerActivosPublicoAsync()
        {
            return await _context.Productos
                .Where(p => p.Activo == true)
                .Include(p => p.Categoria)
                .Include(p => p.ProductoTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.ProductosRelacionadosOrigen)
                    .ThenInclude(pr => pr.ProductoRelacionadoDestino)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Producto> CrearAsync(Producto producto)
        {
            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();
            return producto;
        }

        public async Task<Producto?> ObtenerPorIdAsync(int id)
        {
            return await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.CategoriasSecundarias)
                .Include(p => p.ProductoTags)
                .Include(p => p.ProductosRelacionadosOrigen)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Producto?> ObtenerParaEdicionAsync(int id)
        {
            return await _context.Productos
                .Include(p => p.CategoriasSecundarias)
                .Include(p => p.ProductoTags)
                .Include(p => p.ProductosRelacionadosOrigen)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Producto> ActualizarAsync(Producto producto)
        {
            _context.Productos.Update(producto);
            await _context.SaveChangesAsync();
            return producto;
        }

        public async Task<bool> EliminarAsync(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return false;

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}