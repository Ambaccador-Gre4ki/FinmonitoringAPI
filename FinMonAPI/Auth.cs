using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace FinMonAPI
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://portal.fedsfm.ru";

        // Передаем настроенный HttpClient с сертификатом через конструктор
        public AuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            if (_httpClient.BaseAddress == null) {
                _httpClient.BaseAddress = new Uri(BaseUrl);
            }
        }

        /// <summary>
        /// Выполняет авторизацию и возвращает access_token
        /// </summary>
        public async Task<string> AuthenticateAsync(string login, string password)
        {
            // Формируем тело запроса
            var requestBody = new AuthRequest
            {
                UserName = login,
                Password = password
            };

            try
            {
                var jsonOptions = new JsonSerializerOptions {
                    PropertyNameCaseInsensitive = true                
                };
                // Отправляем POST-запрос с ContentType: application/json
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync("test-contur/authenticate", requestBody);

                // Вызовет исключение, если HTTP-статус не 2xx
                response.EnsureSuccessStatusCode();

                // Читаем и десериализуем ответ
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(jsonOptions);

                if (authResponse != null && authResponse.Success)
                {
                    // Проверяем, что вложенный объект Value и токен существуют
                    if (authResponse.Value?.AccessToken != null)
                    {
                        return authResponse.Value.AccessToken;
                    }
                    throw new Exception("Авторизация успешна, но access_token отсутствует в ответе.");
                }
                else
                {
                    // Если Success = false, выводим ошибки из ответа
                    string errorMessage = authResponse?.Errors ?? "Неизвестная ошибка";
                    throw new Exception($"Ошибка авторизации Росфинмониторинга: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                // Логируем или прокидываем ошибку дальше
                throw new Exception($"Не удалось пройти авторизацию: {ex.Message}", ex);
            }
        }

        #region Модели данных DTO (Data Transfer Object)

        // Модель запроса
        private class AuthRequest
        {
            [JsonPropertyName("userName")]
            public string UserName { get; set; } = string.Empty;

            [JsonPropertyName("Password")]
            public string Password { get; set; } = string.Empty;
        }

        private class AuthResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("value")]
            public AuthValue? Value { get; set; }

            [JsonPropertyName("errors")]
            public string? Errors { get; set; }
        }

        private class AuthValue
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [JsonPropertyName("refreshToken")]
            public string? RefreshToken { get; set; }
        }
        #endregion
    }
}