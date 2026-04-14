using System.Text.Json.Nodes;
using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;
using ACBrBoleto.WebServices.Base;
using Microsoft.Extensions.Caching.Memory;

namespace ACBrBoleto.WebServices.Inter;

/// <summary>
/// WebService Banco Inter — Cobrança API v2/v3 (REST/JSON + mTLS).
/// Produção:    https://cdpj.partners.bancointer.com.br/cobranca/v2
/// Sandbox:     https://cdpj-sandbox.partners.uatinter.co/cobranca/v2
/// OAuth Prod:  https://cdpj.partners.bancointer.com.br/oauth/v2/token
/// OAuth Sbox:  https://cdpj-sandbox.partners.uatinter.co/oauth/v2/token
/// Corresponde a TBoletoW_Inter_API no Delphi.
/// NOTA: requer certificado mTLS (.pfx/.p12) em WebServiceConfig.Certificado.
/// </summary>
public class InterApiWebService : BoletoWebServiceBase
{
    protected override string UrlProducao    => "https://cdpj.partners.bancointer.com.br/cobranca/v2";
    protected override string UrlHomologacao => "https://cdpj-sandbox.partners.uatinter.co/cobranca/v2";
    protected override string? UrlSandbox   => "https://cdpj-sandbox.partners.uatinter.co/cobranca/v2";
    protected override string UrlOAuthProducao    => "https://cdpj.partners.bancointer.com.br/oauth/v2/token";
    protected override string UrlOAuthHomologacao => "https://cdpj-sandbox.partners.uatinter.co/oauth/v2/token";

    public InterApiWebService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        : base(httpClientFactory, cache) { }

    public override string Nome => "Banco Inter API";
    public override TipoCobranca TipoCobranca => TipoCobranca.BancoInter;

    // Inter usa client_credentials mas envia scope e client_id no body
    protected override async Task<string> ObterNovoTokenAsync(WebServiceConfig config, CancellationToken ct)
    {
        using var client = _httpClientFactory.CreateClient();
        client.Timeout = config.Timeout;

        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"]    = "client_credentials",
            ["client_id"]     = config.ClientId,
            ["client_secret"] = config.ClientSecret,
            ["scope"]         = "boleto-cobranca.write boleto-cobranca.read"
        });

        var url  = GetOAuthUrl(config.Ambiente);
        var resp = await client.PostAsync(url, body, ct);
        resp.EnsureSuccessStatusCode();

        var json = JsonNode.Parse(await resp.Content.ReadAsStringAsync(ct));
        return json?["access_token"]?.GetValue<string>()
               ?? throw new WebServiceException("Token não retornado pelo Inter.");
    }

    public override async Task<RetornoWebService> IncluirAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + "/boletos";
        var body  = MontarCorpoIncluir(boleto, beneficiario);

        var retorno = await SendAsync(HttpMethod.Post, url, body.ToJsonString(), HeadersBearer(token), ct);
        ParseResposta(retorno);
        return retorno;
    }

    public override async Task<RetornoWebService> ConsultarDetalheAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + $"/boletos/{boleto.NossoNumero}";

        return await SendAsync(HttpMethod.Get, url, null, HeadersBearer(token), ct);
    }

    public override async Task<RetornoWebService> BaixarAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + $"/boletos/{boleto.NossoNumero}/cancelar";
        var body  = new JsonObject { ["motivoCancelamento"] = "ACERTOS" };

        return await SendAsync(HttpMethod.Post, url, body.ToJsonString(), HeadersBearer(token), ct);
    }

    public override async Task<RetornoWebService> ConsultarAsync(
        FiltroConsulta filtro, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var qs    = MontarQsConsulta(filtro);
        var url   = GetBaseUrl(config.Ambiente) + "/boletos/sumario?" + qs;

        return await SendAsync(HttpMethod.Get, url, null, HeadersBearer(token), ct);
    }

    private static JsonObject MontarCorpoIncluir(Boleto boleto, Beneficiario beneficiario)
    {
        var obj = new JsonObject
        {
            ["seuNumero"]     = Truncar(boleto.NumeroDocumento, 15),
            ["valorNominal"]  = boleto.ValorDocumento,
            ["dataVencimento"]= boleto.Vencimento.ToString("yyyy-MM-dd"),
            ["numDiasAgenda"] = 60,
            ["pagador"] = new JsonObject
            {
                ["cpfCnpj"]    = SomenteNumeros(boleto.Pagador.CnpjCpf),
                ["tipoPessoa"] = boleto.Pagador.Pessoa == TipoPessoa.Fisica ? "FISICA" : "JURIDICA",
                ["nome"]       = Truncar(boleto.Pagador.Nome, 100),
                ["endereco"]   = Truncar(boleto.Pagador.Logradouro, 90),
                ["numero"]     = Truncar(boleto.Pagador.Numero, 10),
                ["bairro"]     = Truncar(boleto.Pagador.Bairro, 60),
                ["cidade"]     = Truncar(boleto.Pagador.Cidade, 60),
                ["uf"]         = boleto.Pagador.UF,
                ["cep"]        = SomenteNumeros(boleto.Pagador.CEP),
                ["email"]      = boleto.Pagador.Email
            }
        };

        if (boleto.ValorDesconto > 0 && boleto.CodigoDesconto != CodigoDesconto.SemDesconto)
        {
            obj["desconto"] = new JsonObject
            {
                ["codigoDesconto"] = boleto.CodigoDesconto == CodigoDesconto.Percentual ? "PERCENTUAL" : "VALOR_FIXO_DATA",
                ["data"]           = boleto.DataDesconto?.ToString("yyyy-MM-dd") ?? boleto.Vencimento.ToString("yyyy-MM-dd"),
                ["taxa"]           = boleto.PercentualDesconto,
                ["valor"]          = boleto.ValorDesconto
            };
        }

        if (boleto.CodigoMulta != CodigoMulta.Isento && boleto.ValorMulta > 0)
        {
            obj["multa"] = new JsonObject
            {
                ["codigoMulta"] = boleto.CodigoMulta == CodigoMulta.Percentual ? "PERCENTUAL" : "VALOR_FIXO",
                ["data"]        = boleto.Vencimento.AddDays(1).ToString("yyyy-MM-dd"),
                ["taxa"]        = boleto.PercentualMulta,
                ["valor"]       = boleto.ValorMulta
            };
        }

        if (boleto.CodigoJuros != CodigoJuros.Isento && boleto.ValorMoraJuros > 0)
        {
            obj["mora"] = new JsonObject
            {
                ["codigoMora"] = "TAXA_MENSAL",
                ["data"]       = boleto.Vencimento.AddDays(1).ToString("yyyy-MM-dd"),
                ["taxa"]       = boleto.PercentualMoraJuros,
                ["valor"]      = boleto.ValorMoraJuros
            };
        }

        return obj;
    }

    private static string MontarQsConsulta(FiltroConsulta filtro)
    {
        var parts = new List<string>();
        if (filtro.DataVencimento != null)
        {
            parts.Add($"dataInicio={filtro.DataVencimento.DataInicio:yyyy-MM-dd}");
            parts.Add($"dataFim={filtro.DataVencimento.DataFinal:yyyy-MM-dd}");
        }
        if (filtro.IndicadorSituacao != IndicadorSituacaoBoleto.Nenhum)
            parts.Add($"situacao={filtro.IndicadorSituacao}");
        return string.Join("&", parts);
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
            retorno.QrCodePix      = json?["pixCopiaECola"]?.GetValue<string>() ?? "";
        }
        catch { /* deixa retorno como está */ }
    }
}
