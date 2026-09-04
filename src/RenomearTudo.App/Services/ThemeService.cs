using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace RenomearTudo.App.Services
{
    public static class ThemeService
    {
        public static void Apply(string mode)
        {
            var light = mode == "Claro" || (mode == "Sistema" && IsSystemLight());
            Set("WindowBackgroundBrush", light ? "#F4F6F8" : "#0F1115");
            Set("SurfaceBrush", light ? "#FFFFFF" : "#171A20");
            Set("SurfaceAltBrush", light ? "#F0F2F5" : "#1E222A");
            Set("BorderBrush", light ? "#D9DEE6" : "#2B303A");
            Set("PrimaryTextBrush", light ? "#171A20" : "#F5F7FA");
            Set("SecondaryTextBrush", light ? "#5F6875" : "#AAB1BC");
        }

        private static bool IsSystemLight()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    var value = key?.GetValue("AppsUseLightTheme");
                    return value == null || (int)value > 0;
                }
            }
            catch { return true; }
        }

        private static void Set(string key, string color)
        {
            Application.Current.Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }
    }
}
