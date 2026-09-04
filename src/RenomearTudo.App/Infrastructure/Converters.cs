using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using RenomearTudo.Core.Models;

namespace RenomearTudo.App.Infrastructure
{
    public sealed class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class StatusBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is RenameItemStatus status)) return Brushes.Gray;
            switch (status)
            {
                case RenameItemStatus.Ready:
                case RenameItemStatus.Completed:
                    return Brushes.MediumSeaGreen;
                case RenameItemStatus.Conflict:
                    return Brushes.Goldenrod;
                case RenameItemStatus.Invalid:
                case RenameItemStatus.Error:
                    return Brushes.IndianRed;
                default:
                    return Brushes.Gray;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
