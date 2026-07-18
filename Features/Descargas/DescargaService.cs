using TiempoBiblia.Api.shared;

namespace TiempoBiblia.Api.Features.Descargas
{
    public class DescargaService
    {
        private readonly DescargaRepository _repository;

        public DescargaService(DescargaRepository repository)
        {
            _repository = repository;
        }

        // Método para el ADMIN: Genera un nuevo link seguro
        public async Task<TokenDescarga> GenerarLinkDescargaAsync(int productoId, string correoCliente)
        {
            var nuevoToken = new TokenDescarga
            {
                ProductoId = productoId,
                CorreoCliente = correoCliente,
                FechaCreacion = DateTime.UtcNow,
                // Le damos exactamente 24 horas de vida
                FechaExpiracion = DateTime.UtcNow.AddHours(24), 
                DescargasRealizadas = 0,
                LimiteDescargas = 1 
            };

            return await _repository.CrearTokenAsync(nuevoToken);
        }

        // Método para el CLIENTE: Valida y actualiza el contador
        public async Task<TokenDescarga?> ValidarYConsumirTokenAsync(Guid tokenId)
        {
            var token = await _repository.ObtenerTokenConProductoAsync(tokenId);

            // Verificamos si existe y si la propiedad EsValido (que evalúa fecha y límite) es true
            if (token == null || !token.EsValido)
            {
                return null;
            }

            // Si es válido, sumamos una descarga y guardamos
            token.DescargasRealizadas++;
            await _repository.ActualizarTokenAsync(token);

            return token;
        }
    }
}