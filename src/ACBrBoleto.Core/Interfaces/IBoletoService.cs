using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;

namespace ACBrBoleto.Core.Interfaces;

/// <summary>
/// Serviço principal de boletos. Corresponde a TACBrBoleto no Delphi.
/// </summary>
public interface IBoletoService
{
    Beneficiario Beneficiario { get; set; }
    IBancoService Banco { get; }
    IReadOnlyList<Boleto> Boletos { get; }
    LayoutRemessa LayoutRemessa { get; set; }

    void AdicionarBoleto(Boleto boleto);
    void RemoverBoleto(Boleto boleto);
    void LimparBoletos();

    void PreencherCodigoBarras(Boleto boleto);

    Task<string> GerarRemessaAsync(int numeroRemessa);
    string GerarRemessa(int numeroRemessa);

    void LerRetorno(string conteudo);
    Task LerRetornoAsync(string conteudo);

    Task<byte[]> GerarPdfAsync(Boleto boleto, LayoutBoleto layout = LayoutBoleto.Padrao);
    Task<byte[]> GerarPdfLoteAsync(IEnumerable<Boleto> boletos, LayoutBoleto layout = LayoutBoleto.Padrao);
}
