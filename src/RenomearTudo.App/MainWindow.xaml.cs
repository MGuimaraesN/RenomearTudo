using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RenomearTudo.App.Models;
using RenomearTudo.App.Services;
using RenomearTudo.App.ViewModels;

namespace RenomearTudo.App
{
    public partial class MainWindow : Window
    {
        private Point _ruleDragStart;
        private BindableRenameRule _draggedRule;
        private bool _isNarrowMode;
        private bool _showRulesInNarrow;
        private MainViewModel ViewModel => (MainViewModel)DataContext;

        public MainWindow()
        {
            ThemeService.Initialize();
            InitializeComponent();
            DataContext = new MainViewModel();
            SelectTheme(ThemeService.CurrentMode);
            UpdateMaximizeGlyph();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FitWindowToWorkArea();
            ApplyResponsiveLayout();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyResponsiveLayout();
        }

        private void FitWindowToWorkArea()
        {
            if (WindowState != WindowState.Normal) return;

            var work = SystemParameters.WorkArea;
            if (work.Width <= 0 || work.Height <= 0) return;

            // Não deixa a janela nascer maior que a área útil, algo comum em 1366x768
            // com escala de 125%/150% ou dentro do Windows Sandbox.
            if (work.Width < MinWidth) MinWidth = Math.Max(720, work.Width - 12);
            if (work.Height < MinHeight) MinHeight = Math.Max(500, work.Height - 12);

            Width = Math.Max(MinWidth, Math.Min(1280, work.Width * 0.94));
            Height = Math.Max(MinHeight, Math.Min(800, work.Height * 0.92));
            Left = work.Left + Math.Max(0, (work.Width - Width) / 2);
            Top = work.Top + Math.Max(0, (work.Height - Height) / 2);
        }

        private void ApplyResponsiveLayout()
        {
            if (!IsInitialized || NavigationColumn == null || RulesPanel == null || PreviewPanel == null) return;

            var width = ActualWidth > 0 ? ActualWidth : Width;
            var height = ActualHeight > 0 ? ActualHeight : Height;
            var narrow = width < 1000;
            var compact = width < 1280;
            var veryNarrow = width < 880;
            var shortWindow = height < 700;

            _isNarrowMode = narrow;

            // NavigationView adaptável: completo em desktop, somente ícones quando falta espaço.
            NavigationColumn.Width = new GridLength(compact ? 68 : 220);
            NavigationIntro.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
            RenameNavLabel.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
            HistoryNavLabel.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
            AppearanceCard.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
            CompactThemeButton.Visibility = compact ? Visibility.Visible : Visibility.Collapsed;
            CompactThemeButton.ToolTip = "Aparência: " + ThemeService.CurrentMode + ". Clique para alternar.";
            VersionInfo.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
            TitleBarSubtitle.Visibility = narrow ? Visibility.Collapsed : Visibility.Visible;
            NavigationFooter.Margin = compact ? new Thickness(8, 0, 8, 2) : new Thickness(16, 0, 16, 2);

            RenamePageRoot.Margin = narrow ? new Thickness(12, 12, 12, 12) : compact ? new Thickness(18, 16, 18, 16) : new Thickness(26, 22, 26, 20);
            HistoryPageRoot.Margin = RenamePageRoot.Margin;
            RenamePageDescription.Visibility = narrow ? Visibility.Collapsed : Visibility.Visible;
            RenamePageTitle.FontSize = narrow ? 24 : 28;
            StatsGrid.Columns = narrow ? 2 : 4;
            StatsGrid.Rows = narrow ? 2 : 1;

            // Em telas pequenas não esprememos regras e tabela lado a lado: alternamos o foco.
            CompactRulesToggleButton.Visibility = narrow ? Visibility.Visible : Visibility.Collapsed;
            if (narrow)
            {
                RulesColumn.Width = new GridLength(1, GridUnitType.Star);
                WorkspaceSplitterColumn.Width = new GridLength(0);
                PreviewColumn.Width = new GridLength(0);
                WorkspaceSplitter.Visibility = Visibility.Collapsed;

                Grid.SetColumn(RulesPanel, 0);
                Grid.SetColumn(PreviewPanel, 0);
                Grid.SetColumnSpan(RulesPanel, 3);
                Grid.SetColumnSpan(PreviewPanel, 3);

                RulesPanel.Visibility = _showRulesInNarrow ? Visibility.Visible : Visibility.Collapsed;
                PreviewPanel.Visibility = _showRulesInNarrow ? Visibility.Collapsed : Visibility.Visible;
                CompactRulesToggleButton.Content = _showRulesInNarrow ? "Arquivos" : "Regras";
            }
            else
            {
                _showRulesInNarrow = false;
                Grid.SetColumnSpan(RulesPanel, 1);
                Grid.SetColumnSpan(PreviewPanel, 1);
                Grid.SetColumn(RulesPanel, 0);
                Grid.SetColumn(PreviewPanel, 2);
                RulesColumn.Width = new GridLength(compact ? 305 : 355);
                WorkspaceSplitterColumn.Width = new GridLength(14);
                PreviewColumn.Width = new GridLength(1, GridUnitType.Star);
                WorkspaceSplitter.Visibility = Visibility.Visible;
                RulesPanel.Visibility = Visibility.Visible;
                PreviewPanel.Visibility = Visibility.Visible;
            }

            // Mantém a lista útil em alturas menores e evita detalhes consumirem a área da tabela.
            var constrainedHeight = shortWindow || (narrow && height < 760);
            RulesListRow.Height = new GridLength(constrainedHeight ? 90 : compact ? 155 : 185);
            PresetsPanel.Visibility = constrainedHeight ? Visibility.Collapsed : Visibility.Visible;
            SelectedDetailsPanel.Visibility = (narrow || shortWindow) ? Visibility.Collapsed : Visibility.Visible;
            SelectedMetadataWrap.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
            ResetPreviewButton.Content = compact ? "Restaurar" : "Restaurar prévia";

            // DataGrid prioriza os nomes em espaço reduzido.
            StatusColumn.Visibility = veryNarrow ? Visibility.Collapsed : Visibility.Visible;
            StatusColumn.Width = new DataGridLength(compact ? 118 : 150);
            IncludeColumn.Width = new DataGridLength(compact ? 40 : 46);
            OriginalNameColumn.Width = new DataGridLength(narrow ? 0.9 : 1.0, DataGridLengthUnitType.Star);
            PreviewNameColumn.Width = new DataGridLength(narrow ? 1.1 : 1.0, DataGridLengthUnitType.Star);

            FilterColumn.Width = new GridLength(veryNarrow ? 105 : 135);
            SortColumn.Width = new GridLength(veryNarrow ? 112 : 145);
            ExportReportButton.Content = veryNarrow ? "CSV" : "Exportar relatório";
            RenameActionButton.MinWidth = narrow ? 160 : 210;
        }

        private void CompactRulesToggle_Click(object sender, RoutedEventArgs e)
        {
            if (!_isNarrowMode) return;
            _showRulesInNarrow = !_showRulesInNarrow;
            ApplyResponsiveLayout();
        }

        private void CycleTheme_Click(object sender, RoutedEventArgs e)
        {
            var next = string.Equals(ThemeService.CurrentMode, "Sistema", StringComparison.OrdinalIgnoreCase)
                ? "Claro"
                : string.Equals(ThemeService.CurrentMode, "Claro", StringComparison.OrdinalIgnoreCase)
                    ? "Escuro"
                    : "Sistema";
            ThemeService.Apply(next);
            SelectTheme(next);
            CompactThemeButton.ToolTip = "Aparência: " + next + ". Clique para alternar.";
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private async void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
                await ViewModel.AddPathsAsync(paths);
        }

        private void AddRuleType_Click(object sender, RoutedEventArgs e)
        {
            if (RuleTypeCombo.SelectedItem is RuleTypeOption option)
                ViewModel.AddRule(option.Type);
        }

        private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(ThemeCombo.SelectedItem is ComboBoxItem item)) return;
            ThemeService.Apply(item.Content?.ToString() ?? "Sistema");
        }

        private void SelectTheme(string mode)
        {
            if (ThemeCombo == null) return;
            foreach (var entry in ThemeCombo.Items)
            {
                if (entry is ComboBoxItem item && string.Equals(item.Content?.ToString(), mode, StringComparison.OrdinalIgnoreCase))
                {
                    ThemeCombo.SelectedItem = item;
                    return;
                }
            }
            ThemeCombo.SelectedIndex = 0;
        }

        private void RenameNav_Click(object sender, RoutedEventArgs e)
        {
            WorkspaceTabs.SelectedIndex = 0;
        }

        private void HistoryNav_Click(object sender, RoutedEventArgs e)
        {
            WorkspaceTabs.SelectedIndex = 1;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed) return;

            try
            {
                DragMove();
            }
            catch (InvalidOperationException)
            {
                // O mouse pode ser liberado entre o evento e o DragMove.
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            UpdateMaximizeGlyph();
        }

        private void ToggleMaximize()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void UpdateMaximizeGlyph()
        {
            if (MaximizeButton == null) return;
            MaximizeButton.Content = WindowState == WindowState.Maximized ? "❐" : "□";
            MaximizeButton.ToolTip = WindowState == WindowState.Maximized ? "Restaurar" : "Maximizar";
        }

        private void RulesList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _ruleDragStart = e.GetPosition(null);
            _draggedRule = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource)?.DataContext as BindableRenameRule;
        }

        private void RulesList_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _draggedRule == null) return;
            var current = e.GetPosition(null);
            if (Math.Abs(current.X - _ruleDragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(current.Y - _ruleDragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            DragDrop.DoDragDrop(RulesList, _draggedRule, DragDropEffects.Move);
        }

        private void RulesList_Drop(object sender, DragEventArgs e)
        {
            if (!(e.Data.GetData(typeof(BindableRenameRule)) is BindableRenameRule rule)) return;
            var targetItem = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
            if (!(targetItem?.DataContext is BindableRenameRule target)) return;

            var targetIndex = ViewModel.Rules.IndexOf(target);
            ViewModel.SelectedRule = rule;
            ViewModel.MoveSelectedRuleTo(targetIndex);
            _draggedRule = null;
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T result) return result;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
