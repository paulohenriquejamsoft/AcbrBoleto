using System.Text.Json.Nodes;
using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;
using ACBrBoleto.WebServices.Base;
using Microsoft.Extensions.Caching.Memory;

namespace ACBrBoleto.WebServices.Santander;

/// <summary>
/// WebService Santander — Collection Bill Management v2 (REST/JSON).
/// Produção:   https://trust-open.api.santander.com.br/collection_bill_management/v2
/// Sandbox:    https://trust-sandbox.api.santander.com.br/collection_bill_management/v2
/// OAuth Prod: https://trust-open.api.santander.com.br/auth/oauth/v2/token
/// OAuth Sbox: https://trust-sandbox.api.santander.com.br/auth/oauth/v2/token
/// Corresponde a TBoletoW_Santander_API no Delphi.
/// </summary>
public class SantanderApiWebService : BoletoWebServiceBase
{
    protected override string UrlProducao    => "https://trust-open.api.santander.com.br/collection_bill_management/v2";
    protected override string UrlHomologacao => "https://trust-sandbox.api.santander.com.br/collection_bill_management/v2";
    protected override string? UrlSandbox   => "https://trust-sandbox.api.santander.com.br/collection_bill_management/v2";
    protected override string UrlOAuthProducao    => "https://trust-open.api.santander.com.br/auth/oauth/v2/token";
    protected override string UrlOAuthHomologacao => "https://trust-sandbox.api.santander.com.br/auth/oauth/v2/token";

    public SantanderApiWebService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        : base(httpClientFactory, cache) { }

    public override string Nome => "Santander API";
    public override TipoCobranca TipoCobranca => TipoCobranca.Santander;

    public override async Task<RetornoWebService> IncluirAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + "/workspaces/" + beneficiario.Convenio + "/boletos";
        var body  = MontarCorpoIncluir(boleto, beneficiario);

        var retorno = await SendAsync(HttpMethod.Post, url, body.ToJsonString(), HeadersBearer(token), ct);
        ParseResposta(retorno);
        return retorno;
    }

    public override async Task<RetornoWebService> ConsultarDetalheAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente)
                  + $"/workspaces/{beneficiario.Convenio}/boletos/{boleto.NossoNumero}";

        return await SendAsync(HttpMethod.Get, url, null, HeadersBearer(token), ct);
    }

    public override async Task<RetornoWebService> BaixarAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente)
                  + $"/workspaces/{beneficiario.Convenio}/boletos/{boleto.NossoNumero}/payment";
        var body  = new JsonObject { ["paymentCancellationReason"] = "01" };

        return await SendAsync(HttpMethod.Delete, url, body.ToJsonString(), HeadersBearer(token), ct);
    }

    private static JsonObject MontarCorpoIncluir(Boleto boleto, Beneficiario beneficiario)
    {
        return new JsonObject
        {
            ["noseNumber"]       = Truncar(boleto.NossoNumero, 12),
            ["yourNumber"]       = Truncar(boleto.NumeroDocumento, 15),
            ["issueDate"]        = boleto.DataDocumento.ToString("yyyy-MM-dd"),
            ["dueDate"]          = boleto.Vencimento.ToString("yyyy-MM-dd"),
            ["totalValue"]       = boleto.ValorDocumento,
            ["payer"] = new JsonObject
            {
                ["documentType"]   = boleto.Pagador.Pessoa == TipoPessoa.Fisica ? "CPF" : "CNPJ",
                ["documentNumber"] = SomenteNumeros(boleto.Pagador.CnpjCpf),
                ["name"]           = Truncar(boleto.Pagador.Nome, 40),
                ["street"]         = Truncar(boleto.Pagador.Logradouro, 40),
                ["city"]           = Truncar(boleto.Pagador.Cidade, 30),
                ["stateCode"]      = boleto.Pagador.UF,
                ["zipCode"]        = SomenteNumeros(boleto.Pagador.CEP)
            }
        };
    }

    private static void ParseResposta(RetornoWebService retorno)
    {
        if (string.IsNullOrEmpty(retorno.RetornoWS)) return;
        try
        {
            var json = JsonNode.Parse(retorno.RetornoWS);
            retorno.NossoNumero    = json?["noseNumber"]?.GetValue<string>() ?? "";
            retorno.LinhaDigitavel = json?["digitableLine"]?.GetValue<string>() ?? "";
            retorno.CodigoBarras   = json?["barCode"]?.GetValue<string>() ?? "";
        }
        catch { /* deixa retorno como está */ }
    }
}
