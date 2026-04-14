using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;

namespace ACBrBoleto.WebServices.Base;

/// <summary>
/// Interface que cada implementação de WebService bancário deve satisfazer.
/// Corresponde a TBoletoWSClass no Delphi.
/// </summary>
public interface IBoletoWebService
{
    string Nome { get; }
    TipoCobranca TipoCobranca { get; }

    Task<RetornoWebService> IncluirAsync(Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default);
    Task<RetornoWebService> AlterarAsync(Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default);
    Task<RetornoWebService> BaixarAsync(Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default);
    Task<RetornoWebService> ConsultarAsync(FiltroConsulta filtro, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default);
    Task<RetornoWebService> ConsultarDetalheAsync(Boleto boleto, Beneficiario beneficiario, WebServiceConfig config, CancellationToken ct = default);
}
