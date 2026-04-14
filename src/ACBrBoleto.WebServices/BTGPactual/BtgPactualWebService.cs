using System.Text.Json.Nodes;
using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;
using ACBrBoleto.WebServices.Base;
using Microsoft.Extensions.Caching.Memory;

namespace ACBrBoleto.WebServices.BTGPactual;

/// <summary>
/// WebService BTG Pactual — Bank Slips API v1 (REST/JSON).
/// Produção:   https://api.empresas.btgpactual.com/v1/bank-slips
/// Homolog.:   https://api.sandbox.empresas.btgpactual.com/v1/bank-slips
/// OAuth Prod: https://id.btgpactual.com/oauth2/token
/// OAuth Hom:  https://id.sandbox.btgpactual.com/oauth2/token
/// Corresponde a TBoletoW_BTGPactual no Delphi.
/// </summary>
public class BtgPactualWebService : BoletoWebServiceBase
{
    protected override string UrlProducao    => "https://api.empresas.btgpactual.com/v1/bank-slips";
    protected override string UrlHomologacao => "https://api.sandbox.empresas.btgpactual.com/v1/bank-slips";
    protected override string? UrlSandbox   => "https://api.sandbox.empresas.btgpactual.com/v1/bank-slips";
    protected override string UrlOAuthProducao    => "https://id.btgpactual.com/oauth2/token";
    protected override string UrlOAuthHomologacao => "https://id.sandbox.btgpactual.com/oauth2/token";

    public BtgPactualWebService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        : base(httpClientFactory, cache) { }

    public override string Nome => "BTG Pactual";
    public override TipoCobranca TipoCobranca => TipoCobranca.BTGPactual;

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

    public override async Task<RetornoWebService> BaixarAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + $"/{boleto.NossoNumero}/cancel";

        return await SendAsync(HttpMethod.Post, url, "{}", HeadersBearer(token), ct);
    }

    private static JsonObject MontarCorpoIncluir(Boleto boleto, Beneficiario beneficiario)
    {
        return new JsonObject
        {
            ["beneficiary"] = new JsonObject
            {
                ["document"]  = SomenteNumeros(beneficiario.CnpjCpf),
                ["bankAccountId"] = beneficiario.Conta
            },
            ["document_number"] = Truncar(boleto.NumeroDocumento, 15),
            ["due_date"]         = boleto.Vencimento.ToString("yyyy-MM-dd"),
            ["amount"]           = boleto.ValorDocumento,
            ["payer"] = new JsonObject
            {
                ["document"]  = SomenteNumeros(boleto.Pagador.CnpjCpf),
                ["name"]      = Truncar(boleto.Pagador.Nome, 60),
                ["email"]     = boleto.Pagador.Email,
                ["address"] = new JsonObject
                {
                    ["street"]      = Truncar(boleto.Pagador.Logradouro, 60),
                    ["number"]      = Truncar(boleto.Pagador.Numero, 10),
                    ["district"]    = Truncar(boleto.Pagador.Bairro, 40),
                    ["city"]        = Truncar(boleto.Pagador.Cidade, 30),
                    ["state"]       = boleto.Pagador.UF,
                    ["postal_code"] = SomenteNumeros(boleto.Pagador.CEP)
                }
            }
        };
    }

    private static void ParseResposta(RetornoWebService retorno)
    {
        if (string.IsNullOrEmpty(retorno.RetornoWS)) return;
        try
        {
            var json = JsonNode.Parse(retorno.RetornoWS);
            retorno.NossoNumero    = json?["id"]?.GetValue<string>() ?? "";
            retorno.LinhaDigitavel = json?["digitable_line"]?.GetValue<string>() ?? "";
            retorno.CodigoBarras   = json?["bar_code"]?.GetValue<string>() ?? "";
        }
        catch { /* deixa retorno como está */ }
    }
}
