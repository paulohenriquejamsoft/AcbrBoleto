using ACBrBoleto.Core.Enums;

namespace ACBrBoleto.WebServices.Base;

/// <summary>
/// Configuração de conexão/autenticação passada a cada operação do WebService.
/// Corresponde às propriedades de TACBrCedenteWS + TBoletoWSClass no Delphi.
/// </summary>
public class WebServiceConfig
{
    public TipoAmbienteWebService Ambiente { get; set; } = TipoAmbienteWebService.Homologacao;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Chave de desenvolvedor/app (gw-dev-app-key no BB, x-api-key em outros).</summary>
    public string KeyUser { get; set; } = string.Empty;

    /// <summary>Caminho para o certificado .p12 / .pfx (mTLS — Inter, Cora, Bradesco Portal).</summary>
    public string? Certificado { get; set; }
    public string? CertificadoSenha { get; set; }

    /// <summary>Indica se deve registrar QR Code PIX junto com o boleto.</summary>
    public bool IndicadorPix { get; set; }

    /// <summary>Timeout das chamadas HTTP.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
