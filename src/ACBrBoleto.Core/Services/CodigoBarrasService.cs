using ACBrBoleto.Core.Helpers;

namespace ACBrBoleto.Core.Services;

/// <summary>
/// Lógica comum de código de barras e linha digitável.
/// Algoritmos críticos migrados do ACBrBoleto.pas (TACBrBancoClass).
/// </summary>
public static class CodigoBarrasService
{
    /// <summary>
    /// Calcula o dígito verificador do código de barras (posição 5).
    /// Módulo 11, pesos 2 a 9. Resto 0 ou 1 → DV = 1.
    /// CRÍTICO: reproduzir exatamente o comportamento Delphi.
    /// </summary>
    public static string CalcularDigitoCodigoBarras(string codigoBarrasSemDv)
    {
        // codigoBarrasSemDv tem 43 posições (sem o DV na posição 5)
        int soma = 0;
        int peso = 2;
        for (int i = codigoBarrasSemDv.Length - 1; i >= 0; i--)
        {
            soma += (codigoBarrasSemDv[i] - '0') * peso;
            peso = peso == 9 ? 2 : peso + 1;
        }
        int resto = soma % 11;
        int dv = (resto == 0 || resto == 1) ? 1 : 11 - resto;
        return dv.ToString();
    }

    /// <summary>
    /// Calcula o fator de vencimento (5 dígitos, posições 5-9 do código de barras).
    /// Data base: 07/10/1997 = fator 1000.
    /// Reset em 22/02/2025: fator volta a 1000.
    /// Boleto sem vencimento: "0000".
    /// </summary>
    public static string CalcularFatorVencimento(DateTime dataVencimento)
    {
        if (dataVencimento == DateTime.MinValue || dataVencimento.Year < 1990)
            return "0000";

        var dataBase = BoletoConstants.DataBaseFatorVencimento;

        // Após o reset de 22/02/2025
        if (dataVencimento >= BoletoConstants.DataResetFatorVencimento)
        {
            // Nova data base para o ciclo pós-reset
            var novaDataBase = new DateTime(2025, 2, 22);
            int fator = (int)(dataVencimento - novaDataBase).TotalDays + 1000;
            return fator.ToString("D4");
        }

        int dias = (int)(dataVencimento - dataBase).TotalDays + 1000;
        return dias.ToString("D4");
    }

    /// <summary>
    /// Calcula o DV de um campo da linha digitável (Módulo 10).
    /// Pesos alternados 2 e 1. Se produto > 9, soma os dígitos.
    /// </summary>
    public static string CalcularDvModulo10(string campo)
    {
        int soma = 0;
        int peso = 2;
        for (int i = campo.Length - 1; i >= 0; i--)
        {
            int produto = (campo[i] - '0') * peso;
            soma += produto > 9 ? produto / 10 + produto % 10 : produto;
            peso = peso == 2 ? 1 : 2;
        }
        int resto = soma % 10;
        return resto == 0 ? "0" : (10 - resto).ToString();
    }

    /// <summary>
    /// Calcula dígito verificador Módulo 11.
    /// pesoInicial de 2, pesoFinal até 9, da direita para esquerda.
    /// Resto 0 ou 1 → DV conforme parâmetros.
    /// </summary>
    public static int CalcularModulo11(string numero, int pesoInicial = 2, int pesoFinal = 9,
        int dvParaResto0 = 1, int dvParaResto1 = 1)
    {
        int soma = 0;
        int peso = pesoInicial;
        for (int i = numero.Length - 1; i >= 0; i--)
        {
            soma += (numero[i] - '0') * peso;
            peso = peso == pesoFinal ? pesoInicial : peso + 1;
        }
        int resto = soma % 11;
        if (resto == 0) return dvParaResto0;
        if (resto == 1) return dvParaResto1;
        return 11 - resto;
    }

    /// <summary>
    /// Monta a linha digitável a partir do código de barras de 44 posições.
    /// Estrutura padrão FEBRABAN (boleto bancário normal).
    /// </summary>
    public static string MontarLinhaDigitavel(string codigoBarras44)
    {
        if (codigoBarras44.Length != 44) return string.Empty;

        // Extrai campo livre (posições 20-44, sem o DV geral na pos 5)
        // Código de barras: BBBM DVVVVVVVVVCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC
        // pos:              1234 5678901234567890123456789012345678901234
        string banco = codigoBarras44[..3];
        string moeda = codigoBarras44[3].ToString();
        string dvGeral = codigoBarras44[4].ToString();
        string fatorVenc = codigoBarras44[5..10];
        string valor = codigoBarras44[10..20];
        string campoLivre = codigoBarras44[19..]; // 25 posições (pos 20-44)

        // Campo 1: banco(3) + moeda(1) + campoLivre[0..4](5) + DV10
        string c1Sem = banco + moeda + campoLivre[..5];
        string campo1 = $"{c1Sem[..4]}.{c1Sem[4..]}{CalcularDvModulo10(c1Sem)}";

        // Campo 2: campoLivre[5..14](10) + DV10
        string c2Sem = campoLivre[5..15];
        string campo2 = $"{c2Sem[..5]}.{c2Sem[5..]}{CalcularDvModulo10(c2Sem)}";

        // Campo 3: campoLivre[15..24](10) + DV10
        string c3Sem = campoLivre[15..25];
        string campo3 = $"{c3Sem[..5]}.{c3Sem[5..]}{CalcularDvModulo10(c3Sem)}";

        // Campo 4: DV geral
        string campo4 = dvGeral;

        // Campo 5: fatorVencimento(4) + valor(10)
        string campo5 = fatorVenc + valor;

        return $"{campo1} {campo2} {campo3} {campo4} {campo5}";
    }
}
