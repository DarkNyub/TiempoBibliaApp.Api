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

        public async Task ActualizarTokenAsync(TokenDescarga token)
        {
            _context.TokensDescarga.Update(token);
            await _context.SaveChangesAsync();
        }
    }
}