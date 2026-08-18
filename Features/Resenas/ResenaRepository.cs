using Microsoft.EntityFrameworkCore;
using TiempoBiblia.Api.Data;

namespace TiempoBiblia.Api.Features.Resenas
{
    public class ResenaRepository
    {
        private readonly AppDbContext _context;

        public ResenaRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todas las reseñas aprobadas de un producto específico, de la más nueva a la más vieja.
        /// </summary>
        public async Task<List<ResenaDto>> ObtenerPorProductoAsync(int productoId)
        {
            return await _context.Resenas
                .Where(r => r.ProductoId == productoId && r.Aprobada)
                .OrderByDescending(r => r.FechaCreacion)
                .Select(r => new ResenaDto
                {
                    Id = r.Id,
                    NombreCliente = r.NombreCliente,
                    Calificacion = r.Calificacion,
                    Comentario = r.Comentario,
                    FechaCreacion = r.FechaCreacion
                })
                .AsNoTracking() // Optimización para lectura rápida
                .ToListAsync();
        }

        /// <summary>
        /// Guarda una nueva reseña en la base de datos.
        /// </summary>
        public async Task GuardarAsync(Resena resena)
        {
            _context.Resenas.Add(resena);
            await _context.SaveChangesAsync();
        }
    }
}