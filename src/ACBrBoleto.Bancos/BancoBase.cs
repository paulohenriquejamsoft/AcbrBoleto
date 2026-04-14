using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Helpers;
using ACBrBoleto.Core.Interfaces;
using ACBrBoleto.Core.Models;
using ACBrBoleto.Core.Services;

namespace ACBrBoleto.Bancos;

/// <summary>
/// Classe base abstrata para todas as implementações bancárias.
/// Corresponde a TACBrBancoClass no Delphi.
///
/// CRÍTICO ao sobrescrever CNAB:
///   - Posições são base-1 (igual ao Delphi). Use linha.CnabSubstring(pos, tam).
///   - Encoding Windows-1252 é gerenciado pelo BoletoService.
///   - Valores monetários: use boleto.ValorDocumento.ToValorCnab(tamanho).
/// </summary>
public abstract class BancoBase : IBancoService
{
    // === Propriedades identificadoras ===
    public abstract int Numero { get; }
    public abstract int Digito { get; }
    public abstract string Nome { get; }
    public abstract TipoCobranca TipoCobranca { get; }
    public virtual int TamanhoMaximoNossoNumero => 10;
    public virtual int TamanhoAgencia => 4;
    public virtual int TamanhoConta => 8;
    public virtual int TamanhoCarteira => 2;

    // =========================================================
    // Código de Barras
    // =========================================================

    public abstract string MontarCodigoBarras(Boleto boleto, Beneficiario beneficiario);

    public virtual string MontarLinhaDigitavel(string codigoBarras, Boleto boleto, Beneficiario beneficiario)
        => CodigoBarrasService.MontarLinhaDigitavel(codigoBarras);

    public virtual string CalcularDigitoVerificador(Boleto boleto, Beneficiario beneficiario)
        => string.Empty;

    public virtual string MontarCampoNossoNumero(Boleto boleto, Beneficiario beneficiario)
        => boleto.NossoNumero;

    public virtual string MontarCampoCodigoCedente(Boleto boleto, Beneficiario beneficiario)
        => $"{beneficiario.Agencia}-{beneficiario.AgenciaDigito}/{beneficiario.Conta}-{beneficiario.ContaDigito}";

    public virtual string MontarCampoCarteira(Boleto boleto, Beneficiario beneficiario)
        => boleto.Carteira;

    // =========================================================
    // CNAB 400 (legado, registros de 400 posições)
    // =========================================================

    public virtual void GerarRegistroHeader400(int numeroRemessa, Beneficiario beneficiario, List<string> remessa)
        => throw new NotImplementedException($"{Nome}: GerarRegistroHeader400 não implementado.");

    public virtual void GerarRegistroTransacao400(Boleto boleto, Beneficiario beneficiario, List<string> remessa, int sequencial)
        => throw new NotImplementedException($"{Nome}: GerarRegistroTransacao400 não implementado.");

    public virtual void GerarRegistroTrailler400(List<string> remessa, Beneficiario beneficiario)
    {
        // Trailler padrão CNAB 400
        var linha = "9"
            + new string(' ', 393)
            + remessa.Count.ToString().PadLeft(6, '0');
        remessa.Add(linha);
    }

    public virtual void LerRetorno400(IList<string> linhas, IList<Boleto> boletos, Beneficiario beneficiario)
        => throw new NotImplementedException($"{Nome}: LerRetorno400 não implementado.");

    // =========================================================
    // CNAB 240 (FEBRABAN, registros de 240 posições)
    // =========================================================

    public virtual string GerarRegistroHeader240(int numeroRemessa, Beneficiario beneficiario)
        => throw new NotImplementedException($"{Nome}: GerarRegistroHeader240 não implementado.");

    public virtual string GerarRegistroTransacao240(Boleto boleto, Beneficiario beneficiario, int sequencial)
        => throw new NotImplementedException($"{Nome}: GerarRegistroTransacao240 não implementado.");

    public virtual string GerarRegistroTrailler240(IReadOnlyList<string> linhas, Beneficiario beneficiario)
        => throw new NotImplementedException($"{Nome}: GerarRegistroTrailler240 não implementado.");

    public virtual void LerRetorno240(IList<string> linhas, IList<Boleto> boletos, Beneficiario beneficiario)
        => throw new NotImplementedException($"{Nome}: LerRetorno240 não implementado.");

    // =========================================================
    // Mapeamento de Ocorrências
    // =========================================================

    public abstract TipoOcorrencia CodOcorrenciaParaTipo(int codOcorrencia);
    public abstract string TipoOcorrenciaParaCod(TipoOcorrencia tipo);
    public abstract string DescricaoOcorrencia(TipoOcorrencia tipo);

    public virtual string DescricaoMotivoRejeicao(TipoOcorrencia tipo, int codMotivo)
        => $"Código {codMotivo}";

    public virtual int CalcularTamanhoMaximoNossoNumero(string carteira, string nossoNumero = "", string convenio = "")
        => TamanhoMaximoNossoNumero;

    // =========================================================
    // Helpers protegidos reutilizados pelas implementações
    // =========================================================

    /// <summary>
    /// Calcula o DV do código de barras e insere na posição 5.
    /// Recebe os primeiros 4 dígitos + campo livre (39) = 43 chars.
    /// </summary>
    protected string InserirDvCodigoBarras(string codigoBarras43)
    {
        string dv = CodigoBarrasService.CalcularDigitoCodigoBarras(codigoBarras43);
        return codigoBarras43[..4] + dv + codigoBarras43[4..];
    }

    /// <summary>
    /// Prefixo padrão do código de barras: NNN9FFFFFFFVVVVVVVVVV
    /// banco(3) + moeda(1) + [DV placeholder] + fator(4) + valor(10)
    /// </summary>
    protected string PrefixoCodigoBarras(Boleto boleto)
    {
        string fator = CodigoBarrasService.CalcularFatorVencimento(boleto.Vencimento);
        string valor = boleto.ValorDocumento.ToValorCnab(10);
        return Numero.ToString().PadLeft(3, '0') + BoletoConstants.MoedaReal + fator + valor;
    }

    protected static string Modulo11Str(string numero, int pesoInicial = 2, int pesoFinal = 9)
    {
        int dv = CodigoBarrasService.CalcularModulo11(numero, pesoInicial, pesoFinal);
        return dv >= 10 ? "0" : dv.ToString();
    }
}
