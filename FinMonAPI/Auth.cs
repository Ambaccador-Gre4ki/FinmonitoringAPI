using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;


namespace FinMonAPI
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://portal.fedsfm.ru:8081/Services/fedsfm-service";

        // Передаем настроенный HttpClient с сертификатом через конструктор
        public AuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
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
                // Отправляем POST-запрос с ContentType: application/json
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync(BaseUrl, requestBody);

                // Вызовет исключение, если HTTP-статус не 2xx
                response.EnsureSuccessStatusCode();

                // Читаем и десериализуем ответ
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

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
            [JsonPropertyName("userName")] // В JSON со строчной
            public string UserName { get; set; } = string.Empty;

            [JsonPropertyName("Password")] // В JSON с заглавной!
            public string Password { get; set; } = string.Empty;
        }

        // Модель ответа
        private class AuthResponse
        {
            [JsonPropertyName("Success")]
            public bool Success { get; set; }

            [JsonPropertyName("Value")]
            public AuthValue? Value { get; set; }

            [JsonPropertyName("Errors")]
            public string? Errors { get; set; }
        }

        // Блок служебных полей Value
        private class AuthValue
        {
            [JsonPropertyName("access_token")] // В JSON через подчеркивание
            public string? AccessToken { get; set; }

            [JsonPropertyName("refreshToken")]
            public string? RefreshToken { get; set; }
        }
        #endregion
    }

}
