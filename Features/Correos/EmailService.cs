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

        public async Task EnviarCorreoCompraAsync(string destinatario, string numeroPedido, List<string> linksDescarga)
        {
            // 🔥 SOLUCIÓN CS8604: Extraemos las variables garantizando que no sean nulas
            string senderName = _config["SmtpConfig:SenderName"] ?? "Tiempo Biblia";
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
                        <h3 style='color: #333; border-bottom: 2px solid #F48FB1; padding-bottom: 10px;'>Tus enlaces de descarga:</h3>
                        <ul style='list-style-type: none; padding: 0;'>
            ");

            foreach (var link in linksDescarga)
            {
                htmlBody.Append($@"
                    <li style='margin-bottom: 15px;'>
                        <a href='{link}' style='display: block; text-align: center; background-color: #F48FB1; color: white; padding: 12px 20px; text-decoration: none; border-radius: 25px; font-weight: bold;'>
                            ⬇️ Descargar Archivo
                        </a>
                    </li>
                ");
            }

            htmlBody.Append($@"
                        </ul>
                        <p style='color: #888; font-size: 13px; text-align: center; margin-top: 20px;'>
                            Nota: Estos enlaces son personales y tienen un límite de descargas por seguridad.
                        </p>
                    </div>
                    <p style='text-align: center; color: #999; font-size: 12px; margin-top: 30px;'>
                        Tiempo Biblia © {DateTime.Now.Year}
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
    }
}