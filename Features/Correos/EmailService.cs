using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using System.Text;
using TiempoBiblia.Api.Features.Resenas; // 🔥 LÍNEA NUEVA: Para que reconozca qué es "Resena"

namespace TiempoBiblia.Api.Features.Correos
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        // 🔥 CAMBIO: Ahora recibimos la lista completa con Imagen y Tutorial
        public async Task EnviarCorreoCompraAsync(string destinatario, string numeroPedido, List<(string NombreProducto, string LinkDescarga, string ImagenUrl, string TutorialUrl, string Tipo)> itemsDescarga )
        {
            string senderName = _config["SmtpConfig:SenderName"] ?? "TiempoBiblia-Luzy";
            string user = _config["SmtpConfig:User"] ?? "";
            string host = _config["SmtpConfig:Host"] ?? "";
            string pass = _config["SmtpConfig:Password"] ?? "";
            int port = int.Parse(_config["SmtpConfig:Port"] ?? "587");
            string dest = string.IsNullOrEmpty(destinatario) ? "cliente@desconocido.com" : destinatario;

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(senderName, user));
            email.To.Add(MailboxAddress.Parse(dest));
            email.Subject = $"TiempoBiblia - Luzy ¡Tu compra fue exitosa! 🎉 Pedido #{numeroPedido.Substring(0, Math.Min(numeroPedido.Length, 8))}...";

            var htmlBody = new StringBuilder();
            htmlBody.Append(@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background-color: #FFF0F5; padding: 30px; border-radius: 15px;'>
                    <div style='text-align: center; margin-bottom: 20px;'>
                        <h1 style='color: #E91E63; margin-bottom: 0;'>¡Gracias por tu compra!</h1>
                        <p style='color: #5D4037; font-size: 16px;'>Tu pedido ha sido procesado con éxito.</p>
                    </div>
                    <div style='background-color: white; padding: 20px; border-radius: 10px; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
                        <h3 style='color: #333; border-bottom: 2px solid #F48FB1; padding-bottom: 10px; margin-bottom: 20px;'>Tus recursos / talleres:</h3>
                        <ul style='list-style-type: none; padding: 0;'>
            ");

            // 🔥 NUEVO: Recorremos los productos y cambiamos el diseño según el Tipo
            foreach (var item in itemsDescarga)
            {
                bool esTallerPresencial = item.Tipo.Equals("presencial", StringComparison.OrdinalIgnoreCase);

                htmlBody.Append($@"
                    <li style='margin-bottom: 25px; text-align: center; background-color: #FAFAFA; padding: 20px; border-radius: 12px; border: 1px solid #FCE4EC;'>
                        <img src='{item.ImagenUrl}' alt='{item.NombreProducto}' style='width: 120px; height: 120px; object-fit: cover; border-radius: 8px; margin-bottom: 15px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);' />
                        <h4 style='color: #5D4037; margin: 0 0 15px 0; font-size: 16px;'>{item.NombreProducto}</h4>
                ");

                // ==========================================
                // CASO A: ES UN TALLER PRESENCIAL
                // ==========================================
                if (esTallerPresencial)
                {
                    // Nota: Aquí pasamos el LinkDescarga que el backend construyó, asumiendo que el controlador 
                    //       de Descargas fue modificado para devolver el link directo de Google Forms sin token,
                    //       o que se manejará el token pero apuntará al Forms.
                    htmlBody.Append($@"
                        <div style='background-color: #E3F2FD; padding: 15px; border-radius: 8px; margin-bottom: 15px;'>
                            <p style='color: #1565C0; margin: 0 0 10px 0; font-size: 14px;'><strong>¡Último paso!</strong> Por favor, llena el formulario de inscripción para enviarte toda la información del taller (fecha, lugar, materiales, etc).</p>
                        </div>
                        <a href='{item.TutorialUrl}' style='display: inline-block; background-color: #2196F3; color: white; padding: 12px 25px; text-decoration: none; border-radius: 25px; font-weight: bold; margin-bottom: 10px;'>
                            📝 Llenar Formulario de Inscripción
                        </a>
                    ");
                }
                // ==========================================
                // CASO B: ES UN IMPRIMIBLE O PRODUCTO DIGITAL
                // ==========================================
                else
                {
                    htmlBody.Append($@"
                        <a href='{item.LinkDescarga}?nombre={Uri.EscapeDataString(item.NombreProducto)}' style='display: inline-block; background-color: #F48FB1; color: white; padding: 12px 25px; text-decoration: none; border-radius: 25px; font-weight: bold; margin-bottom: 10px;'>
                            ⬇️ Descargar Archivo Seguro
                        </a>
                    ");

                    if (!string.IsNullOrEmpty(item.TutorialUrl))
                    {
                        htmlBody.Append($@"
                            <br/>
                            <a href='{item.TutorialUrl}' target='_blank' style='display: inline-block; margin-top: 5px; color: #E91E63; text-decoration: underline; font-size: 14px; font-weight: bold;'>
                                📺 Ver tutorial paso a paso
                            </a>
                        ");
                    }
                }

                htmlBody.Append("</li>");
            }

            // ... (EL RESTO DE TU MÉTODO QUEDA EXACTAMENTE IGUAL)
            // 🔥 LA REGLA DE ORO + EL FOOTER DE LA OVEJITA
            htmlBody.Append($@"
                        </ul>
                        
                        <div style='background-color: #FFF3E0; border-left: 4px solid #FFB74D; padding: 15px; margin-top: 25px; border-radius: 4px;'>
                            <p style='color: #E65100; margin: 0; font-size: 14px; font-weight: bold; text-align: center;'>
                                ⚠️ Recomendación importante:
                            </p>
                            <p style='color: #E65100; margin: 5px 0 0 0; font-size: 14px; text-align: center;'>
                                Preferiblemente imprimir en <strong>opalina</strong> o <strong>papel fotográfico</strong> para garantizar la mejor calidad.
                            </p>
                        </div>

                        <p style='color: #888; font-size: 12px; text-align: center; margin-top: 20px;'>
                            Nota: Los enlaces de descarga son personales y tienen un límite de seguridad.
                        </p>
                    </div>
                    
                    <table width='100%' border='0' cellspacing='0' cellpadding='0' style='margin-top: 30px;'>
                        <tr>
                            <td align='left' valign='bottom' style='width: 60px;'></td>
                            <td align='center' valign='middle'>
                                <img src='https://tiempobiblia-luzy.online/images/ovejitaagradecimiento.png' alt='Ovejita' style='width: 300px; display: block;' />
                                <p style='color: #999; font-size: 14px; font-weight: bold; margin: 0;'>
                                    TiempoBiblia-Luzy © {DateTime.Now.Year}
                                </p>
                            </td>
                            <td align='right' valign='bottom' style='width: 60px;'></td>
                        </tr>
                    </table>
                </div>
            ");

            email.Body = new TextPart(TextFormat.Html) { Text = htmlBody.ToString() };

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(user, pass);
                await smtp.SendAsync(email);
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
        // 🔥 NUEVO: CORREO DE FIDELIZACIÓN (A LAS 48 HORAS)
        public async Task EnviarCorreoFidelizacionAsync(string destinatario, int pedidoId, List<string> nombresProductos)
        {
            string senderName = _config["SmtpConfig:SenderName"] ?? "TiempoBiblia-Luzy";
            string user = _config["SmtpConfig:User"] ?? "";
            string host = _config["SmtpConfig:Host"] ?? "";
            string pass = _config["SmtpConfig:Password"] ?? "";
            int port = int.Parse(_config["SmtpConfig:Port"] ?? "587");
            
            var baseUrl = _config["FrontendSettings:BaseUrl"] ?? "https://tiempobiblia-luzy.online";

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(senderName, user));
            email.To.Add(MailboxAddress.Parse(destinatario));
            email.Subject = "TiempoBiblia - Luzy ¿Qué te parecieron tus recursos? Nos encantaría saberlo 💖";

            var htmlBody = new StringBuilder();
            htmlBody.Append($@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background-color: #FFF0F5; padding: 30px; border-radius: 15px; text-align: center;'>
                    
                    <!-- 🔥 LOGO DE LA TIENDA -->
                    <img src='{baseUrl}/images/tiempobiblia.luzy.png' alt='Tiempo Biblia' style='max-width: 200px; margin-bottom: 20px;' />

                    <h2 style='color: #E91E63; margin-bottom: 10px;'>¡Hola! Han pasado un par de días...</h2>
                    <p style='color: #5D4037; font-size: 16px; margin-bottom: 20px;'>
                        Esperamos que estés disfrutando y sacándole el máximo provecho a estos recursos:
                    </p>

                    <!-- LISTA DE PRODUCTOS -->
                    <div style='background-color: white; padding: 15px; border-radius: 10px; box-shadow: 0 4px 6px rgba(0,0,0,0.05); margin-bottom: 25px; text-align: left;'>
                        <ul style='color: #5D4037; margin: 0; padding-left: 20px;'>");
            
            foreach (var nombre in nombresProductos)
            {
                htmlBody.Append($"<li style='margin-bottom: 5px;'><strong>{nombre}</strong></li>");
            }

            htmlBody.Append($@"
                        </ul>
                    </div>

                    <p style='color: #5D4037; font-size: 16px; margin-bottom: 15px;'>
                        <strong>¿Nos regalarías unos segundos para calificarlos?</strong><br/>
                        Haz clic en una estrella para dejarnos tu opinión anónima:
                    </p>

                    <!-- 🔥 LAS 5 ESTRELLAS CLICABLES (ONE-CLICK REVIEW) -->
                    <div style='font-size: 40px; margin-bottom: 30px;'>
                        <a href='{baseUrl}/calificar/{pedidoId}?estrellas=1' style='text-decoration: none;'>⭐</a>
                        <a href='{baseUrl}/calificar/{pedidoId}?estrellas=2' style='text-decoration: none;'>⭐</a>
                        <a href='{baseUrl}/calificar/{pedidoId}?estrellas=3' style='text-decoration: none;'>⭐</a>
                        <a href='{baseUrl}/calificar/{pedidoId}?estrellas=4' style='text-decoration: none;'>⭐</a>
                        <a href='{baseUrl}/calificar/{pedidoId}?estrellas=5' style='text-decoration: none;'>⭐</a>
                    </div>

                    <p style='color: #888; font-size: 12px; margin-top: 20px;'>
                        Tu opinión ayuda a que más personas conecten con la Palabra de Dios. ¡Gracias!
                    </p>
                </div>
            ");

            email.Body = new TextPart(TextFormat.Html) { Text = htmlBody.ToString() };

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(user, pass);
                await smtp.SendAsync(email);
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
        // 🔥 NUEVO: Alerta de moderación
        public async Task EnviarAlertaNuevaResenaAsync(string correoAdmin, Resena resena, string apiUrl)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Sistema TiendaBiblia-Luzy", _config["SmtpConfig:User"]!));
            email.To.Add(MailboxAddress.Parse(correoAdmin));
            email.Subject = $"TiempoBiblia - Luzy ⚠️ Nueva Reseña de {resena.NombreCliente} ({resena.Calificacion} Estrellas)";

            string linkAprobar = $"{apiUrl}/api/resenas/aprobar/{resena.Id}";

            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                    <h2 style='color: #E91E63;'>Tienes una reseña pendiente de moderación</h2>
                    <p><strong>Cliente:</strong> {resena.NombreCliente}</p>
                    <p><strong>Calificación:</strong> {resena.Calificacion}/5 ⭐</p>
                    <p><strong>Comentario:</strong> <em>""{resena.Comentario}""</em></p>
                    <br/>
                    <a href='{linkAprobar}' style='background-color: #4CAF50; color: white; padding: 15px 25px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>✅ APROBAR Y PUBLICAR</a>
                    <p style='color: #999; font-size: 12px; margin-top: 20px;'>Si no deseas publicarla, ignora este correo.</p>
                </div>";

            email.Body = new TextPart(TextFormat.Html) { Text = body };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_config["SmtpConfig:Host"]!, int.Parse(_config["SmtpConfig:Port"]!), SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_config["SmtpConfig:User"]!, _config["SmtpConfig:Password"]!);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}