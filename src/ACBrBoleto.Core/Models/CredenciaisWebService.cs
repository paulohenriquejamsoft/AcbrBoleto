using ACBrBoleto.Core.Enums;

namespace ACBrBoleto.Core.Models;

/// <summary>
/// Credenciais OAuth2 e configuração de WebService do cedente.
/// Corresponde a TACBrCedenteWS no Delphi.
/// </summary>
public class CredenciaisWebService
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string ChavePix { get; set; } = string.Empty;
    public string Certificado { get; set; } = string.Empty;
    public string SenhaCertificado { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime? TokenExpiracao { get; set; }
    public TipoAmbienteWebService Ambiente { get; set; } = TipoAmbienteWebService.Homologacao;
    public string Scope { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string AppKey { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;

    public bool TokenValido =>
        !string.IsNullOrEmpty(Token) &&
        TokenExpiracao.HasValue &&
        TokenExpiracao.Value > DateTime.Now.AddSeconds(30);
}
