using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NodaStack.Helpers
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isHealthy)
            {
                return isHealthy ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LogLevelToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string level)
            {
                return level.ToLower() switch
                {
                    "error" => new SolidColorBrush(Colors.Red),
                    "warning" => new SolidColorBrush(Colors.Orange),
                    "info" => new SolidColorBrush(Colors.Blue),
                    "debug" => new SolidColorBrush(Colors.Gray),
                    _ => new SolidColorBrush(Colors.Black)
                };
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() switch
                {
                    "running" => new SolidColorBrush(Colors.Green),
                    "stopped" => new SolidColorBrush(Colors.Red),
                    "error" => new SolidColorBrush(Colors.Orange),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CpuUsageToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double cpuUsage)
            {
                if (cpuUsage > 80) return new SolidColorBrush(Colors.Red);
                if (cpuUsage > 60) return new SolidColorBrush(Colors.Orange);
                if (cpuUsage > 40) return new SolidColorBrush(Colors.Yellow);
                return new SolidColorBrush(Colors.Green);
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MemoryUsageToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long memoryUsage)
            {
                var memoryMB = memoryUsage / (1024 * 1024);
                if (memoryMB > 1000) return new SolidColorBrush(Colors.Red);
                if (memoryMB > 500) return new SolidColorBrush(Colors.Orange);
                if (memoryMB > 100) return new SolidColorBrush(Colors.Yellow);
                return new SolidColorBrush(Colors.Green);
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 