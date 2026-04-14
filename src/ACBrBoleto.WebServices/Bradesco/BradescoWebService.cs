using System.Text.Json.Nodes;
using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;
using ACBrBoleto.WebServices.Base;
using Microsoft.Extensions.Caching.Memory;

namespace ACBrBoleto.WebServices.Bradesco;

/// <summary>
/// WebService Bradesco — OpenAPI Portal (REST/JSON + mTLS).
/// Produção:    https://openapi.bradesco.com.br
/// Homologação: https://openapisandbox.prebanco.com.br
/// OAuth Prod:  https://openapi.bradesco.com.br/auth/server-mtls/v2/token
/// OAuth Hom:   https://openapisandbox.prebanco.com.br/auth/server-mtls/v2/token
/// Corresponde a TBoletoW_Bradesco no Delphi.
/// NOTA: requer certificado mTLS (.pfx/.p12) em WebServiceConfig.Certificado.
/// </summary>
public class BradescoWebService : BoletoWebServiceBase
{
    protected override string UrlProducao    => "https://openapi.bradesco.com.br";
    protected override string UrlHomologacao => "https://openapisandbox.prebanco.com.br";
    protected override string UrlOAuthProducao    => "https://openapi.bradesco.com.br/auth/server-mtls/v2/token";
    protected override string UrlOAuthHomologacao => "https://openapisandbox.prebanco.com.br/auth/server-mtls/v2/token";

    private const string PathBoleto = "/v1/boleto/registrarboleto";
    private const string PathConsulta = "/v1/boleto/consultarboleto";
    private const string PathAlteracao = "/v1/boleto/alterarboleto";
    private const string PathBaixa = "/v1/boleto/solicitarbaixa";

    public BradescoWebService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        : base(httpClientFactory, cache) { }

    public override string Nome => "Bradesco";
    public override TipoCobranca TipoCobranca => TipoCobranca.Bradesco;

    public override async Task<RetornoWebService> IncluirAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + PathBoleto;
        var body  = MontarCorpoIncluir(boleto, beneficiario);

        var retorno = await SendAsync(HttpMethod.Post, url, body.ToJsonString(), HeadersBearer(token), ct);
        ParseResposta(retorno);
        return retorno;
    }

    public override async Task<RetornoWebService> ConsultarDetalheAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + PathConsulta;
        var body  = new JsonObject
        {
            ["numeroDocumento"] = boleto.NumeroDocumento,
            ["filialCpfCnpj"]   = SomenteNumeros(beneficiario.CnpjCpf),
            ["controleParticipante"] = boleto.NumeroControle
        };

        return await SendAsync(HttpMethod.Post, url, body.ToJsonString(), HeadersBearer(token), ct);
    }

    public override async Task<RetornoWebService> BaixarAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + PathBaixa;
        var body  = new JsonObject
        {
            ["numeroDocumento"]      = boleto.NumeroDocumento,
            ["filialCpfCnpj"]        = SomenteNumeros(beneficiario.CnpjCpf),
            ["controleParticipante"] = boleto.NumeroControle
        };

        return await SendAsync(HttpMethod.Post, url, body.ToJsonString(), HeadersBearer(token), ct);
    }

    private static JsonObject MontarCorpoIncluir(Boleto boleto, Beneficiario beneficiario)
    {
        return new JsonObject
        {
            ["nuCPFCNPJ"]              = SomenteNumeros(beneficiario.CnpjCpf),
            ["filialCPFCNPJ"]          = "0001",
            ["ctrlCNPJCPFVendedor"]    = "00",
            ["codEmpresa"]             = beneficiario.Convenio,
            ["naturezaOperacao"]       = "05",
            ["numeroDocumento"]        = Truncar(boleto.NumeroDocumento, 10),
            ["tipoEspecieDoc"]         = "02",
            ["dataEmissaoTitulo"]      = boleto.DataDocumento.ToString("dd/MM/yyyy"),
            ["dataVencimentoTitulo"]   = boleto.Vencimento.ToString("dd/MM/yyyy"),
            ["valorTitulo"]            = boleto.ValorDocumento,
            ["pagador"]                = new JsonObject
            {
                ["nome"]       = Truncar(boleto.Pagador.Nome, 40),
                ["CPFCNPJ"]    = SomenteNumeros(boleto.Pagador.CnpjCpf),
                ["endereco"]   = Truncar(boleto.Pagador.Logradouro, 40),
                ["CEP"]        = SomenteNumeros(boleto.Pagador.CEP),
                ["cidade"]     = Truncar(boleto.Pagador.Cidade, 20),
                ["UF"]         = boleto.Pagador.UF
            }
        };
    }

    private static void ParseResposta(RetornoWebService retorno)
    {
        if (string.IsNullOrEmpty(retorno.RetornoWS)) return;
        try
        {
            var json = JsonNode.Parse(retorno.RetornoWS);
            retorno.NossoNumero    = json?["nossoNumero"]?.GetValue<string>() ?? "";
            retorno.LinhaDigitavel = json?["linhaDigitavel"]?.GetValue<string>() ?? "";
            retorno.CodigoBarras   = json?["codigoBarras"]?.GetValue<string>() ?? "";
        }
        catch { /* deixa retorno como está */ }
    }
}
