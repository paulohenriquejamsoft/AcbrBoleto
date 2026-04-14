using ACBrBoleto.Core.Enums;
using ACBrBoleto.Core.Models;

namespace ACBrBoleto.Bancos.Tests.Fixtures;

public static class TestFixtures
{
    public static Beneficiario BeneficiarioBB() => new()
    {
        Nome = "EMPRESA TESTE LTDA",
        CnpjCpf = "12345678000195",
        Agencia = "1234",
        AgenciaDigito = "5",
        Conta = "12345678",
        ContaDigito = "9",
        CodigoCedente = "123456",
        Convenio = "1234567",
        TipoCarteira = TipoCarteira.Registrada
    };

    public static Beneficiario BeneficiarioBradesco() => new()
    {
        Nome = "EMPRESA TESTE LTDA",
        CnpjCpf = "12345678000195",
        Agencia = "1234",
        AgenciaDigito = "5",
        Conta = "1234567",
        ContaDigito = "8",
        CodigoCedente = "123456"
    };

    public static Beneficiario BeneficiarioItau() => new()
    {
        Nome = "EMPRESA TESTE LTDA",
        CnpjCpf = "12345678000195",
        Agencia = "1234",
        AgenciaDigito = "5",
        Conta = "12345",
        ContaDigito = "6"
    };

    public static Beneficiario BeneficiarioSantander() => new()
    {
        Nome = "EMPRESA TESTE LTDA",
        CnpjCpf = "12345678000195",
        Agencia = "1234",
        AgenciaDigito = "5",
        Conta = "12345678",
        ContaDigito = "9",
        CodigoCedente = "1234567"
    };

    public static Beneficiario BeneficiarioCaixa() => new()
    {
        Nome = "EMPRESA TESTE LTDA",
        CnpjCpf = "12345678000195",
        Agencia = "1234",
        AgenciaDigito = "5",
        Conta = "12345678901",
        ContaDigito = "2",
        CodigoCedente = "123456",
        Modalidade = "14"
    };

    public static Boleto BoletoPadrao() => new()
    {
        Vencimento = new DateTime(2025, 6, 15),
        ValorDocumento = 1500.00m,
        NossoNumero = "12345678",
        NumeroDocumento = "DOC001",
        Carteira = "17",
        EspecieDoc = "DM",
        DataDocumento = new DateTime(2025, 5, 15),
        Pagador = PagadorPadrao()
    };

    public static Pagador PagadorPadrao() => new()
    {
        Nome = "CLIENTE TESTE",
        CnpjCpf = "52998224725",
        Logradouro = "Rua das Flores",
        Numero = "100",
        Bairro = "Centro",
        Cidade = "São Paulo",
        UF = "SP",
        CEP = "01310100"
    };
}
