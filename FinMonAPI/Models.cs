using System.Text.Json.Serialization;

namespace FinMonAPI.Models
{
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