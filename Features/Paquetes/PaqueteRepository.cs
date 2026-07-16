using Microsoft.EntityFrameworkCore;
using TiempoBiblia.Api.Data;

namespace TiempoBiblia.Api.Features.Paquetes
{
    /// <summary>
    /// Capa de acceso a datos exclusiva para los Paquetes (Bundles).
    /// </summary>
    public class PaqueteRepository
    {
        private readonly AppDbContext _context;

        public PaqueteRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Trae TODOS los paquetes para el panel de administración
        /// </summary>
        public async Task<List<Paquete>> ObtenerTodosAdminAsync()
        {
            return await _context.Paquetes
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Trae SOLO los paquetes activos para la tienda, incluyendo los IDs de los productos que contiene
        /// </summary>
        public async Task<List<Paquete>> ObtenerActivosPublicoAsync()
        {
            return await _context.Paquetes
                .Where(p => p.Activo == true)
                .Include(p => p.PaqueteProductos) // Traemos la relación para que el front sepa qué incluye
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Paquete> CrearAsync(Paquete paquete)
        {
            _context.Paquetes.Add(paquete);
            await _context.SaveChangesAsync();
            return paquete;
        }
    }
}