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
                    Aprobada = r.Aprobada,
                    FechaCreacion = r.FechaCreacion
                })
                .AsNoTracking() // Optimización para lectura rápida
                .ToListAsync();
        }
        // 🔥 NUEVO: Para el panel de control (Muestra TODAS, incluidas las inactivas)
        public async Task<List<ResenaDto>> ObtenerTodasAdminAsync(int productoId)
        {
            return await _context.Resenas
                .Where(r => r.ProductoId == productoId)
                .OrderByDescending(r => r.FechaCreacion)
                .Select(r => new ResenaDto { Id = r.Id, NombreCliente = r.NombreCliente, Calificacion = r.Calificacion, Comentario = r.Comentario, FechaCreacion = r.FechaCreacion, Aprobada = r.Aprobada })
                .AsNoTracking().ToListAsync();
        }
        // 🔥 NUEVO: Para prender/apagar desde el Panel o el Correo
        public async Task ActualizarEstadoAsync(int id, bool estado)
        {
            var resena = await _context.Resenas.FindAsync(id);
            if (resena != null)
            {
                resena.Aprobada = estado;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Guarda una nueva reseña en la base de datos.
        /// </summary>
        public async Task GuardarAsync(Resena resena)
        {
            _context.Resenas.Add(resena);
            await _context.SaveChangesAsync();
        }
        /// <summary>
        /// Toma una reseña general de un pedido y la clona para cada producto comprado.
        /// </summary>
        public async Task MultiplicarResenaGlobalAsync(int pedidoId, string nombreCliente, int calificacion, string comentario)
        {
            // Buscamos el pedido con sus productos
            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null || !pedido.Detalles.Any()) return;

            // Creamos una reseña por cada producto
            foreach (var detalle in pedido.Detalles)
            {
                var resena = new Resena
                {
                    ProductoId = detalle.ProductoId,
                    NombreCliente = string.IsNullOrWhiteSpace(nombreCliente) ? "Anónimo" : nombreCliente, // Mantienes el anonimato como pediste
                    Calificacion = calificacion,
                    Comentario = string.IsNullOrWhiteSpace(comentario) ? "¡Excelente recurso!" : comentario,
                    FechaCreacion = DateTime.UtcNow,
                    Aprobada = false
                };
                
                _context.Resenas.Add(resena);
            }

            await _context.SaveChangesAsync();
        }
    }
}