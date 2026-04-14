using ACBrBoleto.Core.Enums;

namespace ACBrBoleto.Core.Models;

/// <summary>
/// Dados do pagador (sacado). Corresponde a TACBrSacado no Delphi.
/// </summary>
public class Pagador
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
    public string Mensagem { get; set; } = string.Empty;

    /// <summary>Avalista/Sacador avalista do boleto.</summary>
    public Avalista? Avalista { get; set; }

    public string EnderecoFormatado =>
        $"{Logradouro}, {Numero}{(string.IsNullOrEmpty(Complemento) ? "" : " " + Complemento)} - {Bairro} - {Cidade}/{UF} - CEP: {CEP}";
}
