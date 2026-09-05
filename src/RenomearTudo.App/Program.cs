using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RenomearTudo.App.Services;
using RenomearTudo.Core.Models;

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
            StringBuilder bindingTrace = null;
            TextWriterTraceListener bindingListener = null;

            try
            {
                Directory.CreateDirectory(LogDirectory);
                WriteLog("Inicializando Renomear Tudo " + typeof(Program).Assembly.GetName().Version + ".");
                WriteLog("SO: " + Environment.OSVersion + " | 64-bit OS: " + Environment.Is64BitOperatingSystem + " | 64-bit processo: " + Environment.Is64BitProcess);
                WriteLog("CLR: " + Environment.Version);

                if (startupCheck)
                {
                    // Captura erros de binding que normalmente aparecem apenas no Output do WPF.
                    // Uma Release não pode ser publicada com binding quebrado ou propriedade read-only
                    // usada acidentalmente como TwoWay.
                    bindingTrace = new StringBuilder();
                    bindingListener = new TextWriterTraceListener(new StringWriter(bindingTrace));
                    PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
                    PresentationTraceSources.DataBindingSource.Listeners.Add(bindingListener);
                }

                var app = new App();
                RegisterUnhandledExceptionHandlers(app, startupCheck);
                app.InitializeComponent();

                var window = new MainWindow();

                // Usado pelo GitHub Actions para validar XAML, recursos, ViewModel,
                // DataGrid, detalhes, Histórico, popups e temas antes da Release.
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
                        viewModel.Files[0].Model.Metadata = new Mp3Metadata
                        {
                            Title = "Faixa de teste",
                            Artist = "Artista de teste",
                            Album = "Álbum de teste"
                        };
                        viewModel.Files[0].Refresh();

                        var historyProbe = new OperationHistory
                        {
                            Folder = Path.GetTempPath(),
                            Result = "Concluído"
                        };
                        historyProbe.Records.Add(new RenameRecord
                        {
                            OldPath = probePath,
                            NewPath = Path.Combine(Path.GetTempPath(), "RenomearTudo-startup-renamed.txt")
                        });
                        viewModel.History.Insert(0, historyProbe);

                        window.Show();
                        PumpWindow(window);

                        // Renderiza as duas páginas para que bindings que só aparecem em uma aba
                        // também sejam verificados no CI.
                        var tabs = window.FindName("WorkspaceTabs") as TabControl;
                        if (tabs == null)
                            throw new InvalidOperationException("WorkspaceTabs não foi localizado no teste de UI.");
                        tabs.SelectedIndex = 1;
                        PumpWindow(window);
                        tabs.SelectedIndex = 0;
                        PumpWindow(window);

                        // Abre os popups dos ComboBoxes principais para materializar seus templates.
                        ExerciseComboBox(window, "ThemeCombo");
                        ExerciseComboBox(window, "RuleTypeCombo");

                        // Exercita paleta completa e modo Sistema. Isso detecta recursos ausentes,
                        // templates quebrados e regressões de tema antes da Release.
                        var previousTheme = ThemeService.CurrentMode;
                        foreach (var theme in new[] { "Escuro", "Claro", "Sistema" })
                        {
                            ThemeService.Apply(theme);
                            PumpWindow(window);
                            ExerciseComboBox(window, "ThemeCombo");
                        }
                        ThemeService.Apply(previousTheme);
                        PumpWindow(window);

                        bindingListener?.Flush();
                        var bindingErrors = bindingTrace?.ToString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(bindingErrors))
                        {
                            WriteLog("Erros de binding WPF detectados:\n" + bindingErrors);
                            throw new InvalidOperationException("Foram detectados erros de binding WPF durante o startup-check.");
                        }

                        window.Close();

                        WriteLog("Binding diagnostics: OK.");
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
            finally
            {
                if (bindingListener != null)
                {
                    try
                    {
                        bindingListener.Flush();
                        PresentationTraceSources.DataBindingSource.Listeners.Remove(bindingListener);
                        bindingListener.Dispose();
                    }
                    catch
                    {
                        // Diagnóstico nunca deve mascarar o resultado principal.
                    }
                }
            }
        }

        private static void PumpWindow(Window window)
        {
            window.UpdateLayout();
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();
        }

        private static void ExerciseComboBox(Window window, string name)
        {
            var combo = window.FindName(name) as ComboBox;
            if (combo == null)
                throw new InvalidOperationException("ComboBox obrigatório não localizado: " + name);

            combo.IsDropDownOpen = true;
            PumpWindow(window);
            combo.IsDropDownOpen = false;
            PumpWindow(window);
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
