using CtYun.Services;
using CtYun.ViewModels;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Brushes = System.Windows.Media.Brushes;

namespace CtYun
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private readonly Logger _logger = Logger.Instance;
        private NotifyIcon _notifyIcon;
        private bool _isClosing = false;

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

            // 初始化系统托盘
            InitializeNotifyIcon();
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new NotifyIcon();
            
            // 从嵌入式资源加载图标
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "CtYun.images.ctyun.ico";
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    _notifyIcon.Icon = new System.Drawing.Icon(stream);
                }
            }
            
            _notifyIcon.Text = "天翼云电脑保活工具";
            _notifyIcon.Visible = true;

            // 创建右键菜单
            var contextMenu = new ContextMenuStrip();
            var showItem = new ToolStripMenuItem("显示");
            showItem.Click += (s, e) => ShowWindow();
            
            var exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => ExitApplication();

            contextMenu.Items.Add(showItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;

            // 单击托盘图标显示窗口
            _notifyIcon.Click += (s, e) => ShowWindow();
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void ExitApplication()
        {
            _isClosing = true;
            _notifyIcon?.Dispose();
            this.Close();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            
            // 最小化时隐藏窗口
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();
                _notifyIcon?.ShowBalloonTip(2000, "天翼云电脑保活工具", "程序已最小化到系统托盘", ToolTipIcon.Info);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_isClosing)
            {
                // 不是真正的关闭，而是最小化到托盘
                e.Cancel = true;
                this.WindowState = WindowState.Minimized;
            }
            else
            {
                _notifyIcon?.Dispose();
            }
            
            base.OnClosing(e);
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

        private static System.Windows.Media.Brush GetLevelBrush(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => System.Windows.Media.Brushes.Gray,
                LogLevel.Info => System.Windows.Media.Brushes.Cyan,
                LogLevel.Warning => System.Windows.Media.Brushes.Orange,
                LogLevel.Error => System.Windows.Media.Brushes.Red,
                LogLevel.Success => System.Windows.Media.Brushes.LimeGreen,
                _ => System.Windows.Media.Brushes.LightGray
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
            // 点击关闭按钮时最小化到托盘，而不是真正关闭
            this.WindowState = WindowState.Minimized;
        }
    }
}
