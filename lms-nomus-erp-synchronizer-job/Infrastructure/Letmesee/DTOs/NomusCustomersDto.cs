using System.Text.Json.Serialization;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Letmesee.DTOs;

/// <summary>
/// DTO para resposta do endpoint de clientes Nomus conectados ao Letmesee
/// </summary>
public class NomusCustomersDto
{
    [JsonPropertyName("userGroupId")]
    public long? UserGroupId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("hashToken")]
    public string? HashToken { get; set; }

    [JsonPropertyName("lastUpdate")]
    public DateTime? LastUpdate { get; set; }
}

