using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TiempoBiblia.Api.Features.Upload
{
    /// <summary>
    /// Servicio encargado de procesar archivos locales y enviarlos a un repositorio público de GitHub 
    /// para utilizarlos como un CDN (Content Delivery Network) gratuito de imágenes.
    /// </summary>
    public class ImagenService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public ImagenService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;

            // Configuramos el cliente HTTP con los estándares requeridos por GitHub
            _httpClient.BaseAddress = new Uri("https://api.github.com/");
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("TiempoBibliaApi", "1.0"));
            
            var token = _config["GitHub:Token"];
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        /// <summary>
        /// Recibe un archivo del frontend, lo convierte a Base64 y lo sube a GitHub.
        /// Retorna la URL cruda (Raw) directa a la imagen.
        /// </summary>
        public async Task<string> SubirImagenAGitHubAsync(IFormFile archivo, string subcarpeta = "productos")
        {
            if (archivo == null || archivo.Length == 0)
                throw new ArgumentException("El archivo está vacío o es nulo.");

            // 1. Leemos la configuración
            var owner = _config["GitHub:Owner"];
            var repo = _config["GitHub:Repo"];
            var branch = _config["GitHub:Branch"] ?? "main";

            // 2. Generamos un nombre de archivo único para que no se sobreescriban
            // Ejemplo: productos/6d8a4f...-foto.png
            var extension = Path.GetExtension(archivo.FileName);
            var nombreUnico = $"{Guid.NewGuid():N}{extension}";
            var rutaEnRepo = $"{subcarpeta}/{nombreUnico}";

            // 3. Convertimos el archivo a una cadena Base64 (El formato que exige GitHub)
            using var memoryStream = new MemoryStream();
            await archivo.CopyToAsync(memoryStream);
            var contenidoBase64 = Convert.ToBase64String(memoryStream.ToArray());

            // 4. Preparamos el paquete JSON para GitHub
            var payload = new
            {
                message = $"Subiendo imagen: {nombreUnico}",
                content = contenidoBase64,
                branch = branch
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            // 5. Hacemos la petición PUT a la API de GitHub
            // Ruta de la API: /repos/{owner}/{repo}/contents/{path}
            var response = await _httpClient.PutAsync($"repos/{owner}/{repo}/contents/{rutaEnRepo}", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Fallo al subir la imagen a GitHub. Detalle: {error}");
            }

            // 6. Si fue exitoso, GitHub nos devuelve un JSON. Extraemos el 'download_url'
            var responseData = await response.Content.ReadAsStringAsync();
            using var jsonDocument = JsonDocument.Parse(responseData);
            
            // Navegamos por el JSON: { "content": { "download_url": "https://raw..." } }
            var urlRaw = jsonDocument.RootElement
                .GetProperty("content")
                .GetProperty("download_url")
                .GetString();

            return urlRaw ?? throw new Exception("No se pudo obtener la URL cruda de GitHub.");
        }
    }
}