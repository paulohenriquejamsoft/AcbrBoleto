using ACBrBoleto.Core.Configuration;
using ACBrBoleto.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ACBrBoleto.Core.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra os serviços do ACBrBoleto.Core no contêiner de DI.
    /// </summary>
    public static IServiceCollection AddACBrBoleto(
        this IServiceCollection services,
        Action<BoletoOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);
        else
            services.Configure<BoletoOptions>(_ => { });

        services.AddScoped<IBoletoService, BoletoService>();
        return services;
    }
}
