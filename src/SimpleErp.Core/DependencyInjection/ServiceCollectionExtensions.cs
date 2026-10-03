using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SimpleErp.Core.Services;
using SimpleErp.Core.ViewModels;

namespace SimpleErp.Core.DependencyInjection;

/// <summary>Registers everything from the UI-independent core. The UI project adds its own services on top.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds storage, the shared store and all view models. The caller must also register
    /// <see cref="Abstractions.IDialogService"/> and <see cref="Abstractions.IShellService"/>.
    /// An <see cref="IErpRepository"/> registered before this call is kept (useful for tests or another data file).
    /// </summary>
    public static IServiceCollection AddSimpleErpCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IErpRepository>(_ => JsonErpRepository.CreateDefault());
        services.AddSingleton<ErpStore>();
        services.AddSingleton<AppStatus>();

        // Pages are singletons: they keep their filters, selection and unsaved edits while you switch pages.
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<EmployeesViewModel>();
        services.AddSingleton<ProjectsViewModel>();
        services.AddSingleton<ReportsViewModel>();
        services.AddSingleton<ShellViewModel>();

        return services;
    }
}
