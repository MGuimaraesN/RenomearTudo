using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace RenomearTudo.App.Services
{
    public static class ThemeService
    {
        private const string SystemMode = "Sistema";
        private const string LightMode = "Claro";
        private const string DarkMode = "Escuro";

        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RenomearTudo");

        private static readonly string ThemeFile = Path.Combine(SettingsDirectory, "theme.txt");
        private static bool _initialized;

        public static string CurrentMode { get; private set; } = SystemMode;

        public static void Initialize()
        {
            if (_initialized)
            {
                ApplyPalette();
                return;
            }

            _initialized = true;
            CurrentMode = Normalize(LoadMode());
            ApplyPalette();

            try
            {
                SystemEvents.UserPreferenceChanged += (_, __) =>
                {
                    if (!string.Equals(CurrentMode, SystemMode, StringComparison.OrdinalIgnoreCase)) return;
                    var dispatcher = Application.Current?.Dispatcher;
                    if (dispatcher == null) return;
                    dispatcher.BeginInvoke(new Action(ApplyPalette));
                };
            }
            catch
            {
                // A troca automática é um refinamento; nunca deve impedir o app de abrir.
            }
        }

        public static void Apply(string mode)
        {
            CurrentMode = Normalize(mode);
            SaveMode(CurrentMode);
            ApplyPalette();
        }

        private static string Normalize(string mode)
        {
            if (string.Equals(mode, LightMode, StringComparison.OrdinalIgnoreCase)) return LightMode;
            if (string.Equals(mode, DarkMode, StringComparison.OrdinalIgnoreCase)) return DarkMode;
            return SystemMode;
        }

        private static void ApplyPalette()
        {
            var app = Application.Current;
            if (app == null) return;

            var light = string.Equals(CurrentMode, LightMode, StringComparison.OrdinalIgnoreCase) ||
                        (string.Equals(CurrentMode, SystemMode, StringComparison.OrdinalIgnoreCase) && IsSystemLight());

            var accent = GetSystemAccent(light);
            var accentHover = Shift(accent, light ? -0.08 : 0.08);
            var accentPressed = Shift(accent, light ? -0.16 : -0.06);
            var accentText = RelativeLuminance(accent) > 0.52 ? Color.FromRgb(18, 18, 18) : Colors.White;

            Set("WindowBackgroundBrush", light ? "#F5F5F5" : "#202020");
            Set("NavigationBackgroundBrush", light ? "#FAFAFA" : "#181818");
            Set("SurfaceBrush", light ? "#FFFFFF" : "#2B2B2B");
            Set("SurfaceRaisedBrush", light ? "#FFFFFF" : "#323232");
            Set("SurfaceAltBrush", light ? "#F9F9F9" : "#272727");
            Set("BorderBrush", light ? "#E5E5E5" : "#3D3D3D");
            Set("BorderStrongBrush", light ? "#D0D0D0" : "#505050");
            Set("PrimaryTextBrush", light ? "#1F1F1F" : "#F5F5F5");
            Set("SecondaryTextBrush", light ? "#5D5D5D" : "#C8C8C8");
            Set("TertiaryTextBrush", light ? "#7A7A7A" : "#9B9B9B");
            Set("HoverBrush", light ? "#09000000" : "#12FFFFFF");
            Set("PressedBrush", light ? "#12000000" : "#1CFFFFFF");
            Set("SelectionBrush", light ? "#E8F3FF" : "#173A52");
            Set("DisabledBrush", light ? "#B8B8B8" : "#666666");

            Set("AccentBrush", accent);
            Set("AccentHoverBrush", accentHover);
            Set("AccentPressedBrush", accentPressed);
            Set("AccentSubtleBrush", WithAlpha(accent, light ? 0.11 : 0.18));
            Set("AccentTextBrush", accentText);

            Set("SuccessBrush", light ? "#0F7B0F" : "#6CCB5F");
            Set("SuccessSubtleBrush", light ? "#EAF6EA" : "#163A16");
            Set("WarningBrush", light ? "#8A4B00" : "#F2C661");
            Set("WarningSubtleBrush", light ? "#FFF4CE" : "#49351A");
            Set("DangerBrush", light ? "#C42B1C" : "#FF99A4");
            Set("DangerSubtleBrush", light ? "#FDE7E9" : "#4A2024");
        }

        private static bool IsSystemLight()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    var value = key?.GetValue("AppsUseLightTheme");
                    if (value is int integer) return integer > 0;
                }
            }
            catch
            {
                // Windows 7 não possui AppsUseLightTheme; use tema claro por padrão.
            }

            return true;
        }

        private static Color GetSystemAccent(bool light)
        {
            try
            {
                var color = SystemParameters.WindowGlassColor;
                if (color.R + color.G + color.B > 45 && color.R + color.G + color.B < 735)
                    return Color.FromRgb(color.R, color.G, color.B);
            }
            catch
            {
                // Fallback abaixo.
            }

            return (Color)ColorConverter.ConvertFromString(light ? "#0067C0" : "#60CDFF");
        }

        private static Color Shift(Color color, double amount)
        {
            byte Adjust(byte channel)
            {
                var value = amount >= 0
                    ? channel + (255 - channel) * amount
                    : channel * (1 + amount);
                return (byte)Math.Max(0, Math.Min(255, Math.Round(value)));
            }

            return Color.FromRgb(Adjust(color.R), Adjust(color.G), Adjust(color.B));
        }

        private static Color WithAlpha(Color color, double alpha)
        {
            return Color.FromArgb((byte)Math.Round(Math.Max(0, Math.Min(1, alpha)) * 255), color.R, color.G, color.B);
        }

        private static double RelativeLuminance(Color color)
        {
            double Linear(byte channel)
            {
                var c = channel / 255d;
                return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            }

            return 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
        }

        private static string LoadMode()
        {
            try
            {
                return File.Exists(ThemeFile) ? File.ReadAllText(ThemeFile).Trim() : SystemMode;
            }
            catch
            {
                return SystemMode;
            }
        }

        private static void SaveMode(string mode)
        {
            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                File.WriteAllText(ThemeFile, mode);
            }
            catch
            {
                // Preferência visual não deve impedir o uso do aplicativo.
            }
        }

        private static void Set(string key, string color)
        {
            Set(key, (Color)ColorConverter.ConvertFromString(color));
        }

        private static void Set(string key, Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            Application.Current.Resources[key] = brush;
        }
    }
}
