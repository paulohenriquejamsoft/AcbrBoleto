using System.Text.Json.Nodes;
using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;
using ACBrBoleto.WebServices.Base;
using Microsoft.Extensions.Caching.Memory;

namespace ACBrBoleto.WebServices.Caixa;

/// <summary>
/// WebService Caixa Econômica Federal — SIBAR SOAP.
/// Endpoint: https://barramento.caixa.gov.br/sibar/
/// Corresponde a TBoletoW_Caixa no Delphi.
/// NOTA: Este banco utiliza SOAP/XML. Esta implementação envolve o envelope
/// SOAP básico. Para uso completo em produção considere usar um cliente SOAP dedicado.
/// </summary>
public class CaixaWebService : BoletoWebServiceBase
{
    private const string UrlBase = "https://barramento.caixa.gov.br/sibar/";

    protected override string UrlProducao    => UrlBase;
    protected override string UrlHomologacao => UrlBase;
    protected override string UrlOAuthProducao    => "";  // SOAP: usa certificado cliente
    protected override string UrlOAuthHomologacao => "";

    public CaixaWebService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        : base(httpClientFactory, cache) { }

    public override string Nome => "Caixa Econômica Federal";
    public override TipoCobranca TipoCobranca => TipoCobranca.CaixaEconomica;

    // Caixa usa certificado mTLS + SOAP — sem OAuth token
    protected override Task<string> ObterNovoTokenAsync(WebServiceConfig config, CancellationToken ct)
        => Task.FromResult("n/a");

    public override async Task<RetornoWebService> IncluirAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var url      = UrlBase + "ManutencaoCobrancaBancaria/Boleto/Externo";
        var envelope = MontarEnvelopeIncluir(boleto, beneficiario);

        var headers = new List<(string, string)>
        {
            ("Content-Type", "text/xml;charset=UTF-8"),
            ("SOAPAction",   "incluirBoleto")
        };

        return await SendAsync(HttpMethod.Post, url, envelope, headers, ct);
    }

    public override async Task<RetornoWebService> ConsultarDetalheAsync(
        Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default)
    {
        var url      = UrlBase + "ConsultaCobrancaBancaria/Boleto";
        var envelope = MontarEnvelopeConsulta(boleto, beneficiario);

        var headers = new List<(string, string)>
        {
            ("Content-Type", "text/xml;charset=UTF-8"),
            ("SOAPAction",   "consultarBoleto")
        };

        return await SendAsync(HttpMethod.Post, url, envelope, headers, ct);
    }

    private static string MontarEnvelopeIncluir(Boleto boleto, Beneficiario beneficiario)
    {
        return $"""
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
                              xmlns:sib="http://caixa.gov.br/sibar"
                              xmlns:manutencaocobrancabancaria="http://caixa.gov.br/sibar/manutencao_cobranca_bancaria/boleto/externo">
              <soapenv:Header/>
              <soapenv:Body>
                <manutencaocobrancabancaria:SERVICO_ENTRADA>
                  <sib:HEADER>
                    <sib:VERSAO>220</sib:VERSAO>
                    <sib:AUTENTICACAO>{beneficiario.WebService.Token}</sib:AUTENTICACAO>
                    <sib:USUARIO_SERVICO>MANUTENCAO_BOLETO</sib:USUARIO_SERVICO>
                    <sib:OPERACAO>INCLUIR_BOLETO</sib:OPERACAO>
                    <sib:SISTEMA_ORIGEM>SISBR</sib:SISTEMA_ORIGEM>
                  </sib:HEADER>
                  <DADOS>
                    <BENEFICIARIO>
                      <NU_INSCRICAO>{SomenteNumeros(beneficiario.CnpjCpf)}</NU_INSCRICAO>
                      <NU_AGENCIA>{SomenteNumeros(beneficiario.Agencia)}</NU_AGENCIA>
                      <NU_CONVENIO>{SomenteNumeros(beneficiario.Convenio)}</NU_CONVENIO>
                    </BENEFICIARIO>
                    <TITULO>
                      <NU_SEQUENCIA>1</NU_SEQUENCIA>
                      <DT_VENCIMENTO>{boleto.Vencimento:yyyyMMdd}</DT_VENCIMENTO>
                      <VL_ORIGINAL>{boleto.ValorDocumento:F2}</VL_ORIGINAL>
                      <NU_SEU_NUMERO>{Truncar(boleto.NumeroDocumento, 15)}</NU_SEU_NUMERO>
                      <PAGADOR>
                        <TP_PESSOA>{(boleto.Pagador.Pessoa == TipoPessoa.Fisica ? "F" : "J")}</TP_PESSOA>
                        <NU_INSCRICAO>{SomenteNumeros(boleto.Pagador.CnpjCpf)}</NU_INSCRICAO>
                        <NM_PAGADOR>{Truncar(boleto.Pagador.Nome, 40)}</NM_PAGADOR>
                        <END_PAGADOR>{Truncar(boleto.Pagador.Logradouro, 40)}</END_PAGADOR>
                        <BAIRRO>{Truncar(boleto.Pagador.Bairro, 20)}</BAIRRO>
                        <CIDADE>{Truncar(boleto.Pagador.Cidade, 20)}</CIDADE>
                        <UF>{boleto.Pagador.UF}</UF>
                        <CEP>{SomenteNumeros(boleto.Pagador.CEP)}</CEP>
                      </PAGADOR>
                    </TITULO>
                  </DADOS>
                </manutencaocobrancabancaria:SERVICO_ENTRADA>
              </soapenv:Body>
            </soapenv:Envelope>
            """;
    }

    private static string MontarEnvelopeConsulta(Boleto boleto, Beneficiario beneficiario)
    {
        return $"""
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
                              xmlns:sib="http://caixa.gov.br/sibar"
                              xmlns:consultacobrancabancaria="http://caixa.gov.br/sibar/consulta_cobranca_bancaria/boleto">
              <soapenv:Header/>
              <soapenv:Body>
                <consultacobrancabancaria:SERVICO_ENTRADA>
                  <sib:HEADER>
                    <sib:VERSAO>220</sib:VERSAO>
                    <sib:AUTENTICACAO>{beneficiario.WebService.Token}</sib:AUTENTICACAO>
                    <sib:USUARIO_SERVICO>CONSULTA_BOLETO</sib:USUARIO_SERVICO>
                    <sib:OPERACAO>CONSULTAR_BOLETO</sib:OPERACAO>
                  </sib:HEADER>
                  <DADOS>
                    <BENEFICIARIO>
                      <NU_INSCRICAO>{SomenteNumeros(beneficiario.CnpjCpf)}</NU_INSCRICAO>
                      <NU_CONVENIO>{SomenteNumeros(beneficiario.Convenio)}</NU_CONVENIO>
                    </BENEFICIARIO>
                    <TITULO>
                      <NU_SEU_NUMERO>{Truncar(boleto.NumeroDocumento, 15)}</NU_SEU_NUMERO>
                    </TITULO>
                  </DADOS>
                </consultacobrancabancaria:SERVICO_ENTRADA>
              </soapenv:Body>
            </soapenv:Envelope>
            """;
    }
}
