using ACBrBoleto.Bancos;
using ACBrBoleto.Core.Enums;
using FluentAssertions;

namespace ACBrBoleto.Bancos.Tests;

public class BancoFactoryTests
{
    [Theory]
    [InlineData(TipoCobranca.BancoDoBrasil, 1)]
    [InlineData(TipoCobranca.Bradesco, 237)]
    [InlineData(TipoCobranca.Itau, 341)]
    [InlineData(TipoCobranca.Santander, 33)]
    [InlineData(TipoCobranca.CaixaEconomica, 104)]
    public void Create_DeveRetornarBancoComNumeroCorreto(TipoCobranca tipo, int numeroEsperado)
    {
        var banco = BancoFactory.Create(tipo);
        banco.Numero.Should().Be(numeroEsperado);
    }

    [Fact]
    public void Create_BancoNaoMigrado_DevelancarExcecao()
    {
        var acao = () => BancoFactory.Create(TipoCobranca.HSBC);
        acao.Should().Throw<NotSupportedException>()
            .WithMessage("*ainda não migrado*");
    }
}
