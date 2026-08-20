using Microsoft.EntityFrameworkCore;
using TiempoBiblia.Api.Data;
using TiempoBiblia.Api.Features.Categorias;

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
        /// Trae TODOS los productos mapeados directamente a DTOs con la matemática ya calculada.
        /// </summary>
        public async Task<List<ProductoDto>> ObtenerTodosAdminAsync()
        {
            return await _context.Productos
                .AsNoTracking()
                .Select(p => new ProductoDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    Precio = p.Precio,
                    PrecioUsd = p.PrecioUsd,
                    Descuento = p.Descuento,
                    EsGratuito = p.EsGratuito,
                    ImagenUrl = p.ImagenUrl,
                    Tipo = p.Tipo,
                    PdfUrl = p.PdfUrl,
                    VideoUrl = p.VideoUrl,
                    Activo = p.Activo,
                    CategoriaId = p.CategoriaId,
                    // 🔥 PUNTO 1: Mapeo manual del objeto Categoría (Entity Framework hace el JOIN automático)
                    Categoria = new CategoriaDto { Id = p.Categoria.Id, Nombre = p.Categoria.Nombre },
                    // 🔥 LA MATEMÁTICA EN SQL: Promedia y redondea hacia arriba, o envía 5 si no hay reseñas
                    PromedioCalificacion = p.Resenas.Any(r => r.Aprobada) 
                        ? (int)Math.Ceiling(p.Resenas.Where(r => r.Aprobada).Average(r => r.Calificacion)) 
                        : 5,
                    TotalResenas = p.Resenas.Count(r => r.Aprobada),
                    // Cargamos las imágenes secundarias (las otras relaciones las omites si no se usan en la tabla de admin)
                    ImagenesSecundarias = p.ImagenesSecundarias.Select(img => new ImagenProductoDto { Url = img.Url }).ToList()
                })
                .ToListAsync();
        }

        /// <summary>
        /// Trae los productos activos para la tienda web, calculando el Social Proof.
        /// </summary>
        public async Task<List<ProductoDto>> ObtenerActivosPublicoAsync()
        {
            return await _context.Productos
                .Where(p => p.Activo == true)
                .AsNoTracking()
                .Select(p => new ProductoDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    Precio = p.Precio,
                    PrecioUsd = p.PrecioUsd,
                    Descuento = p.Descuento,
                    EsGratuito = p.EsGratuito,
                    ImagenUrl = p.ImagenUrl,
                    Tipo = p.Tipo,
                    PdfUrl = p.PdfUrl,
                    VideoUrl = p.VideoUrl,
                    Activo = p.Activo,
                    CategoriaId = p.CategoriaId,
                    // 🔥 PUNTO 1: Mapeo manual del objeto Categoría (Entity Framework hace el JOIN automático)
                    Categoria = new CategoriaDto { Id = p.Categoria.Id, Nombre = p.Categoria.Nombre },
                    // 🔥 LA MATEMÁTICA EN SQL
                    PromedioCalificacion = p.Resenas.Any(r => r.Aprobada) 
                        ? (int)Math.Ceiling(p.Resenas.Where(r => r.Aprobada).Average(r => r.Calificacion)) 
                        : 5,
                    TotalResenas = p.Resenas.Count(r => r.Aprobada),
                    // Aquí llenamos las relaciones necesarias para el Home/Detalle
                    CategoriasSecundariasIds = p.CategoriasSecundarias.Select(cs => cs.CategoriaId).ToList(),
                    TagsIds = p.ProductoTags.Select(pt => pt.TagId).ToList(),
                    ProductosRelacionadosIds = p.ProductosRelacionadosOrigen.Select(pr => pr.ProductoRelacionadoId).ToList(),
                    ImagenesSecundarias = p.ImagenesSecundarias.Select(img => new ImagenProductoDto { Url = img.Url }).ToList(),
                    ProductoTags = p.ProductoTags.Select(pt => new ProductoTagDto
                    {
                        TagId = pt.TagId,
                        Tag = new TagDto { Id = pt.Tag.Id, Nombre = pt.Tag.Nombre }
                    }).ToList(),
                    ProductosRelacionadosOrigen = p.ProductosRelacionadosOrigen
                        .Where(pr => pr.ProductoRelacionadoDestino.Activo) // Que no sugiera productos apagados
                        .Select(pr => new ProductoRelacionadoDto
                        {
                            ProductoRelacionadoId = pr.ProductoRelacionadoId,
                            ProductoRelacionadoDestino = new ProductoDto
                            {
                                Id = pr.ProductoRelacionadoDestino.Id,
                                Nombre = pr.ProductoRelacionadoDestino.Nombre,
                                ImagenUrl = pr.ProductoRelacionadoDestino.ImagenUrl,
                                Precio = pr.ProductoRelacionadoDestino.Precio,
                                EsGratuito = pr.ProductoRelacionadoDestino.EsGratuito,
                                PromedioCalificacion = p.Resenas.Any(r => r.Aprobada) 
                                    ? (int)Math.Ceiling(p.Resenas.Where(r => r.Aprobada).Average(r => r.Calificacion)) 
                                    : 5
                            }
                        }).ToList()
                })
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
                    .ThenInclude(pr => pr.ProductoRelacionadoDestino)
                .Include(p => p.ImagenesSecundarias)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Producto?> ObtenerParaEdicionAsync(int id)
        {
            return await _context.Productos
                .Include(p => p.CategoriasSecundarias)
                .Include(p => p.ProductoTags)
                .Include(p => p.ProductosRelacionadosOrigen)
                    .ThenInclude(pr => pr.ProductoRelacionadoDestino)
                .Include(p => p.ImagenesSecundarias)
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