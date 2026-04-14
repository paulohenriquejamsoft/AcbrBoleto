using System.Text;
using ACBrBoleto.Core.Configuration;
using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Helpers;
using ACBrBoleto.Core.Interfaces;
using ACBrBoleto.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ACBrBoleto.Core.Services;

/// <summary>
/// Serviço principal de boletos. Orquestra geração de remessa, leitura de retorno e PDF.
/// Corresponde a TACBrBoleto no Delphi.
/// </summary>
public class BoletoService : IBoletoService
{
    private readonly List<Boleto> _boletos = new();
    private readonly IBancoService _banco;
    private readonly IBoletoRenderer? _renderer;
    private readonly ILogger<BoletoService> _logger;
    private readonly BoletoOptions _options;

    public BoletoService(
        IBancoService banco,
        IOptions<BoletoOptions> options,
        ILogger<BoletoService> logger,
        IBoletoRenderer? renderer = null)
    {
        _banco = banco;
        _options = options.Value;
        _logger = logger;
        _renderer = renderer;
        Beneficiario = new Beneficiario();
        LayoutRemessa = _options.LayoutRemessa;
    }

    public Beneficiario Beneficiario { get; set; }
    public IBancoService Banco => _banco;
    public IReadOnlyList<Boleto> Boletos => _boletos.AsReadOnly();
    public LayoutRemessa LayoutRemessa { get; set; }

    public void AdicionarBoleto(Boleto boleto) => _boletos.Add(boleto);
    public void RemoverBoleto(Boleto boleto) => _boletos.Remove(boleto);
    public void LimparBoletos() => _boletos.Clear();

    public void PreencherCodigoBarras(Boleto boleto)
    {
        boleto.CodigoBarras = _banco.MontarCodigoBarras(boleto, Beneficiario);
        boleto.LinhaDigitavel = _banco.MontarLinhaDigitavel(boleto.CodigoBarras, boleto, Beneficiario);
        boleto.FatorVencimento = CodigoBarrasService.CalcularFatorVencimento(boleto.Vencimento);
    }

    public string GerarRemessa(int numeroRemessa)
    {
        _logger.LogInformation("Gerando remessa {Layout} para {QtdBoletos} boleto(s), banco {Banco}",
            LayoutRemessa, _boletos.Count, _banco.Nome);

        var linhas = new List<string>();
        var encoding = Encoding.GetEncoding(_options.EncodingCnab);

        if (LayoutRemessa == LayoutRemessa.Cnab400)
        {
            _banco.GerarRegistroHeader400(numeroRemessa, Beneficiario, linhas);
            int seq = 1;
            foreach (var boleto in _boletos)
            {
                _banco.GerarRegistroTransacao400(boleto, Beneficiario, linhas, seq++);
            }
            _banco.GerarRegistroTrailler400(linhas, Beneficiario);
        }
        else
        {
            linhas.Add(_banco.GerarRegistroHeader240(numeroRemessa, Beneficiario));
            int seq = 1;
            foreach (var boleto in _boletos)
            {
                linhas.Add(_banco.GerarRegistroTransacao240(boleto, Beneficiario, seq++));
            }
            linhas.Add(_banco.GerarRegistroTrailler240(linhas, Beneficiario));
        }

        return string.Join("\r\n", linhas) + "\r\n";
    }

    public Task<string> GerarRemessaAsync(int numeroRemessa)
        => Task.FromResult(GerarRemessa(numeroRemessa));

    public void LerRetorno(string conteudo)
    {
        var linhas = conteudo
            .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        _logger.LogInformation("Lendo retorno {Layout}, {QtdLinhas} linhas",
            LayoutRemessa, linhas.Count);

        if (LayoutRemessa == LayoutRemessa.Cnab400)
            _banco.LerRetorno400(linhas, _boletos, Beneficiario);
        else
            _banco.LerRetorno240(linhas, _boletos, Beneficiario);
    }

    public Task LerRetornoAsync(string conteudo)
    {
        LerRetorno(conteudo);
        return Task.CompletedTask;
    }

    public Task<byte[]> GerarPdfAsync(Boleto boleto, LayoutBoleto layout = LayoutBoleto.Padrao)
    {
        if (_renderer == null)
            throw new InvalidOperationException(
                "Nenhum IBoletoRenderer registrado. Adicione ACBrBoleto.Pdf ao projeto.");

        return _renderer.GerarPdfAsync(boleto, Beneficiario, _banco, layout);
    }

    public Task<byte[]> GerarPdfLoteAsync(IEnumerable<Boleto> boletos, LayoutBoleto layout = LayoutBoleto.Padrao)
    {
        if (_renderer == null)
            throw new InvalidOperationException(
                "Nenhum IBoletoRenderer registrado. Adicione ACBrBoleto.Pdf ao projeto.");

        return _renderer.GerarPdfLoteAsync(boletos, Beneficiario, _banco, layout);
    }
}
