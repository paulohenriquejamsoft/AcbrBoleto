using System.Text.Json.Nodes;
using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;
using ACBrBoleto.WebServices.Base;
using Microsoft.Extensions.Caching.Memory;

namespace ACBrBoleto.WebServices.Itau;

/// <summary>
/// WebService Itaú — Cash Management API v2 (REST/JSON).
/// Produção:    https://api.itau.com.br/cash_management/v2
/// Sandbox:     https://sandbox.devportal.itau.com.br/itau-ep9-gtw-cash-management-ext-v2/v2
/// OAuth Prod:  https://sts.itau.com.br/api/oauth/token
/// OAuth Sbox:  https://devportal.itau.com.br/api/jwt
/// Corresponde a TBoletoW_Itau_API no Delphi.
/// </summary>
public class ItauApiWebService : BoletoWebServiceBase
{
    protected override string UrlProducao    => "https://api.itau.com.br/cash_management/v2";
    protected override string UrlHomologacao => "https://sandbox.devportal.itau.com.br/itau-ep9-gtw-cash-management-ext-v2/v2";
    protected override string? UrlSandbox   => "https://sandbox.devportal.itau.com.br/itau-ep9-gtw-cash-management-ext-v2/v2";
    protected override string UrlOAuthProducao    => "https://sts.itau.com.br/api/oauth/token";
    protected override string UrlOAuthHomologacao => "https://devportal.itau.com.br/api/jwt";

    public ItauApiWebService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        : base(httpClientFactory, cache) { }

    public override string Nome => "Itaú API";
    public override TipoCobranca TipoCobranca => TipoCobranca.Itau;

    public override async Task<RetornoWebService> IncluirAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + "/boletos";
        var body  = MontarCorpoIncluir(boleto, beneficiario);

        var retorno = await SendAsync(HttpMethod.Post, url, body.ToJsonString(), HeadersItau(token, beneficiario), ct);
        ParseResposta(retorno);
        return retorno;
    }

    public override async Task<RetornoWebService> ConsultarDetalheAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + $"/boletos/{boleto.NossoNumero}";

        return await SendAsync(HttpMethod.Get, url, null, HeadersItau(token, beneficiario), ct);
    }

    public override async Task<RetornoWebService> BaixarAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + $"/boletos/{boleto.NossoNumero}/cancelamento";

        return await SendAsync(HttpMethod.Post, url, "{}", HeadersItau(token, beneficiario), ct);
    }

    public override async Task<RetornoWebService> ConsultarAsync(
        FiltroConsulta filtro, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var qs    = MontarQsConsulta(filtro);
        var url   = GetBaseUrl(config.Ambiente) + "/boletos?" + qs;

        return await SendAsync(HttpMethod.Get, url, null, HeadersItau(token, beneficiario), ct);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static IEnumerable<(string, string)> HeadersItau(string token, Beneficiario beneficiario) =>
    [
        ("Authorization", $"Bearer {token}"),
        ("Accept",        "application/json"),
        ("x-itau-apikey", beneficiario.WebService.AppKey)
    ];

    private static JsonObject MontarCorpoIncluir(Boleto boleto, Beneficiario beneficiario)
    {
        return new JsonObject
        {
            ["etapa_processo_boleto"] = "efetivacao",
            ["codigo_canal_operacao"] = "API",
            ["beneficiario"] = new JsonObject
            {
                ["id_beneficiario"] = beneficiario.CodigoCedente
            },
            ["dado_boleto"] = new JsonObject
            {
                ["descricao_instrumento_cobranca"] = "boleto",
                ["tipo_boleto"]                    = "a vista",
                ["codigo_carteira"]                = boleto.Carteira,
                ["valor_total_titulo"]             = boleto.ValorDocumento,
                ["data_emissao"]                   = boleto.DataDocumento.ToString("yyyy-MM-dd"),
                ["data_vencimento"]                = boleto.Vencimento.ToString("yyyy-MM-dd"),
                ["pagador"] = new JsonObject
                {
                    ["pessoa"] = new JsonObject
                    {
                        ["nome_pessoa"] = Truncar(boleto.Pagador.Nome, 45),
                        ["tipo_pessoa"] = new JsonObject
                        {
                            ["codigo_tipo_pessoa"] = boleto.Pagador.Pessoa == TipoPessoa.Fisica ? "F" : "J",
                            ["numero_cadastro_pessoa"] = SomenteNumeros(boleto.Pagador.CnpjCpf)
                        }
                    },
                    ["endereco"] = new JsonObject
                    {
                        ["nome_logradouro"]    = Truncar(boleto.Pagador.Logradouro, 90),
                        ["nome_bairro"]        = Truncar(boleto.Pagador.Bairro, 60),
                        ["nome_cidade"]        = Truncar(boleto.Pagador.Cidade, 60),
                        ["sigla_UF"]           = boleto.Pagador.UF,
                        ["numero_CEP"]         = SomenteNumeros(boleto.Pagador.CEP)
                    }
                }
            }
        };
    }

    private static string MontarQsConsulta(FiltroConsulta filtro)
    {
        var parts = new List<string>();
        if (filtro.DataVencimento != null)
        {
            parts.Add($"data_inicio={filtro.DataVencimento.DataInicio:yyyy-MM-dd}");
            parts.Add($"data_fim={filtro.DataVencimento.DataFinal:yyyy-MM-dd}");
        }
        parts.Add($"pagina={filtro.Pagina}");
        return string.Join("&", parts);
    }

    private static void ParseResposta(RetornoWebService retorno)
    {
        if (string.IsNullOrEmpty(retorno.RetornoWS)) return;
        try
        {
            var json = JsonNode.Parse(retorno.RetornoWS);
            var dados = json?["dado_boleto"];
            retorno.NossoNumero    = dados?["numero_nosso_numero"]?.GetValue<string>() ?? "";
            retorno.LinhaDigitavel = dados?["texto_linha_digitavel"]?.GetValue<string>() ?? "";
            retorno.CodigoBarras   = dados?["codigo_barras"]?.GetValue<string>() ?? "";
        }
        catch { /* deixa retorno como está */ }
    }
}
