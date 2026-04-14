using ACBrBoleto.Core.Enums;

namespace ACBrBoleto.Core.Models;

/// <summary>
/// Filtros para consulta de boletos via WebService.
/// Corresponde a TACBrBoletoWSFiltroConsulta no Delphi.
/// </summary>
public class FiltroConsulta
{
    public int ContaCaucao { get; set; }
    public string? CnpjCpfPagador { get; set; }
    public PeriodoData? DataVencimento { get; set; }
    public PeriodoData? DataRegistro { get; set; }
    public PeriodoData? DataMovimento { get; set; }
    public IndicadorSituacaoBoleto IndicadorSituacao { get; set; } = IndicadorSituacaoBoleto.Nenhum;
    public int CodigoEstadoTituloCobranca { get; set; }
    public IndicadorBoletoVencido BoletoVencido { get; set; } = IndicadorBoletoVencido.Nenhum;
    public string? NossoNumero { get; set; }
    public string? SeuNumero { get; set; }
    public int Pagina { get; set; } = 1;
    public int ItensPorPagina { get; set; } = 50;
}

public class PeriodoData
{
    public DateTime DataInicio { get; set; }
    public DateTime DataFinal { get; set; }

    public PeriodoData() { }
    public PeriodoData(DateTime inicio, DateTime fim)
    {
        DataInicio = inicio;
        DataFinal = fim;
    }
}
