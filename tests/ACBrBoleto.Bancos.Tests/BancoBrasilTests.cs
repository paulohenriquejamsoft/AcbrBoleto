using ACBrBoleto.Bancos.BancoDoBrasil;
using ACBrBoleto.Bancos.Tests.Fixtures;
using ACBrBoleto.Core.Enums;
using FluentAssertions;

namespace ACBrBoleto.Bancos.Tests;

public class BancoBrasilTests
{
    private readonly BancoBrasil _banco = new();

    [Fact]
    public void Banco_DeveSerNumero001()
    {
        _banco.Numero.Should().Be(1);
        _banco.Nome.Should().Be("Banco do Brasil");
        _banco.TipoCobranca.Should().Be(TipoCobranca.BancoDoBrasil);
    }

    [Fact]
    public void MontarCodigoBarras_DeveTer44Posicoes()
    {
        var boleto = TestFixtures.BoletoPadrao();
        var beneficiario = TestFixtures.BeneficiarioBB();
        boleto.Carteira = "17";

        var cb = _banco.MontarCodigoBarras(boleto, beneficiario);

        cb.Length.Should().Be(44, "código de barras deve ter exatamente 44 posições");
    }

    [Fact]
    public void MontarCodigoBarras_DeveIniciarComCodigo001()
    {
        var boleto = TestFixtures.BoletoPadrao();
        var beneficiario = TestFixtures.BeneficiarioBB();

        var cb = _banco.MontarCodigoBarras(boleto, beneficiario);

        cb[..3].Should().Be("001", "primeiras 3 posições são o código do banco");
    }

    [Fact]
    public void MontarCodigoBarras_Posicao4DeveSer9()
    {
        var boleto = TestFixtures.BoletoPadrao();
        var beneficiario = TestFixtures.BeneficiarioBB();

        var cb = _banco.MontarCodigoBarras(boleto, beneficiario);

        cb[3].Should().Be('9', "posição 4 é o código da moeda Real (9)");
    }

    [Fact]
    public void MontarLinhaDigitavel_DeveTer5Campos()
    {
        var boleto = TestFixtures.BoletoPadrao();
        var beneficiario = TestFixtures.BeneficiarioBB();

        var cb = _banco.MontarCodigoBarras(boleto, beneficiario);
        var ld = _banco.MontarLinhaDigitavel(cb, boleto, beneficiario);

        ld.Split(' ').Should().HaveCount(5);
    }

    [Fact]
    public void GerarRemessa400_DeveGerarLinhasDe400Caracteres()
    {
        var boleto = TestFixtures.BoletoPadrao();
        var beneficiario = TestFixtures.BeneficiarioBB();
        boleto.Carteira = "17";

        var remessa = new List<string>();
        _banco.GerarRegistroHeader400(1, beneficiario, remessa);
        _banco.GerarRegistroTransacao400(boleto, beneficiario, remessa, 1);
        _banco.GerarRegistroTrailler400(remessa, beneficiario);

        remessa.Should().HaveCount(3);
        foreach (var linha in remessa)
        {
            linha.Length.Should().Be(400, $"linha '{linha[..20]}...' deve ter 400 caracteres");
        }
    }

    [Fact]
    public void GerarRemessa400_HeaderDeveComecarCom0()
    {
        var remessa = new List<string>();
        _banco.GerarRegistroHeader400(1, TestFixtures.BeneficiarioBB(), remessa);
        remessa[0][0].Should().Be('0');
    }

    [Fact]
    public void GerarRemessa400_TransacaoDeveComecarCom1()
    {
        var boleto = TestFixtures.BoletoPadrao();
        var beneficiario = TestFixtures.BeneficiarioBB();
        var remessa = new List<string>();
        _banco.GerarRegistroTransacao400(boleto, beneficiario, remessa, 1);
        remessa[0][0].Should().Be('1');
    }

    [Fact]
    public void GerarRemessa400_TraillerDeveComecarCom9()
    {
        var remessa = new List<string> { "linha1", "linha2" };
        _banco.GerarRegistroTrailler400(remessa, TestFixtures.BeneficiarioBB());
        remessa.Last()[0].Should().Be('9');
    }

    [Fact]
    public void CodOcorrenciaParaTipo_6_DeveSerLiquidado()
        => _banco.CodOcorrenciaParaTipo(6).Should().Be(TipoOcorrencia.RetornoLiquidado);

    [Fact]
    public void CodOcorrenciaParaTipo_9_DeveSerBaixado()
        => _banco.CodOcorrenciaParaTipo(9).Should().Be(TipoOcorrencia.RetornoBaixado);

    [Fact]
    public void CodOcorrenciaParaTipo_2_DeveSerRegistroConfirmado()
        => _banco.CodOcorrenciaParaTipo(2).Should().Be(TipoOcorrencia.RetornoRegistroConfirmado);
}
