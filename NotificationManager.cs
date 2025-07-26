using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace NodaStack
{
    public static class NotificationManager
    {
        private static Window? currentToast;
        private static DispatcherTimer? toastTimer;

        public static void ShowNotification(string title, string message, NotificationType type = NotificationType.Info, int durationMs = 3000)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CloseCurrentToast();
                    CreateToast(title, message, type, durationMs);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing notification: {ex.Message}");
            }
        }

        public static void ShowNotification(string title, string message, NotificationType type, int durationMs, Action onClick)
        {
            ShowNotification(title, message, type, durationMs);
        }

        private static void CreateToast(string title, string message, NotificationType type, int durationMs)
        {
            var toast = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize,
                Width = 350,
                Height = 120
            };

            var border = new Border
            {
                Background = GetNotificationBackground(type),
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(0),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    ShadowDepth = 2,
                    BlurRadius = 10,
                    Opacity = 0.3
                }
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(15),
                Orientation = Orientation.Vertical
            };

            var titleBlock = new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = GetNotificationForeground(type),
                Margin = new Thickness(0, 0, 0, 5)
            };

            var messageBlock = new TextBlock
            {
                Text = message,
                FontSize = 12,
                Foreground = GetNotificationForeground(type),
                TextWrapping = TextWrapping.Wrap
            };

            stackPanel.Children.Add(titleBlock);
            stackPanel.Children.Add(messageBlock);
            border.Child = stackPanel;
            toast.Content = border;

            PositionToast(toast);

            var fadeInAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            toast.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
            toast.Show();

            currentToast = toast;

            toastTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(durationMs)
            };
            toastTimer.Tick += (s, e) =>
            {
                toastTimer.Stop();
                CloseCurrentToast();
            };
            toastTimer.Start();

            toast.MouseLeftButtonDown += (s, e) => CloseCurrentToast();
        }

        private static void PositionToast(Window toast)
        {
            var workingArea = SystemParameters.WorkArea;
            toast.Left = workingArea.Right - toast.Width - 30;
            toast.Top = workingArea.Bottom - toast.Height - 20;
        }

        private static void CloseCurrentToast()
        {
            if (currentToast != null)
            {
                var fadeOutAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(200)
                };

                fadeOutAnimation.Completed += (s, e) =>
                {
                    currentToast.Close();
                    currentToast = null;
                };

                currentToast.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
            }

            toastTimer?.Stop();
            toastTimer = null;
        }

        private static SolidColorBrush GetNotificationBackground(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                NotificationType.Warning => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                NotificationType.Error => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                _ => new SolidColorBrush(Color.FromRgb(33, 150, 243))
            };
        }

        private static SolidColorBrush GetNotificationForeground(NotificationType type)
        {
            return new SolidColorBrush(Colors.White);
        }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}