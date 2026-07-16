namespace TiempoBiblia.Api.Features.Paquetes
{
    /// <summary>
    /// Lógica de negocio para los paquetes antes de tocar la base de datos.
    /// </summary>
    public class PaqueteService
    {
        private readonly PaqueteRepository _repository;

        public PaqueteService(PaqueteRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Paquete>> ObtenerTodosAdminAsync()
        {
            return await _repository.ObtenerTodosAdminAsync();
        }

        public async Task<List<Paquete>> ObtenerActivosPublicoAsync()
        {
            return await _repository.ObtenerActivosPublicoAsync();
        }

        public async Task<Paquete> CrearAsync(Paquete paquete)
        {
            if (string.IsNullOrWhiteSpace(paquete.Nombre))
            {
                throw new ArgumentException("El nombre del paquete es obligatorio.");
            }

            if (paquete.Precio < 0 || paquete.Descuento < 0)
            {
                throw new ArgumentException("El precio y el descuento no pueden ser negativos.");
            }

            return await _repository.CrearAsync(paquete);
        }
    }
}