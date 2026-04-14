using ACBrBoleto.Core.Enums;

namespace ACBrBoleto.Core.Models;

/// <summary>
/// Sacador/avalista do boleto. Corresponde a TACBrSacadoAvalista no Delphi.
/// </summary>
public class Avalista
{
    public TipoPessoa Pessoa { get; set; } = TipoPessoa.Fisica;
    public string Nome { get; set; } = string.Empty;
    public string CnpjCpf { get; set; } = string.Empty;
    public string Logradouro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Complemento { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string UF { get; set; } = string.Empty;
    public string CEP { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Fone { get; set; } = string.Empty;
    public string InscricaoNr { get; set; } = string.Empty;
}
