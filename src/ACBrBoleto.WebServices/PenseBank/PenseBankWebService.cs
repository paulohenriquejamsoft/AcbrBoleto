using System.Text.Json.Nodes;
using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;
using ACBrBoleto.WebServices.Base;
using Microsoft.Extensions.Caching.Memory;

namespace ACBrBoleto.WebServices.PenseBank;

/// <summary>
/// WebService PenseBank — API REST/JSON.
/// Produção:   https://pensebank.com.br
/// Homolog.:   https://sandbox.pensebank.com.br
/// Corresponde a TBoletoW_PenseBank_API no Delphi.
/// </summary>
public class PenseBankWebService : BoletoWebServiceBase
{
    protected override string UrlProducao    => "https://pensebank.com.br";
    protected override string UrlHomologacao => "https://sandbox.pensebank.com.br";
    protected override string? UrlSandbox   => "https://sandbox.pensebank.com.br";
    // PenseBank usa o mesmo base URL para OAuth
    protected override string UrlOAuthProducao    => "https://pensebank.com.br/oauth/token";
    protected override string UrlOAuthHomologacao => "https://sandbox.pensebank.com.br/oauth/token";

    public PenseBankWebService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        : base(httpClientFactory, cache) { }

    public override string Nome => "PenseBank API";
    public override TipoCobranca TipoCobranca => TipoCobranca.PenseBankAPI;

    public override async Task<RetornoWebService> IncluirAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + "/api/v1/boletos";
        var body  = MontarCorpoIncluir(boleto, beneficiario);

        var retorno = await SendAsync(HttpMethod.Post, url, body.ToJsonString(), HeadersBearer(token), ct);
        ParseResposta(retorno);
        return retorno;
    }

    public override async Task<RetornoWebService> ConsultarDetalheAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + $"/api/v1/boletos/{boleto.NossoNumero}";

        return await SendAsync(HttpMethod.Get, url, null, HeadersBearer(token), ct);
    }

    public override async Task<RetornoWebService> BaixarAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + $"/api/v1/boletos/{boleto.NossoNumero}/baixar";

        return await SendAsync(HttpMethod.Post, url, "{}", HeadersBearer(token), ct);
    }

    private static JsonObject MontarCorpoIncluir(Boleto boleto, Beneficiario beneficiario)
    {
        return new JsonObject
        {
            ["beneficiario"] = new JsonObject
            {
                ["codigoCedente"] = beneficiario.CodigoCedente
            },
            ["seuNumero"]      = Truncar(boleto.NumeroDocumento, 15),
            ["nossoNumero"]    = Truncar(boleto.NossoNumero, 13),
            ["dataVencimento"] = boleto.Vencimento.ToString("yyyy-MM-dd"),
            ["valor"]          = boleto.ValorDocumento,
            ["pagador"] = new JsonObject
            {
                ["tipoPessoa"]   = boleto.Pagador.Pessoa == TipoPessoa.Fisica ? "F" : "J",
                ["cpfCnpj"]      = SomenteNumeros(boleto.Pagador.CnpjCpf),
                ["nome"]         = Truncar(boleto.Pagador.Nome, 40),
                ["logradouro"]   = Truncar(boleto.Pagador.Logradouro, 40),
                ["numero"]       = Truncar(boleto.Pagador.Numero, 10),
                ["bairro"]       = Truncar(boleto.Pagador.Bairro, 30),
                ["cidade"]       = Truncar(boleto.Pagador.Cidade, 20),
                ["uf"]           = boleto.Pagador.UF,
                ["cep"]          = SomenteNumeros(boleto.Pagador.CEP)
            }
        };
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
