using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;
using ACBrBoleto.WebServices.Base;
using Microsoft.Extensions.Caching.Memory;

namespace ACBrBoleto.WebServices.Kobana;

/// <summary>
/// WebService Kobana (Boleto Simples) — API REST/JSON com Token Bearer.
/// Produção: https://api.kobana.com.br
/// Sandbox:  https://api-sandbox.kobana.com.br
/// Autenticação: Bearer token direto (sem OAuth flow — ApiKey é o token).
/// Corresponde a TBoletoW_Kobana no Delphi.
/// </summary>
public class KobanaWebService : BoletoWebServiceBase
{
    protected override string UrlProducao    => "https://api.kobana.com.br";
    protected override string UrlHomologacao => "https://api-sandbox.kobana.com.br";
    protected override string? UrlSandbox   => "https://api-sandbox.kobana.com.br";
    protected override string UrlOAuthProducao    => ""; // sem OAuth — usa ApiKey como token
    protected override string UrlOAuthHomologacao => "";

    public KobanaWebService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        : base(httpClientFactory, cache) { }

    public override string Nome => "Kobana";
    public override TipoCobranca TipoCobranca => TipoCobranca.Kobana;

    // Kobana usa ApiKey diretamente como Bearer token
    protected override Task<string> ObterNovoTokenAsync(WebServiceConfig config, CancellationToken ct)
        => Task.FromResult(config.KeyUser);

    public override async Task<RetornoWebService> IncluirAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + "/v1/bank_billets";
        var body  = MontarCorpoIncluir(boleto, beneficiario);

        var retorno = await SendAsync(HttpMethod.Post, url, body.ToJsonString(), HeadersBearer(token), ct);
        ParseResposta(retorno);
        return retorno;
    }

    public override async Task<RetornoWebService> ConsultarDetalheAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + $"/v1/bank_billets/{boleto.NossoNumero}";

        return await SendAsync(HttpMethod.Get, url, null, HeadersBearer(token), ct);
    }

    public override async Task<RetornoWebService> BaixarAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + $"/v1/bank_billets/{boleto.NossoNumero}/cancel";

        return await SendAsync(HttpMethod.Put, url, "{}", HeadersBearer(token), ct);
    }

    public override async Task<RetornoWebService> ConsultarAsync(
        FiltroConsulta filtro, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var qs    = MontarQsConsulta(filtro);
        var url   = GetBaseUrl(config.Ambiente) + "/v1/bank_billets?" + qs;

        return await SendAsync(HttpMethod.Get, url, null, HeadersBearer(token), ct);
    }

    private static JsonObject MontarCorpoIncluir(Boleto boleto, Beneficiario beneficiario)
    {
        var obj = new JsonObject
        {
            ["bank_billet"] = new JsonObject
            {
                ["amount"]         = boleto.ValorDocumento.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                ["expire_at"]      = boleto.Vencimento.ToString("yyyy-MM-dd"),
                ["document_number"]= Truncar(boleto.NumeroDocumento, 15),
                ["instructions"]   = Truncar(boleto.Instrucao1, 255),
                ["customer_person_name"]    = Truncar(boleto.Pagador.Nome, 40),
                ["customer_cnpj_cpf"]       = SomenteNumeros(boleto.Pagador.CnpjCpf),
                ["customer_state"]          = boleto.Pagador.UF,
                ["customer_city_name"]      = Truncar(boleto.Pagador.Cidade, 30),
                ["customer_address"]        = Truncar(boleto.Pagador.Logradouro, 40),
                ["customer_neighborhood"]   = Truncar(boleto.Pagador.Bairro, 30),
                ["customer_zipcode"]        = SomenteNumeros(boleto.Pagador.CEP)
            }
        };

        return obj;
    }

    private static string MontarQsConsulta(FiltroConsulta filtro)
    {
        var parts = new List<string>();
        if (filtro.DataVencimento != null)
        {
            parts.Add($"expire_from={filtro.DataVencimento.DataInicio:yyyy-MM-dd}");
            parts.Add($"expire_to={filtro.DataVencimento.DataFinal:yyyy-MM-dd}");
        }
        parts.Add($"page={filtro.Pagina}");
        return string.Join("&", parts);
    }

    private static void ParseResposta(RetornoWebService retorno)
    {
        if (string.IsNullOrEmpty(retorno.RetornoWS)) return;
        try
        {
            var json = JsonNode.Parse(retorno.RetornoWS);
            var bb   = json?["bank_billet"];
            retorno.NossoNumero    = bb?["our_number"]?.GetValue<string>() ?? "";
            retorno.LinhaDigitavel = bb?["line"]?.GetValue<string>() ?? "";
            retorno.CodigoBarras   = bb?["barcode"]?.GetValue<string>() ?? "";
        }
        catch { /* deixa retorno como está */ }
    }
}
