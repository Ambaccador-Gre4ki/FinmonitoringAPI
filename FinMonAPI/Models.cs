using System;
using System.Text.Json.Serialization;

namespace FinMonAPI.Models
{
    // --- Модели Авторизации ---
    public class AuthRequest
    {
        [JsonPropertyName("userName")]
        public string UserName { get; set; } = string.Empty;

        [JsonPropertyName("Password")]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("value")]
        public AuthValue? Value { get; set; }

        [JsonPropertyName("errors")]
        public System.Text.Json.JsonElement Errors { get; set; }
    }

    public class AuthValue
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refreshToken")]
        public string? RefreshToken { get; set; }
    }

    // --- Модель Каталога Террористов и Экстремистов (TE2) ---
    public class Te2CatalogResponse
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("idXml")]
        public Guid IdXml { get; set; }
    }

    // --- Модель Сводных Перечней ООН (EN / RU) ---
    public class UnCatalogResponse
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("idRecStatus")]
        public bool IdRecStatus { get; set; }

        [JsonPropertyName("idXml")]
        public Guid IdXml { get; set; }
    }
}