using ACBrBoleto.Core.Enums;
using ACBrBoleto.WebServices.Asaas;
using ACBrBoleto.WebServices.BancoBrasil;
using ACBrBoleto.WebServices.Banrisul;
using ACBrBoleto.WebServices.Bradesco;
using ACBrBoleto.WebServices.BTGPactual;
using ACBrBoleto.WebServices.C6;
using ACBrBoleto.WebServices.Caixa;
using ACBrBoleto.WebServices.Cora;
using ACBrBoleto.WebServices.Credisis;
using ACBrBoleto.WebServices.Cresol;
using ACBrBoleto.WebServices.Inter;
using ACBrBoleto.WebServices.Itau;
using ACBrBoleto.WebServices.Kobana;
using ACBrBoleto.WebServices.PenseBank;
using ACBrBoleto.WebServices.Safra;
using ACBrBoleto.WebServices.Santander;
using ACBrBoleto.WebServices.Sicoob;
using ACBrBoleto.WebServices.Sicredi;
using Microsoft.Extensions.Caching.Memory;

namespace ACBrBoleto.WebServices.Base;

/// <summary>
/// Fábrica que instancia a implementação correta de <see cref="IBoletoWebService"/>
/// com base no <see cref="TipoCobranca"/>.
/// Corresponde ao switch de instanciação em TACBrBancoClass no Delphi.
/// </summary>
public static class BoletoWebServiceFactory
{
    public static IBoletoWebService Create(
        TipoCobranca tipo,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache) => tipo switch
    {
        TipoCobranca.BancoDoBrasilAPI   => new BancoBrasilApiWebService(httpClientFactory, cache),
        TipoCobranca.Bradesco           => new BradescoWebService(httpClientFactory, cache),
        TipoCobranca.Itau               => new ItauApiWebService(httpClientFactory, cache),
        TipoCobranca.BancoInter         => new InterApiWebService(httpClientFactory, cache),
        TipoCobranca.Sicredi            => new SicrediApiWebService(httpClientFactory, cache),
        TipoCobranca.BancoSicoob        => new SicoobV3WebService(httpClientFactory, cache),
        TipoCobranca.Santander          => new SantanderApiWebService(httpClientFactory, cache),
        TipoCobranca.Banrisul           => new BanrisulWebService(httpClientFactory, cache),
        TipoCobranca.BancoC6            => new C6WebService(httpClientFactory, cache),
        TipoCobranca.CaixaEconomica     => new CaixaWebService(httpClientFactory, cache),
        TipoCobranca.BancoCora          => new CoraWebService(httpClientFactory, cache),
        TipoCobranca.BancoAsaas         => new AsaasWebService(httpClientFactory, cache),
        TipoCobranca.BTGPactual         => new BtgPactualWebService(httpClientFactory, cache),
        TipoCobranca.CrediSIS           => new CredisisWebService(httpClientFactory, cache),
        TipoCobranca.BancoCresolSCRS    => new CresolWebService(httpClientFactory, cache),
        TipoCobranca.Kobana             => new KobanaWebService(httpClientFactory, cache),
        TipoCobranca.PenseBankAPI       => new PenseBankWebService(httpClientFactory, cache),
        TipoCobranca.BancoSafra         => new SafraWebService(httpClientFactory, cache),
        _ => throw new NotSupportedException(
                 $"Banco '{tipo}' não possui WebService implementado nesta versão.")
    };
}
