using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;
using Microsoft.Extensions.Caching.Memory;

namespace ACBrBoleto.WebServices.Base;

/// <summary>
/// Classe base abstrata para todos os WebServices bancários.
/// Gerencia autenticação OAuth2 com cache de token e envio HTTP.
/// Corresponde a TBoletoWSREST no Delphi.
/// </summary>
public abstract class BoletoWebServiceBase : IBoletoWebService
{
    protected readonly IHttpClientFactory _httpClientFactory;
    protected readonly IMemoryCache _cache;

    protected BoletoWebServiceBase(IHttpClientFactory httpClientFactory, IMemoryCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
    }

    // ──────────────────────────────────────────────────────────────
    // Identidade
    // ──────────────────────────────────────────────────────────────
    public abstract string Nome { get; }
    public abstract TipoCobranca TipoCobranca { get; }

    // ──────────────────────────────────────────────────────────────
    // URLs — cada banco sobrescreve
    // ──────────────────────────────────────────────────────────────
    protected abstract string UrlProducao { get; }
    protected abstract string UrlHomologacao { get; }
    protected virtual string? UrlSandbox => null;
    protected abstract string UrlOAuthProducao { get; }
    protected abstract string UrlOAuthHomologacao { get; }

    protected string GetBaseUrl(TipoAmbienteWebService ambiente) => ambiente switch
    {
        TipoAmbienteWebService.Producao  => UrlProducao,
        TipoAmbienteWebService.Sandbox   => UrlSandbox ?? UrlHomologacao,
        _                                => UrlHomologacao
    };

    protected string GetOAuthUrl(TipoAmbienteWebService ambiente) =>
        ambiente == TipoAmbienteWebService.Producao ? UrlOAuthProducao : UrlOAuthHomologacao;

    // ──────────────────────────────────────────────────────────────
    // OAuth2 — token cacheado por ~55 minutos
    // ──────────────────────────────────────────────────────────────
    protected async Task<string> GetTokenAsync(WebServiceConfig config, CancellationToken ct)
    {
        string cacheKey = $"ws_token_{TipoCobranca}_{config.Ambiente}_{config.ClientId}";
        if (_cache.TryGetValue(cacheKey, out string? cached) && cached != null)
            return cached;

        var token = await ObterNovoTokenAsync(config, ct);
        _cache.Set(cacheKey, token, TimeSpan.FromMinutes(55));
        return token;
    }

    /// <summary>
    /// Obtém um novo token OAuth2 via client_credentials com Basic Auth.
    /// Bancos com fluxo diferente devem sobrescrever este método.
    /// </summary>
    protected virtual async Task<string> ObterNovoTokenAsync(WebServiceConfig config, CancellationToken ct)
    {
        using var client = _httpClientFactory.CreateClient();
        client.Timeout = config.Timeout;

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{config.ClientId}:{config.ClientSecret}"));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);

        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"]    = "client_credentials",
            ["client_id"]     = config.ClientId,
            ["client_secret"] = config.ClientSecret
        });

        var url  = GetOAuthUrl(config.Ambiente);
        var resp = await client.PostAsync(url, body, ct);
        resp.EnsureSuccessStatusCode();

        var json = JsonNode.Parse(await resp.Content.ReadAsStringAsync(ct));
        return json?["access_token"]?.GetValue<string>()
               ?? throw new WebServiceException("Token não retornado pelo servidor de autorização.");
    }

    // ──────────────────────────────────────────────────────────────
    // HTTP helpers
    // ──────────────────────────────────────────────────────────────

    /// <summary>Envia uma requisição HTTP e devolve <see cref="RetornoWebService"/>.</summary>
    protected async Task<RetornoWebService> SendAsync(
        HttpMethod method,
        string url,
        string? jsonBody,
        IEnumerable<(string Key, string Value)> headers,
        CancellationToken ct)
    {
        using var client = _httpClientFactory.CreateClient();
        using var req    = new HttpRequestMessage(method, url);

        foreach (var (k, v) in headers)
            req.Headers.TryAddWithoutValidation(k, v);

        if (jsonBody != null)
            req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var resp    = await client.SendAsync(req, ct);
        var content = await resp.Content.ReadAsStringAsync(ct);

        return new RetornoWebService
        {
            Sucesso       = resp.IsSuccessStatusCode,
            RetornoWS     = content,
            CodigoRetorno = ((int)resp.StatusCode).ToString()
        };
    }

    /// <summary>Constrói cabeçalhos padrão Bearer + Accept JSON.</summary>
    protected static IEnumerable<(string, string)> HeadersBearer(string token) =>
    [
        ("Authorization", $"Bearer {token}"),
        ("Accept",        "application/json")
    ];

    /// <summary>Constrói cabeçalhos com API Key.</summary>
    protected static IEnumerable<(string, string)> HeadersApiKey(string apiKey, string headerName = "access_token") =>
    [
        (headerName, apiKey),
        ("Accept",   "application/json")
    ];

    // ──────────────────────────────────────────────────────────────
    // Operações — implementação padrão retorna NotImplemented
    // ──────────────────────────────────────────────────────────────
    public virtual Task<RetornoWebService> IncluirAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
        => Task.FromResult(RetornoWebService.Erro("Operação Incluir não implementada para este banco."));

    public virtual Task<RetornoWebService> AlterarAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
        => Task.FromResult(RetornoWebService.Erro("Operação Alterar não implementada para este banco."));

    public virtual Task<RetornoWebService> BaixarAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
        => Task.FromResult(RetornoWebService.Erro("Operação Baixar não implementada para este banco."));

    public virtual Task<RetornoWebService> ConsultarAsync(
        FiltroConsulta filtro, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
        => Task.FromResult(RetornoWebService.Erro("Operação Consultar não implementada para este banco."));

    public virtual Task<RetornoWebService> ConsultarDetalheAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
        => Task.FromResult(RetornoWebService.Erro("Operação ConsultarDetalhe não implementada para este banco."));

    // ──────────────────────────────────────────────────────────────
    // Utilitários de string/número
    // ──────────────────────────────────────────────────────────────
    protected static string SomenteNumeros(string? s) =>
        string.IsNullOrEmpty(s) ? "" : new string(s.Where(char.IsDigit).ToArray());

    protected static string Truncar(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : s[..Math.Min(s.Length, max)];
}
