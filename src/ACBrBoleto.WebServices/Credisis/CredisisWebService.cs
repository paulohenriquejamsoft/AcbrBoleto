using System.Text.Json.Nodes;
using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;
using ACBrBoleto.WebServices.Base;
using Microsoft.Extensions.Caching.Memory;

namespace ACBrBoleto.WebServices.Credisis;

/// <summary>
/// WebService Credisis — SOAP/XML (cobrança via WebService WSDL).
/// Endpoint: https://credisiscobranca.com.br/v2/ws?wsdl
/// Corresponde a TBoletoW_Credisis no Delphi.
/// NOTA: Esta implementação gera envelope SOAP básico.
/// Para uso completo em produção considere usar um cliente SOAP dedicado.
/// </summary>
public class CredisisWebService : BoletoWebServiceBase
{
    private const string UrlWsdl = "https://credisiscobranca.com.br/v2/ws";

    protected override string UrlProducao    => UrlWsdl;
    protected override string UrlHomologacao => UrlWsdl;
    protected override string UrlOAuthProducao    => "";  // SOAP: sem OAuth
    protected override string UrlOAuthHomologacao => "";

    public CredisisWebService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        : base(httpClientFactory, cache) { }

    public override string Nome => "Credisis";
    public override TipoCobranca TipoCobranca => TipoCobranca.CrediSIS;

    protected override Task<string> ObterNovoTokenAsync(WebServiceConfig config, CancellationToken ct)
        => Task.FromResult("n/a");

    public override async Task<RetornoWebService> IncluirAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var envelope = MontarEnvelopeIncluir(boleto, beneficiario);
        var headers = SoapHeaders("registrarBoleto");
        return await SendAsync(HttpMethod.Post, UrlWsdl, envelope, headers, ct);
    }

    public override async Task<RetornoWebService> ConsultarDetalheAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var envelope = MontarEnvelopeConsulta(boleto, beneficiario);
        var headers = SoapHeaders("consultarBoleto");
        return await SendAsync(HttpMethod.Post, UrlWsdl, envelope, headers, ct);
    }

    public override async Task<RetornoWebService> BaixarAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var envelope = MontarEnvelopeBaixa(boleto, beneficiario);
        var headers = SoapHeaders("cancelarBoleto");
        return await SendAsync(HttpMethod.Post, UrlWsdl, envelope, headers, ct);
    }

    private static IEnumerable<(string, string)> SoapHeaders(string action) =>
    [
        ("Content-Type", "text/xml;charset=UTF-8"),
        ("SOAPAction",   action)
    ];

    private static string MontarEnvelopeIncluir(Boleto boleto, Beneficiario beneficiario)
    {
        return $"""
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
                              xmlns:cred="http://credisis.com.br/cobranca/ws">
              <soapenv:Header/>
              <soapenv:Body>
                <cred:registrarBoleto>
                  <cred:usuario>{beneficiario.WebService.Usuario}</cred:usuario>
                  <cred:senha>{beneficiario.WebService.Senha}</cred:senha>
                  <cred:convenio>{SomenteNumeros(beneficiario.Convenio)}</cred:convenio>
                  <cred:nossoNumero>{Truncar(boleto.NossoNumero, 13)}</cred:nossoNumero>
                  <cred:seuNumero>{Truncar(boleto.NumeroDocumento, 10)}</cred:seuNumero>
                  <cred:dataVencimento>{boleto.Vencimento:dd/MM/yyyy}</cred:dataVencimento>
                  <cred:valor>{boleto.ValorDocumento:F2}</cred:valor>
                  <cred:pagadorNome>{Truncar(boleto.Pagador.Nome, 40)}</cred:pagadorNome>
                  <cred:pagadorCpfCnpj>{SomenteNumeros(boleto.Pagador.CnpjCpf)}</cred:pagadorCpfCnpj>
                  <cred:pagadorEndereco>{Truncar(boleto.Pagador.Logradouro, 40)}</cred:pagadorEndereco>
                  <cred:pagadorBairro>{Truncar(boleto.Pagador.Bairro, 20)}</cred:pagadorBairro>
                  <cred:pagadorCidade>{Truncar(boleto.Pagador.Cidade, 20)}</cred:pagadorCidade>
                  <cred:pagadorUF>{boleto.Pagador.UF}</cred:pagadorUF>
                  <cred:pagadorCEP>{SomenteNumeros(boleto.Pagador.CEP)}</cred:pagadorCEP>
                </cred:registrarBoleto>
              </soapenv:Body>
            </soapenv:Envelope>
            """;
    }

    private static string MontarEnvelopeConsulta(Boleto boleto, Beneficiario beneficiario)
    {
        return $"""
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
                              xmlns:cred="http://credisis.com.br/cobranca/ws">
              <soapenv:Header/>
              <soapenv:Body>
                <cred:consultarBoleto>
                  <cred:usuario>{beneficiario.WebService.Usuario}</cred:usuario>
                  <cred:senha>{beneficiario.WebService.Senha}</cred:senha>
                  <cred:convenio>{SomenteNumeros(beneficiario.Convenio)}</cred:convenio>
                  <cred:nossoNumero>{Truncar(boleto.NossoNumero, 13)}</cred:nossoNumero>
                </cred:consultarBoleto>
              </soapenv:Body>
            </soapenv:Envelope>
            """;
    }

    private static string MontarEnvelopeBaixa(Boleto boleto, Beneficiario beneficiario)
    {
        return $"""
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
                              xmlns:cred="http://credisis.com.br/cobranca/ws">
              <soapenv:Header/>
              <soapenv:Body>
                <cred:cancelarBoleto>
                  <cred:usuario>{beneficiario.WebService.Usuario}</cred:usuario>
                  <cred:senha>{beneficiario.WebService.Senha}</cred:senha>
                  <cred:convenio>{SomenteNumeros(beneficiario.Convenio)}</cred:convenio>
                  <cred:nossoNumero>{Truncar(boleto.NossoNumero, 13)}</cred:nossoNumero>
                  <cred:motivo>01</cred:motivo>
                </cred:cancelarBoleto>
              </soapenv:Body>
            </soapenv:Envelope>
            """;
    }
}
