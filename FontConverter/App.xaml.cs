using System.Windows;
using UniversalConverter.Services;
using Velopack;

namespace UniversalConverter;

public partial class App : Application
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        await AppUpdateService.TryCheckForUpdatesAsync();
    }
}
