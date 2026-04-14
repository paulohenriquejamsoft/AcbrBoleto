using ACBrBoleto.Core.Enums;

namespace ACBrBoleto.Core.Models;

/// <summary>
/// Dados do beneficiário (cedente/emissor do boleto). Corresponde a TACBrCedente no Delphi.
/// </summary>
public class Beneficiario
{
    public string Nome { get; set; } = string.Empty;
    public string NomeFantasia { get; set; } = string.Empty;
    public string CnpjCpf { get; set; } = string.Empty;
    public TipoPessoa TipoPessoa { get; set; } = TipoPessoa.Juridica;

    // Dados bancários
    public string Agencia { get; set; } = string.Empty;
    public string AgenciaDigito { get; set; } = string.Empty;
    public string Conta { get; set; } = string.Empty;
    public string ContaDigito { get; set; } = string.Empty;
    public string DigitoVerificadorAgenciaConta { get; set; } = string.Empty;
    public string CodigoCedente { get; set; } = string.Empty;
    public string CodigoTransmissao { get; set; } = string.Empty;
    public string Convenio { get; set; } = string.Empty;
    public string Modalidade { get; set; } = string.Empty;
    public string CodigoFlash { get; set; } = string.Empty;
    public string Operacao { get; set; } = string.Empty;

    // Tipo/carteira
    public TipoDocumento TipoDocumento { get; set; } = TipoDocumento.Tradicional;
    public TipoCarteira TipoCarteira { get; set; } = TipoCarteira.Simples;
    public ResponsavelEmissao ResponsavelEmissao { get; set; } = ResponsavelEmissao.ClienteEmite;
    public CaracteristicaTitulo CaracteristicaTitulo { get; set; } = CaracteristicaTitulo.Simples;
    public IdentificacaoDistribuicao IdentificacaoDistribuicao { get; set; } = IdentificacaoDistribuicao.ClienteDistribui;

    // Endereço
    public string Logradouro { get; set; } = string.Empty;
    public string NumeroRes { get; set; } = string.Empty;
    public string Complemento { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string UF { get; set; } = string.Empty;
    public string CEP { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;

    // PIX
    public ChavePix? PIX { get; set; }

    // WebService
    public CredenciaisWebService WebService { get; set; } = new();

    // Integrador
    public IntegradoraBoleto IntegradoraBoleto { get; set; } = IntegradoraBoleto.Nenhum;
}
