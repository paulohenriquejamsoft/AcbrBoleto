namespace ACBrBoleto.Core.Models;

/// <summary>
/// Dados de NF-e vinculada ao boleto. Corresponde a TACBrDadosNFe no Delphi.
/// </summary>
public class DadosNFe
{
    public string NumNFe { get; set; } = string.Empty;
    public decimal ValorNFe { get; set; }
    public DateTime? EmissaoNFe { get; set; }
    public string ChaveNFe { get; set; } = string.Empty;
}
