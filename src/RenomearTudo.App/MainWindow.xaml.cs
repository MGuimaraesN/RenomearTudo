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
        private MainViewModel ViewModel => (MainViewModel)DataContext;

        public MainWindow()
        {
            ThemeService.Initialize();
            InitializeComponent();
            DataContext = new MainViewModel();
            SelectTheme(ThemeService.CurrentMode);
            UpdateMaximizeGlyph();
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
                ViewModel.AddPaths(paths);
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
