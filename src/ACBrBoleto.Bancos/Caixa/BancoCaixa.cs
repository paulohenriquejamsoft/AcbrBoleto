using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Helpers;
using ACBrBoleto.Core.Models;
using ACBrBoleto.Core.Services;

namespace ACBrBoleto.Bancos.Caixa;

/// <summary>
/// Caixa Econômica Federal — cobrança CNAB.
/// Migrado de ACBrBancoCaixa.pas.
///
/// Modalidade "14": RG (Sem Registro) — campo livre diferente
/// Modalidade "24": CS (Simples com registro)
/// NossoNum: Modalidade(2) + Sequencial(15) + DV(1) = 18 posições
/// Campo livre (mod 14): Modalidade(2) + CodCedente(6) + NossoNum(15) + DV + "0"
/// </summary>
public class BancoCaixa : BancoBase
{
    public override int Numero => 104;
    public override int Digito => 0;
    public override string Nome => "CAIXA ECONÔMICA FEDERAL";
    public override TipoCobranca TipoCobranca => TipoCobranca.CaixaEconomica;
    public override int TamanhoMaximoNossoNumero => 15;
    public override int TamanhoAgencia => 4;
    public override int TamanhoConta => 11;
    public override int TamanhoCarteira => 2;

    protected virtual string Modalidade => "14";

    public override string CalcularDigitoVerificador(Boleto boleto, Beneficiario beneficiario)
    {
        string modal = beneficiario.Modalidade.IsNullOrEmpty() ? Modalidade : beneficiario.Modalidade;
        string codCedente = beneficiario.CodigoCedente.PadLeftZero(6);
        string nossoNum = boleto.NossoNumero.PadLeftZero(15);
        string doc = modal + codCedente + nossoNum;

        int dv = CodigoBarrasService.CalcularModulo11(doc, dvParaResto0: 0, dvParaResto1: 1);
        return dv.ToString();
    }

    public override string MontarCampoNossoNumero(Boleto boleto, Beneficiario beneficiario)
    {
        string modal = beneficiario.Modalidade.IsNullOrEmpty() ? Modalidade : beneficiario.Modalidade;
        return modal + boleto.NossoNumero.PadLeftZero(15)
               + "-" + CalcularDigitoVerificador(boleto, beneficiario);
    }

    public override string MontarCodigoBarras(Boleto boleto, Beneficiario beneficiario)
    {
        string modal = beneficiario.Modalidade.IsNullOrEmpty() ? Modalidade : beneficiario.Modalidade;
        string codCedente = beneficiario.CodigoCedente.PadLeftZero(6);
        string nossoNum = boleto.NossoNumero.PadLeftZero(15);
        string dv = CalcularDigitoVerificador(boleto, beneficiario);

        // Campo livre 25 posições: Modal(2) + CodCed(6) + NossoNum(15) + DV(1) + "0"(1) = 25
        string campoLivre = modal + codCedente + nossoNum + dv + "0";
        if (campoLivre.Length > 25) campoLivre = campoLivre[..25];

        string fator = CodigoBarrasService.CalcularFatorVencimento(boleto.Vencimento);
        string valor = boleto.ValorDocumento.ToValorCnab(10);
        string banco = Numero.ToString().PadLeft(3, '0');

        string semDv = banco + "9" + fator + valor + campoLivre;
        string dvBarra = CodigoBarrasService.CalcularDigitoCodigoBarras(semDv[..4] + semDv[5..]);

        return semDv[..4] + dvBarra + semDv[4..];
    }

    public override void GerarRegistroHeader400(int numeroRemessa, Beneficiario beneficiario, List<string> remessa)
    {
        var sb = new System.Text.StringBuilder(400);
        sb.Append('0');
        sb.Append('1');
        sb.Append("REMESSA");
        sb.Append("01");
        sb.Append("COBRANÇA".PreparaCnabAlfa(15));
        sb.Append(beneficiario.CodigoCedente.PadLeftZero(20));
        sb.Append(new string(' ', 8));
        sb.Append(beneficiario.Nome.PreparaCnabAlfa(30));
        sb.Append("104");
        sb.Append("CAIXA ECONOMICA FED".PadRight(15));
        sb.Append(DateTime.Today.ToDataCnab("ddMMyy"));
        sb.Append(new string(' ', 294));
        sb.Append("000001");
        remessa.Add(sb.ToString().TruncateOrPad(400));
    }

    public override void GerarRegistroTransacao400(Boleto boleto, Beneficiario beneficiario, List<string> remessa, int sequencial)
    {
        string modal = beneficiario.Modalidade.IsNullOrEmpty() ? Modalidade : beneficiario.Modalidade;
        string nossoNum = boleto.NossoNumero.PadLeftZero(15);
        string dvNN = CalcularDigitoVerificador(boleto, beneficiario);

        var sb = new System.Text.StringBuilder(400);
        sb.Append('1');
        sb.Append(boleto.Pagador.CnpjCpf.Length == 11 ? "01" : "02");
        sb.Append(beneficiario.CnpjCpf.OnlyNumbers().PadLeft(14, '0'));
        sb.Append(beneficiario.CodigoCedente.PadLeftZero(6).PadRight(20));
        sb.Append(modal + nossoNum + dvNN);
        sb.Append(new string(' ', 5));
        sb.Append(boleto.Carteira.PadLeft(2, '0'));
        sb.Append(boleto.NumeroDocumento.PreparaCnabAlfa(10));
        sb.Append(boleto.Vencimento.ToDataCnab("ddMMyy"));
        sb.Append(boleto.ValorDocumento.ToValorCnab(13));
        sb.Append("104");
        sb.Append("0000");
        sb.Append('1');
        sb.Append(boleto.EspecieDoc.PreparaCnabAlfa(2));
        sb.Append(boleto.Aceite == AceiteTitulo.Sim ? "A" : "N");
        sb.Append(boleto.DataDocumento.ToDataCnab("ddMMyy"));
        sb.Append("00");
        sb.Append("00");
        sb.Append(boleto.ValorMoraJuros.ToValorCnab(13));
        sb.Append(boleto.DataDesconto.ToDataCnabOuZeros("ddMMyy"));
        sb.Append(boleto.ValorDesconto.ToValorCnab(13));
        sb.Append(boleto.ValorIOF.ToValorCnab(13));
        sb.Append(boleto.ValorAbatimento.ToValorCnab(13));
        sb.Append(boleto.Pagador.CnpjCpf.OnlyNumbers().PadLeft(14, '0'));
        sb.Append(boleto.Pagador.Nome.PreparaCnabAlfa(30));
        sb.Append(boleto.Pagador.Logradouro.PreparaCnabAlfa(40));
        sb.Append(boleto.Pagador.CEP.OnlyNumbers().PadLeft(8, '0'));
        sb.Append(boleto.Pagador.Cidade.PreparaCnabAlfa(15));
        sb.Append(boleto.Pagador.UF.PreparaCnabAlfa(2));
        sb.Append(new string(' ', 40));
        sb.Append(new string(' ', 60));
        sb.Append(boleto.DiasProtesto.ToString().PadLeft(2, '0'));
        sb.Append(sequencial.ToString().PadLeft(6, '0'));
        remessa.Add(sb.ToString().TruncateOrPad(400));
    }

    public override void LerRetorno400(IList<string> linhas, IList<Boleto> boletos, Beneficiario beneficiario)
    {
        foreach (var linha in linhas)
        {
            if (linha.Length < 400 || linha[0] == '0' || linha[0] == '9') continue;
            var boleto = new Boleto();
            boleto.NossoNumero = linha.CnabSubstring(38, 15).Trim();

            int cod = int.Parse(linha.CnabSubstring(109, 2));
            boleto.OcorrenciaOriginal = new Ocorrencia
            {
                Tipo = CodOcorrenciaParaTipo(cod),
                CodigoBanco = cod.ToString()
            };

            var liq = new Liquidacao();
            liq.ValorPago = ParseValor(linha.CnabSubstring(253, 13));
            boleto.Liquidacao = liq;
            boletos.Add(boleto);
        }
    }

    public override TipoOcorrencia CodOcorrenciaParaTipo(int cod) => cod switch
    {
        2  => TipoOcorrencia.RetornoRegistroConfirmado,
        3  => TipoOcorrencia.RetornoRegistroRejeitado,
        6  => TipoOcorrencia.RetornoLiquidado,
        9  => TipoOcorrencia.RetornoBaixado,
        15 => TipoOcorrencia.RetornoEncaminhadoACartorio,
        16 => TipoOcorrencia.RetornoDevolvidoPeloCartorio,
        17 => TipoOcorrencia.RetornoLiquidadoParcialmente,
        19 => TipoOcorrencia.RetornoConfirmacaoEntradaCobrancaSimples,
        _  => TipoOcorrencia.RetornoOutrosEventos
    };

    public override string TipoOcorrenciaParaCod(TipoOcorrencia tipo) => tipo switch
    {
        TipoOcorrencia.RemessaRegistrar => "01",
        TipoOcorrencia.RemessaBaixar    => "02",
        _ => "01"
    };

    public override string DescricaoOcorrencia(TipoOcorrencia tipo) => tipo switch
    {
        TipoOcorrencia.RetornoLiquidado => "Liquidação",
        TipoOcorrencia.RetornoBaixado   => "Baixado",
        _ => tipo.ToString()
    };

    private static decimal ParseValor(string v)
        => string.IsNullOrWhiteSpace(v) ? 0 : decimal.Parse(v.Trim()) / 100;
}

/// <summary>Caixa com SICOB.</summary>
public class BancoCaixaSicob : BancoCaixa
{
    public override TipoCobranca TipoCobranca => TipoCobranca.CaixaSicob;
    public override string Nome => "CAIXA SICOB";
    protected override string Modalidade => "24";
}
