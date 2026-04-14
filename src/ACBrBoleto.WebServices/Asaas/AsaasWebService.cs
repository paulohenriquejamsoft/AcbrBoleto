using System.Text.Json.Nodes;
using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;
using ACBrBoleto.WebServices.Base;
using Microsoft.Extensions.Caching.Memory;

namespace ACBrBoleto.WebServices.Asaas;

/// <summary>
/// WebService Asaas — API de Cobranças v3 (REST/JSON, autenticação via API Key).
/// Produção: https://api.asaas.com/v3
/// Sandbox:  https://api-sandbox.asaas.com/v3
/// Sem OAuth — usa header "access_token: {apiKey}".
/// Corresponde a TBoletoW_Asaas no Delphi.
/// </summary>
public class AsaasWebService : BoletoWebServiceBase
{
    protected override string UrlProducao    => "https://api.asaas.com/v3";
    protected override string UrlHomologacao => "https://api-sandbox.asaas.com/v3";
    protected override string? UrlSandbox   => "https://api-sandbox.asaas.com/v3";
    protected override string UrlOAuthProducao    => "";  // não usa OAuth
    protected override string UrlOAuthHomologacao => "";

    public AsaasWebService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        : base(httpClientFactory, cache) { }

    public override string Nome => "Asaas";
    public override TipoCobranca TipoCobranca => TipoCobranca.BancoAsaas;

    // Asaas não usa OAuth, token é a própria ApiKey
    protected override Task<string> ObterNovoTokenAsync(WebServiceConfig config, CancellationToken ct)
        => Task.FromResult(config.KeyUser);

    private IEnumerable<(string, string)> HeadersAsaas(WebServiceConfig config) =>
    [
        ("access_token", config.KeyUser),
        ("Accept",       "application/json")
    ];

    public override async Task<RetornoWebService> IncluirAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var url  = GetBaseUrl(config.Ambiente) + "/payments";
        var body = MontarCorpoIncluir(boleto);

        var retorno = await SendAsync(HttpMethod.Post, url, body.ToJsonString(), HeadersAsaas(config), ct);
        ParseResposta(retorno);
        return retorno;
    }

    public override async Task<RetornoWebService> ConsultarDetalheAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var url = GetBaseUrl(config.Ambiente) + $"/payments/{boleto.NossoNumero}";
        return await SendAsync(HttpMethod.Get, url, null, HeadersAsaas(config), ct);
    }

    public override async Task<RetornoWebService> BaixarAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var url = GetBaseUrl(config.Ambiente) + $"/payments/{boleto.NossoNumero}/cancel";
        return await SendAsync(HttpMethod.Post, url, "{}", HeadersAsaas(config), ct);
    }

    public override async Task<RetornoWebService> ConsultarAsync(
        FiltroConsulta filtro, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var qs  = MontarQsConsulta(filtro);
        var url = GetBaseUrl(config.Ambiente) + "/payments?" + qs;
        return await SendAsync(HttpMethod.Get, url, null, HeadersAsaas(config), ct);
    }

    private static JsonObject MontarCorpoIncluir(Boleto boleto)
    {
        var obj = new JsonObject
        {
            ["billingType"] = "BOLETO",
            ["value"]       = boleto.ValorDocumento,
            ["dueDate"]     = boleto.Vencimento.ToString("yyyy-MM-dd"),
            ["description"] = Truncar(boleto.UsoCedente, 500),
            ["externalReference"] = Truncar(boleto.NumeroDocumento, 40),
            ["customer"] = new JsonObject
            {
                // Asaas precisa do customer id pré-existente OU criação inline
                // Por simplicidade, assume que o NumeroControle é o customer id Asaas
                ["cpfCnpj"] = SomenteNumeros(boleto.Pagador.CnpjCpf),
                ["name"]    = Truncar(boleto.Pagador.Nome, 60),
                ["email"]   = boleto.Pagador.Email,
                ["phone"]   = SomenteNumeros(boleto.Pagador.Fone)
            }
        };

        if (boleto.ValorDesconto > 0)
        {
            obj["discount"] = new JsonObject
            {
                ["value"]            = boleto.ValorDesconto,
                ["dueDateLimitDays"] = 0,
                ["type"]             = boleto.CodigoDesconto == CodigoDesconto.Percentual ? "PERCENTAGE" : "FIXED"
            };
        }

        if (boleto.CodigoJuros != CodigoJuros.Isento && boleto.PercentualMoraJuros > 0)
        {
            obj["interest"] = new JsonObject
            {
                ["value"] = boleto.PercentualMoraJuros
            };
        }

        if (boleto.CodigoMulta != CodigoMulta.Isento && boleto.PercentualMulta > 0)
        {
            obj["fine"] = new JsonObject
            {
                ["value"] = boleto.PercentualMulta
            };
        }

        return obj;
    }

    private static string MontarQsConsulta(FiltroConsulta filtro)
    {
        var parts = new List<string> { "billingType=BOLETO" };
        if (filtro.DataVencimento != null)
        {
            parts.Add($"dueDateGe={filtro.DataVencimento.DataInicio:yyyy-MM-dd}");
            parts.Add($"dueDateLe={filtro.DataVencimento.DataFinal:yyyy-MM-dd}");
        }
        parts.Add($"offset={(filtro.Pagina - 1) * filtro.ItensPorPagina}");
        parts.Add($"limit={filtro.ItensPorPagina}");
        return string.Join("&", parts);
    }

    private static void ParseResposta(RetornoWebService retorno)
    {
        if (string.IsNullOrEmpty(retorno.RetornoWS)) return;
        try
        {
            var json = JsonNode.Parse(retorno.RetornoWS);
            retorno.NossoNumero    = json?["id"]?.GetValue<string>() ?? "";
            retorno.LinhaDigitavel = json?["bankSlipUrl"]?.GetValue<string>() ?? "";
        }
        catch { /* deixa retorno como está */ }
    }
}
