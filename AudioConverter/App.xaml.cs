using System.Windows;

namespace UniversalConverter;

public partial class App : Application
{
    [STAThread]
    public static void Main(string[] args)
    {
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
