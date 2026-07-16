using Microsoft.EntityFrameworkCore;
using TiempoBiblia.Api.Data;

namespace TiempoBiblia.Api.Features.Tags
{
    /// <summary>
    /// Capa de acceso a datos para las Etiquetas (Tags).
    /// </summary>
    public class TagRepository
    {
        private readonly AppDbContext _context;

        public TagRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Trae TODOS los tags (Para el panel de administración).
        /// </summary>
        public async Task<List<Tag>> ObtenerTodosAdminAsync()
        {
            return await _context.Tags
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Trae SOLO los tags activos (Para los filtros de búsqueda en el Frontend público).
        /// </summary>
        public async Task<List<Tag>> ObtenerActivosPublicoAsync()
        {
            return await _context.Tags
                .Where(t => t.Activo == true)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Tag> CrearAsync(Tag tag)
        {
            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();
            return tag;
        }
    }
}