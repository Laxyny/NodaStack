using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace NodaStack
{
    public class StatusBarManager : IDisposable
    {
        private readonly DockPanel statusBar;
        private readonly TextBlock statusText;
        private readonly TextBlock timeText;
        private readonly TextBlock servicesText;
        private readonly DispatcherTimer timer;
        private bool disposed = false;

        public StatusBarManager(DockPanel statusBar)
        {
            this.statusBar = statusBar;

            // Chercher les éléments existants ou les créer
            statusText = statusBar.FindName("StatusText") as TextBlock ?? CreateStatusText();
            timeText = statusBar.FindName("TimeText") as TextBlock ?? CreateTimeText();
            servicesText = statusBar.FindName("ServicesText") as TextBlock ?? CreateServicesText();

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private TextBlock CreateStatusText()
        {
            var textBlock = new TextBlock
            {
                Text = "Ready",
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Name = "StatusText"
            };

            DockPanel.SetDock(textBlock, Dock.Left);
            statusBar.Children.Add(textBlock);
            return textBlock;
        }

        private TextBlock CreateTimeText()
        {
            var textBlock = new TextBlock
            {
                Text = "00:00:00",
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Name = "TimeText"
            };

            DockPanel.SetDock(textBlock, Dock.Right);
            statusBar.Children.Add(textBlock);
            return textBlock;
        }

        private TextBlock CreateServicesText()
        {
            var textBlock = new TextBlock
            {
                Text = "Services: 0/4 running",
                Margin = new Thickness(20, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Name = "ServicesText"
            };

            statusBar.Children.Add(textBlock);
            return textBlock;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!disposed && timeText != null)
            {
                timeText.Text = DateTime.Now.ToString("HH:mm:ss");
            }
        }

        public void UpdateStatus(string status)
        {
            if (!disposed && statusText != null)
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    statusText.Text = status;
                });
            }
        }

        public void UpdateServicesCount(int running, int total)
        {
            if (!disposed && servicesText != null)
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    servicesText.Text = $"Services: {running}/{total} running";
                });
            }
        }

        public void ShowTemporaryStatus(string message, int durationMs = 3000)
        {
            if (disposed || statusText == null) return;

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                var originalText = statusText.Text;
                statusText.Text = message;

                var tempTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(durationMs)
                };

                tempTimer.Tick += (s, e) =>
                {
                    tempTimer.Stop();
                    if (!disposed && statusText != null)
                    {
                        statusText.Text = originalText;
                    }
                };

                tempTimer.Start();
            });
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                timer?.Stop();
                if (timer != null)
                {
                    timer.Tick -= Timer_Tick;
                }
            }
        }
    }
}