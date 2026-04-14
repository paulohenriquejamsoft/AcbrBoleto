namespace ACBrBoleto.Core.Models;

/// <summary>
/// Dados de liquidação de um boleto. Corresponde a TACBrTituloLiquidacao no Delphi.
/// </summary>
public class Liquidacao
{
    public DateTime? DataOcorrencia { get; set; }
    public DateTime? DataCredito { get; set; }
    public decimal ValorPago { get; set; }
    public decimal ValorTarifas { get; set; }
    public decimal ValorIOF { get; set; }
    public decimal ValorAbatimento { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal ValorMoraJuros { get; set; }
    public decimal ValorOutrosCreditos { get; set; }
    public decimal ValorOutrosDebitos { get; set; }
    public decimal ValorLiquidado { get; set; }
    public string CanalLiquidacao { get; set; } = string.Empty;
    public string NossoNumero { get; set; } = string.Empty;
    public string SeuNumero { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public string Agencia { get; set; } = string.Empty;
    public string Conta { get; set; } = string.Empty;
}
