using Hefi.Mobile.Pages;

namespace Hefi.Mobile;
using Microsoft.Extensions.DependencyInjection;
using Hefi.Mobile.Pages;

public partial class App : Application
{
    private readonly IServiceProvider _sp;

    public App(IServiceProvider sp)
    {
        InitializeComponent();
        _sp = sp;

        MainPage = new NavigationPage(_sp.GetRequiredService<LoadingPage>());
    }
}

    