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

        // 🔥 CAMBIO: Ahora recibimos la lista completa con Imagen y Tutorial
        public async Task EnviarCorreoCompraAsync(string destinatario, string numeroPedido, List<(string NombreProducto, string LinkDescarga, string ImagenUrl, string TutorialUrl)> itemsDescarga)
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

            // 🔥 NUEVO: Recorremos los productos mostrando Imagen, Título, Link y Tutorial
            foreach (var item in itemsDescarga)
            {
                htmlBody.Append($@"
                    <li style='margin-bottom: 25px; text-align: center; background-color: #FAFAFA; padding: 20px; border-radius: 12px; border: 1px solid #FCE4EC;'>
                        <img src='{item.ImagenUrl}' alt='{item.NombreProducto}' style='width: 120px; height: 120px; object-fit: cover; border-radius: 8px; margin-bottom: 15px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);' />
                        
                        <h4 style='color: #5D4037; margin: 0 0 15px 0; font-size: 16px;'>{item.NombreProducto}</h4>
                        
                        <a href='{item.LinkDescarga}' style='display: inline-block; background-color: #F48FB1; color: white; padding: 12px 25px; text-decoration: none; border-radius: 25px; font-weight: bold; margin-bottom: 10px;'>
                            ⬇️ Descargar Archivo Seguro
                        </a>
                ");

                // Si el producto tiene un link de YouTube o Instagram, lo mostramos
                if (!string.IsNullOrEmpty(item.TutorialUrl))
                {
                    htmlBody.Append($@"
                        <br/>
                        <a href='{item.TutorialUrl}' target='_blank' style='display: inline-block; margin-top: 5px; color: #E91E63; text-decoration: underline; font-size: 14px; font-weight: bold;'>
                            📺 Ver tutorial paso a paso
                        </a>
                    ");
                }

                htmlBody.Append("</li>");
            }

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
    }
}