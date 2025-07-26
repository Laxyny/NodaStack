using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using ModernWpf;

namespace NodaStack
{
    public static class ThemeManager
    {
        private static readonly Color DefaultAccentColor = Color.FromRgb(0, 122, 204);
        public static event Action<bool>? ThemeChanged;

        private static bool _isDarkTheme = false;
        public static bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (_isDarkTheme != value)
                {
                    _isDarkTheme = value;
                    ApplyTheme(value);
                    ThemeChanged?.Invoke(value);
                }
            }
        }

        public static void ApplyTheme(bool isDark)
        {
            var app = Application.Current;
            if (app == null) return;

            try
            {
                app.Resources.Clear();
                ModernWpf.ThemeManager.Current.ApplicationTheme = isDark ? ApplicationTheme.Dark : ApplicationTheme.Light;
                ModernWpf.ThemeManager.Current.AccentColor = DefaultAccentColor;

                if (isDark)
                {
                    app.Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(32, 32, 32));
                    app.Resources["ForegroundBrush"] = new SolidColorBrush(Colors.White);
                    app.Resources["ButtonBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(64, 64, 64));
                    app.Resources["ButtonHoverBrush"] = new SolidColorBrush(Color.FromRgb(80, 80, 80));
                    app.Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(80, 80, 80));
                    app.Resources["TextBoxBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(48, 48, 48));
                    app.Resources["GroupBoxBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                    app.Resources["ListViewBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(48, 48, 48));
                    app.Resources["LogBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(16, 16, 16));
                    app.Resources["StatusBarBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(24, 24, 24));
                }
                else
                {
                    app.Resources["BackgroundBrush"] = new SolidColorBrush(Colors.White);
                    app.Resources["ForegroundBrush"] = new SolidColorBrush(Colors.Black);
                    app.Resources["ButtonBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                    app.Resources["ButtonHoverBrush"] = new SolidColorBrush(Color.FromRgb(230, 230, 230));
                    app.Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                    app.Resources["TextBoxBackgroundBrush"] = new SolidColorBrush(Colors.White);
                    app.Resources["GroupBoxBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(250, 250, 250));
                    app.Resources["ListViewBackgroundBrush"] = new SolidColorBrush(Colors.White);
                    app.Resources["LogBackgroundBrush"] = new SolidColorBrush(Colors.Black);
                    app.Resources["StatusBarBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                }

                ApplyButtonStyle();
                ApplyTextBoxStyle();
                ApplyListViewStyle();
                ApplyGroupBoxStyle();
                ApplyTextBlockStyle();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying theme: {ex.Message}");
            }
        }

        private static void ApplyButtonStyle()
        {
            var app = Application.Current;
            if (app == null) return;

            var buttonStyle = new Style(typeof(Button));
            buttonStyle.Setters.Add(new Setter(Button.BackgroundProperty, app.Resources["ButtonBackgroundBrush"]));
            buttonStyle.Setters.Add(new Setter(Button.ForegroundProperty, app.Resources["ForegroundBrush"]));
            buttonStyle.Setters.Add(new Setter(Button.BorderBrushProperty, app.Resources["BorderBrush"]));
            buttonStyle.Setters.Add(new Setter(Button.PaddingProperty, new Thickness(5)));
            buttonStyle.Setters.Add(new Setter(Button.MarginProperty, new Thickness(2)));
            buttonStyle.Setters.Add(new Setter(Control.FontSizeProperty, 12.0));

            var trigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(Button.BackgroundProperty, app.Resources["ButtonHoverBrush"]));
            buttonStyle.Triggers.Add(trigger);

            app.Resources[typeof(Button)] = buttonStyle;
        }

        private static void ApplyTextBoxStyle()
        {
            var app = Application.Current;
            if (app == null) return;

            var textBoxStyle = new Style(typeof(TextBox));
            textBoxStyle.Setters.Add(new Setter(TextBox.BackgroundProperty, app.Resources["TextBoxBackgroundBrush"]));
            textBoxStyle.Setters.Add(new Setter(TextBox.ForegroundProperty, app.Resources["ForegroundBrush"]));
            textBoxStyle.Setters.Add(new Setter(TextBox.BorderBrushProperty, app.Resources["BorderBrush"]));
            textBoxStyle.Setters.Add(new Setter(Control.FontSizeProperty, 12.0));

            app.Resources[typeof(TextBox)] = textBoxStyle;
        }

        private static void ApplyListViewStyle()
        {
            var app = Application.Current;
            if (app == null) return;

            var listViewStyle = new Style(typeof(ListView));
            listViewStyle.Setters.Add(new Setter(ListView.BackgroundProperty, app.Resources["ListViewBackgroundBrush"]));
            listViewStyle.Setters.Add(new Setter(ListView.ForegroundProperty, app.Resources["ForegroundBrush"]));
            listViewStyle.Setters.Add(new Setter(ListView.BorderBrushProperty, app.Resources["BorderBrush"]));

            app.Resources[typeof(ListView)] = listViewStyle;
        }

        private static void ApplyGroupBoxStyle()
        {
            var app = Application.Current;
            if (app == null) return;

            var groupBoxStyle = new Style(typeof(GroupBox));
            groupBoxStyle.Setters.Add(new Setter(GroupBox.BackgroundProperty, app.Resources["GroupBoxBackgroundBrush"]));
            groupBoxStyle.Setters.Add(new Setter(GroupBox.ForegroundProperty, app.Resources["ForegroundBrush"]));
            groupBoxStyle.Setters.Add(new Setter(GroupBox.BorderBrushProperty, app.Resources["BorderBrush"]));

            app.Resources[typeof(GroupBox)] = groupBoxStyle;
        }

        private static void ApplyTextBlockStyle()
        {
            var app = Application.Current;
            if (app == null) return;

            var textBlockStyle = new Style(typeof(TextBlock));
            textBlockStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, app.Resources["ForegroundBrush"]));
            textBlockStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, 12.0));

            app.Resources[typeof(TextBlock)] = textBlockStyle;
        }

        public static void Initialize(bool isDarkTheme)
        {
            _isDarkTheme = isDarkTheme;
            ApplyTheme(isDarkTheme);
        }
    }
}