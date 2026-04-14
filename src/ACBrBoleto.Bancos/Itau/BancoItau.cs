using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Helpers;
using ACBrBoleto.Core.Models;
using ACBrBoleto.Core.Services;

namespace ACBrBoleto.Bancos.Itau;

/// <summary>
/// Itaú — cobrança CNAB.
/// Migrado de ACBrBancoItau.pas.
///
/// Campo livre: Carteira(3) + NossoNum(8) + Agência(4) + Conta(5) + DV(1) + "000" + Tipo(1)
/// DV campo livre: DAC Módulo 10 sobre Agência(4)+Conta(5)+DV_AC+Carteira(3)+NossoNum(8)
/// </summary>
public class BancoItau : BancoBase
{
    public override int Numero => 341;
    public override int Digito => 7;
    public override string Nome => "ITAÚ";
    public override TipoCobranca TipoCobranca => TipoCobranca.Itau;
    public override int TamanhoMaximoNossoNumero => 8;
    public override int TamanhoAgencia => 4;
    public override int TamanhoConta => 5;
    public override int TamanhoCarteira => 3;

    /// <summary>
    /// DV Agência/Conta Itaú (DAC Módulo 10).
    /// </summary>
    private string CalcularDvAgenciaConta(Beneficiario beneficiario)
    {
        string agencia = beneficiario.Agencia.OnlyNumbers().PadLeft(4, '0');
        string conta = beneficiario.Conta.OnlyNumbers().PadLeft(5, '0');
        return CodigoBarrasService.CalcularDvModulo10(agencia + conta);
    }

    public override string CalcularDigitoVerificador(Boleto boleto, Beneficiario beneficiario)
    {
        string agencia = beneficiario.Agencia.OnlyNumbers().PadLeft(4, '0');
        string conta = beneficiario.Conta.OnlyNumbers().PadLeft(5, '0');
        string dvAC = CalcularDvAgenciaConta(beneficiario);
        string carteira = boleto.Carteira.PadLeft(3, '0');
        string nossoNum = boleto.NossoNumero.PadLeftZero(8);
        return CodigoBarrasService.CalcularDvModulo10(agencia + conta + dvAC + carteira + nossoNum);
    }

    public override string MontarCampoNossoNumero(Boleto boleto, Beneficiario beneficiario)
        => boleto.Carteira.PadLeft(3, '0')
           + "/" + boleto.NossoNumero.PadLeftZero(8)
           + "-" + CalcularDigitoVerificador(boleto, beneficiario);

    public override string MontarCodigoBarras(Boleto boleto, Beneficiario beneficiario)
    {
        string agencia = beneficiario.Agencia.OnlyNumbers().PadLeft(4, '0');
        string conta = beneficiario.Conta.OnlyNumbers().PadLeft(5, '0');
        string dvAC = CalcularDvAgenciaConta(beneficiario);
        string carteira = boleto.Carteira.PadLeft(3, '0');
        string nossoNum = boleto.NossoNumero.PadLeftZero(8);
        string dv = CalcularDigitoVerificador(boleto, beneficiario);

        // Campo livre: Carteira(3) + NossoNum(8) + Agência(4) + Conta(5) + DV(1) + "000" + "1"
        string campoLivre = carteira + nossoNum + agencia + conta + dv + "000" + "1";

        string fator = CodigoBarrasService.CalcularFatorVencimento(boleto.Vencimento);
        string valor = boleto.ValorDocumento.ToValorCnab(10);
        string banco = Numero.ToString().PadLeft(3, '0');

        string semDv = banco + "9" + fator + valor + campoLivre;
        string dvBarra = CodigoBarrasService.CalcularDigitoCodigoBarras(semDv[..4] + semDv[5..]);

        return semDv[..4] + dvBarra + semDv[4..];
    }

    public override void GerarRegistroHeader400(int numeroRemessa, Beneficiario beneficiario, List<string> remessa)
    {
        string agencia = beneficiario.Agencia.OnlyNumbers().PadLeft(4, '0');
        string conta = beneficiario.Conta.OnlyNumbers().PadLeft(5, '0');
        string contaDig = beneficiario.ContaDigito;

        var sb = new System.Text.StringBuilder(400);
        sb.Append('0');
        sb.Append('1');
        sb.Append("REMESSA");
        sb.Append("01");
        sb.Append("COBRANÇA".PreparaCnabAlfa(15));
        sb.Append(agencia + conta + contaDig);
        sb.Append(new string(' ', 8));
        sb.Append(beneficiario.Nome.PreparaCnabAlfa(30));
        sb.Append("341");
        sb.Append("BANCO ITAU SA".PadRight(15));
        sb.Append(DateTime.Today.ToDataCnab("ddMMyy"));
        sb.Append(new string(' ', 294));
        sb.Append("000001");

        remessa.Add(sb.ToString().TruncateOrPad(400));
    }

    public override void GerarRegistroTransacao400(Boleto boleto, Beneficiario beneficiario, List<string> remessa, int sequencial)
    {
        string agencia = beneficiario.Agencia.OnlyNumbers().PadLeft(4, '0');
        string conta = beneficiario.Conta.OnlyNumbers().PadLeft(5, '0');
        string contaDig = beneficiario.ContaDigito;
        string carteira = boleto.Carteira.PadLeft(3, '0');
        string nossoNum = boleto.NossoNumero.PadLeftZero(8);
        string dvNN = CalcularDigitoVerificador(boleto, beneficiario);

        var sb = new System.Text.StringBuilder(400);
        sb.Append('1');
        sb.Append(boleto.Pagador.CnpjCpf.Length == 11 ? "01" : "02");
        sb.Append(beneficiario.CnpjCpf.OnlyNumbers().PadLeft(14, '0'));
        sb.Append(agencia);
        sb.Append(conta);
        sb.Append(contaDig);
        sb.Append(carteira);
        sb.Append(nossoNum);
        sb.Append(dvNN);
        sb.Append(new string(' ', 10));
        sb.Append(boleto.NumeroDocumento.PreparaCnabAlfa(10));
        sb.Append(boleto.Vencimento.ToDataCnab("ddMMyy"));
        sb.Append(boleto.ValorDocumento.ToValorCnab(13));
        sb.Append("341");
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
            if (linha.Length < 400) continue;
            if (linha[0] == '0' || linha[0] == '9') continue;

            var boleto = new Boleto();
            boleto.NossoNumero = linha.CnabSubstring(48, 8).Trim();
            boleto.NumeroDocumento = linha.CnabSubstring(117, 10).Trim();

            int cod = int.Parse(linha.CnabSubstring(109, 2));
            boleto.OcorrenciaOriginal = new Ocorrencia
            {
                Tipo = CodOcorrenciaParaTipo(cod),
                CodigoBanco = cod.ToString()
            };

            var liq = new Liquidacao();
            liq.DataOcorrencia = ParseData(linha.CnabSubstring(111, 6));
            liq.ValorPago = ParseValor(linha.CnabSubstring(253, 13));
            boleto.Liquidacao = liq;
            boletos.Add(boleto);
        }
    }

    public override TipoOcorrencia CodOcorrenciaParaTipo(int cod) => cod switch
    {
        2  => TipoOcorrencia.RetornoRegistroConfirmado,
        3  => TipoOcorrencia.RetornoRegistroRejeitado,
        5  => TipoOcorrencia.RetornoLiquidado,
        6  => TipoOcorrencia.RetornoBaixado,
        9  => TipoOcorrencia.RetornoBaixadoInstAgencia,
        10 => TipoOcorrencia.RetornoAbatimentoConcedido,
        11 => TipoOcorrencia.RetornoAbatimentoCancelado,
        14 => TipoOcorrencia.RetornoEncaminhadoACartorio,
        15 => TipoOcorrencia.RetornoDevolvidoPeloCartorio,
        17 => TipoOcorrencia.RetornoLiquidadoParcialmente,
        19 => TipoOcorrencia.RetornoConfirmacaoEntradaCobrancaSimples,
        20 => TipoOcorrencia.RetornoInstrucaoRejeitada,
        23 => TipoOcorrencia.RetornoBaixaSolicitada,
        _  => TipoOcorrencia.RetornoOutrosEventos
    };

    public override string TipoOcorrenciaParaCod(TipoOcorrencia tipo) => tipo switch
    {
        TipoOcorrencia.RemessaRegistrar         => "01",
        TipoOcorrencia.RemessaBaixar            => "02",
        TipoOcorrencia.RemessaAlterarVencimento => "08",
        TipoOcorrencia.RemessaProtestar         => "11",
        _ => "01"
    };

    public override string DescricaoOcorrencia(TipoOcorrencia tipo) => tipo switch
    {
        TipoOcorrencia.RetornoRegistroConfirmado => "Entrada Confirmada",
        TipoOcorrencia.RetornoLiquidado          => "Liquidação",
        TipoOcorrencia.RetornoBaixado            => "Baixado",
        _ => tipo.ToString()
    };

    private static DateTime ParseData(string v)
        => DateTime.TryParseExact(v.Trim(), "ddMMyy",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var dt) ? dt : DateTime.MinValue;

    private static decimal ParseValor(string v)
        => string.IsNullOrWhiteSpace(v) || v.All(c => c == '0' || c == ' ')
            ? 0 : decimal.Parse(v.Trim()) / 100;
}
