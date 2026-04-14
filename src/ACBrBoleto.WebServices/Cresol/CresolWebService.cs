using System.Text.Json.Nodes;
using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;
using ACBrBoleto.WebServices.Base;
using Microsoft.Extensions.Caching.Memory;

namespace ACBrBoleto.WebServices.Cresol;

/// <summary>
/// WebService Cresol — API Governarti (REST/JSON + OpenID Connect).
/// Produção:   https://cresolapi.governarti.com.br/
/// Homolog.:   https://api-dev.governarti.com.br/
/// OAuth Prod: https://cresolauth.governarti.com.br/auth/realms/cresol/protocol/openid-connect/token
/// OAuth Hom:  https://auth-dev.governarti.com.br/auth/realms/cresol/protocol/openid-connect/token
/// Corresponde a TBoletoW_Cresol no Delphi.
/// </summary>
public class CresolWebService : BoletoWebServiceBase
{
    protected override string UrlProducao    => "https://cresolapi.governarti.com.br";
    protected override string UrlHomologacao => "https://api-dev.governarti.com.br";
    protected override string UrlOAuthProducao    => "https://cresolauth.governarti.com.br/auth/realms/cresol/protocol/openid-connect/token";
    protected override string UrlOAuthHomologacao => "https://auth-dev.governarti.com.br/auth/realms/cresol/protocol/openid-connect/token";

    public CresolWebService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        : base(httpClientFactory, cache) { }

    public override string Nome => "Cresol";
    public override TipoCobranca TipoCobranca => TipoCobranca.BancoCresolSCRS;

    public override async Task<RetornoWebService> IncluirAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + "/cobranca/v1/boletos";
        var body  = MontarCorpoIncluir(boleto, beneficiario);

        var retorno = await SendAsync(HttpMethod.Post, url, body.ToJsonString(), HeadersBearer(token), ct);
        ParseResposta(retorno);
        return retorno;
    }

    public override async Task<RetornoWebService> ConsultarDetalheAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + $"/cobranca/v1/boletos/{boleto.NossoNumero}";

        return await SendAsync(HttpMethod.Get, url, null, HeadersBearer(token), ct);
    }

    public override async Task<RetornoWebService> BaixarAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(config, ct);
        var url   = GetBaseUrl(config.Ambiente) + $"/cobranca/v1/boletos/{boleto.NossoNumero}/baixa";

        return await SendAsync(HttpMethod.Post, url, "{}", HeadersBearer(token), ct);
    }

    private static JsonObject MontarCorpoIncluir(Boleto boleto, Beneficiario beneficiario)
    {
        return new JsonObject
        {
            ["convenio"]       = SomenteNumeros(beneficiario.Convenio),
            ["seuNumero"]      = Truncar(boleto.NumeroDocumento, 15),
            ["dataVencimento"] = boleto.Vencimento.ToString("yyyy-MM-dd"),
            ["valor"]          = boleto.ValorDocumento,
            ["pagador"] = new JsonObject
            {
                ["cpfCnpj"]  = SomenteNumeros(boleto.Pagador.CnpjCpf),
                ["nome"]     = Truncar(boleto.Pagador.Nome, 40),
                ["endereco"] = Truncar(boleto.Pagador.Logradouro, 40),
                ["bairro"]   = Truncar(boleto.Pagador.Bairro, 20),
                ["cidade"]   = Truncar(boleto.Pagador.Cidade, 20),
                ["uf"]       = boleto.Pagador.UF,
                ["cep"]      = SomenteNumeros(boleto.Pagador.CEP)
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
