namespace ACBrBoleto.WebServices.Base;

/// <summary>
/// Exceção lançada quando uma chamada ao WebService bancário falha de forma inesperada.
/// </summary>
public class WebServiceException : Exception
{
    public int? HttpStatusCode { get; }
    public string? ResponseBody { get; }

    public WebServiceException(string message) : base(message) { }

    public WebServiceException(string message, int httpStatusCode, string? responseBody = null)
        : base(message)
    {
        HttpStatusCode = httpStatusCode;
        ResponseBody = responseBody;
    }

    public WebServiceException(string message, Exception innerException)
        : base(message, innerException) { }
}
