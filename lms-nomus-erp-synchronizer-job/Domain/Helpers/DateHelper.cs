using System.Text.Json;

namespace lms_nomus_erp_synchronizer_job.Domain.Helpers;

/// <summary>
/// Helper para parsing robusto de datas em diferentes formatos
/// </summary>
public static class DateHelper
{
    private static readonly string[] DateFormats = new[]
    {
        "yyyy-MM-dd",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ss.fffZ",
        "dd/MM/yyyy",
        "dd/MM/yyyy HH:mm:ss",
        "yyyy/MM/dd",
        "yyyy/MM/dd HH:mm:ss"
    };

    /// <summary>
    /// Converte uma string de data para DateTime usando múltiplos formatos
    /// </summary>
    public static DateTime ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DateTime.MinValue;

        // Tenta parsing direto (para objetos DateTime serializados como string)
        if (DateTime.TryParse(value, out var result))
            return result;

        // Tenta formatos específicos
        foreach (var format in DateFormats)
        {
            if (DateTime.TryParseExact(value, format, null, System.Globalization.DateTimeStyles.None, out result))
                return result;
        }

        return DateTime.MinValue;
    }

    /// <summary>
    /// Converte um objeto (string, DateTime ou DateTimeOffset) para DateTime
    /// </summary>
    public static DateTime ParseDate(object? value)
    {
        if (value == null)
            return DateTime.MinValue;

        if (value is DateTime dt)
            return dt;

        if (value is DateTimeOffset dto)
            return dto.DateTime;
        if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.String)
            {
                var strs = jsonElement.GetString();
                return ParseDate(strs);
            }
        }

        if (value is string str)
            return ParseDate(str);

        return DateTime.MinValue;
    }
}

