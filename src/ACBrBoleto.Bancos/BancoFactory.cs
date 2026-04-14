using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Interfaces;
using ACBrBoleto.Bancos.BancoDoBrasil;
using ACBrBoleto.Bancos.Bradesco;
using ACBrBoleto.Bancos.Caixa;
using ACBrBoleto.Bancos.Itau;
using ACBrBoleto.Bancos.Santander;

namespace ACBrBoleto.Bancos;

/// <summary>
/// Cria a implementação correta de IBancoService para um TipoCobranca.
/// Corresponde ao switch interno de SetTipoCobranca no Delphi.
/// </summary>
public static class BancoFactory
{
    public static IBancoService Create(TipoCobranca tipo) => tipo switch
    {
        TipoCobranca.BancoDoBrasil      => new BancoBrasil(),
        TipoCobranca.BancoDoBrasilAPI   => new BancoBrasilApi(),
        TipoCobranca.Bradesco           => new BancoBradesco(),
        TipoCobranca.BradescoSICOOB    => new BancoBradescoSicoob(),
        TipoCobranca.Itau              => new BancoItau(),
        TipoCobranca.Santander         => new BancoSantander(),
        TipoCobranca.CaixaEconomica    => new BancoCaixa(),
        TipoCobranca.CaixaSicob        => new BancoCaixaSicob(),
        _ => throw new NotSupportedException(
            $"Banco '{tipo}' ainda não migrado para C#. Consulte docs/migracao/05-FASES-MIGRACAO.md.")
    };
}
