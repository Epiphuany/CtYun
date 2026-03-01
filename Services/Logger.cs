using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Media;

namespace CtYun.Services
{
    public class Logger
    {
        private static readonly Lazy<Logger> _instance = new(() => new Logger());
        public static Logger Instance => _instance.Value;

        private readonly object _lock = new();
        private readonly List<LogEntry> _logLines = new();
        private const int MaxDisplayLines = 100;
        private string _logDir;
        private DateTime _currentLogDate;

        public event Action<LogEntry> OnLogAdded;
        public event Action OnClear;

        private Logger()
        {
            _logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(_logDir);
            _currentLogDate = DateTime.Now;
        }

        /// <summary>
        /// 获取当前日期的日志文件路径
        /// </summary>
        private string GetCurrentLogFilePath()
        {
            var today = DateTime.Now;
            // 如果日期变了，更新当前日期
            if (today.Date != _currentLogDate.Date)
            {
                _currentLogDate = today;
            }
            return Path.Combine(_logDir, $"CtYun_{today:yyyyMMdd}.log");
        }

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            var now = DateTime.Now;
            var timestamp = now.ToString("HH:mm:ss.ff");
            var logEntry = new LogEntry
            {
                Timestamp = timestamp,
                Level = level,
                Message = message,
                Color = GetColorForLevel(level)
            };

            lock (_lock)
            {
                _logLines.Add(logEntry);

                // 保持只有最新的100行用于显示
                while (_logLines.Count > MaxDisplayLines)
                {
                    _logLines.RemoveAt(0);
                }

                // 写入文件（按天分割）
                try
                {
                    var logFilePath = GetCurrentLogFilePath();
                    var logText = $"[{timestamp}] [{level}] {message}";
                    File.AppendAllText(logFilePath, logText + Environment.NewLine, Encoding.UTF8);
                }
                catch { }
            }

            OnLogAdded?.Invoke(logEntry);
        }

        public List<LogEntry> GetDisplayLog()
        {
            lock (_lock)
            {
                return _logLines.ToList();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _logLines.Clear();
            }
            OnClear?.Invoke();
        }

        /// <summary>
        /// 获取今天的日志文件路径
        /// </summary>
        public string GetTodayLogFilePath()
        {
            return GetCurrentLogFilePath();
        }

        /// <summary>
        /// 获取所有日志文件列表
        /// </summary>
        public List<string> GetAllLogFiles()
        {
            try
            {
                return Directory.GetFiles(_logDir, "CtYun_*.log")
                    .OrderByDescending(f => f)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static Brush GetColorForLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => Brushes.Gray,
                LogLevel.Info => Brushes.LightGray,
                LogLevel.Warning => Brushes.Orange,
                LogLevel.Error => Brushes.Red,
                LogLevel.Success => Brushes.LimeGreen,
                _ => Brushes.LightGray
            };
        }
    }

    public class LogEntry
    {
        public string Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; }
        public Brush Color { get; set; }
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Success
    }
}
