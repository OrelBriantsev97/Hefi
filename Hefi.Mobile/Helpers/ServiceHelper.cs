namespace Hefi.Mobile.Helpers;

// provides easy access to di services from anywhere in the app
public static class ServiceHelper
{
    // retrieves a registered service of type TService from the MAUI dependency container.
    // throws an exception if the service is not registered or context is unavailable.
    public static TService GetService<TService>() =>
        Current.GetService<TService>() ?? throw new InvalidOperationException($"{typeof(TService)} not registered.");

    // gets the current "IServiceProvider from the app context.
    // throws exception if app context or handler is not yet initialized.
    public static IServiceProvider Current =>
        Application.Current?.Handler?.MauiContext?.Services
        ?? throw new InvalidOperationException("MauiContext not available");
}
