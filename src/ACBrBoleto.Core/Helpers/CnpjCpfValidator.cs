namespace ACBrBoleto.Core.Helpers;

public static class CnpjCpfValidator
{
    public static bool ValidarCpf(string cpf)
    {
        var digits = cpf.OnlyNumbers();
        if (digits.Length != 11 || digits.All(c => c == digits[0])) return false;

        int soma = 0;
        for (int i = 0; i < 9; i++) soma += int.Parse(digits[i].ToString()) * (10 - i);
        int d1 = soma % 11 < 2 ? 0 : 11 - soma % 11;

        soma = 0;
        for (int i = 0; i < 10; i++) soma += int.Parse(digits[i].ToString()) * (11 - i);
        int d2 = soma % 11 < 2 ? 0 : 11 - soma % 11;

        return digits[9] - '0' == d1 && digits[10] - '0' == d2;
    }

    public static bool ValidarCnpj(string cnpj)
    {
        var digits = cnpj.OnlyNumbers();
        if (digits.Length != 14 || digits.All(c => c == digits[0])) return false;

        int[] pesos1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] pesos2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        int soma = 0;
        for (int i = 0; i < 12; i++) soma += (digits[i] - '0') * pesos1[i];
        int d1 = soma % 11 < 2 ? 0 : 11 - soma % 11;

        soma = 0;
        for (int i = 0; i < 13; i++) soma += (digits[i] - '0') * pesos2[i];
        int d2 = soma % 11 < 2 ? 0 : 11 - soma % 11;

        return digits[12] - '0' == d1 && digits[13] - '0' == d2;
    }

    public static bool Validar(string cnpjCpf)
    {
        var digits = cnpjCpf.OnlyNumbers();
        return digits.Length == 11 ? ValidarCpf(digits) : ValidarCnpj(digits);
    }
}
