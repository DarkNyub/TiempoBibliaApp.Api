using TiempoBiblia.Api.Features.Correos;

namespace TiempoBiblia.Api.Features.Pedidos
{
    public class PedidoService
    {
        private readonly PedidoRepository _pedidoRepository;
        private readonly EmailService _emailService;
        private readonly IConfiguration _config;

        public PedidoService(PedidoRepository pedidoRepository, EmailService emailService, IConfiguration config)
        {
            _pedidoRepository = pedidoRepository;
            _emailService = emailService;
            _config = config;
        }

        /// <summary>
        /// Lógica de negocio para buscar un pedido, rescatar sus links y reenviar el correo al cliente.
        /// </summary>
        public async Task<bool> ReenviarCorreoPedidoAsync(int pedidoId)
        {
            // 1. Buscamos el pedido en la BD (a través del repositorio)
            var pedido = await _pedidoRepository.ObtenerPedidoConDetallesPorIdAsync(pedidoId);
            if (pedido == null || string.IsNullOrWhiteSpace(pedido.CorreoCliente))
            {
                throw new Exception("Pedido no encontrado o no tiene un correo válido asociado.");
            }

            // 2. Extraemos los IDs de los productos que compró
            var productosIds = pedido.Detalles.Select(d => d.ProductoId).ToList();

            // 3. Buscamos los tokens (enlaces mágicos) que le pertenecen a ese cliente y esos productos
            var tokens = await _pedidoRepository.ObtenerTokensParaReenvioAsync(pedido.CorreoCliente, productosIds);

            if (!tokens.Any())
            {
                throw new Exception("No se encontraron enlaces de descarga generados para este pedido.");
            }

            // 4. Formateamos los datos para la plantilla del correo
            var baseUrl = _config["FrontendSettings:BaseUrl"] ?? "https://tiempobiblia-luzy.online";
            var itemsDescarga = tokens.Select(t => (
                NombreProducto: t.Producto?.Nombre ?? "Recurso Digital",
                LinkDescarga: $"{baseUrl}/descargar/{t.Id}",
                ImagenUrl: string.IsNullOrEmpty(t.Producto?.ImagenUrl) ? $"{baseUrl}/images/default.jpg" : t.Producto.ImagenUrl,
                TutorialUrl: t.Producto?.VideoUrl ?? ""
            )).ToList();

            // 5. Despachamos el correo utilizando el servicio de correos
            await _emailService.EnviarCorreoCompraAsync(pedido.CorreoCliente, pedido.TransaccionGatewayId, itemsDescarga);

            return true;
        }
    }
}