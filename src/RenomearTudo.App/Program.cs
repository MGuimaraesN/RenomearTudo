using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using RenomearTudo.App.Services;

namespace RenomearTudo.App
{
    internal static class Program
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RenomearTudo",
            "logs");

        private static readonly string StartupLog = Path.Combine(LogDirectory, "startup.log");

        [STAThread]
        public static int Main(string[] args)
        {
            var startupCheck = args != null && args.Any(a => string.Equals(a, "--startup-check", StringComparison.OrdinalIgnoreCase));

            try
            {
                Directory.CreateDirectory(LogDirectory);
                WriteLog("Inicializando Renomear Tudo " + typeof(Program).Assembly.GetName().Version + ".");
                WriteLog("SO: " + Environment.OSVersion + " | 64-bit OS: " + Environment.Is64BitOperatingSystem + " | 64-bit processo: " + Environment.Is64BitProcess);
                WriteLog("CLR: " + Environment.Version);

                var app = new App();
                RegisterUnhandledExceptionHandlers(app, startupCheck);
                app.InitializeComponent();

                var window = new MainWindow();

                // Usado pelo GitHub Actions para validar que XAML, recursos, ViewModel,
                // bindings da lista e a janela conseguem ser inicializados antes da Release.
                if (startupCheck)
                {
                    var probePath = Path.Combine(Path.GetTempPath(), "RenomearTudo-startup-" + Guid.NewGuid().ToString("N") + ".txt");
                    try
                    {
                        File.WriteAllText(probePath, "startup-check", Encoding.UTF8);
                        var viewModel = window.DataContext as ViewModels.MainViewModel;
                        if (viewModel == null)
                            throw new InvalidOperationException("DataContext da janela principal não foi inicializado.");

                        viewModel.AddPaths(new[] { probePath });
                        if (viewModel.TotalCount != 1)
                            throw new InvalidOperationException("O arquivo de teste não foi carregado na lista.");

                        viewModel.SelectedFile = viewModel.Files[0];
                        window.Show();
                        window.UpdateLayout();
                        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                        window.UpdateLayout();

                        // Exercita a nova paleta completa. Isso detecta recursos ausentes e
                        // regressões de tema antes de uma Release chegar ao usuário.
                        var previousTheme = ThemeService.CurrentMode;
                        foreach (var theme in new[] { "Escuro", "Claro" })
                        {
                            ThemeService.Apply(theme);
                            window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                            window.UpdateLayout();
                        }
                        ThemeService.Apply(previousTheme);
                        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                        window.UpdateLayout();

                        window.Close();

                        WriteLog("Theme switch check: OK.");
                        WriteLog("Startup check: OK.");
                        return 0;
                    }
                    finally
                    {
                        try
                        {
                            if (File.Exists(probePath)) File.Delete(probePath);
                        }
                        catch
                        {
                            // O arquivo temporário nunca deve mascarar o resultado do diagnóstico.
                        }
                    }
                }

                WriteLog("Janela principal inicializada com sucesso.");
                return app.Run(window);
            }
            catch (Exception ex)
            {
                WriteException("Falha fatal durante a inicialização", ex);
                if (!startupCheck)
                {
                    try
                    {
                        MessageBox.Show(
                            "O Renomear Tudo não conseguiu iniciar.\n\n" +
                            "Um relatório foi salvo em:\n" + StartupLog + "\n\n" +
                            "Detalhes: " + ex.Message,
                            "Renomear Tudo",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                    catch
                    {
                        // Se o próprio subsistema gráfico estiver indisponível, o log continua sendo gravado.
                    }
                }

                return 1;
            }
        }

        private static void RegisterUnhandledExceptionHandlers(Application app, bool startupCheck)
        {
            app.DispatcherUnhandledException += (sender, e) =>
            {
                WriteException("DispatcherUnhandledException", e.Exception);
                if (startupCheck)
                    return;

                try
                {
                    MessageBox.Show(
                        "Ocorreu um erro inesperado e o Renomear Tudo será encerrado para proteger seus arquivos.\n\n" +
                        "Log: " + StartupLog + "\n\n" + e.Exception.Message,
                        "Renomear Tudo",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                finally
                {
                    e.Handled = true;
                    Application.Current?.Shutdown(1);
                }
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                WriteException("AppDomain.UnhandledException", e.ExceptionObject as Exception ?? new Exception(Convert.ToString(e.ExceptionObject)));
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                WriteException("TaskScheduler.UnobservedTaskException", e.Exception);
                e.SetObserved();
            };
        }

        private static void WriteException(string context, Exception exception)
        {
            var builder = new StringBuilder();
            builder.AppendLine(context);
            builder.AppendLine(exception.ToString());
            WriteLog(builder.ToString());
        }

        private static void WriteLog(string message)
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
                File.AppendAllText(
                    StartupLog,
                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] " + message + Environment.NewLine,
                    Encoding.UTF8);
            }
            catch
            {
                // Diagnóstico nunca deve impedir a inicialização do aplicativo.
            }
        }
    }
}
