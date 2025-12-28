using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NodaStack.Services
{
    public static class ServiceIconManager
    {
        private static readonly Dictionary<string, string> ServiceIcons = new()
        {
            { "Apache", "🌐" },
            { "PHP", "🐘" },
            { "MySQL", "🗃️" },
            { "phpMyAdmin", "🛠️" },
            { "Docker", "🐳" },
            { "Project", "📁" },
            { "Log", "📋" },
            { "Configuration", "⚙️" },
            { "Monitoring", "📊" },
            { "Success", "✅" },
            { "Error", "❌" },
            { "Warning", "⚠️" },
            { "Info", "ℹ️" }
        };

        public static string GetIcon(string serviceName)
        {
            return ServiceIcons.TryGetValue(serviceName, out var icon) ? icon : "🔧";
        }

        public static TextBlock CreateIconTextBlock(string serviceName, double fontSize = 16)
        {
            return new TextBlock
            {
                Text = GetIcon(serviceName),
                FontSize = fontSize,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };
        }

        public static void UpdateButtonWithIcon(Button button, string serviceName, string text)
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var icon = CreateIconTextBlock(serviceName, 14);
            var textBlock = new TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0)
            };

            stackPanel.Children.Add(icon);
            stackPanel.Children.Add(textBlock);
            button.Content = stackPanel;
        }

        public static Grid CreateServiceIndicator(string serviceName, bool isRunning)
        {
            var grid = new Grid();

            var ellipse = new System.Windows.Shapes.Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = isRunning ? Brushes.LimeGreen : Brushes.Red,
                Margin = new Thickness(0, 0, 5, 0)
            };

            var icon = CreateIconTextBlock(serviceName, 12);
            icon.Margin = new Thickness(0, -2, 0, 0);

            grid.Children.Add(ellipse);
            grid.Children.Add(icon);

            return grid;
        }
    }
}