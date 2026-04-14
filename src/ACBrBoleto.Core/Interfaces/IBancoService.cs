using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;

namespace ACBrBoleto.Core.Interfaces;

/// <summary>
/// Contrato para implementações bancárias.
/// Corresponde a TACBrBancoClass (abstrata) no Delphi.
/// </summary>
public interface IBancoService
{
    int Numero { get; }
    int Digito { get; }
    string Nome { get; }
    TipoCobranca TipoCobranca { get; }
    int TamanhoMaximoNossoNumero { get; }
    int TamanhoAgencia { get; }
    int TamanhoConta { get; }
    int TamanhoCarteira { get; }

    string CalcularDigitoVerificador(Boleto boleto, Beneficiario beneficiario);
    string MontarCodigoBarras(Boleto boleto, Beneficiario beneficiario);
    string MontarLinhaDigitavel(string codigoBarras, Boleto boleto, Beneficiario beneficiario);
    string MontarCampoNossoNumero(Boleto boleto, Beneficiario beneficiario);
    string MontarCampoCodigoCedente(Boleto boleto, Beneficiario beneficiario);
    string MontarCampoCarteira(Boleto boleto, Beneficiario beneficiario);

    // CNAB
    string GerarRegistroHeader240(int numeroRemessa, Beneficiario beneficiario);
    string GerarRegistroTransacao240(Boleto boleto, Beneficiario beneficiario, int sequencial);
    string GerarRegistroTrailler240(IReadOnlyList<string> linhas, Beneficiario beneficiario);
    void GerarRegistroHeader400(int numeroRemessa, Beneficiario beneficiario, List<string> remessa);
    void GerarRegistroTransacao400(Boleto boleto, Beneficiario beneficiario, List<string> remessa, int sequencial);
    void GerarRegistroTrailler400(List<string> remessa, Beneficiario beneficiario);
    void LerRetorno240(IList<string> linhas, IList<Boleto> boletos, Beneficiario beneficiario);
    void LerRetorno400(IList<string> linhas, IList<Boleto> boletos, Beneficiario beneficiario);

    // Mapeamento de ocorrências (cada banco tem códigos próprios)
    TipoOcorrencia CodOcorrenciaParaTipo(int codOcorrencia);
    string TipoOcorrenciaParaCod(TipoOcorrencia tipo);
    string DescricaoOcorrencia(TipoOcorrencia tipo);
    string DescricaoMotivoRejeicao(TipoOcorrencia tipo, int codMotivo);

    int CalcularTamanhoMaximoNossoNumero(string carteira, string nossoNumero = "", string convenio = "");
}
