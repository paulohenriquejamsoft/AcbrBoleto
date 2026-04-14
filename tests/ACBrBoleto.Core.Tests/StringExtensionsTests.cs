using ACBrBoleto.Core.Helpers;
using FluentAssertions;

namespace ACBrBoleto.Core.Tests;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("ABC123", "123")]
    [InlineData("12.345/0001-99", "12345000199")]
    [InlineData("", "")]
    public void OnlyNumbers_DeveRetornarApenasDigitos(string input, string esperado)
        => input.OnlyNumbers().Should().Be(esperado);

    [Theory]
    [InlineData("João", "Joao")]
    [InlineData("São Paulo", "Sao Paulo")]
    [InlineData("Conceição", "Conceicao")]
    public void RemoveAcentos_DeveEliminarAcentos(string input, string esperado)
        => input.RemoveAcentos().Should().Be(esperado);

    // CnabSubstring usa posição base 1 (igual ao Delphi)
    [Fact]
    public void CnabSubstring_Base1_DeveRetornarCampoCorreto()
    {
        string linha = "ABCDE12345";
        linha.CnabSubstring(1, 5).Should().Be("ABCDE");
        linha.CnabSubstring(6, 5).Should().Be("12345");
    }

    [Fact]
    public void ToValorCnab_DeveFormatarSemVirgula()
    {
        1500.00m.ToValorCnab(10).Should().Be("0000150000");
        0.50m.ToValorCnab(10).Should().Be("0000000050");
        9999999.99m.ToValorCnab(10).Should().Be("0999999999");
    }

    [Fact]
    public void PadLeftZero_DevePreencherComZeros()
        => "123".PadLeftZero(8).Should().Be("00000123");

    [Fact]
    public void TruncateOrPad_DeveTruncarSeExceder()
        => "ABCDEFGHIJ".TruncateOrPad(5).Should().Be("ABCDE");

    [Fact]
    public void ToDataCnab_DeveFormatarData()
    {
        var dt = new DateTime(2025, 6, 15);
        dt.ToDataCnab("ddMMyyyy").Should().Be("15062025");
        dt.ToDataCnab("ddMMyy").Should().Be("150625");
    }
}
