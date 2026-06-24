using FinMonAPI.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace FinMonAPI
{
    public class CatalogService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public CatalogService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        /// <summary>
        /// Получение информации о каталоге ТЭ
        /// </summary>
        public async Task<Te2CatalogResponse?> GetTe2CatalogAsync()
        {
            var response = await _httpClient.PostAsync("test-contur/suspect-catalogs/current-te2-catalog", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Te2CatalogResponse>(_jsonOptions);
        }

        /// <summary>
        /// Скачивание zip-файла перечня ТЭ (id в форме)
        /// </summary>
        public async Task DownloadTe2FileAsync(Guid catalogId, string destinationPath)
        {
            var formData = new FormUrlEncodedContent(new Dictionary<string, string> { { "id", catalogId.ToString() } });
            var response = await _httpClient.PostAsync("test-contur/suspect-catalogs/current-te2-file", formData);
            response.EnsureSuccessStatusCode();

            await SaveStreamToFileAsync(response, destinationPath);
        }

        /// <summary>
        /// Получение информации о каталоге МВК
        /// </summary>
        public async Task<MVKCatalogResponse?> GetMVKCatalogAsync()
        {
            var response = await _httpClient.PostAsync("test-contur/suspect-catalogs/current-mvk-catalog", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MVKCatalogResponse>(_jsonOptions);
        }

        /// <summary>
        /// Скачивание zip-файла перечня МВК
        /// </summary>
        public async Task DownloadMVKFileAsync(Guid catalogId, string destinationPath)
        {
            var formData = new FormUrlEncodedContent(new Dictionary<string, string> { { "id", catalogId.ToString()} });
            var response = await _httpClient.PostAsync("test-contur/suspect-catalogs/current-mvk-file-zip", formData);
            response.EnsureSuccessStatusCode();
            await SaveStreamToFileAsync(response, destinationPath);
        }

        private static async Task SaveStreamToFileAsync(HttpResponseMessage response, string destinationPath)
        {
            using var responseStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await responseStream.CopyToAsync(fileStream);
        }
    }
}
