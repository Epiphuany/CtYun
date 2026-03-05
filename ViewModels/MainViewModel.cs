using CtYun.Models;
using CtYun.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Brushes = System.Windows.Media.Brushes;
using Brush = System.Windows.Media.Brush;

namespace CtYun.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly Logger _logger = Logger.Instance;
        private readonly ConfigService _configService = ConfigService.Instance;
        private CtYunApi _api;
        private CancellationTokenSource _globalCts;
        private List<Desktop> _activeDesktops = new();
        private string _password;

        // 绑定属性
        private string _phone;
        public string Phone
        {
            get => _phone;
            set { _phone = value; OnPropertyChanged(); UpdateCanLogin(); }
        }

        private string _deviceCode;
        public string DeviceCode
        {
            get => _deviceCode;
            set { _deviceCode = value; OnPropertyChanged(); }
        }

        private bool _isDeviceCodeReadOnly = false;
        public bool IsDeviceCodeReadOnly
        {
            get => _isDeviceCodeReadOnly;
            set { _isDeviceCodeReadOnly = value; OnPropertyChanged(); }
        }

        private string _smsCode;
        public string SmsCode
        {
            get => _smsCode;
            set { _smsCode = value; OnPropertyChanged(); }
        }

        private int _keepAliveInterval = 60;
        public int KeepAliveInterval
        {
            get => _keepAliveInterval;
            set { _keepAliveInterval = value; OnPropertyChanged(); }
        }

        private string _statusText = "就绪";
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        private System.Windows.Media.Brush _statusBrush = System.Windows.Media.Brushes.Black;
        public System.Windows.Media.Brush StatusBrush
        {
            get => _statusBrush;
            set { _statusBrush = value; OnPropertyChanged(); }
        }

        private int _desktopCount;
        public int DesktopCount
        {
            get => _desktopCount;
            set { _desktopCount = value; OnPropertyChanged(); }
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set { _isRunning = value; OnPropertyChanged(); UpdateCanStart(); }
        }

        private bool _isLoggedIn;
        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set { _isLoggedIn = value; OnPropertyChanged(); UpdateCanStart(); UpdateCanLogin(); }
        }

        private bool _isPasswordVisible = false;
        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set { _isPasswordVisible = value; OnPropertyChanged(); OnPropertyChanged(nameof(PasswordToggleIcon)); }
        }

        public string PasswordToggleIcon => IsPasswordVisible ? "🙈" : "👁";

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
                UpdateCanLogin();
            }
        }

        public bool CanLogin => !string.IsNullOrEmpty(Phone) && !string.IsNullOrEmpty(_password) && !IsLoggedIn;
        public bool CanStart => IsLoggedIn && !IsRunning;

        // 命令
        public ICommand LoginCommand { get; }
        public ICommand GetSmsCodeCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand TogglePasswordVisibilityCommand { get; }

        public MainViewModel()
        {
            LoadDeviceCode();
            LoadSavedCredentials();

            LoginCommand = new RelayCommand(async () => await LoginAsync(), () => CanLogin);
            GetSmsCodeCommand = new RelayCommand(async () => await GetSmsCodeAsync(), () => !string.IsNullOrEmpty(Phone) && _api != null);
            StartCommand = new RelayCommand(async () => await StartKeepAliveAsync(), () => CanStart);
            StopCommand = new RelayCommand(StopKeepAlive, () => IsRunning);
            ClearLogCommand = new RelayCommand(ClearLog);
            TogglePasswordVisibilityCommand = new RelayCommand(() => IsPasswordVisible = !IsPasswordVisible);
        }

        private void LoadSavedCredentials()
        {
            try
            {
                var (phone, password) = _configService.LoadCredentials();
                if (!string.IsNullOrEmpty(phone))
                {
                    Phone = phone;
                    _logger.Log("已加载保存的手机号", LogLevel.Info);
                }
                if (!string.IsNullOrEmpty(password))
                {
                    _password = password;
                    _logger.Log("已加载保存的密码", LogLevel.Info);
                    // 通知密码已加载，让界面更新
                    OnPropertyChanged(nameof(CanLogin));
                    (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"加载保存的凭据失败: {ex.Message}", LogLevel.Error);
            }
        }

        public void SaveCredentials()
        {
            try
            {
                if (!string.IsNullOrEmpty(Phone) && !string.IsNullOrEmpty(_password))
                {
                    _configService.SaveCredentials(Phone, _password);
                    _logger.Log("凭据已保存", LogLevel.Success);
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"保存凭据失败: {ex.Message}", LogLevel.Error);
            }
        }

        private void LoadDeviceCode()
        {
            try
            {
                if (File.Exists("DeviceCode.txt"))
                {
                    DeviceCode = File.ReadAllText("DeviceCode.txt");
                    IsDeviceCodeReadOnly = true;
                    _logger.Log($"已加载设备标识: {DeviceCode}", LogLevel.Info);
                }
                else
                {
                    DeviceCode = "";
                    IsDeviceCodeReadOnly = false;
                    _logger.Log("未找到 DeviceCode.txt，设备标识留空", LogLevel.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"加载设备标识失败: {ex.Message}", LogLevel.Error);
                IsDeviceCodeReadOnly = false;
            }
        }

        private void SaveDeviceCode()
        {
            try
            {
                if (!string.IsNullOrEmpty(DeviceCode))
                {
                    File.WriteAllText("DeviceCode.txt", DeviceCode);
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"保存设备标识失败: {ex.Message}", LogLevel.Error);
            }
        }

        public void SetPassword(string password)
        {
            _password = password;
            UpdateCanLogin();
        }

        private void UpdateCanLogin()
        {
            OnPropertyChanged(nameof(CanLogin));
            (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void UpdateCanStart()
        {
            OnPropertyChanged(nameof(CanStart));
            (StartCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StopCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private async Task LoginAsync()
        {
            try
            {
                StatusText = "正在登录...";
                StatusBrush = Brushes.Orange;
                _logger.Log("开始登录流程...", LogLevel.Info);

                // 如果设备码为空，生成一个
                if (string.IsNullOrEmpty(DeviceCode))
                {
                    DeviceCode = "web_" + GenerateRandomString(32);
                    SaveDeviceCode();
                    IsDeviceCodeReadOnly = true;
                    _logger.Log($"生成新设备标识: {DeviceCode}", LogLevel.Info);
                }

                _api = new CtYunApi(DeviceCode);
                _api.OnLog = (msg, color) =>
                {
                    var level = color switch
                    {
                        ConsoleColor.Green => LogLevel.Success,
                        ConsoleColor.Red => LogLevel.Error,
                        ConsoleColor.Yellow => LogLevel.Warning,
                        ConsoleColor.White => LogLevel.Debug,
                        _ => LogLevel.Info
                    };
                    _logger.Log(msg, level);
                };

                // 登录
                if (!await _api.LoginAsync(Phone, _password))
                {
                    StatusText = "登录失败";
                    StatusBrush = Brushes.Red;
                    _logger.Log("登录失败，请检查账号密码", LogLevel.Error);
                    return;
                }

                _logger.Log($"登录成功，用户: {_api.LoginInfo.UserName}", LogLevel.Success);

                // 保存凭据
                SaveCredentials();

                // 检查是否需要绑定设备
                if (!_api.LoginInfo.BondedDevice)
                {
                    StatusText = "需要短信验证";
                    StatusBrush = Brushes.Orange;
                    _logger.Log("设备未绑定，需要短信验证码", LogLevel.Warning);

                    if (string.IsNullOrEmpty(SmsCode))
                    {
                        _logger.Log("请点击\"获取验证码\"按钮获取短信验证码", LogLevel.Info);
                        return;
                    }

                    if (!await _api.BindingDeviceAsync(SmsCode))
                    {
                        StatusText = "设备绑定失败";
                        StatusBrush = Brushes.Red;
                        _logger.Log("设备绑定失败，请检查验证码", LogLevel.Error);
                        return;
                    }

                    _logger.Log("设备绑定成功", LogLevel.Success);
                }

                IsLoggedIn = true;
                StatusText = "登录成功";
                StatusBrush = Brushes.Green;

                // 获取云电脑列表
                await LoadDesktopListAsync();
            }
            catch (Exception ex)
            {
                StatusText = "登录异常";
                StatusBrush = Brushes.Red;
                _logger.Log($"登录异常: {ex.Message}", LogLevel.Error);
            }
        }

        private async Task GetSmsCodeAsync()
        {
            try
            {
                if (_api == null)
                {
                    _api = new CtYunApi(DeviceCode);
                }

                _logger.Log("正在获取短信验证码...", LogLevel.Info);
                if (await _api.GetSmsCodeAsync(Phone))
                {
                    _logger.Log("短信验证码已发送，请注意查收", LogLevel.Success);
                }
                else
                {
                    _logger.Log("获取短信验证码失败", LogLevel.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"获取验证码异常: {ex.Message}", LogLevel.Error);
            }
        }

        private async Task LoadDesktopListAsync()
        {
            try
            {
                _logger.Log("正在获取云电脑列表...", LogLevel.Info);
                var desktopList = await _api.GetLlientListAsync();

                if (desktopList == null)
                {
                    _logger.Log("获取云电脑列表失败", LogLevel.Error);
                    return;
                }

                DesktopCount = desktopList.Count;
                _logger.Log($"找到 {desktopList.Count} 台云电脑", LogLevel.Info);

                foreach (var d in desktopList)
                {
                    if (d.UseStatusText != "运行中")
                    {
                        _logger.Log($"[{d.DesktopCode}] 电脑未开机，状态: {d.UseStatusText}", LogLevel.Warning);
                    }
                    else
                    {
                        _logger.Log($"[{d.DesktopCode}] 电脑运行中", LogLevel.Info);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"获取云电脑列表异常: {ex.Message}", LogLevel.Error);
            }
        }

        private async Task StartKeepAliveAsync()
        {
            try
            {
                _globalCts = new CancellationTokenSource();
                IsRunning = true;
                StatusText = "保活运行中";
                StatusBrush = Brushes.Green;

                _logger.Log($"开始保活任务，间隔: {KeepAliveInterval} 秒", LogLevel.Info);

                // 获取云电脑列表并连接
                var desktopList = await _api.GetLlientListAsync();
                _activeDesktops = new List<Desktop>();

                foreach (var d in desktopList)
                {
                    if (d.UseStatusText != "运行中")
                    {
                        _logger.Log($"[{d.DesktopCode}] 电脑未开机，正在开机，请在2分钟后重新运行软件", LogLevel.Warning);
                    }

                    var connectResult = await _api.ConnectAsync(d.DesktopId);
                    if (connectResult.Success && connectResult.Data.DesktopInfo != null)
                    {
                        d.DesktopInfo = connectResult.Data.DesktopInfo;
                        _activeDesktops.Add(d);
                        _logger.Log($"[{d.DesktopCode}] 连接成功", LogLevel.Success);
                    }
                    else
                    {
                        _logger.Log($"[{d.DesktopId}] 连接失败: {connectResult.Msg}", LogLevel.Error);
                    }
                }

                if (_activeDesktops.Count == 0)
                {
                    _logger.Log("没有可用的云电脑", LogLevel.Error);
                    StopKeepAlive();
                    return;
                }

                // 为每台设备开启保活任务
                var tasks = _activeDesktops.Select(d => KeepAliveWorkerAsync(d, _globalCts.Token));
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                _logger.Log("保活任务已取消", LogLevel.Info);
            }
            catch (Exception ex)
            {
                _logger.Log($"保活异常: {ex.Message}", LogLevel.Error);
            }
            finally
            {
                IsRunning = false;
                StatusText = "已停止";
                StatusBrush = Brushes.Black;
            }
        }

        private async Task KeepAliveWorkerAsync(Desktop desktop, CancellationToken globalToken)
        {
            var initialPayload = Convert.FromBase64String("UkVEUQIAAAACAAAAGgAAAAAAAAABAAEAAAABAAAAEgAAAAkAAAAECAAA");
            var uri = new Uri($"wss://{desktop.DesktopInfo.ClinkLvsOutHost}/clinkProxy/{desktop.DesktopId}/MAIN");

            while (!globalToken.IsCancellationRequested)
            {
                using var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(globalToken);
                sessionCts.CancelAfter(TimeSpan.FromSeconds(KeepAliveInterval));

                using var client = new System.Net.WebSockets.ClientWebSocket();
                client.Options.SetRequestHeader("Origin", "https://pc.ctyun.cn");
                client.Options.AddSubProtocol("binary");

                try
                {
                    _logger.Log($"[{desktop.DesktopCode}] 正在建立连接...", LogLevel.Info);
                    await client.ConnectAsync(uri, sessionCts.Token);

                    // 发送 JSON 握手信息
                    var connectMessage = new ConnecMessage
                    {
                        type = 1,
                        ssl = 1,
                        host = desktop.DesktopInfo.ClinkLvsOutHost.Split(":")[0],
                        port = desktop.DesktopInfo.ClinkLvsOutHost.Split(":")[1],
                        ca = desktop.DesktopInfo.CaCert,
                        cert = desktop.DesktopInfo.ClientCert,
                        key = desktop.DesktopInfo.ClientKey,
                        servername = desktop.DesktopInfo.Host + ":" + desktop.DesktopInfo.Port,
                        oqs = 0
                    };
                    var msgBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(connectMessage, AppJsonSerializerContext.Default.ConnecMessage);
                    await client.SendAsync(msgBytes, System.Net.WebSockets.WebSocketMessageType.Text, true, sessionCts.Token);

                    // 发送 sendHDR
                    await Task.Delay(500, sessionCts.Token);
                    await client.SendAsync(initialPayload, System.Net.WebSockets.WebSocketMessageType.Binary, true, sessionCts.Token);

                    _logger.Log($"[{desktop.DesktopCode}] 连接已就绪，保持 {KeepAliveInterval} 秒...", LogLevel.Success);

                    await ReceiveLoopAsync(client, desktop, sessionCts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.Log($"[{desktop.DesktopCode}] {KeepAliveInterval}秒时间到，准备重连...", LogLevel.Warning);
                }
                catch (Exception ex)
                {
                    _logger.Log($"[{desktop.DesktopCode}] 异常: {ex.Message}", LogLevel.Error);
                    await Task.Delay(5000, globalToken);
                }
                finally
                {
                    if (client.State == System.Net.WebSockets.WebSocketState.Open)
                    {
                        await client.CloseOutputAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Timeout Reset", CancellationToken.None);
                    }
                }
            }
        }

        private async Task ReceiveLoopAsync(System.Net.WebSockets.ClientWebSocket ws, Desktop desktop, CancellationToken ct)
        {
            var buffer = new byte[8192];
            var encryptor = new Encryption();

            while (ws.State == System.Net.WebSockets.WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close) break;

                if (result.Count > 0)
                {
                    var data = buffer.AsSpan(0, result.Count).ToArray();
                    var hex = BitConverter.ToString(data).Replace("-", "");
                    if (hex.StartsWith("52454451", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.Log($"[{desktop.DesktopCode}] 收到保活校验", LogLevel.Info);
                        var response = encryptor.Execute(data);
                        await ws.SendAsync(response, System.Net.WebSockets.WebSocketMessageType.Binary, true, ct);
                        _logger.Log($"[{desktop.DesktopCode}] 发送保活响应成功", LogLevel.Success);
                    }
                    else
                    {
                        try
                        {
                            var infos = SendInfo.FromBuffer(data);
                            foreach (var info in infos)
                            {
                                if (info.Type == 103) // CLINK_MSG_MAIN_INIT
                                {
                                    var byUserName = new SendInfo
                                    {
                                        Type = 118,
                                        Data = Encoding.UTF8.GetBytes("{\"type\":1,\"userName\":\"" + _api.LoginInfo.UserName + "\",\"userInfo\":\"\",\"userId\":" + _api.LoginInfo.UserId + "}")
                                    }.ToBuffer(true);
                                    await ws.SendAsync(byUserName, System.Net.WebSockets.WebSocketMessageType.Binary, true, ct);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Log($"处理消息异常: {ex.Message}", LogLevel.Error);
                        }
                    }
                }
            }
        }

        private void StopKeepAlive()
        {
            _globalCts?.Cancel();
            IsRunning = false;
            StatusText = "已停止";
            StatusBrush = Brushes.Black;
            _logger.Log("保活任务已停止", LogLevel.Info);
        }

        private void ClearLog()
        {
            _logger.Clear();
        }

        private static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[RandomNumberGenerator.GetInt32(s.Length)]).ToArray());
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
