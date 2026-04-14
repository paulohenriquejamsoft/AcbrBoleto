using ACBrBoleto.Core.Enums;

namespace ACBrBoleto.Core.Models;

/// <summary>
/// Representa um boleto bancário individual.
/// Corresponde a TACBrTitulo no Delphi (~100 propriedades).
/// </summary>
public class Boleto
{
    // === Identificação ===
    public string NossoNumero { get; set; } = string.Empty;
    public string DigitoNossoNumero { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public string SeuNumero { get; set; } = string.Empty;
    public string NumeroControle { get; set; } = string.Empty;

    // === Carteira ===
    public string Carteira { get; set; } = string.Empty;
    public string CarteiraFormatada { get; set; } = string.Empty;
    public TipoCarteira TipoCarteira { get; set; } = TipoCarteira.Simples;

    // === Datas ===
    public DateTime Vencimento { get; set; } = DateTime.Today.AddDays(3);
    public DateTime DataDocumento { get; set; } = DateTime.Today;
    public DateTime DataProcessamento { get; set; } = DateTime.Today;
    public DateTime? DataProtesto { get; set; }
    public DateTime? DataBaixa { get; set; }
    public DateTime? DataDesconto { get; set; }
    public DateTime? DataDesconto2 { get; set; }
    public DateTime? DataDesconto3 { get; set; }
    public DateTime? DataMoraJuros { get; set; }
    public DateTime? DataMulta { get; set; }
    public DateTime? DataLimiteRecebimento { get; set; }
    public DateTime? DataEmissao { get; set; }

    // === Valores ===
    public decimal ValorDocumento { get; set; }
    public decimal ValorAbatimento { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal ValorDesconto2 { get; set; }
    public decimal ValorDesconto3 { get; set; }
    public decimal ValorMoraJuros { get; set; }
    public decimal ValorMulta { get; set; }
    public decimal ValorMinimo { get; set; }
    public decimal ValorMaximo { get; set; }
    public decimal ValorIOF { get; set; }
    public decimal PercentualDesconto { get; set; }
    public decimal PercentualDesconto2 { get; set; }
    public decimal PercentualDesconto3 { get; set; }
    public decimal PercentualMoraJuros { get; set; }
    public decimal PercentualMulta { get; set; }
    public decimal PercentualValorMinimo { get; set; }
    public decimal PercentualValorMaximo { get; set; }

    // === Códigos ===
    public CodigoDesconto CodigoDesconto { get; set; } = CodigoDesconto.SemDesconto;
    public CodigoDesconto CodigoDesconto2 { get; set; } = CodigoDesconto.SemDesconto;
    public CodigoDesconto CodigoDesconto3 { get; set; } = CodigoDesconto.SemDesconto;
    public CodigoJuros CodigoJuros { get; set; } = CodigoJuros.Isento;
    public CodigoMulta CodigoMulta { get; set; } = CodigoMulta.Isento;
    public CodigoNegativacao CodigoNegativacao { get; set; } = CodigoNegativacao.Nenhum;
    public TipoDesconto TipoDesconto { get; set; } = TipoDesconto.NaoConceder;
    public TipoDesconto TipoDesconto2 { get; set; } = TipoDesconto.NaoConceder;
    public TipoDesconto TipoDesconto3 { get; set; } = TipoDesconto.NaoConceder;

    // === Espécie e Aceite ===
    public string EspecieDoc { get; set; } = "DM";
    public AceiteTitulo Aceite { get; set; } = AceiteTitulo.Nao;
    public CaracteristicaTitulo CaracteristicaTitulo { get; set; } = CaracteristicaTitulo.Simples;
    public TipoDiasInstrucao TipoDiasProtesto { get; set; } = TipoDiasInstrucao.Corridos;
    public TipoDiasInstrucao TipoDiasBaixa { get; set; } = TipoDiasInstrucao.Corridos;
    public int DiasProtesto { get; set; }
    public int DiasBaixa { get; set; }
    public TipoPagamento TipoPagamento { get; set; } = TipoPagamento.NaoAceitaValorDivergente;
    public TipoImpressao TipoImpressao { get; set; } = TipoImpressao.Normal;
    public string QuantidadeParcela { get; set; } = string.Empty;

    // === Pagador ===
    public Pagador Pagador { get; set; } = new();

    // === Campos auxiliares gerados ===
    public string CodigoBarras { get; set; } = string.Empty;
    public string LinhaDigitavel { get; set; } = string.Empty;
    public string FatorVencimento { get; set; } = string.Empty;

    // === PIX ===
    public ChavePix? PIX { get; set; }

    // === NFe ===
    public List<DadosNFe> NFes { get; set; } = new();

    // === Ocorrências ===
    public Ocorrencia? OcorrenciaOriginal { get; set; }
    public Liquidacao? Liquidacao { get; set; }

    // === Instruções de impressão ===
    public string Instrucao1 { get; set; } = string.Empty;
    public string Instrucao2 { get; set; } = string.Empty;
    public string Instrucao3 { get; set; } = string.Empty;
    public string Instrucao4 { get; set; } = string.Empty;
    public string Instrucao5 { get; set; } = string.Empty;
    public string Instrucao6 { get; set; } = string.Empty;
    public string LocalPagamento { get; set; } = string.Empty;
    public List<string> MensagensImpressao { get; set; } = new();

    // === Dados adicionais ===
    public string UsoCedente { get; set; } = string.Empty;
    public string ConvenioTaxa { get; set; } = string.Empty;
    public string ConvenioLote { get; set; } = string.Empty;
    public string CIPCodigo { get; set; } = string.Empty;
    public string GrupoRateio { get; set; } = string.Empty;
    public bool Hibrido { get; set; }
}
