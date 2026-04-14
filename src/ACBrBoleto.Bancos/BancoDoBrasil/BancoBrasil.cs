using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Helpers;
using ACBrBoleto.Core.Models;
using ACBrBoleto.Core.Services;

namespace ACBrBoleto.Bancos.BancoDoBrasil;

/// <summary>
/// Banco do Brasil — cobrança CNAB (registrada/simples).
/// Migrado de ACBrBancoBrasil.pas.
///
/// Particularidades do BB:
///  - Convênio 4 dígitos: NossoNum = Conv(4) + Seq(7)
///  - Convênio 5 dígitos: NossoNum = Conv(6) + Seq(5)
///  - Convênio 6 dígitos + cart 16/18: pode ser 17 posições
///  - Convênio 6 dígitos: NossoNum = Conv(6) + Seq(11)
///  - Convênio 7 dígitos: NossoNum = Conv(7) + Seq(10)
/// </summary>
public class BancoBrasil : BancoBase
{
    public override int Numero => 1;
    public override int Digito => 9;
    public override string Nome => "Banco do Brasil";
    public override TipoCobranca TipoCobranca => TipoCobranca.BancoDoBrasil;
    public override int TamanhoAgencia => 4;
    public override int TamanhoConta => 12;
    public override int TamanhoCarteira => 2;

    // -------------------------------------------------------
    // Nosso Número
    // -------------------------------------------------------

    protected virtual string FormatarNossoNumero(Boleto boleto, Beneficiario beneficiario)
    {
        string convenio = beneficiario.Convenio.Trim();
        string nossoNum = boleto.NossoNumero;
        int tamConv = convenio.Length;
        int tamMax = CalcularTamanhoMaximoNossoNumero(boleto.Carteira, nossoNum, convenio);

        if ((boleto.Carteira is "16" or "18") && tamConv == 6 && tamMax == 17)
            return nossoNum.PadLeftZero(17);
        if (tamConv <= 4)
            return convenio.PadLeftZero(4) + nossoNum.PadLeftZero(7);
        if (tamConv > 4 && tamConv <= 6)
            return convenio.PadLeftZero(6) + nossoNum.PadLeftZero(5);
        if (tamConv == 7)
            return convenio.PadLeftZero(7) + (nossoNum.Length > 10 ? nossoNum[^10..] : nossoNum.PadLeftZero(10));

        return convenio.PadLeftZero(6) + nossoNum.PadLeftZero(11);
    }

    public override int CalcularTamanhoMaximoNossoNumero(string carteira, string nossoNumero = "", string convenio = "")
    {
        if (string.IsNullOrEmpty(convenio))
            return 10;

        string cart = carteira.Trim();
        int tamConv = convenio.Trim().Length;

        if (nossoNumero.Trim().Length > 10 &&
            ((tamConv == 6 && cart is "16" or "18") ||
             (tamConv == 7 && cart is "17" or "18")))
            return 17;

        if (tamConv <= 4) return 7;
        if (tamConv < 6 || (tamConv == 6 && cart is "12" or "15" or "17" or "18")) return 5;
        if (tamConv == 6) return 11;
        return 10; // tamConv == 7
    }

    public override string MontarCampoNossoNumero(Boleto boleto, Beneficiario beneficiario)
    {
        string nn = FormatarNossoNumero(boleto, beneficiario);
        string convenio = beneficiario.Convenio.Trim();
        int tamConv = convenio.Length;
        int tamMax = CalcularTamanhoMaximoNossoNumero(boleto.Carteira, boleto.NossoNumero, convenio);

        if ((tamConv == 6 && tamMax == 17) || tamConv == 7)
            return nn;

        return nn + "-" + CalcularDigitoVerificador(boleto, beneficiario);
    }

    public override string CalcularDigitoVerificador(Boleto boleto, Beneficiario beneficiario)
    {
        string doc = FormatarNossoNumero(boleto, beneficiario);
        int dv = CodigoBarrasService.CalcularModulo11(doc, pesoInicial: 2, pesoFinal: 9);
        return dv >= 10 ? "X" : dv.ToString();
    }

    // -------------------------------------------------------
    // Código de Barras
    // -------------------------------------------------------

    public override string MontarCodigoBarras(Boleto boleto, Beneficiario beneficiario)
    {
        string convenio = beneficiario.Convenio.Trim();
        string nn = FormatarNossoNumero(boleto, beneficiario);
        int tamConv = convenio.Length;
        int tamMax = CalcularTamanhoMaximoNossoNumero(boleto.Carteira, boleto.NossoNumero, convenio);

        string fator = CodigoBarrasService.CalcularFatorVencimento(boleto.Vencimento);
        string valor = boleto.ValorDocumento.ToValorCnab(10);
        string banco = Numero.ToString().PadLeft(3, '0');
        string agencia = beneficiario.Agencia.OnlyNumbers().PadLeft(4, '0');
        string conta = beneficiario.Conta.OnlyNumbers().PadLeft(8, '0');

        string campoLivre;

        if ((boleto.Carteira is "18" or "16") && tamConv == 6 && tamMax == 17)
        {
            // Conv(6) + NossoNum(17) + "21"
            campoLivre = convenio.PadLeftZero(6) + nn + "21";
        }
        else if (tamConv == 7)
        {
            // "000000" + Conv(7) + NossoNum(10) + Carteira(2)
            campoLivre = "000000" + nn + boleto.Carteira.PadLeft(2, '0');
        }
        else
        {
            // NossoNum + Agencia(4) + Conta(8) + Carteira(2)
            campoLivre = nn + agencia + conta + boleto.Carteira.PadLeft(2, '0');
        }

        string semDv = banco + "9" + fator + valor + campoLivre;
        return semDv[..4] + CodigoBarrasService.CalcularDigitoCodigoBarras(semDv[..4] + semDv[5..]) + semDv[4..];
    }

    public override string MontarCampoCodigoCedente(Boleto boleto, Beneficiario beneficiario)
        => $"{beneficiario.Agencia}-{beneficiario.AgenciaDigito}/" +
           $"{int.Parse(beneficiario.Conta.OnlyNumbers())}-{beneficiario.ContaDigito}";

    public override string MontarCampoCarteira(Boleto boleto, Beneficiario beneficiario)
        => string.IsNullOrEmpty(beneficiario.Modalidade)
            ? boleto.Carteira
            : $"{boleto.Carteira}/{beneficiario.Modalidade}";

    // -------------------------------------------------------
    // CNAB 400
    // -------------------------------------------------------

    public override void GerarRegistroHeader400(int numeroRemessa, Beneficiario beneficiario, List<string> remessa)
    {
        string agencia = beneficiario.Agencia.OnlyNumbers().PadLeft(4, '0');
        string conta = beneficiario.Conta.OnlyNumbers().PadLeft(8, '0');
        string dig = beneficiario.ContaDigito.PadLeft(1, '0');

        var linha = new System.Text.StringBuilder(400);
        linha.Append('0');                                                    // 001 - Tipo
        linha.Append('1');                                                    // 002 - Operação (remessa)
        linha.Append("REMESSA".PadRight(7));                                  // 003-009
        linha.Append("01");                                                   // 010-011 - Serviço cobrança
        linha.Append("COBRANCA".PadRight(15));                                // 012-026
        linha.Append((agencia + dig + conta).PadRight(20));                   // 027-046 - Cod transmissão
        linha.Append(new string(' ', 8));                                     // 047-054
        linha.Append(beneficiario.Nome.PreparaCnabAlfa(30));                  // 055-084 - Nome empresa
        linha.Append("001");                                                  // 085-087 - Banco
        linha.Append("BANCO DO BRASIL".PadRight(15));                         // 088-102 - Nome banco
        linha.Append(DateTime.Today.ToDataCnab("ddMMyy"));                   // 103-108 - Data
        linha.Append(new string(' ', 286));                                   // 109-394 - Brancos
        linha.Append("000001");                                               // 395-400 - Sequencial

        remessa.Add(linha.ToString());
    }

    public override void GerarRegistroTransacao400(Boleto boleto, Beneficiario beneficiario, List<string> remessa, int sequencial)
    {
        string agencia = beneficiario.Agencia.OnlyNumbers().PadLeft(4, '0');
        string conta = beneficiario.Conta.OnlyNumbers().PadLeft(8, '0');
        string contaDig = beneficiario.ContaDigito;
        string nn = FormatarNossoNumero(boleto, beneficiario);
        string dvNN = CalcularDigitoVerificador(boleto, beneficiario);
        string carteira = boleto.Carteira.PadLeft(2, '0');

        var linha = new System.Text.StringBuilder(400);
        linha.Append('1');                                                                        // 001
        linha.Append(boleto.Pagador.CnpjCpf.Length == 11 ? "01" : "02");                        // 002-003
        linha.Append(beneficiario.CnpjCpf.OnlyNumbers().PadLeft(14, '0'));                      // 004-017
        linha.Append(agencia.PadRight(20));                                                      // 018-037 (agência + espaços)
        linha.Append(nn.PadLeft(25, '0'));                                                       // 038-062 - Nosso número
        linha.Append(new string(' ', 8));                                                        // 063-070
        linha.Append(carteira);                                                                  // 071-072
        linha.Append(boleto.NumeroDocumento.PreparaCnabAlfa(10));                               // 073-082
        linha.Append(boleto.Vencimento.ToDataCnab("ddMMyy"));                                  // 083-088
        linha.Append(boleto.ValorDocumento.ToValorCnab(13));                                   // 089-101
        linha.Append("001");                                                                     // 102-104 - Banco cobrador
        linha.Append("0000");                                                                    // 105-108 - Agência cobradora
        linha.Append('1');                                                                       // 109 - Identificação (remessa)
        linha.Append(boleto.EspecieDoc.PreparaCnabAlfa(2));                                     // 110-111
        linha.Append(boleto.Aceite == AceiteTitulo.Sim ? "A" : "N");                           // 112
        linha.Append(boleto.DataDocumento.ToDataCnab("ddMMyy"));                               // 113-118
        // Instrução 1 e 2
        linha.Append("00");                                                                     // 119-120
        linha.Append("00");                                                                     // 121-122
        linha.Append(boleto.ValorMoraJuros.ToValorCnab(13));                                  // 123-135
        linha.Append(boleto.DataDesconto.ToDataCnabOuZeros("ddMMyy"));                        // 136-141
        linha.Append(boleto.ValorDesconto.ToValorCnab(13));                                    // 142-154
        linha.Append(boleto.ValorIOF.ToValorCnab(13));                                         // 155-167
        linha.Append(boleto.ValorAbatimento.ToValorCnab(13));                                  // 168-180
        linha.Append(boleto.Pagador.CnpjCpf.OnlyNumbers().PadLeft(14, '0'));                  // 181-194
        linha.Append(boleto.Pagador.Nome.PreparaCnabAlfa(30));                                 // 195-224
        linha.Append(boleto.Pagador.Logradouro.PreparaCnabAlfa(40));                          // 225-264
        linha.Append(boleto.Pagador.CEP.OnlyNumbers().PadLeft(8, '0'));                       // 265-272 (CEP sem hífen)
        linha.Append(boleto.Pagador.Cidade.PreparaCnabAlfa(15));                              // 273-287
        linha.Append(boleto.Pagador.UF.PreparaCnabAlfa(2));                                   // 288-289
        linha.Append(new string(' ', 40));                                                     // 290-329 - Sacador
        linha.Append(new string(' ', 60));                                                     // 330-389 - Mensagem
        linha.Append(new string(' ', 2));                                                      // 390-391
        linha.Append(boleto.DiasProtesto.ToString().PadLeft(2, '0'));                         // 392-393
        linha.Append(' ');                                                                     // 394
        linha.Append(sequencial.ToString().PadLeft(6, '0'));                                  // 395-400

        remessa.Add(linha.ToString());
    }

    public override void LerRetorno400(IList<string> linhas, IList<Boleto> boletos, Beneficiario beneficiario)
    {
        foreach (var linha in linhas)
        {
            if (linha.Length < 400) continue;
            char tipoReg = linha[0];

            if (tipoReg == '0') continue; // Header
            if (tipoReg == '9') continue; // Trailler

            // Detalhe
            var boleto = new Boleto();
            boleto.NossoNumero = linha.CnabSubstring(38, 25).Trim().TrimStart('0');
            boleto.NumeroDocumento = linha.CnabSubstring(117, 10).Trim();
            boleto.Vencimento = ParseDataCnab(linha.CnabSubstring(147, 6), "ddMMyy");

            int codOcorrencia = int.Parse(linha.CnabSubstring(109, 2));
            boleto.OcorrenciaOriginal = new Ocorrencia
            {
                Tipo = CodOcorrenciaParaTipo(codOcorrencia),
                CodigoBanco = codOcorrencia.ToString()
            };

            var liquidacao = new Liquidacao();
            liquidacao.DataOcorrencia = ParseDataCnab(linha.CnabSubstring(111, 6), "ddMMyy");
            liquidacao.ValorPago = ParseValorCnab(linha.CnabSubstring(253, 13));
            liquidacao.ValorTarifas = ParseValorCnab(linha.CnabSubstring(176, 13));
            liquidacao.ValorDesconto = ParseValorCnab(linha.CnabSubstring(241, 13));
            liquidacao.ValorMoraJuros = ParseValorCnab(linha.CnabSubstring(267, 13));
            liquidacao.DataCredito = ParseDataCnab(linha.CnabSubstring(296, 6), "ddMMyy");
            boleto.Liquidacao = liquidacao;

            boletos.Add(boleto);
        }
    }

    // -------------------------------------------------------
    // Mapeamento de Ocorrências (banco 001)
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
        15 => TipoOcorrencia.RetornoBaixaLiquidadoEdital,
        16 => TipoOcorrencia.RetornoDebitoEmConta,
        17 => TipoOcorrencia.RetornoLiquidadoParcialmente,
        18 => TipoOcorrencia.RetornoAcertoDepositaria,
        19 => TipoOcorrencia.RetornoConfirmacaoEntradaCobrancaSimples,
        21 => TipoOcorrencia.RetornoLiquidadoSemRegistro,
        23 => TipoOcorrencia.RetornoEncaminhadoACartorio,
        24 => TipoOcorrencia.RetornoRetiradoCartorioMantidoEmCarteira,
        25 => TipoOcorrencia.RetornoDevolvidoPeloCartorio,
        26 => TipoOcorrencia.RetornoAlteracaoDataEmissao,
        27 => TipoOcorrencia.RetornoSustacaoSolicitada,
        28 => TipoOcorrencia.RetornoBaixaSolicitada,
        29 => TipoOcorrencia.RetornoInstrucaoCancelada,
        30 => TipoOcorrencia.RetornoInstrucaoRejeitada,
        32 => TipoOcorrencia.RetornoBaixaAutomatica,
        33 => TipoOcorrencia.RetornoNaoRecebido,
        34 => TipoOcorrencia.RetornoProtestadoCartorio,
        _  => TipoOcorrencia.RetornoOutrosEventos
    };

    public override string TipoOcorrenciaParaCod(TipoOcorrencia tipo) => tipo switch
    {
        TipoOcorrencia.RemessaRegistrar          => "01",
        TipoOcorrencia.RemessaBaixar             => "02",
        TipoOcorrencia.RemessaConcederAbatimento => "04",
        TipoOcorrencia.RemessaCancelarAbatimento => "05",
        TipoOcorrencia.RemessaConcederDesconto   => "06",
        TipoOcorrencia.RemessaCancelarDesconto   => "07",
        TipoOcorrencia.RemessaAlterarVencimento  => "09",
        TipoOcorrencia.RemessaProtestar          => "33",
        TipoOcorrencia.RemessaSustarProtesto     => "34",
        _ => "01"
    };

    public override string DescricaoOcorrencia(TipoOcorrencia tipo) => tipo switch
    {
        TipoOcorrencia.RetornoRegistroConfirmado    => "Entrada Confirmada",
        TipoOcorrencia.RetornoLiquidado             => "Liquidação",
        TipoOcorrencia.RetornoBaixado               => "Baixado",
        TipoOcorrencia.RetornoProtestadoCartorio    => "Protestado em Cartório",
        TipoOcorrencia.RetornoInstrucaoRejeitada    => "Instrução Rejeitada",
        _ => tipo.ToString()
    };

    // -------------------------------------------------------
    // Helpers privados
    // -------------------------------------------------------

    private static DateTime ParseDataCnab(string valor, string formato)
    {
        if (string.IsNullOrWhiteSpace(valor) || valor.All(c => c == '0'))
            return DateTime.MinValue;
        return DateTime.TryParseExact(valor.Trim(), formato,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var dt) ? dt : DateTime.MinValue;
    }

    private static decimal ParseValorCnab(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || valor.All(c => c == '0' || c == ' '))
            return 0;
        return decimal.Parse(valor.Trim()) / 100;
    }
}

/// <summary>Banco do Brasil via API REST (cobBancoDoBrasilAPI).</summary>
public class BancoBrasilApi : BancoBrasil
{
    public override TipoCobranca TipoCobranca => TipoCobranca.BancoDoBrasilAPI;

    protected override string FormatarNossoNumero(Boleto boleto, Beneficiario beneficiario)
    {
        string convenio = beneficiario.Convenio.Trim();
        string nossoNum = boleto.NossoNumero;
        return "000" + convenio.PadLeftZero(7) + nossoNum.PadLeftZero(10);
    }
}
