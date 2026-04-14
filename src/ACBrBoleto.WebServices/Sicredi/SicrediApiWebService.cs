using System.Text.Json.Nodes;
using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;
using ACBrBoleto.WebServices.Base;
using Microsoft.Extensions.Caching.Memory;

namespace ACBrBoleto.WebServices.Sicredi;

/// <summary>
/// WebService Sicredi — API E-COMM de cobrança de boleto (REST/JSON).
/// Produção/Hom: https://cobrancaonline.sicredi.com.br/sicredi-cobranca-ws-ecomm-api/ecomm/v1/boleto
/// OAuth:        https://cobrancaonline.sicredi.com.br/sicredi-cobranca-ws-ecomm-api/ecomm/v1/boleto/autenticacao
/// Corresponde a TBoletoW_Sicredi_APIECOMM no Delphi.
/// </summary>
public class SicrediApiWebService : BoletoWebServiceBase
{
    private const string BaseEcomm = "https://cobrancaonline.sicredi.com.br/sicredi-cobranca-ws-ecomm-api/ecomm/v1/boleto";

    protected override string UrlProducao    => BaseEcomm;
    protected override string UrlHomologacao => BaseEcomm;
    protected override string UrlOAuthProducao    => BaseEcomm + "/autenticacao";
    protected override string UrlOAuthHomologacao => BaseEcomm + "/autenticacao";

    public SicrediApiWebService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        : base(httpClientFactory, cache) { }

    public override string Nome => "Sicredi API";
    public override TipoCobranca TipoCobranca => TipoCobranca.Sicredi;

    // Sicredi autentica com usuario/senha no body JSON
    protected override async Task<string> ObterNovoTokenAsync(WebServiceConfig config, CancellationToken ct)
    {
        using var client = _httpClientFactory.CreateClient();
        client.Timeout = config.Timeout;

        var body = new JsonObject
        {
            ["usuario"] = config.ClientId,
            ["senha"]   = config.ClientSecret,
            ["posto"]   = config.KeyUser
        };

        var content = new System.Net.Http.StringContent(body.ToJsonString(), System.Text.Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(UrlOAuthProducao, content, ct);
        resp.EnsureSuccessStatusCode();

        var json = JsonNode.Parse(await resp.Content.ReadAsStringAsync(ct));
        return json?["token"]?.GetValue<string>()
               ?? throw new WebServiceException("Token não retornado pelo Sicredi.");
    }

    public override async Task<RetornoWebService> IncluirAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente);
        var body  = MontarCorpoIncluir(boleto, beneficiario);

        var retorno = await SendAsync(HttpMethod.Post, url, body.ToJsonString(), HeadersBearer(token), ct);
        ParseResposta(retorno);
        return retorno;
    }

    public override async Task<RetornoWebService> ConsultarDetalheAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + $"/{boleto.NossoNumero}";

        return await SendAsync(HttpMethod.Get, url, null, HeadersBearer(token), ct);
    }

    private static JsonObject MontarCorpoIncluir(Boleto boleto, Beneficiario beneficiario)
    {
        return new JsonObject
        {
            ["codigoBeneficiario"] = beneficiario.Convenio,
            ["tipoCobranca"]       = "NORMAL",
            ["especieDocumento"]   = "A",
            ["seuNumero"]          = Truncar(boleto.NumeroDocumento, 10),
            ["dataVencimento"]     = boleto.Vencimento.ToString("dd/MM/yyyy"),
            ["valor"]              = boleto.ValorDocumento,
            ["pagador"] = new JsonObject
            {
                ["tipoPessoa"]   = boleto.Pagador.Pessoa == TipoPessoa.Fisica ? "FISICA" : "JURIDICA",
                ["cpfCnpj"]      = SomenteNumeros(boleto.Pagador.CnpjCpf),
                ["nome"]         = Truncar(boleto.Pagador.Nome, 40),
                ["endereco"]     = Truncar(boleto.Pagador.Logradouro, 40),
                ["cidade"]       = Truncar(boleto.Pagador.Cidade, 20),
                ["uf"]           = boleto.Pagador.UF,
                ["cep"]          = SomenteNumeros(boleto.Pagador.CEP)
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
