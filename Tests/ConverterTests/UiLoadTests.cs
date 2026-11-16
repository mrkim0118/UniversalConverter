using Xunit;

namespace ConverterTests;

public class UiLoadTests
{
    // Builds the app resources (Fluent theme + styles) and the main window with all screens,
    // without ever showing a window. Catches XAML, missing-resource and theme-loading errors.
    [Fact]
    public void App_resources_and_main_window_load_without_showing()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var app = new UniversalConverter.App();
                app.InitializeComponent();
                var window = new UniversalConverter.MainWindow();
                Assert.NotNull(window.Content);
                Assert.NotNull(app.TryFindResource("TextFillColorPrimaryBrush"));   // Fluent theme is merged
                window.Close();
                app.Shutdown();
            }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(error);
    }
}
