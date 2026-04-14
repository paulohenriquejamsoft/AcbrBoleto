using System.Text.Json.Nodes;
using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;
using ACBrBoleto.WebServices.Base;
using Microsoft.Extensions.Caching.Memory;

namespace ACBrBoleto.WebServices.BancoBrasil;

/// <summary>
/// WebService do Banco do Brasil — API de Cobrança v2 (REST/JSON).
/// Documentação: https://apoio.developers.bb.com.br
/// Corresponde a TBoletoW_BancoBrasil_API no Delphi.
/// </summary>
public class BancoBrasilApiWebService : BoletoWebServiceBase
{
    // ── URLs ──────────────────────────────────────────────────────
    protected override string UrlProducao    => "https://api.bb.com.br/cobrancas/v2";
    protected override string UrlHomologacao => "https://api.hm.bb.com.br/cobrancas/v2";
    protected override string? UrlSandbox   => "https://api.sandbox.bb.com.br/cobrancas/v2";
    protected override string UrlOAuthProducao    => "https://oauth.bb.com.br/oauth/token";
    protected override string UrlOAuthHomologacao => "https://oauth.sandbox.bb.com.br/oauth/token";

    public BancoBrasilApiWebService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        : base(httpClientFactory, cache) { }

    public override string Nome => "Banco do Brasil API";
    public override TipoCobranca TipoCobranca => TipoCobranca.BancoDoBrasilAPI;

    // ── OAuth: BB usa Basic auth + form-urlencoded (padrão da base) ──

    // ── Helpers de URL ─────────────────────────────────────────────
    private string DevAppParam(WebServiceConfig config) =>
        $"?gw-dev-app-key={config.KeyUser}";

    private string BoletoPath(string nossoNumero, string devApp) =>
        $"/boletos/{nossoNumero}{devApp}";

    // ── Operações ─────────────────────────────────────────────────

    public override async Task<RetornoWebService> IncluirAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token  = await GetTokenAsync(config, ct);
        var devApp = DevAppParam(config);
        var url    = GetBaseUrl(config.Ambiente) + "/boletos" + devApp;
        var body   = MontarCorpoIncluir(boleto, beneficiario);

        var retorno = await SendAsync(HttpMethod.Post, url, body.ToJsonString(), HeadersBearer(token), ct);
        ParseResposta(retorno);
        return retorno;
    }

    public override async Task<RetornoWebService> AlterarAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token  = await GetTokenAsync(config, ct);
        var devApp = DevAppParam(config);
        var url    = GetBaseUrl(config.Ambiente) + BoletoPath(boleto.NossoNumero, devApp);
        var body   = MontarCorpoAlterar(boleto);

        return await SendAsync(HttpMethod.Patch, url, body.ToJsonString(), HeadersBearer(token), ct);
    }

    public override async Task<RetornoWebService> BaixarAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token  = await GetTokenAsync(config, ct);
        var devApp = DevAppParam(config);
        var url    = GetBaseUrl(config.Ambiente) + $"/boletos/{boleto.NossoNumero}/baixar{devApp}";

        return await SendAsync(HttpMethod.Post, url, "{}", HeadersBearer(token), ct);
    }

    public override async Task<RetornoWebService> ConsultarAsync(
        FiltroConsulta filtro, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token  = await GetTokenAsync(config, ct);
        var devApp = DevAppParam(config);
        var qs     = MontarQueryStringConsulta(filtro, beneficiario);
        var url    = GetBaseUrl(config.Ambiente) + "/boletos" + devApp + "&" + qs;

        return await SendAsync(HttpMethod.Get, url, null, HeadersBearer(token), ct);
    }

    public override async Task<RetornoWebService> ConsultarDetalheAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token     = await GetTokenAsync(config, ct);
        var devApp    = DevAppParam(config);
        var convenio  = SomenteNumeros(beneficiario.Convenio);
        var url       = GetBaseUrl(config.Ambiente)
                      + $"/boletos/{boleto.NossoNumero}{devApp}&numeroConvenio={convenio}";

        var retorno = await SendAsync(HttpMethod.Get, url, null, HeadersBearer(token), ct);
        ParseResposta(retorno);
        return retorno;
    }

    // ── Montagem de corpo JSON ─────────────────────────────────────

    private static JsonObject MontarCorpoIncluir(Boleto boleto, Beneficiario beneficiario)
    {
        var obj = new JsonObject
        {
            ["numeroConvenio"]             = int.Parse(SomenteNumeros(beneficiario.Convenio)),
            ["numeroCarteira"]             = int.Parse(boleto.Carteira),
            ["numeroVariacaoCarteira"]     = int.Parse(boleto.CarteiraFormatada.PadLeft(3, '0')[..3]),
            ["codigoModalidade"]           = 1,
            ["dataEmissao"]                = boleto.DataDocumento.ToString("dd.MM.yyyy"),
            ["dataVencimento"]             = boleto.Vencimento.ToString("dd.MM.yyyy"),
            ["valorOriginal"]              = boleto.ValorDocumento,
            ["codigoAceite"]               = boleto.Aceite == AceiteTitulo.Sim ? "A" : "N",
            ["codigoTipoTitulo"]           = 2,
            ["indicadorPermissaoRecebimentoParcial"] = "N",
            ["numeroTituloCliente"]        = Truncar(boleto.NossoNumero, 20),
            ["textoCampoUtilizacaoCedente"]= Truncar(boleto.UsoCedente, 25),
            ["indicadorPix"]               = "N",
            ["pagador"]                    = new JsonObject
            {
                ["tipoInscricao"]   = boleto.Pagador.Pessoa == TipoPessoa.Fisica ? 1 : 2,
                ["numeroInscricao"] = long.Parse(SomenteNumeros(boleto.Pagador.CnpjCpf)),
                ["nome"]            = Truncar(boleto.Pagador.Nome, 60),
                ["endereco"]        = Truncar(boleto.Pagador.Logradouro, 60),
                ["cep"]             = int.Parse(SomenteNumeros(boleto.Pagador.CEP)),
                ["cidade"]          = Truncar(boleto.Pagador.Cidade, 20),
                ["bairro"]          = Truncar(boleto.Pagador.Bairro, 20),
                ["uf"]              = boleto.Pagador.UF,
                ["telefone"]        = SomenteNumeros(boleto.Pagador.Fone)
            }
        };

        // Juros / mora
        if (boleto.CodigoJuros != CodigoJuros.Isento && boleto.ValorMoraJuros > 0)
        {
            obj["jurosMora"] = new JsonObject
            {
                ["tipo"]  = boleto.CodigoJuros == CodigoJuros.TaxaMensal ? "PERCENTUAL_MENSAL" : "VALOR_DIA",
                ["valor"] = boleto.ValorMoraJuros
            };
        }

        // Multa
        if (boleto.CodigoMulta != CodigoMulta.Isento && boleto.ValorMulta > 0)
        {
            obj["multa"] = new JsonObject
            {
                ["tipo"]  = boleto.CodigoMulta == CodigoMulta.Percentual ? "PERCENTUAL" : "VALOR_FIXO",
                ["valor"] = boleto.ValorMulta
            };
        }

        // Desconto
        if (boleto.CodigoDesconto != CodigoDesconto.SemDesconto && boleto.ValorDesconto > 0)
        {
            obj["desconto"] = new JsonObject
            {
                ["tipo"]  = boleto.CodigoDesconto == CodigoDesconto.Percentual ? "PERCENTUAL" : "VALOR_FIXO",
                ["dataExpiracao"] = boleto.DataDesconto?.ToString("dd.MM.yyyy") ?? "",
                ["valor"] = boleto.ValorDesconto
            };
        }

        return obj;
    }

    private static JsonObject MontarCorpoAlterar(Boleto boleto)
    {
        return new JsonObject
        {
            ["dataVencimento"]  = boleto.Vencimento.ToString("dd.MM.yyyy"),
            ["novoValorNominal"]= boleto.ValorDocumento
        };
    }

    private static string MontarQueryStringConsulta(FiltroConsulta filtro, Beneficiario beneficiario)
    {
        var parts = new List<string>
        {
            $"numeroConvenio={SomenteNumeros(beneficiario.Convenio)}"
        };

        if (filtro.DataVencimento != null)
        {
            parts.Add($"dataInicioVencimento={filtro.DataVencimento.DataInicio:dd.MM.yyyy}");
            parts.Add($"dataFinalVencimento={filtro.DataVencimento.DataFinal:dd.MM.yyyy}");
        }

        if (filtro.IndicadorSituacao != IndicadorSituacaoBoleto.Nenhum)
            parts.Add($"indicadorSituacao={filtro.IndicadorSituacao.ToString()[0]}");

        parts.Add($"indice={filtro.Pagina}");

        return string.Join("&", parts);
    }

    private static void ParseResposta(RetornoWebService retorno)
    {
        if (string.IsNullOrEmpty(retorno.RetornoWS)) return;
        try
        {
            var json = JsonNode.Parse(retorno.RetornoWS);
            retorno.NossoNumero    = json?["numero"]?.GetValue<string>() ?? "";
            retorno.LinhaDigitavel = json?["linhaDigitavel"]?.GetValue<string>() ?? "";
            retorno.CodigoBarras   = json?["codigoBarraNumerico"]?.GetValue<string>() ?? "";
            retorno.QrCodePix      = json?["pixCopiaECola"]?.GetValue<string>() ?? "";
        }
        catch { /* deixa retorno como está */ }
    }
}
