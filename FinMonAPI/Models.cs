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

    // --- Модель МВК (MVK) ---
    public class MVKCatalogResponse
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("isActive")]
        public bool? IsActive { get; set; }

        [JsonPropertyName("idXml")]
        public Guid IdXml { get; set; }
    }
}