using ACBrBoleto.Core.Models;

namespace ACBrBoleto.Core.Interfaces;

/// <summary>Comunicação com API do banco.</summary>
public interface IBoletoWebService
{
    Task<RetornoWebService> RegistrarAsync(Boleto boleto, Beneficiario beneficiario);
    Task<RetornoWebService> AlterarAsync(Boleto boleto, Beneficiario beneficiario);
    Task<RetornoWebService> BaixarAsync(Boleto boleto, Beneficiario beneficiario);
    Task<RetornoWebService> ConsultarAsync(string nossoNumero, Beneficiario beneficiario);
    Task<RetornoWebService> CancelarAsync(Boleto boleto, Beneficiario beneficiario);
}
