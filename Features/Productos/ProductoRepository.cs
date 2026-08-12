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
        /// Trae TODOS los productos para el panel de administración (carga ligera).
        /// </summary>
        public async Task<List<Producto>> ObtenerTodosAdminAsync()
        {
            return await _context.Productos
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
                .Include(p => p.Categoria) // Para mostrar "Categoría: Cursos"
                .Include(p => p.ProductoTags).ThenInclude(pt => pt.Tag) // Para mostrar los Tags
                .Include(p => p.ProductosRelacionadosOrigen)
                    .ThenInclude(pr => pr.ProductoRelacionadoDestino) // ¡El Cross-Selling!
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Producto> CrearAsync(Producto producto)
        {
            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();
            return producto;
        }

        // 🔥 NUEVO: Buscar un producto por su ID
        public async Task<Producto?> ObtenerPorIdAsync(int id)
        {
            return await _context.Productos.FindAsync(id);
        }

        // 🔥 NUEVO: Actualizar un producto existente
        public async Task<Producto> ActualizarAsync(Producto producto)
        {
            _context.Productos.Update(producto);
            await _context.SaveChangesAsync();
            return producto;
        }

        // 🔥 NUEVO: Eliminar físicamente un producto de la base de datos
        public async Task<bool> EliminarAsync(int id)
        {
            var producto = await ObtenerPorIdAsync(id);
            if (producto == null) return false;

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}