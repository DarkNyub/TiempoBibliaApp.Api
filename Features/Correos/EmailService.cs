using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using System.Text;

namespace TiempoBiblia.Api.Features.Correos
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        // 🔥 CAMBIO: Ahora recibimos una lista que contiene (NombreProducto, LinkDescarga)
        public async Task EnviarCorreoCompraAsync(string destinatario, string numeroPedido, List<(string NombreProducto, string LinkDescarga)> itemsDescarga)
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
            email.Subject = $"¡Tu compra fue exitosa! 🎉 Pedido #{numeroPedido.Substring(0, Math.Min(numeroPedido.Length, 8))}...";

            var htmlBody = new StringBuilder();
            htmlBody.Append(@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background-color: #FFF0F5; padding: 30px; border-radius: 15px;'>
                    <div style='text-align: center; margin-bottom: 20px;'>
                        <h1 style='color: #E91E63; margin-bottom: 0;'>¡Gracias por tu compra!</h1>
                        <p style='color: #5D4037; font-size: 16px;'>Tu pedido ha sido procesado con éxito.</p>
                    </div>
                    <div style='background-color: white; padding: 20px; border-radius: 10px; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
                        <h3 style='color: #333; border-bottom: 2px solid #F48FB1; padding-bottom: 10px; margin-bottom: 20px;'>Tus recursos listos para descargar:</h3>
                        <ul style='list-style-type: none; padding: 0;'>
            ");

            // 🔥 NUEVO: Recorremos los productos mostrando su Título y su Botón
            foreach (var item in itemsDescarga)
            {
                htmlBody.Append($@"
                    <li style='margin-bottom: 25px; text-align: center; background-color: #FAFAFA; padding: 15px; border-radius: 12px; border: 1px solid #FCE4EC;'>
                        <h4 style='color: #5D4037; margin: 0 0 15px 0; font-size: 16px;'>{item.NombreProducto}</h4>
                        <a href='{item.LinkDescarga}' style='display: inline-block; background-color: #F48FB1; color: white; padding: 12px 25px; text-decoration: none; border-radius: 25px; font-weight: bold;'>
                            ⬇️ Descargar Archivo
                        </a>
                    </li>
                ");
            }

            // 🔥 EL FOOTER DE LA OVEJITA (Usando tablas para compatibilidad total con Gmail/Outlook)
            htmlBody.Append($@"
                        </ul>
                        <p style='color: #888; font-size: 13px; text-align: center; margin-top: 20px;'>
                            Nota: Estos enlaces son personales y tienen un límite de descargas por seguridad.
                        </p>
                    </div>
                    
                    <table width='100%' border='0' cellspacing='0' cellpadding='0' style='margin-top: 30px;'>
                        <tr>
                            <td align='left' valign='bottom' style='width: 60px;'>
                                <!-- Espacio vacío para equilibrar -->
                            </td>
                            <td align='center' valign='middle'>
                                <p style='color: #999; font-size: 14px; font-weight: bold; margin: 0;'>
                                    TiempoBiblia-Luzy © {DateTime.Now.Year}
                                </p>
                            </td>
                            <td align='right' valign='bottom' style='width: 60px;'>
                                <!-- 🔥 REEMPLAZA EL SRC CON LA URL REAL DE TU OVEJITA -->
                                <img src='https://tiempobiblia-luzy.online/images/logo_agradecimiento.png' alt='Ovejita' style='width: 60px; display: block;' />
                            </td>
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
    }
}