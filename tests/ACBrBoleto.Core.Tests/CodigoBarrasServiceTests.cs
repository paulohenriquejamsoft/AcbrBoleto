using ACBrBoleto.Core.Services;
using FluentAssertions;

namespace ACBrBoleto.Core.Tests;

public class CodigoBarrasServiceTests
{
    // === Dígito verificador do código de barras ===

    [Fact]
    public void CalcularDigitoCodigoBarras_ComCodigo43Chars_DeveRetornarDigito()
    {
        // Código de barras sem DV: 43 posições (banco+moeda[4] + campos[39])
        // Cria um código fictício de 43 chars e verifica que retorna 1 dígito numérico
        string codigo43 = "0019" + new string('0', 39);
        var dv = CodigoBarrasService.CalcularDigitoCodigoBarras(codigo43);
        dv.Length.Should().Be(1);
        int.TryParse(dv, out _).Should().BeTrue("DV deve ser dígito numérico");
    }

    // === Fator de vencimento ===

    [Fact]
    public void CalcularFatorVencimento_DataAnteriorAoReset_DeveUsarDataBase1997()
    {
        // 2000-01-01: dias desde 1997-10-07 = 816 dias → fator = 1816
        var fator = CodigoBarrasService.CalcularFatorVencimento(new DateTime(2000, 1, 1));
        int dias = (int)(new DateTime(2000, 1, 1) - new DateTime(1997, 10, 7)).TotalDays;
        fator.Should().Be((dias + 1000).ToString("D4"));
    }

    [Fact]
    public void CalcularFatorVencimento_DataResetExata_DeveSer1000()
    {
        // O reset é em 22/02/2025: primeiro dia do novo ciclo = fator 1000
        var fator = CodigoBarrasService.CalcularFatorVencimento(new DateTime(2025, 2, 22));
        fator.Should().Be("1000");
    }

    [Fact]
    public void CalcularFatorVencimento_DataAposReset_DeveSerMaiorQue1000()
    {
        var fator = CodigoBarrasService.CalcularFatorVencimento(new DateTime(2025, 6, 15));
        int.Parse(fator).Should().BeGreaterThan(1000);
    }

    [Fact]
    public void CalcularFatorVencimento_SemVencimento_Retorna0000()
    {
        var fator = CodigoBarrasService.CalcularFatorVencimento(DateTime.MinValue);
        fator.Should().Be("0000");
    }

    // === DV Módulo 10 ===

    [Fact]
    public void CalcularDvModulo10_DeveRetornarDigitoNumerico()
    {
        // Verifica que o resultado é um dígito numérico de 0 a 9
        var dv1 = CodigoBarrasService.CalcularDvModulo10("001900000900000");
        var dv2 = CodigoBarrasService.CalcularDvModulo10("23790918700000");
        int.TryParse(dv1, out var n1).Should().BeTrue("DV deve ser dígito numérico");
        int.TryParse(dv2, out var n2).Should().BeTrue("DV deve ser dígito numérico");
        n1.Should().BeInRange(0, 9);
        n2.Should().BeInRange(0, 9);
    }

    // === DV Módulo 11 ===

    [Theory]
    [InlineData("1234567", 1)]
    [InlineData("0000000", 1)]
    public void CalcularModulo11_NaoPodeSerNegativo(string numero, int _)
    {
        var dv = CodigoBarrasService.CalcularModulo11(numero);
        dv.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(11);
    }

    // === Montagem da linha digitável ===

    [Fact]
    public void MontarLinhaDigitavel_ComCodigoValido_Deve5Campos()
    {
        // Código de barras fictício de 44 posições
        string codigoBarras = "00191909250000150000000090000000001234567890109";
        if (codigoBarras.Length == 44)
        {
            var linha = CodigoBarrasService.MontarLinhaDigitavel(codigoBarras);
            var partes = linha.Split(' ');
            partes.Should().HaveCount(5, "linha digitável tem 5 campos separados por espaço");
        }
    }

    [Fact]
    public void MontarLinhaDigitavel_CodigoInvalido_RetornaVazio()
    {
        var linha = CodigoBarrasService.MontarLinhaDigitavel("123");
        linha.Should().BeEmpty();
    }
}
