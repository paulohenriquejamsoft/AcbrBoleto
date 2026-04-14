namespace ACBrBoleto.Core.Enums;

public enum LayoutRemessa { Cnab400, Cnab240 }

public enum ResponsavelEmissao
{
    ClienteEmite,
    BancoEmite,
    BancoReemite,
    BancoNaoReemite,
    BancoPreEmite
}

public enum CaracteristicaTitulo
{
    Simples, Vinculada, Caucionada, Descontada, Vendor, Direta,
    SimplesRapComReg, CaucionadaRapComReg, DiretaEspecial
}

public enum TipoPessoa { Fisica, Juridica, Outras, Nenhum }

public enum LayoutBoleto
{
    Padrao, Carne, Fatura, PadraoEntrega, ReciboTopo, PadraoEntrega2,
    FaturaDetal, Termica80mm, PadraoPIX, PrestaServicos, CarneA5
}

public enum AceiteTitulo { Sim, Nao }

public enum TipoDiasInstrucao { Corridos, Uteis }

public enum TipoImpressao { Carne, Normal }

public enum TipoDocumento { Tradicional = 1, Escritural = 2 }

public enum TipoCarteira { Simples, Registrada, Eletronica }

public enum CarteiraEnvio { Cedente, Banco, BancoEmail }

public enum CodigoDesconto { SemDesconto, ValorFixo, Percentual }

public enum CodigoJuros { ValorDia, TaxaMensal, Isento, ValorMensal, TaxaDiaria }

public enum CodigoMulta { ValorFixo, Percentual, Isento }

public enum CodigoNegativacao
{
    Nenhum, ProtestarCorrido, ProtestarUteis, NaoProtestar,
    Negativar, NaoNegativar, Cancelamento, NegativarUteis
}

public enum TipoOperacao
{
    Inclui, Altera, Baixa, Consulta, ConsultaDetalhe,
    PixCriar, PixCancelar, PixConsultar, Cancelar, Ticket
}

public enum TipoPagamento
{
    AceitaQualquerValor,
    AceitaValoresEntreMinMax,
    NaoAceitaValorDivergente,
    SomenteValorMinimo
}

public enum IndicadorSituacaoBoleto { Nenhum, Aberto, Baixado, Cancelado }

public enum IndicadorBoletoVencido { Nenhum, Nao, Sim }

public enum MetodoHttp { Post, Get, Patch, Put, Delete }

public enum TipoAmbienteWebService { Producao, Homologacao, Sandbox }

public enum TipoDesconto
{
    NaoConceder,
    ValorFixoAteData,
    PercentualAteData,
    ValorAntecipacaoDiaCorrido,
    ValorAntecipacaoDiaUtil,
    PercentualNominalDiaCorrido,
    PercentualNominalDiaUtil,
    CancelamentoDesconto
}

public enum FiltroBoletoFc { Nenhum, Pdf, Html, Jpg }

public enum IdentificacaoDistribuicao { BancoDistribui, ClienteDistribui }

public enum IntegradoraBoleto { Nenhum, Kobana }

public enum CalculoDigito { Modulo10, Modulo11 }

public enum TipoPIXChave { CPF, CNPJ, Telefone, Email, ChaveAleatoria }

public enum ComplementoOcorrenciaOutrosDados
{
    Desconto,
    JurosDia,
    DescontoDiasAntecipacao,
    DataLimiteDesconto,
    CancelaProtestoAutomatico,
    CarteiraCobranca,
    CancelaNegativacaoAutomatica
}
