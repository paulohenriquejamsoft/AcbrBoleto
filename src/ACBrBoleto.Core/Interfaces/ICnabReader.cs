using ACBrBoleto.Core.Models;

namespace ACBrBoleto.Core.Interfaces;

/// <summary>Leitor de arquivo CNAB (retorno).</summary>
public interface ICnabReader
{
    IList<Boleto> Ler(string conteudo, Beneficiario beneficiario, IBancoService banco);
}
