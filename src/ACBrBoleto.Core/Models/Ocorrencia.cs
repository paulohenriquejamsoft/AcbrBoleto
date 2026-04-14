using ACBrBoleto.Core.Enums;

namespace ACBrBoleto.Core.Models;

/// <summary>
/// Ocorrência de remessa ou retorno. Corresponde a TACBrOcorrencia no Delphi.
/// </summary>
public class Ocorrencia
{
    public TipoOcorrencia Tipo { get; set; }
    public ComplementoOcorrenciaOutrosDados ComplementoOutrosDados { get; set; }
    public string CodigoBanco { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;

    /// <summary>Motivos de rejeição (códigos por banco).</summary>
    public List<MotivoRejeicao> Motivos { get; set; } = new();
}

public class MotivoRejeicao
{
    public int Codigo { get; set; }
    public string Descricao { get; set; } = string.Empty;
}
