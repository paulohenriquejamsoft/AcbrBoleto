namespace ACBrBoleto.Core.Models;

/// <summary>
/// Resultado de uma operação via WebService.
/// Corresponde a TACBrBoletoRetornoWS no Delphi.
/// </summary>
public class RetornoWebService
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string CodigoRetorno { get; set; } = string.Empty;
    public string NossoNumero { get; set; } = string.Empty;
    public string LinhaDigitavel { get; set; } = string.Empty;
    public string CodigoBarras { get; set; } = string.Empty;
    public string QrCodePix { get; set; } = string.Empty;
    public string RetornoWS { get; set; } = string.Empty;
    public List<string> Erros { get; set; } = new();

    public static RetornoWebService Ok(string nossoNumero = "", string mensagem = "")
        => new() { Sucesso = true, NossoNumero = nossoNumero, Mensagem = mensagem };

    public static RetornoWebService Erro(string mensagem, string codigo = "")
        => new() { Sucesso = false, Mensagem = mensagem, CodigoRetorno = codigo };
}
