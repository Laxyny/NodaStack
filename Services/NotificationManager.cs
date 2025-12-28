using System;
using System.Windows.Forms;
using System.Drawing;

namespace NodaStack.Services
{
    public static class NotificationManager
    {
        private static NotifyIcon? _notifyIcon;

        static NotificationManager()
        {
            // Initialiser le NotifyIcon pour les notifications
            _notifyIcon = new NotifyIcon
            {
                Visible = false,
                Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location)
            };
        }

        public static void ShowNotification(string title, string message, NotificationType type = NotificationType.Info)
        {
            try
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = true;
                    _notifyIcon.ShowBalloonTip(5000, title, message, GetToolTipIcon(type));
                }
            }
            catch (Exception ex)
            {
                // Fallback console
                Console.WriteLine($"Notification: {title} - {message} ({type})");
                Console.WriteLine($"Error showing notification: {ex.Message}");
            }
        }

        public static void ShowNotification(string title, string message, NotificationType type, int durationMs, Action? onClick = null)
        {
            ShowNotification(title, message, type);
        }

        public static void ShowToastNotification(string title, string message, NotificationType type = NotificationType.Info)
        {
            ShowNotification(title, message, type);
        }

        public static void ClearAllNotifications()
        {
            try
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing notifications: {ex.Message}");
            }
        }

        private static ToolTipIcon GetToolTipIcon(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => ToolTipIcon.Info,
                NotificationType.Warning => ToolTipIcon.Warning,
                NotificationType.Error => ToolTipIcon.Error,
                NotificationType.Info => ToolTipIcon.Info,
                _ => ToolTipIcon.Info
            };
        }

        public enum NotificationType
        {
            Info,
            Success,
            Warning,
            Error
        }
    }
} 