using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace ReNative3D36S;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Register BEFORE WPF initializes the StartupUri window: XAML image
        // failures can otherwise terminate the application without any UI.
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex) WriteStartupLog(ex);
        };
        base.OnStartup(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
        var path = WriteStartupLog(args.Exception);
        try
        {
            MessageBox.Show(
                "O ReNative3D36S encontrou um erro ao abrir.\n\n" +
                "Detalhes salvos em:\n" + path + "\n\n" +
                args.Exception.GetBaseException().Message,
                "ReNative3D36S - Erro na inicialização",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch { /* Keep the original crash report even if a dialog cannot open. */ }
        args.Handled = true;
        Shutdown(1);
    }

    private static string WriteStartupLog(Exception ex)
    {
        try
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ReNative3D36S", "logs");
            Directory.CreateDirectory(folder);
            string file = Path.Combine(folder,
                "inicializacao-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".log");
            File.WriteAllText(file,
                "ReNative3D36S 0.2.0\n" +
                "UTC: " + DateTime.UtcNow.ToString("O") + "\n" +
                ex.ToString());
            return file;
        }
        catch (Exception logError)
        {
            return "(não foi possível salvar o log: " + logError.Message + ")";
        }
    }
}
