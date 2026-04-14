using ACBrBoleto.Core.Enums;

namespace ACBrBoleto.Core.Configuration;

/// <summary>
/// Configurações da biblioteca via Options Pattern (.NET DI).
/// Corresponde a TConfiguracoes no Delphi.
/// </summary>
public class BoletoOptions
{
    public const string SectionName = "ACBrBoleto";

    public TipoCobranca TipoCobranca { get; set; } = TipoCobranca.Nenhum;
    public LayoutRemessa LayoutRemessa { get; set; } = LayoutRemessa.Cnab240;
    public bool Homologacao { get; set; } = true;

    // Arquivos
    public string PastaRemessa { get; set; } = string.Empty;
    public string PastaRetorno { get; set; } = string.Empty;
    public string PastaPDF { get; set; } = string.Empty;

    // Comportamento
    public bool RemoverAcentosCnab { get; set; } = true;
    public string EncodingCnab { get; set; } = "windows-1252";
    public bool UsarStringBuilderCnab { get; set; } = true;
}

public class WebServiceOptions
{
    public const string SectionName = "ACBrBoletoWebService";

    public TipoAmbienteWebService Ambiente { get; set; } = TipoAmbienteWebService.Homologacao;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string CaminhosCertificado { get; set; } = string.Empty;
    public string SenhaCertificado { get; set; } = string.Empty;
    public int TimeoutSegundos { get; set; } = 30;
    public bool LogarRequests { get; set; } = false;
}
