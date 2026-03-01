using CtYun.Services;
using CtYun.ViewModels;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace CtYun
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private readonly Logger _logger = Logger.Instance;

        public MainWindow()
        {
            InitializeComponent();

            // 设置窗口标题和版本号（从程序集版本获取）
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionString = $"v{version?.Major}.{version?.Minor}";
            this.Title = $"天翼云电脑保活工具 {versionString}";
            txtVersion.Text = versionString;

            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // 设置日志事件处理
            _logger.OnLogAdded += OnLogAdded;
            _logger.OnClear += OnClearLog;

            // 加载现有日志
            LoadExistingLogs();

            // 延迟设置密码框（等待 ViewModel 加载保存的凭据）
            Dispatcher.BeginInvoke(() =>
            {
                if (!string.IsNullOrEmpty(_viewModel.Password))
                {
                    txtPassword.Password = _viewModel.Password;
                }
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private void LoadExistingLogs()
        {
            var logs = _logger.GetDisplayLog();
            foreach (var log in logs)
            {
                AppendLogToRichTextBox(log);
            }
        }

        private void OnLogAdded(LogEntry logEntry)
        {
            Dispatcher.Invoke(() =>
            {
                AppendLogToRichTextBox(logEntry);
                ScrollToEnd();
            });
        }

        private void AppendLogToRichTextBox(LogEntry logEntry)
        {
            var paragraph = new Paragraph();

            // 时间戳 - 灰色
            var timeRun = new Run($"[{logEntry.Timestamp}] ")
            {
                Foreground = Brushes.Gray
            };
            paragraph.Inlines.Add(timeRun);

            // 日志级别 - 根据级别着色
            var levelRun = new Run($"[{logEntry.Level}] ")
            {
                Foreground = GetLevelBrush(logEntry.Level)
            };
            paragraph.Inlines.Add(levelRun);

            // 消息 - 根据级别着色
            var messageRun = new Run(logEntry.Message)
            {
                Foreground = logEntry.Color
            };
            paragraph.Inlines.Add(messageRun);

            txtLog.Document.Blocks.Add(paragraph);

            // 限制行数
            while (txtLog.Document.Blocks.Count > 100)
            {
                txtLog.Document.Blocks.Remove(txtLog.Document.Blocks.First());
            }
        }

        private static Brush GetLevelBrush(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => Brushes.Gray,
                LogLevel.Info => Brushes.Cyan,
                LogLevel.Warning => Brushes.Orange,
                LogLevel.Error => Brushes.Red,
                LogLevel.Success => Brushes.LimeGreen,
                _ => Brushes.LightGray
            };
        }

        private void ScrollToEnd()
        {
            txtLog.ScrollToEnd();
        }

        private void OnClearLog()
        {
            Dispatcher.Invoke(() =>
            {
                txtLog.Document.Blocks.Clear();
            });
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && sender is PasswordBox pb)
            {
                _viewModel.Password = pb.Password;
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
