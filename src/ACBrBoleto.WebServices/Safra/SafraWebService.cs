using System.Text.Json.Nodes;
using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;
using ACBrBoleto.WebServices.Base;
using Microsoft.Extensions.Caching.Memory;

namespace ACBrBoleto.WebServices.Safra;

/// <summary>
/// WebService Banco Safra — Cobranças Gateway API v1 (REST/JSON).
/// Produção:   https://api.safranegocios.com.br/gateway/cobrancas/v1
/// Homolog.:   https://api-hml.safranegocios.com.br/gateway/cobrancas/v1
/// OAuth Prod: https://api.safranegocios.com.br/gateway/v1/oauth2/token
/// OAuth Hom:  https://api-hml.safranegocios.com.br/gateway/v1/oauth2/token
/// Corresponde a TBoletoW_Safra no Delphi.
/// </summary>
public class SafraWebService : BoletoWebServiceBase
{
    // Note: the Delphi source has a leading space in prod URL — trimmed here
    protected override string UrlProducao    => "https://api.safranegocios.com.br/gateway/cobrancas/v1";
    protected override string UrlHomologacao => "https://api-hml.safranegocios.com.br/gateway/cobrancas/v1";
    protected override string UrlOAuthProducao    => "https://api.safranegocios.com.br/gateway/v1/oauth2/token";
    protected override string UrlOAuthHomologacao => "https://api-hml.safranegocios.com.br/gateway/v1/oauth2/token";

    public SafraWebService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        : base(httpClientFactory, cache) { }

    public override string Nome => "Banco Safra";
    public override TipoCobranca TipoCobranca => TipoCobranca.BancoSafra;

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

    public override async Task<RetornoWebService> AlterarAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + $"/boletos/{boleto.NossoNumero}";
        var body  = new JsonObject
        {
            ["dataVencimento"] = boleto.Vencimento.ToString("yyyy-MM-dd"),
            ["valor"]          = boleto.ValorDocumento
        };

        return await SendAsync(HttpMethod.Put, url, body.ToJsonString(), HeadersBearer(token), ct);
    }

    public override async Task<RetornoWebService> BaixarAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + $"/boletos/{boleto.NossoNumero}/baixa";

        return await SendAsync(HttpMethod.Post, url, "{}", HeadersBearer(token), ct);
    }

    public override async Task<RetornoWebService> ConsultarAsync(
        FiltroConsulta filtro, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var qs    = MontarQsConsulta(filtro, beneficiario);
        var url   = GetBaseUrl(config.Ambiente) + "/boletos?" + qs;

        return await SendAsync(HttpMethod.Get, url, null, HeadersBearer(token), ct);
    }

    private static JsonObject MontarCorpoIncluir(Boleto boleto, Beneficiario beneficiario)
    {
        var obj = new JsonObject
        {
            ["convenio"]       = SomenteNumeros(beneficiario.Convenio),
            ["carteira"]       = boleto.Carteira,
            ["seuNumero"]      = Truncar(boleto.NumeroDocumento, 15),
            ["nossoNumero"]    = Truncar(boleto.NossoNumero, 13),
            ["dataEmissao"]    = boleto.DataDocumento.ToString("yyyy-MM-dd"),
            ["dataVencimento"] = boleto.Vencimento.ToString("yyyy-MM-dd"),
            ["valor"]          = boleto.ValorDocumento,
            ["pagador"] = new JsonObject
            {
                ["tipoPessoa"] = boleto.Pagador.Pessoa == TipoPessoa.Fisica ? "F" : "J",
                ["cpfCnpj"]   = SomenteNumeros(boleto.Pagador.CnpjCpf),
                ["nome"]      = Truncar(boleto.Pagador.Nome, 40),
                ["endereco"]  = Truncar(boleto.Pagador.Logradouro, 40),
                ["numero"]    = Truncar(boleto.Pagador.Numero, 10),
                ["bairro"]    = Truncar(boleto.Pagador.Bairro, 30),
                ["cidade"]    = Truncar(boleto.Pagador.Cidade, 20),
                ["uf"]        = boleto.Pagador.UF,
                ["cep"]       = SomenteNumeros(boleto.Pagador.CEP)
            }
        };

        if (boleto.CodigoMulta != CodigoMulta.Isento && boleto.ValorMulta > 0)
        {
            obj["multa"] = new JsonObject
            {
                ["tipo"]  = boleto.CodigoMulta == CodigoMulta.Percentual ? "P" : "V",
                ["valor"] = boleto.ValorMulta
            };
        }

        if (boleto.CodigoJuros != CodigoJuros.Isento && boleto.ValorMoraJuros > 0)
        {
            obj["juros"] = new JsonObject
            {
                ["tipo"]  = "P",
                ["valor"] = boleto.PercentualMoraJuros
            };
        }

        return obj;
    }

    private static string MontarQsConsulta(FiltroConsulta filtro, Beneficiario beneficiario)
    {
        var parts = new List<string>
        {
            $"convenio={SomenteNumeros(beneficiario.Convenio)}"
        };
        if (filtro.DataVencimento != null)
        {
            parts.Add($"dataVencimentoInicio={filtro.DataVencimento.DataInicio:yyyy-MM-dd}");
            parts.Add($"dataVencimentoFim={filtro.DataVencimento.DataFinal:yyyy-MM-dd}");
        }
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
        }
        catch { /* deixa retorno como está */ }
    }
}
