namespace ACBrBoleto.Core.Helpers;

public static class BoletoConstants
{
    public const string InstrucaoPagamento = "Pagar preferencialmente nas agencias do {0}";
    public const string InstrucaoPagamentoLoterica = "Preferencialmente nas Casas Lotericas ate o valor limite";
    public const string InstrucaoPagamentoRegistro = "Pagavel em qualquer banco ate o vencimento.";
    public const string InstrucaoPagamentoTodaRede = "Pagavel em toda rede bancaria";

    public const string MoedaReal = "9";

    /// <summary>
    /// Data base para cálculo do fator de vencimento (07/10/1997 = fator 1000).
    /// CRÍTICO: não alterar.
    /// </summary>
    public static readonly DateTime DataBaseFatorVencimento = new(1997, 10, 7);

    /// <summary>
    /// Data de reset do fator de vencimento: a partir de 22/02/2025 o fator volta a 1000.
    /// </summary>
    public static readonly DateTime DataResetFatorVencimento = new(2025, 2, 22);
}
