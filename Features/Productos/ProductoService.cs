namespace TiempoBiblia.Api.Features.Productos
{
    /// <summary>
    /// Capa de lógica de negocio para los Productos.
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

        public async Task<Producto> CrearAsync(Producto producto)
        {
            if (string.IsNullOrWhiteSpace(producto.Nombre)) throw new ArgumentException("El nombre es obligatorio.");
            if (producto.Precio < 0 || producto.Descuento < 0) throw new ArgumentException("Precios no pueden ser negativos.");
            if (producto.EsGratuito) { producto.Precio = 0; producto.Descuento = 0; }
            return await _repository.CrearAsync(producto);
        }

        // 🔥 NUEVO: Obtener por ID
        public async Task<Producto?> ObtenerPorIdAsync(int id)
        {
            return await _repository.ObtenerPorIdAsync(id);
        }

        // 🔥 NUEVO: Validar y modificar producto
        public async Task<Producto> ActualizarAsync(int id, Producto productoActualizado)
        {
            if (id != productoActualizado.Id)
                throw new ArgumentException("El ID de la URL no coincide con el del producto.");

            if (string.IsNullOrWhiteSpace(productoActualizado.Nombre))
                throw new ArgumentException("El nombre del producto es obligatorio.");

            if (productoActualizado.Precio < 0 || productoActualizado.Descuento < 0)
                throw new ArgumentException("El precio y el descuento no pueden ser negativos.");

            if (productoActualizado.EsGratuito)
            {
                productoActualizado.Precio = 0;
                productoActualizado.Descuento = 0;
            }

            var productoExistente = await _repository.ObtenerPorIdAsync(id);
            if (productoExistente == null)
                throw new KeyNotFoundException("El producto que intentas modificar no existe.");

            // Nota: Aquí podrías mapear campo por campo si lo prefieres, 
            // pero Update() en EF Core sobreescribirá todo el objeto.
            return await _repository.ActualizarAsync(productoActualizado);
        }

        // 🔥 NUEVO: Eliminar producto
        public async Task<bool> EliminarAsync(int id)
        {
            // Ojo de Arquitecto: Como tu clase Producto ya tiene la propiedad "Activo" (bool)[cite: 10], 
            // a futuro podrías cambiar este método para hacer un "Soft Delete" (solo poner Activo = false) 
            // en lugar de borrarlo físicamente, para no romper historiales de facturas.
            return await _repository.EliminarAsync(id);
        }
    }
}