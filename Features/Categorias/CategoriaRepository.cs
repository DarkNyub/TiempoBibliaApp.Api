using Microsoft.EntityFrameworkCore;
using TiempoBiblia.Api.Data;

namespace TiempoBiblia.Api.Features.Categorias
{
    /// <summary>
    /// Capa de acceso a datos. Su única responsabilidad es hablar con PostgreSQL.
    /// </summary>
    public class CategoriaRepository
    {
        private readonly AppDbContext _context;

        public CategoriaRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Trae TODAS las categorías (Usado para el panel de administración)
        /// </summary>
        public async Task<List<Categoria>> ObtenerTodasAdminAsync()
        {
            return await _context.Categorias.AsNoTracking().ToListAsync();
        }

        /// <summary>
        /// Trae SOLO las categorías activas (Usado para la web pública tipo Netflix)
        /// </summary>
        public async Task<List<Categoria>> ObtenerActivasPublicoAsync()
        {
            return await _context.Categorias
                .Where(c => c.Activo == true)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Inserta una nueva categoría en la base de datos
        /// </summary>
        public async Task<Categoria> CrearAsync(Categoria categoria)
        {
            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();
            return categoria;
        }
    }
}