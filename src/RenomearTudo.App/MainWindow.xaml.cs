using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RenomearTudo.App.Models;
using RenomearTudo.App.Services;
using RenomearTudo.App.ViewModels;
using RenomearTudo.Core.Models;

namespace RenomearTudo.App
{
    public partial class MainWindow : Window
    {
        private Point _ruleDragStart;
        private BindableRenameRule _draggedRule;
        private MainViewModel ViewModel => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            ThemeService.Apply("Sistema");
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
            if (!IsLoaded || !(ThemeCombo.SelectedItem is ComboBoxItem item)) return;
            ThemeService.Apply(item.Content?.ToString() ?? "Sistema");
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
            if (Math.Abs(current.X - _ruleDragStart.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(current.Y - _ruleDragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
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
