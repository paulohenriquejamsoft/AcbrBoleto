using System.Text.Json.Nodes;
using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;
using ACBrBoleto.WebServices.Base;
using Microsoft.Extensions.Caching.Memory;

namespace ACBrBoleto.WebServices.Sicoob;

/// <summary>
/// WebService Sicoob — Cobrança Bancária API v3 (REST/JSON + OpenID Connect).
/// Produção:   https://api.sicoob.com.br/cobranca-bancaria/v3
/// Sandbox:    https://sandbox.sicoob.com.br/sicoob/sandbox/cobranca-bancaria/v3
/// OAuth:      https://auth.sicoob.com.br/auth/realms/cooperado/protocol/openid-connect/token
/// Corresponde a TBoletoW_Sicoob_V3 no Delphi.
/// </summary>
public class SicoobV3WebService : BoletoWebServiceBase
{
    protected override string UrlProducao    => "https://api.sicoob.com.br/cobranca-bancaria/v3";
    protected override string UrlHomologacao => "https://sandbox.sicoob.com.br/sicoob/sandbox/cobranca-bancaria/v3";
    protected override string? UrlSandbox   => "https://sandbox.sicoob.com.br/sicoob/sandbox/cobranca-bancaria/v3";
    protected override string UrlOAuthProducao    => "https://auth.sicoob.com.br/auth/realms/cooperado/protocol/openid-connect/token";
    protected override string UrlOAuthHomologacao => "https://auth.sicoob.com.br/auth/realms/cooperado/protocol/openid-connect/token";

    public SicoobV3WebService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        : base(httpClientFactory, cache) { }

    public override string Nome => "Sicoob V3";
    public override TipoCobranca TipoCobranca => TipoCobranca.BancoSicoob;

    private IEnumerable<(string, string)> HeadersSicoob(string token, WebServiceConfig config) =>
    [
        ("Authorization",  $"Bearer {token}"),
        ("Accept",         "application/json"),
        ("client_id",      config.ClientId)
    ];

    public override async Task<RetornoWebService> IncluirAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + "/boletos";
        var body  = MontarCorpoIncluir(boleto, beneficiario);

        var retorno = await SendAsync(HttpMethod.Post, url, body.ToJsonString(), HeadersSicoob(token, config), ct);
        ParseResposta(retorno);
        return retorno;
    }

    public override async Task<RetornoWebService> ConsultarDetalheAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + $"/boletos/{boleto.NossoNumero}";

        return await SendAsync(HttpMethod.Get, url, null, HeadersSicoob(token, config), ct);
    }

    public override async Task<RetornoWebService> BaixarAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + $"/boletos/{boleto.NossoNumero}/baixa";

        return await SendAsync(HttpMethod.Post, url, "{}", HeadersSicoob(token, config), ct);
    }

    public override async Task<RetornoWebService> ConsultarAsync(
        FiltroConsulta filtro, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var qs    = MontarQsConsulta(filtro, beneficiario);
        var url   = GetBaseUrl(config.Ambiente) + "/boletos?" + qs;

        return await SendAsync(HttpMethod.Get, url, null, HeadersSicoob(token, config), ct);
    }

    private static JsonObject MontarCorpoIncluir(Boleto boleto, Beneficiario beneficiario)
    {
        return new JsonObject
        {
            ["numeroContrato"]    = int.Parse(SomenteNumeros(beneficiario.Convenio)),
            ["modalidade"]        = int.Parse(beneficiario.Modalidade.PadLeft(2, '0')),
            ["numeroDocumento"]   = Truncar(boleto.NumeroDocumento, 15),
            ["dataVencimento"]    = boleto.Vencimento.ToString("yyyy-MM-dd"),
            ["valor"]             = boleto.ValorDocumento,
            ["codigoEspecieDocumento"] = 2,
            ["aceite"]            = boleto.Aceite == AceiteTitulo.Sim,
            ["pagador"] = new JsonObject
            {
                ["cpfCnpj"]    = SomenteNumeros(boleto.Pagador.CnpjCpf),
                ["nome"]       = Truncar(boleto.Pagador.Nome, 60),
                ["endereco"]   = Truncar(boleto.Pagador.Logradouro, 90),
                ["bairro"]     = Truncar(boleto.Pagador.Bairro, 40),
                ["cidade"]     = Truncar(boleto.Pagador.Cidade, 30),
                ["uf"]         = boleto.Pagador.UF,
                ["cep"]        = SomenteNumeros(boleto.Pagador.CEP)
            }
        };
    }

    private static string MontarQsConsulta(FiltroConsulta filtro, Beneficiario beneficiario)
    {
        var parts = new List<string>
        {
            $"numeroContrato={SomenteNumeros(beneficiario.Convenio)}"
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
            var result = json?["resultado"];
            retorno.NossoNumero    = result?["nossoNumero"]?.GetValue<string>() ?? "";
            retorno.LinhaDigitavel = result?["linhaDigitavel"]?.GetValue<string>() ?? "";
            retorno.CodigoBarras   = result?["codigoBarras"]?.GetValue<string>() ?? "";
        }
        catch { /* deixa retorno como está */ }
    }
}
