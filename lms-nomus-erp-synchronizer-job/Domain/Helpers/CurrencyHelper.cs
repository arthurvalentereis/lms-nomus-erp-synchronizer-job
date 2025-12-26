using System.Globalization;

namespace lms_nomus_erp_synchronizer_job.Domain.Helpers;

/// <summary>
/// Helper para conversão de valores monetários no formato brasileiro (ex: "1.000,00")
/// </summary>
public static class CurrencyHelper
{
    private static readonly CultureInfo BrazilianCulture = new("pt-BR");

    /// <summary>
    /// Converte uma string monetária no formato brasileiro para decimal
    /// Exemplos: "1.000,00" -> 1000.00, "50,75" -> 50.75
    /// </summary>
    public static decimal ParseBrazilianCurrency(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0m;

        // Remove espaços e tenta converter
        value = value.Trim();
        
        // Se for vazio após trim, retorna 0
        if (string.IsNullOrEmpty(value))
            return 0m;

        // Tenta converter usando a cultura brasileira
        if (decimal.TryParse(value, NumberStyles.Currency | NumberStyles.Number, BrazilianCulture, out var result))
            return result;

        // Se falhar, tenta remover pontos (separadores de milhar) e substituir vírgula por ponto
        var cleanedValue = value.Replace(".", "").Replace(",", ".");
        if (decimal.TryParse(cleanedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
            return result;

        // Se ainda falhar, retorna 0
        return 0m;
    }
}

