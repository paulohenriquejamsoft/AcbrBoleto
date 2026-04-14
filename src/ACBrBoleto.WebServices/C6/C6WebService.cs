using System.Text.Json.Nodes;
using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;
using ACBrBoleto.WebServices.Base;
using Microsoft.Extensions.Caching.Memory;

namespace ACBrBoleto.WebServices.C6;

/// <summary>
/// WebService C6 Bank — BaaS API de boletos (REST/JSON).
/// Produção:   https://baas-api.c6bank.info/v1/bank_slips
/// Sandbox:    https://baas-api-sandbox.c6bank.info/v1/bank_slips
/// OAuth Prod: https://baas-api.c6bank.info/v1/auth
/// OAuth Sbox: https://baas-api-sandbox.c6bank.info/v1/auth
/// Corresponde a TBoletoW_C6 no Delphi.
/// </summary>
public class C6WebService : BoletoWebServiceBase
{
    protected override string UrlProducao    => "https://baas-api.c6bank.info/v1/bank_slips";
    protected override string UrlHomologacao => "https://baas-api-sandbox.c6bank.info/v1/bank_slips";
    protected override string? UrlSandbox   => "https://baas-api-sandbox.c6bank.info/v1/bank_slips";
    protected override string UrlOAuthProducao    => "https://baas-api.c6bank.info/v1/auth";
    protected override string UrlOAuthHomologacao => "https://baas-api-sandbox.c6bank.info/v1/auth";

    public C6WebService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        : base(httpClientFactory, cache) { }

    public override string Nome => "C6 Bank";
    public override TipoCobranca TipoCobranca => TipoCobranca.BancoC6;

    // C6: POST /v1/auth com JSON {"username","password"}
    protected override async Task<string> ObterNovoTokenAsync(WebServiceConfig config, CancellationToken ct)
    {
        using var client = _httpClientFactory.CreateClient();
        client.Timeout = config.Timeout;

        var body = new JsonObject
        {
            ["username"] = config.ClientId,
            ["password"] = config.ClientSecret
        };

        var content = new System.Net.Http.StringContent(body.ToJsonString(), System.Text.Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(GetOAuthUrl(config.Ambiente), content, ct);
        resp.EnsureSuccessStatusCode();

        var json = JsonNode.Parse(await resp.Content.ReadAsStringAsync(ct));
        return json?["access_token"]?.GetValue<string>()
               ?? throw new WebServiceException("Token não retornado pelo C6 Bank.");
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

    public override async Task<RetornoWebService> BaixarAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + $"/{boleto.NossoNumero}/cancel";

        return await SendAsync(HttpMethod.Post, url, "{}", HeadersBearer(token), ct);
    }

    private static JsonObject MontarCorpoIncluir(Boleto boleto, Beneficiario beneficiario)
    {
        var obj = new JsonObject
        {
            ["external_reference_id"] = Truncar(boleto.NumeroDocumento, 50),
            ["amount"]                = boleto.ValorDocumento,
            ["due_date"]              = boleto.Vencimento.ToString("yyyy-MM-dd"),
            ["payer"] = new JsonObject
            {
                ["name"]    = Truncar(boleto.Pagador.Nome, 33),
                ["tax_id"]  = SomenteNumeros(boleto.Pagador.CnpjCpf),
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

        if (boleto.ValorDesconto > 0 && boleto.CodigoDesconto != CodigoDesconto.SemDesconto)
        {
            obj["discount"] = new JsonObject
            {
                ["type"]  = boleto.CodigoDesconto == CodigoDesconto.Percentual ? "percentage" : "fixed",
                ["value"] = boleto.ValorDesconto
            };
        }

        if (boleto.CodigoJuros != CodigoJuros.Isento && boleto.ValorMoraJuros > 0)
        {
            obj["interest"] = new JsonObject
            {
                ["type"]  = "percentage_per_month",
                ["value"] = boleto.PercentualMoraJuros
            };
        }

        if (boleto.CodigoMulta != CodigoMulta.Isento && boleto.ValorMulta > 0)
        {
            obj["fine"] = new JsonObject
            {
                ["type"]  = boleto.CodigoMulta == CodigoMulta.Percentual ? "percentage" : "fixed",
                ["value"] = boleto.ValorMulta
            };
        }

        return obj;
    }

    private static void ParseResposta(RetornoWebService retorno)
    {
        if (string.IsNullOrEmpty(retorno.RetornoWS)) return;
        try
        {
            var json = JsonNode.Parse(retorno.RetornoWS);
            retorno.NossoNumero    = json?["id"]?.GetValue<string>() ?? "";
            retorno.LinhaDigitavel = json?["digitable_line"]?.GetValue<string>() ?? "";
            retorno.CodigoBarras   = json?["barcode"]?.GetValue<string>() ?? "";
        }
        catch { /* deixa retorno como está */ }
    }
}
