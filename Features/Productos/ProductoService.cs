using TiempoBiblia.Api.Features.Relaciones;

namespace TiempoBiblia.Api.Features.Productos
{
    /// <summary>
    /// Capa de lógica de negocio y mapeo relacional.
    /// </summary>
    public class ProductoService
    {
        private readonly ProductoRepository _repository;

        public ProductoService(ProductoRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Producto>> ObtenerTodosAdminAsync() => await _repository.ObtenerTodosAdminAsync();
        public async Task<List<Producto>> ObtenerActivosPublicoAsync() => await _repository.ObtenerActivosPublicoAsync();
        public async Task<Producto?> ObtenerPorIdAsync(int id) => await _repository.ObtenerPorIdAsync(id);

        // 🔥 CREAR CON RELACIONES MÚLTIPLES
        public async Task<Producto> CrearAsync(ProductoDto dto)
        {
            ValidarReglasNegocio(dto);

            var producto = new Producto
            {
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                Precio = dto.Precio,
                Descuento = dto.Descuento,
                EsGratuito = dto.EsGratuito,
                ImagenUrl = dto.ImagenUrl,
                Tipo = dto.Tipo,
                PdfUrl = dto.PdfUrl,
                VideoUrl = dto.VideoUrl,
                Activo = dto.Activo,
                CategoriaId = dto.CategoriaId,
                // Mapeo mágico de relaciones
                CategoriasSecundarias = dto.CategoriasSecundariasIds.Select(id => new ProductoCategoriaSecundaria { CategoriaId = id }).ToList(),
                ProductoTags = dto.TagsIds.Select(id => new ProductoTag { TagId = id }).ToList(),
                ProductosRelacionadosOrigen = dto.ProductosRelacionadosIds.Select(id => new ProductoRelacionado { ProductoRelacionadoId = id }).ToList(),
                ImagenesSecundarias = dto.ImagenesSecundarias.Select(img => new ImagenProducto { Url = img.Url }).ToList()
            };

            return await _repository.CrearAsync(producto);
        }

        // 🔥 ACTUALIZAR CON RELACIONES MÚLTIPLES
        public async Task<Producto> ActualizarAsync(int id, ProductoDto dto)
        {
            ValidarReglasNegocio(dto);

            // 1. Buscamos el producto con sus tablas intermedias cargadas
            var producto = await _repository.ObtenerParaEdicionAsync(id);
            if (producto == null)
                throw new KeyNotFoundException("El producto que intentas modificar no existe.");

            // 2. Actualizamos campos básicos
            producto.Nombre = dto.Nombre;
            producto.Descripcion = dto.Descripcion;
            producto.Precio = dto.Precio;
            producto.Descuento = dto.Descuento;
            producto.EsGratuito = dto.EsGratuito;
            producto.ImagenUrl = dto.ImagenUrl;
            producto.Tipo = dto.Tipo;
            producto.PdfUrl = dto.PdfUrl;
            producto.VideoUrl = dto.VideoUrl;
            producto.Activo = dto.Activo;
            producto.CategoriaId = dto.CategoriaId;

            // 3. Limpiamos las relaciones viejas y agregamos las nuevas
            producto.CategoriasSecundarias.Clear();
            foreach (var catId in dto.CategoriasSecundariasIds)
            {
                producto.CategoriasSecundarias.Add(new ProductoCategoriaSecundaria { CategoriaId = catId, ProductoId = id });
            }

            producto.ProductoTags.Clear();
            foreach (var tagId in dto.TagsIds)
            {
                producto.ProductoTags.Add(new ProductoTag { TagId = tagId, ProductoId = id });
            }

            producto.ProductosRelacionadosOrigen.Clear();
            foreach (var relId in dto.ProductosRelacionadosIds)
            {
                producto.ProductosRelacionadosOrigen.Add(new ProductoRelacionado { ProductoRelacionadoId = relId, ProductoOrigenId = id });
            }
            // 🔥 LIMPIAR Y ACTUALIZAR IMÁGENES
            producto.ImagenesSecundarias.Clear();
            foreach (var img in dto.ImagenesSecundarias)
            {
                producto.ImagenesSecundarias.Add(new ImagenProducto { Url = img.Url, ProductoId = id });
            }

            // 4. Guardamos
            return await _repository.ActualizarAsync(producto);
        }

        public async Task<bool> EliminarAsync(int id)
        {
            return await _repository.EliminarAsync(id);
        }

        // Método auxiliar centralizado para validaciones
        private void ValidarReglasNegocio(ProductoDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre)) throw new ArgumentException("El nombre es obligatorio.");
            if (dto.Precio < 0 || dto.Descuento < 0) throw new ArgumentException("Precios no pueden ser negativos.");
            if (dto.EsGratuito)
            {
                dto.Precio = 0;
                dto.Descuento = 0;
            }
        }
    }
}