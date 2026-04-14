using ACBrBoleto.Core.Helpers;
using FluentAssertions;

namespace ACBrBoleto.Core.Tests;

public class CnpjCpfValidatorTests
{
    [Theory]
    [InlineData("529.982.247-25", true)]
    [InlineData("111.111.111-11", false)]
    [InlineData("000.000.000-00", false)]
    public void ValidarCpf_DeveRetornarResultadoCorreto(string cpf, bool esperado)
        => CnpjCpfValidator.ValidarCpf(cpf).Should().Be(esperado);

    [Theory]
    [InlineData("11.222.333/0001-81", true)]
    [InlineData("11.111.111/1111-11", false)]
    public void ValidarCnpj_DeveRetornarResultadoCorreto(string cnpj, bool esperado)
        => CnpjCpfValidator.ValidarCnpj(cnpj).Should().Be(esperado);

    [Fact]
    public void Validar_CPF_DeveDetectarAutomaticamente()
        => CnpjCpfValidator.Validar("52998224725").Should().BeTrue();

    [Fact]
    public void Validar_CNPJ_DeveDetectarAutomaticamente()
        => CnpjCpfValidator.Validar("11222333000181").Should().BeTrue();
}
