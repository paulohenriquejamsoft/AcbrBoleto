using System.Text.Json.Nodes;
using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;
using ACBrBoleto.WebServices.Base;
using Microsoft.Extensions.Caching.Memory;

namespace ACBrBoleto.WebServices.Cora;

/// <summary>
/// WebService Cora — API de Boletos v2 (REST/JSON + mTLS).
/// Produção:   https://matls-clients.api.cora.com.br/v2
/// Homolog.:   https://matls-clients.api.stage.cora.com.br/v2
/// OAuth Prod: https://matls-clients.api.cora.com.br/token
/// OAuth Hom:  https://matls-clients.api.stage.cora.com.br/token
/// Corresponde a TBoletoW_Cora no Delphi.
/// NOTA: requer certificado mTLS (.pfx/.p12) em WebServiceConfig.Certificado.
/// </summary>
public class CoraWebService : BoletoWebServiceBase
{
    protected override string UrlProducao    => "https://matls-clients.api.cora.com.br/v2";
    protected override string UrlHomologacao => "https://matls-clients.api.stage.cora.com.br/v2";
    protected override string UrlOAuthProducao    => "https://matls-clients.api.cora.com.br/token";
    protected override string UrlOAuthHomologacao => "https://matls-clients.api.stage.cora.com.br/token";

    public CoraWebService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        : base(httpClientFactory, cache) { }

    public override string Nome => "Cora";
    public override TipoCobranca TipoCobranca => TipoCobranca.BancoCora;

    public override async Task<RetornoWebService> IncluirAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + "/invoices";
        var body  = MontarCorpoIncluir(boleto);

        var retorno = await SendAsync(HttpMethod.Post, url, body.ToJsonString(), HeadersBearer(token), ct);
        ParseResposta(retorno);
        return retorno;
    }

    public override async Task<RetornoWebService> ConsultarDetalheAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + $"/invoices/{boleto.NossoNumero}";

        return await SendAsync(HttpMethod.Get, url, null, HeadersBearer(token), ct);
    }

    public override async Task<RetornoWebService> BaixarAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + $"/invoices/{boleto.NossoNumero}/cancel";

        return await SendAsync(HttpMethod.Post, url, "{}", HeadersBearer(token), ct);
    }

    private static JsonObject MontarCorpoIncluir(Boleto boleto)
    {
        var obj = new JsonObject
        {
            ["code"]       = Truncar(boleto.NumeroDocumento, 40),
            ["due_date"]   = boleto.Vencimento.ToString("yyyy-MM-dd"),
            ["services"] = new JsonArray
            {
                new JsonObject
                {
                    ["name"]     = "Boleto",
                    ["amount"]   = boleto.ValorDocumento,
                    ["quantity"] = 1
                }
            },
            ["customer"] = new JsonObject
            {
                ["name"]          = Truncar(boleto.Pagador.Nome, 64),
                ["email"]         = boleto.Pagador.Email,
                ["document_type"] = boleto.Pagador.Pessoa == TipoPessoa.Fisica ? "CPF" : "CNPJ",
                ["document"]      = SomenteNumeros(boleto.Pagador.CnpjCpf),
                ["address"] = new JsonObject
                {
                    ["street"]      = Truncar(boleto.Pagador.Logradouro, 64),
                    ["number"]      = Truncar(boleto.Pagador.Numero, 10),
                    ["district"]    = Truncar(boleto.Pagador.Bairro, 64),
                    ["city"]        = Truncar(boleto.Pagador.Cidade, 64),
                    ["state"]       = boleto.Pagador.UF,
                    ["zip_code"]    = SomenteNumeros(boleto.Pagador.CEP)
                }
            }
        };

        if (boleto.ValorDesconto > 0)
        {
            obj["payment_terms"] = new JsonObject
            {
                ["discount"] = new JsonObject
                {
                    ["percentage"] = boleto.PercentualDesconto
                }
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
            retorno.LinhaDigitavel = json?["bank_slip"]?["digitable_line"]?.GetValue<string>() ?? "";
            retorno.CodigoBarras   = json?["bank_slip"]?["barcode"]?.GetValue<string>() ?? "";
            retorno.QrCodePix      = json?["pix"]?["emv"]?.GetValue<string>() ?? "";
        }
        catch { /* deixa retorno como está */ }
    }
}
