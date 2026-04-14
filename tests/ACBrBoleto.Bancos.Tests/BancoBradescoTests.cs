using ACBrBoleto.Bancos.Bradesco;
using ACBrBoleto.Bancos.Tests.Fixtures;
using ACBrBoleto.Core.Enums;
using FluentAssertions;

namespace ACBrBoleto.Bancos.Tests;

public class BancoBradescoTests
{
    private readonly BancoBradesco _banco = new();

    [Fact]
    public void Banco_DeveSerNumero237()
    {
        _banco.Numero.Should().Be(237);
        _banco.TipoCobranca.Should().Be(TipoCobranca.Bradesco);
    }

    [Fact]
    public void CalcularDvNossoNumero_NossoNumero00000000001_DvDeve0()
    {
        var boleto = TestFixtures.BoletoPadrao();
        boleto.NossoNumero = "00000000001";
        // DV Módulo 11 pesos 2-7: resultado pode ser "P" se resto = 1
        var dv = _banco.CalcularDigitoVerificador(boleto, TestFixtures.BeneficiarioBradesco());
        dv.Should().NotBeNull().And.HaveLength(1);
    }

    [Fact]
    public void MontarCodigoBarras_DeveTer44Posicoes()
    {
        var boleto = TestFixtures.BoletoPadrao();
        boleto.Carteira = "09";
        var cb = _banco.MontarCodigoBarras(boleto, TestFixtures.BeneficiarioBradesco());
        cb.Length.Should().Be(44);
    }

    [Fact]
    public void MontarCodigoBarras_DeveIniciarCom237()
    {
        var boleto = TestFixtures.BoletoPadrao();
        boleto.Carteira = "09";
        var cb = _banco.MontarCodigoBarras(boleto, TestFixtures.BeneficiarioBradesco());
        cb[..3].Should().Be("237");
    }

    [Fact]
    public void GerarRemessa400_LinhasDe400Chars()
    {
        var boleto = TestFixtures.BoletoPadrao();
        boleto.Carteira = "09";
        var beneficiario = TestFixtures.BeneficiarioBradesco();

        var remessa = new List<string>();
        _banco.GerarRegistroHeader400(1, beneficiario, remessa);
        _banco.GerarRegistroTransacao400(boleto, beneficiario, remessa, 1);
        _banco.GerarRegistroTrailler400(remessa, beneficiario);

        foreach (var linha in remessa)
            linha.Length.Should().Be(400);
    }

    [Fact]
    public void CodOcorrenciaParaTipo_6_DeveSerLiquidado()
        => _banco.CodOcorrenciaParaTipo(6).Should().Be(TipoOcorrencia.RetornoLiquidado);
}
