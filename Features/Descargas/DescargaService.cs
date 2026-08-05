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

        // ============================================================
        // 1. SERVICIOS PARA EL ADMINISTRADOR
        // ============================================================

        /// <summary>
        /// Genera un nuevo token seguro de 24 horas con límite de uso.
        /// </summary>
        public async Task<TokenDescarga> GenerarLinkDescargaAsync(int productoId, string correoCliente)
        {
            var nuevoToken = new TokenDescarga
            {
                ProductoId = productoId,
                CorreoCliente = correoCliente,
                FechaCreacion = DateTime.UtcNow,
                FechaExpiracion = DateTime.UtcNow.AddHours(24), 
                DescargasRealizadas = 0,
                LimiteDescargas = 2 
            };

            return await _repository.CrearTokenAsync(nuevoToken);
        }

        // ============================================================
        // 2. SERVICIOS PARA EL CLIENTE (FLUJO ATÓMICO)
        // ============================================================

        /// <summary>
        /// Verifica si el token existe y si aún le quedan intentos válidos.
        /// </summary>
        public async Task<bool> ValidarTokenAsync(Guid tokenId)
        {
            var token = await _repository.ObtenerTokenConProductoAsync(tokenId);
            return token != null && token.EsValido;
        }

        /// <summary>
        /// Retorna el token con la información del producto listo para descargar.
        /// </summary>
        public async Task<TokenDescarga?> ObtenerDatosArchivoAsync(Guid tokenId)
        {
            var token = await _repository.ObtenerTokenConProductoAsync(tokenId);
            
            if (token == null || !token.EsValido)
            {
                return null;
            }

            return token;
        }

        /// <summary>
        /// Suma una descarga realizada al token en la Base de Datos.
        /// </summary>
        public async Task<bool> ConsumirTokenAsync(Guid tokenId)
        {
            var token = await _repository.ObtenerTokenConProductoAsync(tokenId);

            if (token == null || !token.EsValido)
            {
                return false;
            }

            token.DescargasRealizadas++;
            await _repository.ActualizarTokenAsync(token);
            return true;
        }
        /// <summary>
        /// Procesa un carrito de compras completo, generando un token por cada producto.
        /// </summary>
        public async Task<List<TokenDescarga>> ProcesarPedidoAsync(string correoCliente, List<int> productosIds)
        {
            var tokens = new List<TokenDescarga>();

            foreach (var productoId in productosIds)
            {
                var nuevoToken = new TokenDescarga
                {
                    ProductoId = productoId,
                    CorreoCliente = correoCliente,
                    FechaCreacion = DateTime.UtcNow,
                    FechaExpiracion = DateTime.UtcNow.AddDays(7), // 7 días para descargas compradas
                    DescargasRealizadas = 0,
                    LimiteDescargas = 2 // 2 intentos para evitar abusos
                };

                var tokenGuardado = await _repository.CrearTokenAsync(nuevoToken);
                tokens.Add(tokenGuardado);
            }

            return tokens;
        }
    }
}