using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ACBrBoleto.Core.Helpers;

public static class StringExtensions
{
    /// <summary>
    /// Lê campo CNAB (base-1 para base-0).
    /// CRÍTICO: posicoes Delphi são base 1. Usar este helper para não errar.
    /// </summary>
    public static string CnabSubstring(this string linha, int posInicio, int tamanho)
        => linha.Length >= posInicio + tamanho - 1
            ? linha.Substring(posInicio - 1, tamanho)
            : string.Empty;

    public static string OnlyNumbers(this string value)
        => Regex.Replace(value ?? string.Empty, @"\D", string.Empty);

    public static string RemoveAcentos(this string texto)
    {
        if (string.IsNullOrEmpty(texto)) return texto;
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string PadLeftZero(this string value, int totalWidth)
        => (value ?? string.Empty).PadLeft(totalWidth, '0');

    public static string PadRightSpace(this string value, int totalWidth)
        => (value ?? string.Empty).PadRight(totalWidth, ' ');

    /// <summary>Formata valor monetário para CNAB (sem ponto decimal, centavos nas últimas posições).</summary>
    public static string ToValorCnab(this decimal valor, int tamanho)
        => ((long)Math.Round(valor * 100)).ToString().PadLeft(tamanho, '0');

    public static string ToDataCnab(this DateTime data, string formato = "ddMMyyyy")
        => data.ToString(formato, CultureInfo.InvariantCulture);

    public static string ToDataCnabOuZeros(this DateTime? data, string formato = "ddMMyyyy")
        => data.HasValue && data.Value != DateTime.MinValue
            ? data.Value.ToString(formato, CultureInfo.InvariantCulture)
            : new string('0', formato.Length);

    public static bool IsNullOrEmpty(this string? value) => string.IsNullOrEmpty(value);

    public static string TruncateOrPad(this string value, int tamanho, char padChar = ' ', bool padRight = true)
    {
        value ??= string.Empty;
        if (value.Length > tamanho) return value[..tamanho];
        return padRight ? value.PadRight(tamanho, padChar) : value.PadLeft(tamanho, padChar);
    }

    public static string PreparaCnabAlfa(this string value, int tamanho)
        => value.RemoveAcentos().TruncateOrPad(tamanho);

    public static string PreparaCnabNum(this string value, int tamanho)
        => value.OnlyNumbers().PadLeft(tamanho, '0').TruncateOrPad(tamanho, '0', false);
}
