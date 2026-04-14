using ACBrBoleto.Bancos.Itau;
using ACBrBoleto.Bancos.Tests.Fixtures;
using ACBrBoleto.Core.Enums;
using FluentAssertions;

namespace ACBrBoleto.Bancos.Tests;

public class BancoItauTests
{
    private readonly BancoItau _banco = new();

    [Fact]
    public void Banco_DeveSerNumero341()
    {
        _banco.Numero.Should().Be(341);
        _banco.TipoCobranca.Should().Be(TipoCobranca.Itau);
    }

    [Fact]
    public void MontarCodigoBarras_DeveTer44Posicoes()
    {
        var boleto = TestFixtures.BoletoPadrao();
        boleto.Carteira = "109";
        var cb = _banco.MontarCodigoBarras(boleto, TestFixtures.BeneficiarioItau());
        cb.Length.Should().Be(44);
    }

    [Fact]
    public void MontarCodigoBarras_DeveIniciarCom341()
    {
        var boleto = TestFixtures.BoletoPadrao();
        boleto.Carteira = "109";
        var cb = _banco.MontarCodigoBarras(boleto, TestFixtures.BeneficiarioItau());
        cb[..3].Should().Be("341");
    }

    [Fact]
    public void GerarRemessa400_LinhasDe400Chars()
    {
        var boleto = TestFixtures.BoletoPadrao();
        boleto.Carteira = "109";
        var beneficiario = TestFixtures.BeneficiarioItau();

        var remessa = new List<string>();
        _banco.GerarRegistroHeader400(1, beneficiario, remessa);
        _banco.GerarRegistroTransacao400(boleto, beneficiario, remessa, 1);
        _banco.GerarRegistroTrailler400(remessa, beneficiario);

        foreach (var linha in remessa)
            linha.Length.Should().Be(400);
    }
}
