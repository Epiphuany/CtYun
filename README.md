# CtYun 云电脑保活工具 v2.1

一个基于 .NET 8 WPF 开发的天翼云电脑自动保活工具，通过 WebSocket 连接维持云电脑在线状态。

![界面预览](https://github.com/Epiphuany/CtYun/blob/master/images/screenshot.png?raw=true)

## 功能特性

- **现代化 WPF 界面** - 无边框窗口设计，支持拖动和自定义按钮
- **彩色日志显示** - 不同级别的日志显示不同颜色，自动滚动到最新
- **凭据自动保存** - 手机号和密码加密保存，下次启动自动填入
- **密码可见性切换** - 点击眼睛图标可显示/隐藏密码
- **自动登录** - 支持账号密码登录，自动处理验证码识别
- **设备绑定** - 自动处理新设备短信验证绑定
- **云电脑管理** - 自动获取云电脑列表，检测运行状态
- **自动保活** - 通过 WebSocket 连接自动重连，保持云电脑在线
- **保活间隔自定义** - 可自定义设置保活间隔时间（秒）
- **多设备支持** - 同时支持多台云电脑的保活任务
- **日志文件记录** - 全部日志自动保存到文件（按天分割）

## 界面截图

界面包含以下元素：
- **登录配置区域**：手机号、密码（带眼睛图标）、保活间隔、短信验证码、设备标识输入框
- **操作按钮**：登录、获取验证码、开始保活、停止、清空日志
- **状态显示**：当前状态、云电脑数量
- **日志区域**：彩色日志显示（类似终端风格），自动滚动到最新

## 技术栈

- **.NET 8** - 目标框架
- **WPF** - Windows Presentation Foundation 界面框架
- **MVVM** - Model-View-ViewModel 架构模式
- **WebSocket** - 实时通信
- **RSA-OAEP** - 加密通信
- **AES** - 凭据加密存储
- **System.Text.Json** - JSON 序列化

## 项目结构

```
CtYun/
├── App.xaml                          # 应用程序资源
├── App.xaml.cs                       # 应用程序入口
├── MainWindow.xaml                   # 主界面
├── MainWindow.xaml.cs                # 主界面代码（彩色日志处理）
├── CtYun.csproj                      # 项目文件
├── CtYunApi.cs                       # 天翼云 API 封装
├── Encryption.cs                     # RSA-OAEP 加密实现
├── Converters/
│   └── BooleanToVisibilityConverter.cs  # 布尔转可见性转换器
├── Services/
│   ├── Logger.cs                     # 日志服务（支持彩色日志）
│   └── ConfigService.cs              # 配置服务（凭据加密保存）
├── ViewModels/
│   └── MainViewModel.cs              # 主界面视图模型
└── Models/                           # 数据模型
    ├── ChallengeData.cs              # 登录挑战数据
    ├── ClientInfo.cs                 # 客户端信息/云电脑列表
    ├── ConnectInfo.cs                # 连接信息
    ├── LoginInfo.cs                  # 登录信息
    ├── ResultBase.cs                 # API 响应基类
    ├── SendInfo.cs                   # WebSocket 消息结构
    └── AppJsonSerializerContext.cs   # JSON 序列化上下文
```

## 使用方法

### 1. 编译运行

```bash
# 克隆仓库
git clone <repository-url>
cd CtYun

# 编译项目
dotnet build

# 运行程序
dotnet run

# 或发布为单文件
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

### 2. 界面操作

1. **输入账号信息**：
   - 手机号：输入天翼云账号（手机号），自动保存下次自动填入
   - 密码：输入登录密码，点击右侧 👁 图标可显示/隐藏密码，自动保存
   - 保活间隔：默认60秒，可自定义修改
   - 短信验证码：首次登录需要

2. **设备标识**：
   - 自动读取 `DeviceCode.txt`，不存在则留空
   - 首次运行自动生成并保存
   - 读取成功后不可编辑

3. **首次登录**：
   - 点击「登录」按钮
   - 如果设备未绑定，点击「获取验证码」获取短信验证码
   - 输入短信验证码后再次点击「登录」
   - 登录成功后手机号和密码自动加密保存

4. **开始保活**：
   - 登录成功后，点击「开始保活」按钮
   - 程序会自动连接所有运行中的云电脑
   - 每N秒（自定义间隔）自动重连保持在线

5. **停止保活**：
   - 点击「停止」按钮终止保活任务

6. **日志查看**：
   - 界面下方实时显示彩色日志
   - 日志自动滚动到最新
   - 完整日志保存在 `logs/CtYun_YYYYMMDD.log` 文件中（按天分割）

## 日志颜色说明

| 级别 | 颜色 | 说明 |
|------|------|------|
| Debug | 灰色 | 调试信息 |
| Info | 青色 | 普通信息 |
| Warning | 橙色 | 警告信息 |
| Error | 红色 | 错误信息 |
| Success | 绿色 | 成功信息 |

## 安全说明

- **凭据加密**：手机号和密码使用 AES-256 加密存储在 `config/user.config`
- **加密密钥**：密钥保存在 `config/key.bin`，每个设备不同
- **设备标识**：设备码保存在 `DeviceCode.txt`，用于识别设备

## 工作原理

1. **登录流程**
   - 获取挑战码和验证码
   - 使用 OCR 服务自动识别验证码
   - SHA256 加密密码进行登录
   - 新设备需要短信验证绑定
   - 登录成功后自动保存凭据

2. **保活机制**
   - 按设定间隔重新建立 WebSocket 连接
   - 发送 JSON 握手信息
   - 处理保活校验（RSA-OAEP 加密响应）
   - 发送初始化消息保持会话

3. **加密通信**
   - 使用 RSA-OAEP 填充模式
   - MGF1 掩码生成
   - 1024 位 RSA 密钥

## API 接口

- `POST /api/auth/client/login` - 用户登录
- `POST /api/auth/client/genChallengeData` - 获取挑战码
- `GET /api/auth/client/captcha` - 获取登录验证码
- `GET /api/cdserv/client/device/getSmsCode` - 获取短信验证码
- `POST /api/cdserv/client/device/binding` - 绑定设备
- `POST /api/desktop/client/pageDesktop` - 获取云电脑列表
- `POST /api/desktop/client/connect` - 连接云电脑

## 注意事项

1. **验证码识别**：使用第三方 OCR 服务 `https://orc.1999111.xyz/ocr`
2. **设备绑定**：每个设备码只需绑定一次，绑定信息保存在服务端
3. **保活频率**：建议保持默认60秒，过短可能增加服务器负担
4. **设备标识**：首次运行自动生成，保存在 `DeviceCode.txt` 文件中
5. **日志文件**：日志文件保存在程序目录下的 `logs` 文件夹中，按天分割
6. **凭据安全**：凭据使用 AES 加密存储，但请勿在公共电脑上保存密码

## 版本历史

- **v2.1** - 当前版本
  - 新增凭据自动保存功能（AES 加密）
  - 新增密码可见性切换（眼睛图标）
  - 日志文件按天分割
  - 优化界面布局

- **v2.0** - 历史版本
  - 全新 WPF 图形界面，无边框设计
  - 彩色日志显示，类似终端风格
  - 支持保活间隔自定义设置
  - 日志自动滚动到最新
  - 改进的设备码管理

- **v1.1.4** - 历史版本
  - 控制台应用程序
  - 基础保活功能

## 开发计划

- [x] 支持配置文件保存用户设置
- [x] 添加密码可见性切换
- [ ] 添加系统托盘最小化功能
- [ ] 支持开机自启动
- [ ] 添加多账号管理
- [ ] 支持定时任务

## 免责声明

本工具仅供学习研究使用，请勿用于商业用途。使用本工具产生的任何后果由使用者自行承担。

## License

MIT License

## 贡献

欢迎提交 Issue 和 Pull Request。

## 致谢

- 天翼云电脑服务由天翼云提供
- 验证码识别服务由第三方提供
