using System.Text.Json.Serialization;

namespace lms_nomus_erp_synchronizer_job.Domain.Models
{
    public class RateLimitResponse
    {
        [JsonPropertyName("tempoAteLiberar")]
        public string? TempoAteLiberar { get; set; }
    }
}
