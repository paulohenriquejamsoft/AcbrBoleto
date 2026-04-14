using ACBrBoleto.Core.Models;

namespace ACBrBoleto.Core.Interfaces;

/// <summary>Gerador de arquivo CNAB (remessa).</summary>
public interface ICnabWriter
{
    string Gerar(IEnumerable<Boleto> boletos, Beneficiario beneficiario,
                 IBancoService banco, int numeroRemessa);
}
