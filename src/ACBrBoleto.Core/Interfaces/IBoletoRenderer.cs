using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;

namespace ACBrBoleto.Core.Interfaces;

/// <summary>Renderizador de boleto para PDF/imagem.</summary>
public interface IBoletoRenderer
{
    Task<byte[]> GerarPdfAsync(Boleto boleto, Beneficiario beneficiario,
                               IBancoService banco, LayoutBoleto layout = LayoutBoleto.Padrao);

    Task<byte[]> GerarPdfLoteAsync(IEnumerable<Boleto> boletos, Beneficiario beneficiario,
                                   IBancoService banco, LayoutBoleto layout = LayoutBoleto.Padrao);
}
