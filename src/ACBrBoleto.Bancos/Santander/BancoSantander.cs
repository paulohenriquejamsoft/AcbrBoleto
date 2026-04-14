using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Helpers;
using ACBrBoleto.Core.Models;
using ACBrBoleto.Core.Services;

namespace ACBrBoleto.Bancos.Santander;

/// <summary>
/// Santander — cobrança CNAB.
/// Migrado de ACBrBancoSantander.pas.
///
/// Campo livre: "9" + IOS(1) + CodigoCedente(7) + NossoNum(13) + "0"
/// DV NossoNum: Módulo 11, pesos 2-9
/// </summary>
public class BancoSantander : BancoBase
{
    public override int Numero => 33;
    public override int Digito => 7;
    public override string Nome => "SANTANDER";
    public override TipoCobranca TipoCobranca => TipoCobranca.Santander;
    public override int TamanhoMaximoNossoNumero => 13;
    public override int TamanhoAgencia => 4;
    public override int TamanhoConta => 8;
    public override int TamanhoCarteira => 2;

    public override string CalcularDigitoVerificador(Boleto boleto, Beneficiario beneficiario)
    {
        string doc = boleto.NossoNumero.PadLeftZero(13);
        int dv = CodigoBarrasService.CalcularModulo11(doc);
        return dv >= 10 ? "0" : dv.ToString();
    }

    public override string MontarCampoNossoNumero(Boleto boleto, Beneficiario beneficiario)
        => boleto.NossoNumero.PadLeftZero(13) + "-" + CalcularDigitoVerificador(boleto, beneficiario);

    public override string MontarCodigoBarras(Boleto boleto, Beneficiario beneficiario)
    {
        string ios = "0"; // IOS = 0 (não registrado) ou configurado via Modalidade
        string codCedente = beneficiario.CodigoCedente.PadLeftZero(7);
        string nossoNum = boleto.NossoNumero.PadLeftZero(13);
        string dv = CalcularDigitoVerificador(boleto, beneficiario);

        // Campo livre: "9" + IOS(1) + CodigoCedente(7) + NossoNum(13) + DV(1) + "0" = 23
        // Mas o campo livre do boleto são 25 posições; diferenças dependem da carteira
        string campoLivre = "9" + ios + codCedente + nossoNum + dv + "0";

        if (campoLivre.Length < 25)
            campoLivre = campoLivre.PadRight(25, '0');
        else if (campoLivre.Length > 25)
            campoLivre = campoLivre[..25];

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
        sb.Append("033");
        sb.Append("SANTANDER".PadRight(15));
        sb.Append(DateTime.Today.ToDataCnab("ddMMyy"));
        sb.Append(new string(' ', 294));
        sb.Append("000001");
        remessa.Add(sb.ToString().TruncateOrPad(400));
    }

    public override void GerarRegistroTransacao400(Boleto boleto, Beneficiario beneficiario, List<string> remessa, int sequencial)
    {
        string codCedente = beneficiario.CodigoCedente.PadLeftZero(7);
        string nossoNum = boleto.NossoNumero.PadLeftZero(13);
        string dvNN = CalcularDigitoVerificador(boleto, beneficiario);

        var sb = new System.Text.StringBuilder(400);
        sb.Append('1');
        sb.Append(boleto.Pagador.CnpjCpf.Length == 11 ? "01" : "02");
        sb.Append(beneficiario.CnpjCpf.OnlyNumbers().PadLeft(14, '0'));
        sb.Append(codCedente.PadRight(20));
        sb.Append(nossoNum + dvNN);
        sb.Append(new string(' ', 10));
        sb.Append(boleto.Carteira.PadLeft(2, '0'));
        sb.Append(boleto.NumeroDocumento.PreparaCnabAlfa(10));
        sb.Append(boleto.Vencimento.ToDataCnab("ddMMyy"));
        sb.Append(boleto.ValorDocumento.ToValorCnab(13));
        sb.Append("033");
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
            boleto.NossoNumero = linha.CnabSubstring(55, 13).Trim();

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
