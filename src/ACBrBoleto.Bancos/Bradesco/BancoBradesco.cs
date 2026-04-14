using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Helpers;
using ACBrBoleto.Core.Models;
using ACBrBoleto.Core.Services;

namespace ACBrBoleto.Bancos.Bradesco;

/// <summary>
/// Bradesco — cobrança CNAB.
/// Migrado de ACBrBancoBradesco.pas.
///
/// Campo livre: Agência(4) + Carteira(2) + NossoNum(11) + Conta(7) + "0"
/// DV NossoNum: Módulo 11, pesos 2-7 (0 a 9), resto 1 → "P"
/// </summary>
public class BancoBradesco : BancoBase
{
    public override int Numero => 237;
    public override int Digito => 2;
    public override string Nome => "BRADESCO";
    public override TipoCobranca TipoCobranca => TipoCobranca.Bradesco;
    public override int TamanhoMaximoNossoNumero => 11;
    public override int TamanhoAgencia => 4;
    public override int TamanhoConta => 7;
    public override int TamanhoCarteira => 2;

    // -------------------------------------------------------
    // DV Nosso Número — Módulo 11, pesos 2-7
    // -------------------------------------------------------

    public override string CalcularDigitoVerificador(Boleto boleto, Beneficiario beneficiario)
    {
        string doc = boleto.NossoNumero.PadLeftZero(TamanhoMaximoNossoNumero);
        int soma = 0;
        int peso = 2;
        for (int i = doc.Length - 1; i >= 0; i--)
        {
            soma += (doc[i] - '0') * peso;
            peso = peso == 7 ? 2 : peso + 1;
        }
        int resto = soma % 11;
        if (resto == 0) return "0";
        if (resto == 1) return "P";
        return (11 - resto).ToString();
    }

    public override string MontarCampoNossoNumero(Boleto boleto, Beneficiario beneficiario)
        => boleto.NossoNumero.PadLeftZero(TamanhoMaximoNossoNumero)
           + "-" + CalcularDigitoVerificador(boleto, beneficiario);

    // -------------------------------------------------------
    // Código de Barras
    // -------------------------------------------------------

    public override string MontarCodigoBarras(Boleto boleto, Beneficiario beneficiario)
    {
        string agencia = beneficiario.Agencia.OnlyNumbers().PadLeft(4, '0');
        string carteira = boleto.Carteira.PadLeft(2, '0');
        string nossoNum = boleto.NossoNumero.PadLeftZero(11);
        string conta = beneficiario.Conta.OnlyNumbers();
        conta = conta.Length > 7 ? conta[^7..] : conta.PadLeft(7, '0');

        // Campo livre: Agência(4) + Carteira(2) + NossoNum(11) + Conta(7) + "0"
        string campoLivre = agencia + carteira + nossoNum + conta + "0";

        string fator = CodigoBarrasService.CalcularFatorVencimento(boleto.Vencimento);
        string valor = boleto.ValorDocumento.ToValorCnab(10);
        string banco = Numero.ToString().PadLeft(3, '0');

        string semDv = banco + "9" + fator + valor + campoLivre;
        string dv = CodigoBarrasService.CalcularDigitoCodigoBarras(
            semDv[..4] + semDv[5..]);

        return semDv[..4] + dv + semDv[4..];
    }

    // -------------------------------------------------------
    // CNAB 400
    // -------------------------------------------------------

    public override void GerarRegistroHeader400(int numeroRemessa, Beneficiario beneficiario, List<string> remessa)
    {
        string agencia = beneficiario.Agencia.OnlyNumbers().PadLeft(4, '0');
        string agDig = beneficiario.AgenciaDigito.PadLeft(1, '0');
        string conta = beneficiario.Conta.OnlyNumbers().PadLeft(7, '0');
        string contaDig = beneficiario.ContaDigito.PadLeft(1, '0');

        var sb = new System.Text.StringBuilder(400);
        sb.Append('0');
        sb.Append('1');
        sb.Append("REMESSA");
        sb.Append("01");
        sb.Append("COBRANÇA".PreparaCnabAlfa(15));
        sb.Append(agencia);
        sb.Append(agDig);
        sb.Append(conta);
        sb.Append(contaDig);
        sb.Append(new string(' ', 8));
        sb.Append(beneficiario.Nome.PreparaCnabAlfa(30));
        sb.Append("237");
        sb.Append("BRADESCO".PadRight(15));
        sb.Append(DateTime.Today.ToDataCnab("ddMMyy"));
        sb.Append(new string(' ', 294));
        sb.Append("000001");

        remessa.Add(sb.ToString().TruncateOrPad(400));
    }

    public override void GerarRegistroTransacao400(Boleto boleto, Beneficiario beneficiario, List<string> remessa, int sequencial)
    {
        string agencia = beneficiario.Agencia.OnlyNumbers().PadLeft(4, '0');
        string agDig = beneficiario.AgenciaDigito.PadLeft(1, '0');
        string conta = beneficiario.Conta.OnlyNumbers().PadLeft(7, '0');
        string contaDig = beneficiario.ContaDigito;
        string nossoNum = boleto.NossoNumero.PadLeftZero(11);
        string dvNN = CalcularDigitoVerificador(boleto, beneficiario);
        string carteira = boleto.Carteira.PadLeft(2, '0');

        var sb = new System.Text.StringBuilder(400);
        sb.Append('1');
        sb.Append(boleto.Pagador.CnpjCpf.Length == 11 ? "01" : "02");
        sb.Append(beneficiario.CnpjCpf.OnlyNumbers().PadLeft(14, '0'));
        sb.Append(agencia);
        sb.Append(agDig);
        sb.Append(conta);
        sb.Append(contaDig);
        sb.Append(new string(' ', 3));
        sb.Append(nossoNum);
        sb.Append(dvNN.PadLeft(1));
        sb.Append(new string(' ', 10));
        sb.Append(carteira);
        sb.Append(boleto.NumeroDocumento.PreparaCnabAlfa(10));
        sb.Append(boleto.Vencimento.ToDataCnab("ddMMyy"));
        sb.Append(boleto.ValorDocumento.ToValorCnab(13));
        sb.Append("237");
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
            char tipo = linha[0];
            if (tipo == '0' || tipo == '9') continue;

            var boleto = new Boleto();
            boleto.NossoNumero = linha.CnabSubstring(47, 11).Trim().TrimStart('0');
            boleto.NumeroDocumento = linha.CnabSubstring(117, 10).Trim();

            int codOcorr = int.Parse(linha.CnabSubstring(109, 2));
            boleto.OcorrenciaOriginal = new Ocorrencia
            {
                Tipo = CodOcorrenciaParaTipo(codOcorr),
                CodigoBanco = codOcorr.ToString()
            };

            var liq = new Liquidacao();
            liq.DataOcorrencia = ParseDataCnab(linha.CnabSubstring(111, 6));
            liq.ValorPago = ParseValorCnab(linha.CnabSubstring(253, 13));
            liq.ValorTarifas = ParseValorCnab(linha.CnabSubstring(176, 13));
            liq.DataCredito = ParseDataCnab(linha.CnabSubstring(296, 6));
            boleto.Liquidacao = liq;

            boletos.Add(boleto);
        }
    }

    // -------------------------------------------------------
    // Ocorrências Bradesco
    // -------------------------------------------------------

    public override TipoOcorrencia CodOcorrenciaParaTipo(int cod) => cod switch
    {
        2  => TipoOcorrencia.RetornoRegistroConfirmado,
        3  => TipoOcorrencia.RetornoRegistroRejeitado,
        4  => TipoOcorrencia.RetornoAlteracaoDadosNovaEntrada,
        5  => TipoOcorrencia.RetornoAlteracaoDadosRejeitados,
        6  => TipoOcorrencia.RetornoLiquidado,
        9  => TipoOcorrencia.RetornoBaixado,
        10 => TipoOcorrencia.RetornoBaixadoInstAgencia,
        11 => TipoOcorrencia.RetornoAbatimentoConcedido,
        12 => TipoOcorrencia.RetornoAbatimentoCancelado,
        13 => TipoOcorrencia.RetornoDescontoConcedido,
        14 => TipoOcorrencia.RetornoDescontoCancelado,
        19 => TipoOcorrencia.RetornoConfirmacaoEntradaCobrancaSimples,
        24 => TipoOcorrencia.RetornoEncaminhadoACartorio,
        27 => TipoOcorrencia.RetornoBaixaSolicitada,
        28 => TipoOcorrencia.RetornoBaixaAutomatica,
        30 => TipoOcorrencia.RetornoInstrucaoRejeitada,
        32 => TipoOcorrencia.RetornoLiquidadoSemRegistro,
        _  => TipoOcorrencia.RetornoOutrosEventos
    };

    public override string TipoOcorrenciaParaCod(TipoOcorrencia tipo) => tipo switch
    {
        TipoOcorrencia.RemessaRegistrar          => "01",
        TipoOcorrencia.RemessaBaixar             => "02",
        TipoOcorrencia.RemessaAlterarVencimento  => "09",
        TipoOcorrencia.RemessaProtestar          => "33",
        TipoOcorrencia.RemessaSustarProtesto     => "34",
        _ => "01"
    };

    public override string DescricaoOcorrencia(TipoOcorrencia tipo) => tipo switch
    {
        TipoOcorrencia.RetornoRegistroConfirmado => "Entrada Confirmada",
        TipoOcorrencia.RetornoLiquidado          => "Liquidação",
        TipoOcorrencia.RetornoBaixado            => "Baixado",
        _ => tipo.ToString()
    };

    private static DateTime ParseDataCnab(string v)
    {
        if (string.IsNullOrWhiteSpace(v) || v.Trim() == "000000") return DateTime.MinValue;
        return DateTime.TryParseExact(v.Trim(), "ddMMyy",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var dt) ? dt : DateTime.MinValue;
    }

    private static decimal ParseValorCnab(string v)
        => string.IsNullOrWhiteSpace(v) || v.All(c => c == '0' || c == ' ')
            ? 0 : decimal.Parse(v.Trim()) / 100;
}

/// <summary>Bradesco com SICOOB (herda Bradesco, número 756).</summary>
public class BancoBradescoSicoob : BancoBradesco
{
    public override TipoCobranca TipoCobranca => TipoCobranca.BradescoSICOOB;
    public override string Nome => "BRADESCO SICOOB";
}
