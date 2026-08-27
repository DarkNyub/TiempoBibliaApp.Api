using TiempoBiblia.Api.Features.Correos;
using TiempoBiblia.Api.Features.Descargas;

namespace TiempoBiblia.Api.Features.Pedidos
{
    public class PedidoService
    {
        private readonly PedidoRepository _pedidoRepository;
        private readonly EmailService _emailService;
        private readonly IConfiguration _config;// 🔥 ¡ESTA ES LA LÍNEA QUE TE FALTA!
        private readonly DescargaService _descargaService;

        public PedidoService(PedidoRepository pedidoRepository, EmailService emailService, IConfiguration config, DescargaService descargaService)
        {
            _pedidoRepository = pedidoRepository;
            _emailService = emailService;
            _config = config;
            _descargaService = descargaService;
        }

        /// <summary>
        /// Lógica de negocio para buscar un pedido, rescatar sus links y reenviar el correo al cliente.
        /// </summary>
        public async Task<bool> ReenviarCorreoPedidoAsync(int pedidoId, string nuevoCorreo) // 🔥 Recibe el nuevo
        {
            // 1. Buscamos el pedido en la BD (a través del repositorio)
            var pedido = await _pedidoRepository.ObtenerPedidoConDetallesPorIdAsync(pedidoId);
            if (pedido == null || string.IsNullOrWhiteSpace(pedido.CorreoCliente))
            {
                throw new Exception("Pedido no encontrado o no tiene un correo válido asociado.");
            }

            // 2. Extraemos los IDs de los productos que compró
            var productosIds = pedido.Detalles.Select(d => d.ProductoId).ToList();
            string correoOriginal = pedido.CorreoCliente;

            // 🔥 Si el correo cambió, actualizamos la base de datos
            if (!string.Equals(correoOriginal, nuevoCorreo, StringComparison.OrdinalIgnoreCase))
            {
                await _pedidoRepository.ActualizarCorreoPedidoYTokensAsync(pedido.Id, correoOriginal, nuevoCorreo, productosIds);
                pedido.CorreoCliente = nuevoCorreo; // Para la búsqueda de abajo
            }

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
                TutorialUrl: t.Producto?.VideoUrl ?? "",
                Tipo: t.Producto?.Tipo ?? ""
            )).ToList();

            // 5. Despachamos el correo utilizando el servicio de correos
            await _emailService.EnviarCorreoCompraAsync(pedido.CorreoCliente, pedido.TransaccionGatewayId, itemsDescarga);

            return true;
        }
        /// <summary>
        /// 🔥 NUEVO: Crea un pedido manual simulando una compra real, genera links y envía el correo.
        /// </summary>
        public async Task<bool> CrearPedidoManualAsync(CrearPedidoManualDto request)
        {
            // 1. Guardamos en Base de Datos reutilizando tu flujo blindado de Checkout
            var tokens = await _descargaService.ProcesarPedidoAsync(
                request.CorreoCliente, 
                request.TransaccionGatewayId, 
                request.Pasarela, 
                "Ingreso Manual", // Franquicia
                null,             // Últimos 4
                request.ProductosIds, 
                request.TotalCobrado, 
                request.Moneda
            );

            // 2. Preparamos el correo
            var baseUrl = _config["FrontendSettings:BaseUrl"] ?? "https://tiempobiblia-luzy.online";
            var itemsDescarga = tokens.Select(t => (
                NombreProducto: t.Producto?.Nombre ?? "Recurso Digital",
                LinkDescarga: $"{baseUrl}/descargar/{t.Id}",
                ImagenUrl: string.IsNullOrEmpty(t.Producto?.ImagenUrl) ? $"{baseUrl}/images/default.jpg" : t.Producto.ImagenUrl,
                TutorialUrl: t.Producto?.VideoUrl ?? "",
                Tipo: t.Producto?.Tipo ?? ""
            )).ToList();

            // 3. Despachamos el correo al cliente
            await _emailService.EnviarCorreoCompraAsync(request.CorreoCliente, request.TransaccionGatewayId, itemsDescarga);

            return true;
        }
    }
}