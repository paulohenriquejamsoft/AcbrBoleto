using ACBrBoleto.Core.Enums;

namespace ACBrBoleto.Core.Models;

/// <summary>
/// Chave PIX vinculada ao boleto. Corresponde a TACBrBoletoChavePIX no Delphi.
/// </summary>
public class ChavePix
{
    public TipoPIXChave TipoChave { get; set; }
    public string Chave { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
    public string TxId { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string PixCopiaECola { get; set; } = string.Empty;
}
