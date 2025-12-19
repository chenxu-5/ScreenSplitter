using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;

namespace ScreenSplitter
{
    /// <summary>
    /// 竖屏接力分屏软件核心实现（多显示器多应用扩展版本）
    /// 实现屏幕左右分屏的内容连续滚动效果，支持多显示器和多应用
    /// </summary>
    public class ScreenSplitterRelayDemo : IDisposable
    {
        // Windows API常量定义
        private const int WM_HOTKEY = 0x0312;
        private const int WM_VSCROLL = 0x115;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WH_CALLWNDPROC = 4;
        private const int MOD_CONTROL = 0x0002;
        private const int MOD_ALT = 0x0001;

        // 分屏快捷键定义
        private const int HOTKEY_ENABLE_RELAY = 1;
        private const int HOTKEY_DISABLE_RELAY = 2;
        private const int HOTKEY_REFRESH_DISPLAYS = 3;

        // 窗口句柄和状态
        private NotifyIcon _trayIcon;
        private bool _isRelayEnabled = false;

        // 钩子相关
        private IntPtr _hookId = IntPtr.Zero;
        private HookProc _hookProc;

        // 核心服务模块
        private DisplayManager _displayManager;
        private ApplicationManager _appManager;
        private CrossDisplayRelayService _crossDisplayRelayService;
        private RelayConfig _relayConfig;

        /// <summary>
        /// 初始化竖屏接力分屏软件
        /// </summary>
        public ScreenSplitterRelayDemo()
        {
            // 初始化配置
            _relayConfig = new RelayConfig
            {
                IsGloballyEnabled = true,
                DefaultOverlapHeight = 50,
                DefaultSplitType = SplitType.LeftRight,
                DefaultSplitRatio = 0.5,
                ScrollThreshold = 50,
                Whitelist = new List<string>(),
                Blacklist = new List<string>()
            };

            // 初始化核心服务
            _displayManager = new DisplayManager();
            _appManager = new ApplicationManager();
            _crossDisplayRelayService = new CrossDisplayRelayService(_displayManager, _appManager, _relayConfig);
        }

        /// <summary>
        /// 启动软件
        /// </summary>
        public void Initialize()
        {
            // 注册全局快捷键
            RegisterHotkeys();
            
            // 创建系统托盘图标
            CreateTrayIcon();
            
            // 初始化钩子服务
            InitializeHook();
            
            // 检测显示器配置
            RefreshDisplayConfig();
        }

        /// <summary>
        /// 注册全局快捷键
        /// </summary>
        private void RegisterHotkeys()
        {
            // Ctrl + Alt + R - 启用竖屏接力
            RegisterHotKey(IntPtr.Zero, HOTKEY_ENABLE_RELAY, MOD_CONTROL | MOD_ALT, (int)'R');
            
            // Ctrl + Alt + F - 禁用竖屏接力
            RegisterHotKey(IntPtr.Zero, HOTKEY_DISABLE_RELAY, MOD_CONTROL | MOD_ALT, (int)'F');
            
            // Ctrl + Alt + D - 刷新显示器配置
            RegisterHotKey(IntPtr.Zero, HOTKEY_REFRESH_DISPLAYS, MOD_CONTROL | MOD_ALT, (int)'D');
        }

        /// <summary>
        /// 创建系统托盘图标
        /// </summary>
        private void CreateTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "竖屏接力分屏工具 - 已禁用"
            };

            // 创建上下文菜单
            ContextMenu contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add("启用竖屏接力", OnEnableRelayClick);
            contextMenu.MenuItems.Add("禁用竖屏接力", OnDisableRelayClick);
            contextMenu.MenuItems.Add("刷新显示器配置", OnRefreshDisplaysClick);
            contextMenu.MenuItems.Add("退出", OnExitClick);
            _trayIcon.ContextMenu = contextMenu;
        }

        /// <summary>
        /// 初始化Windows钩子
        /// </summary>
        private void InitializeHook()
        {
            _hookProc = new HookProc(HookCallback);
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                _hookId = SetWindowsHookEx(WH_CALLWNDPROC, _hookProc, 
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        /// <summary>
        /// 钩子回调函数，处理滚动事件
        /// </summary>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _isRelayEnabled)
            {
                // 解析消息结构
                CWPSTRUCT cwp = (CWPSTRUCT)Marshal.PtrToStructure(lParam, typeof(CWPSTRUCT));
                
                // 检查是否为滚动消息
                if (cwp.message == WM_VSCROLL || cwp.message == WM_MOUSEWHEEL)
                {
                    // 处理滚动事件
                    HandleScrollMessage(cwp.hwnd, cwp.message, cwp.wParam, cwp.lParam);
                }
            }
            
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        /// <summary>
        /// 处理滚动消息
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="message">消息类型</param>
        /// <param name="wParam">消息参数W</param>
        /// <param name="lParam">消息参数L</param>
        private void HandleScrollMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam)
        {
            // 获取应用程序配置
            ApplicationConfig appConfig = _appManager.GetAppConfig(hWnd);
            if (!appConfig.IsRelayEnabled)
                return;

            // 解析滚动方向和偏移量
            ScrollDirection direction;
            int scrollOffset = 0;
            
            if (message == WM_MOUSEWHEEL)
            {
                // 鼠标滚轮事件
                int delta = (short)HIWORD(wParam);
                direction = delta > 0 ? ScrollDirection.Up : ScrollDirection.Down;
                scrollOffset = Math.Abs(delta) / 120 * 15; // 计算滚动行数
            }
            else
            {
                // 垂直滚动条事件
                int scrollCode = LOWORD(wParam);
                if (scrollCode == SB_LINEDOWN)
                {
                    direction = ScrollDirection.Down;
                    scrollOffset = 15;
                }
                else if (scrollCode == SB_LINEUP)
                {
                    direction = ScrollDirection.Up;
                    scrollOffset = 15;
                }
                else
                {
                    return; // 忽略其他滚动类型
                }
            }

            // 处理跨显示器滚动
            _crossDisplayRelayService.HandleCrossDisplayScroll(hWnd, direction, scrollOffset);
        }

        /// <summary>
        /// 刷新显示器配置
        /// </summary>
        public void RefreshDisplayConfig()
        {
            _displayManager.DetectDisplayChanges();
            _trayIcon.ShowBalloonTip(1000, "显示器配置", 
                $"已检测到 {_displayManager.GetAllDisplayConfigs().Count} 个显示器", 
                ToolTipIcon.Info);
        }

        /// <summary>
        /// 启用竖屏接力模式
        /// </summary>
        public void EnableRelayMode()
        {
            _isRelayEnabled = true;
            _trayIcon.Text = "竖屏接力分屏工具 - 已启用";
            _trayIcon.ShowBalloonTip(1000, "竖屏接力", "竖屏接力模式已启用", ToolTipIcon.Info);
        }

        /// <summary>
        /// 禁用竖屏接力模式
        /// </summary>
        public void DisableRelayMode()
        {
            _isRelayEnabled = false;
            _trayIcon.Text = "竖屏接力分屏工具 - 已禁用";
            _trayIcon.ShowBalloonTip(1000, "竖屏接力", "竖屏接力模式已禁用", ToolTipIcon.Info);
        }

        /// <summary>
        /// 处理快捷键消息
        /// </summary>
        public void HandleHotkey(int hotkeyId)
        {
            switch (hotkeyId)
            {
                case HOTKEY_ENABLE_RELAY:
                    EnableRelayMode();
                    break;
                case HOTKEY_DISABLE_RELAY:
                    DisableRelayMode();
                    break;
                case HOTKEY_REFRESH_DISPLAYS:
                    RefreshDisplayConfig();
                    break;
            }
        }

        /// <summary>
        /// 启用接力菜单点击事件
        /// </summary>
        private void OnEnableRelayClick(object sender, EventArgs e)
        {
            EnableRelayMode();
        }

        /// <summary>
        /// 禁用接力菜单点击事件
        /// </summary>
        private void OnDisableRelayClick(object sender, EventArgs e)
        {
            DisableRelayMode();
        }

        /// <summary>
        /// 刷新显示器配置菜单点击事件
        /// </summary>
        private void OnRefreshDisplaysClick(object sender, EventArgs e)
        {
            RefreshDisplayConfig();
        }

        /// <summary>
        /// 退出菜单点击事件
        /// </summary>
        private void OnExitClick(object sender, EventArgs e)
        {
            // 清理资源
            Dispose();
            Application.Exit();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // 注销快捷键
            UnregisterHotKey(IntPtr.Zero, HOTKEY_ENABLE_RELAY);
            UnregisterHotKey(IntPtr.Zero, HOTKEY_DISABLE_RELAY);
            UnregisterHotKey(IntPtr.Zero, HOTKEY_REFRESH_DISPLAYS);
            
            // 移除钩子
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
            
            // 释放托盘图标
            _trayIcon.Dispose();
        }

        // Windows API结构体定义
        [StructLayout(LayoutKind.Sequential)]
        private struct CWPSTRUCT
        {
            public IntPtr lParam;
            public IntPtr wParam;
            public int message;
            public IntPtr hwnd;
        }

        // Windows API常量补充
        private const int SB_VERT = 1;
        private const int SB_LINEDOWN = 1;
        private const int SB_LINEUP = 0;
        private const int SB_THUMBPOSITION = 4;

        // 辅助函数
        private static int HIWORD(IntPtr ptr)
        {
            return (unchecked((int)(long)ptr) >> 16) & 0xffff;
        }

        private static int LOWORD(IntPtr ptr)
        {
            return unchecked((int)(long)ptr) & 0xffff;
        }

        // 钩子委托
        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        // Windows API函数声明
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }

    #region 数据模型扩展

    /// <summary>
    /// 显示器配置类
    /// 存储单个显示器的分屏和接力配置
    /// </summary>
    public class DisplayConfig
    {
        /// <summary>
        /// 显示器唯一标识符
        /// </summary>
        public string DisplayId { get; set; }
        
        /// <summary>
        /// 显示器设备名称
        /// </summary>
        public string DeviceName { get; set; }
        
        /// <summary>
        /// 显示器边界信息
        /// </summary>
        public Rectangle Bounds { get; set; }
        
        /// <summary>
        /// 显示器工作区域
        /// </summary>
        public Rectangle WorkingArea { get; set; }
        
        /// <summary>
        /// 分屏类型
        /// </summary>
        public SplitType SplitType { get; set; }
        
        /// <summary>
        /// 分屏比例（0.0-1.0）
        /// </summary>
        public double SplitRatio { get; set; }
        
        /// <summary>
        /// 是否启用竖屏接力
        /// </summary>
        public bool IsRelayEnabled { get; set; }
        
        /// <summary>
        /// 重叠区域高度（像素）
        /// </summary>
        public int OverlapHeight { get; set; }
        
        /// <summary>
        /// 相邻显示器ID列表
        /// </summary>
        public List<string> NeighborDisplayIds { get; set; }
        
        /// <summary>
        /// 显示器方向（水平/垂直）
        /// </summary>
        public DisplayOrientation Orientation { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public DisplayConfig()
        {
            NeighborDisplayIds = new List<string>();
        }
    }

    /// <summary>
    /// 显示器方向枚举
    /// </summary>
    public enum DisplayOrientation
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// 扩展分屏类型枚举
    /// </summary>
    public enum SplitType
    {
        LeftRight,      // 左右分屏
        TopBottom,      // 上下分屏
        LeftCenterRight,// 左中右三分屏
        TopCenterBottom,// 上中下三分屏
        Custom          // 自定义分屏
    }

    /// <summary>
    /// 应用程序配置类
    /// 存储单个应用程序的分屏偏好
    /// </summary>
    public class ApplicationConfig
    {
        /// <summary>
        /// 应用程序进程名
        /// </summary>
        public string ProcessName { get; set; }
        
        /// <summary>
        /// 应用程序窗口标题（支持通配符）
        /// </summary>
        public string WindowTitle { get; set; }
        
        /// <summary>
        /// 是否启用分屏接力
        /// </summary>
        public bool IsRelayEnabled { get; set; }
        
        /// <summary>
        /// 首选分屏类型
        /// </summary>
        public SplitType PreferredSplitType { get; set; }
        
        /// <summary>
        /// 首选分屏比例
        /// </summary>
        public double PreferredSplitRatio { get; set; }
        
        /// <summary>
        /// 应用程序类型
        /// </summary>
        public ApplicationType AppType { get; set; }
        
        /// <summary>
        /// 上次使用的显示器ID
        /// </summary>
        public string LastDisplayId { get; set; }
        
        /// <summary>
        /// 自定义分屏区域
        /// </summary>
        public List<Rectangle> CustomSplitRegions { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ApplicationConfig()
        {
            CustomSplitRegions = new List<Rectangle>();
        }
    }

    /// <summary>
    /// 应用程序类型枚举
    /// </summary>
    public enum ApplicationType
    {
        Browser,        // 浏览器
        Document,       // 文档阅读器
        CodeEditor,     // 代码编辑器
        ImageViewer,    // 图片查看器
        VideoPlayer,    // 视频播放器
        Game,           // 游戏
        Other           // 其他类型
    }

    /// <summary>
    /// 全局接力配置类
    /// </summary>
    public class RelayConfig
    {
        /// <summary>
        /// 是否全局启用竖屏接力
        /// </summary>
        public bool IsGloballyEnabled { get; set; }
        
        /// <summary>
        /// 默认重叠区域高度
        /// </summary>
        public int DefaultOverlapHeight { get; set; }
        
        /// <summary>
        /// 默认分屏类型
        /// </summary>
        public SplitType DefaultSplitType { get; set; }
        
        /// <summary>
        /// 默认分屏比例
        /// </summary>
        public double DefaultSplitRatio { get; set; }
        
        /// <summary>
        /// 滚动触发阈值
        /// </summary>
        public int ScrollThreshold { get; set; }
        
        /// <summary>
        /// 应用程序白名单
        /// </summary>
        public List<string> Whitelist { get; set; }
        
        /// <summary>
        /// 应用程序黑名单
        /// </summary>
        public List<string> Blacklist { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public RelayConfig()
        {
            Whitelist = new List<string>();
            Blacklist = new List<string>();
        }
    }

    /// <summary>
    /// 滚动方向枚举
    /// </summary>
    public enum ScrollDirection
    {
        Up,
        Down
    }

    #endregion

    #region 核心服务模块

    /// <summary>
    /// 显示器管理类
    /// 负责管理所有显示器的信息和配置
    /// </summary>
    public class DisplayManager
    {
        private List<DisplayConfig> _displayConfigs;
        private Dictionary<string, DisplayConfig> _displayIdToConfigMap;
        
        /// <summary>
        /// 初始化显示器管理器
        /// </summary>
        public DisplayManager()
        {
            _displayConfigs = new List<DisplayConfig>();
            _displayIdToConfigMap = new Dictionary<string, DisplayConfig>();
            InitializeDisplays();
        }
        
        /// <summary>
        /// 初始化显示器配置
        /// </summary>
        private void InitializeDisplays()
        {
            _displayConfigs.Clear();
            _displayIdToConfigMap.Clear();

            // 获取所有显示器
            foreach (var screen in Screen.AllScreens)
            {
                DisplayConfig config = new DisplayConfig
                {
                    DisplayId = screen.DeviceName, // 使用设备名称作为唯一ID
                    DeviceName = screen.DeviceName,
                    Bounds = screen.Bounds,
                    WorkingArea = screen.WorkingArea,
                    SplitType = SplitType.LeftRight,
                    SplitRatio = 0.5,
                    IsRelayEnabled = true,
                    OverlapHeight = 50,
                    Orientation = screen.Bounds.Width > screen.Bounds.Height ? 
                        DisplayOrientation.Horizontal : DisplayOrientation.Vertical
                };
                
                // 查找相邻显示器
                config.NeighborDisplayIds = FindNeighborDisplays(screen);
                
                _displayConfigs.Add(config);
                _displayIdToConfigMap[config.DisplayId] = config;
            }
        }
        
        /// <summary>
        /// 查找相邻显示器
        /// </summary>
        /// <param name="screen">当前显示器</param>
        /// <returns>相邻显示器ID列表</returns>
        private List<string> FindNeighborDisplays(Screen screen)
        {
            List<string> neighbors = new List<string>();
            Rectangle currentBounds = screen.Bounds;
            
            foreach (var otherScreen in Screen.AllScreens)
            {
                if (otherScreen == screen) continue;
                
                Rectangle otherBounds = otherScreen.Bounds;
                
                // 检查是否相邻（水平或垂直）
                bool isLeftNeighbor = currentBounds.Left == otherBounds.Right;
                bool isRightNeighbor = currentBounds.Right == otherBounds.Left;
                bool isTopNeighbor = currentBounds.Top == otherBounds.Bottom;
                bool isBottomNeighbor = currentBounds.Bottom == otherBounds.Top;
                
                if (isLeftNeighbor || isRightNeighbor || isTopNeighbor || isBottomNeighbor)
                {
                    neighbors.Add(otherScreen.DeviceName);
                }
            }
            
            return neighbors;
        }
        
        /// <summary>
        /// 获取显示器配置
        /// </summary>
        /// <param name="displayId">显示器ID</param>
        /// <returns>显示器配置</returns>
        public DisplayConfig GetDisplayConfig(string displayId)
        {
            if (_displayIdToConfigMap.ContainsKey(displayId))
            {
                return _displayIdToConfigMap[displayId];
            }
            return null;
        }
        
        /// <summary>
        /// 获取所有显示器配置
        /// </summary>
        /// <returns>显示器配置列表</returns>
        public List<DisplayConfig> GetAllDisplayConfigs()
        {
            return _displayConfigs;
        }
        
        /// <summary>
        /// 更新显示器配置
        /// </summary>
        /// <param name="config">显示器配置</param>
        public void UpdateDisplayConfig(DisplayConfig config)
        {
            if (_displayIdToConfigMap.ContainsKey(config.DisplayId))
            {
                _displayIdToConfigMap[config.DisplayId] = config;
            }
        }
        
        /// <summary>
        /// 检测显示器变化
        /// </summary>
        public void DetectDisplayChanges()
        {
            // 重新初始化显示器配置，处理显示器添加/移除
            InitializeDisplays();
        }
    }

    /// <summary>
    /// 应用程序管理类
    /// 负责管理应用程序的分屏配置
    /// </summary>
    public class ApplicationManager
    {
        private List<ApplicationConfig> _appConfigs;
        private Dictionary<string, ApplicationConfig> _processNameToConfigMap;
        
        /// <summary>
        /// 初始化应用程序管理器
        /// </summary>
        public ApplicationManager()
        {
            _appConfigs = new List<ApplicationConfig>();
            _processNameToConfigMap = new Dictionary<string, ApplicationConfig>();
            LoadDefaultAppConfigs();
        }
        
        /// <summary>
        /// 加载默认应用程序配置
        /// </summary>
        private void LoadDefaultAppConfigs()
        {
            // 添加常见应用程序的默认配置
            var defaultConfigs = new List<ApplicationConfig>
            {
                new ApplicationConfig
                {
                    ProcessName = "chrome",
                    WindowTitle = "*Google Chrome*",
                    IsRelayEnabled = true,
                    PreferredSplitType = SplitType.LeftRight,
                    PreferredSplitRatio = 0.5,
                    AppType = ApplicationType.Browser
                },
                new ApplicationConfig
                {
                    ProcessName = "msedge",
                    WindowTitle = "*Microsoft Edge*",
                    IsRelayEnabled = true,
                    PreferredSplitType = SplitType.LeftRight,
                    PreferredSplitRatio = 0.5,
                    AppType = ApplicationType.Browser
                },
                new ApplicationConfig
                {
                    ProcessName = "code",
                    WindowTitle = "*Visual Studio Code*",
                    IsRelayEnabled = true,
                    PreferredSplitType = SplitType.LeftRight,
                    PreferredSplitRatio = 0.5,
                    AppType = ApplicationType.CodeEditor
                },
                new ApplicationConfig
                {
                    ProcessName = "Acrobat",
                    WindowTitle = "*Adobe Acrobat*",
                    IsRelayEnabled = true,
                    PreferredSplitType = SplitType.LeftRight,
                    PreferredSplitRatio = 0.5,
                    AppType = ApplicationType.Document
                },
                new ApplicationConfig
                {
                    ProcessName = "notepad",
                    WindowTitle = "*Notepad*",
                    IsRelayEnabled = true,
                    PreferredSplitType = SplitType.LeftRight,
                    PreferredSplitRatio = 0.5,
                    AppType = ApplicationType.Other
                }
            };
            
            foreach (var config in defaultConfigs)
            {
                _appConfigs.Add(config);
                _processNameToConfigMap[config.ProcessName] = config;
            }
        }
        
        /// <summary>
        /// 获取应用程序配置
        /// </summary>
        /// <param name="processName">进程名</param>
        /// <returns>应用程序配置</returns>
        public ApplicationConfig GetAppConfig(string processName)
        {
            if (_processNameToConfigMap.ContainsKey(processName))
            {
                return _processNameToConfigMap[processName];
            }
            
            // 返回默认配置
            return new ApplicationConfig
            {
                ProcessName = processName,
                IsRelayEnabled = true,
                PreferredSplitType = SplitType.LeftRight,
                PreferredSplitRatio = 0.5,
                AppType = ApplicationType.Other
            };
        }
        
        /// <summary>
        /// 获取应用程序配置
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <returns>应用程序配置</returns>
        public ApplicationConfig GetAppConfig(IntPtr windowHandle)
        {
            // 获取窗口所属进程名
            string processName = GetProcessNameFromWindowHandle(windowHandle);
            return GetAppConfig(processName);
        }
        
        /// <summary>
        /// 从窗口句柄获取进程名
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <returns>进程名</returns>
        private string GetProcessNameFromWindowHandle(IntPtr windowHandle)
        {
            uint processId;
            GetWindowThreadProcessId(windowHandle, out processId);
            using (var process = Process.GetProcessById((int)processId))
            {
                return process.ProcessName.ToLower();
            }
        }
        
        /// <summary>
        /// 更新应用程序配置
        /// </summary>
        /// <param name="config">应用程序配置</param>
        public void UpdateAppConfig(ApplicationConfig config)
        {
            if (_processNameToConfigMap.ContainsKey(config.ProcessName))
            {
                _processNameToConfigMap[config.ProcessName] = config;
            }
            else
            {
                _appConfigs.Add(config);
                _processNameToConfigMap[config.ProcessName] = config;
            }
        }
        
        /// <summary>
        /// 检查应用程序是否支持分屏接力
        /// </summary>
        /// <param name="processName">进程名</param>
        /// <returns>是否支持</returns>
        public bool IsRelaySupported(string processName)
        {
            // 实际应用中应该检查白名单和黑名单
            return true;
        }

        // Windows API函数声明
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }

    /// <summary>
    /// 跨显示器内容接力服务
    /// 实现多显示器间的内容连续滚动
    /// </summary>
    public class CrossDisplayRelayService
    {
        private DisplayManager _displayManager;
        private ApplicationManager _appManager;
        private RelayConfig _relayConfig;
        
        /// <summary>
        /// 初始化跨显示器接力服务
        /// </summary>
        /// <param name="displayManager">显示器管理器</param>
        /// <param name="appManager">应用程序管理器</param>
        /// <param name="relayConfig">接力配置</param>
        public CrossDisplayRelayService(DisplayManager displayManager, 
                                       ApplicationManager appManager, 
                                       RelayConfig relayConfig)
        {
            _displayManager = displayManager;
            _appManager = appManager;
            _relayConfig = relayConfig;
        }
        
        /// <summary>
        /// 计算跨显示器的滚动位置
        /// </summary>
        /// <param name="sourceDisplayId">源显示器ID</param>
        /// <param name="targetDisplayId">目标显示器ID</param>
        /// <param name="sourceScrollPos">源显示器滚动位置</param>
        /// <returns>目标显示器应该显示的滚动位置</returns>
        public int CalculateCrossDisplayScrollPosition(string sourceDisplayId, 
                                                     string targetDisplayId, 
                                                     int sourceScrollPos)
        {
            DisplayConfig sourceConfig = _displayManager.GetDisplayConfig(sourceDisplayId);
            DisplayConfig targetConfig = _displayManager.GetDisplayConfig(targetDisplayId);
            
            if (sourceConfig == null || targetConfig == null)
                return 0;
            
            // 计算源显示器的分屏区域高度
            int sourceSplitHeight = GetSplitRegionHeight(sourceConfig);
            
            // 计算跨显示器的滚动偏移量
            int targetScrollPos = sourceScrollPos - sourceSplitHeight + _relayConfig.DefaultOverlapHeight;
            
            return Math.Max(0, targetScrollPos);
        }
        
        /// <summary>
        /// 获取分屏区域高度
        /// </summary>
        /// <param name="displayConfig">显示器配置</param>
        /// <returns>分屏区域高度</returns>
        private int GetSplitRegionHeight(DisplayConfig displayConfig)
        {
            switch (displayConfig.SplitType)
            {
                case SplitType.LeftRight:
                case SplitType.LeftCenterRight:
                    return displayConfig.Bounds.Height;
                case SplitType.TopBottom:
                case SplitType.TopCenterBottom:
                    return (int)(displayConfig.Bounds.Height * displayConfig.SplitRatio);
                default:
                    return displayConfig.Bounds.Height;
            }
        }
        
        /// <summary>
        /// 处理跨显示器滚动事件
        /// </summary>
        /// <param name="sourceWindowHandle">源窗口句柄</param>
        /// <param name="direction">滚动方向</param>
        /// <param name="scrollOffset">滚动偏移量</param>
        public void HandleCrossDisplayScroll(IntPtr sourceWindowHandle, ScrollDirection direction, int scrollOffset)
        {
            // 获取源窗口所在的显示器
            Screen sourceScreen = Screen.FromHandle(sourceWindowHandle);
            DisplayConfig sourceDisplayConfig = _displayManager.GetDisplayConfig(sourceScreen.DeviceName);
            
            // 获取应用程序配置
            ApplicationConfig appConfig = _appManager.GetAppConfig(sourceWindowHandle);
            
            if (!appConfig.IsRelayEnabled || !sourceDisplayConfig.IsRelayEnabled)
                return;
            
            // 查找目标显示器
            string targetDisplayId = FindTargetDisplay(sourceDisplayConfig, direction);
            if (string.IsNullOrEmpty(targetDisplayId))
                return;
            
            // 获取目标显示器配置
            DisplayConfig targetDisplayConfig = _displayManager.GetDisplayConfig(targetDisplayId);
            if (targetDisplayConfig == null || !targetDisplayConfig.IsRelayEnabled)
                return;
            
            // 计算目标窗口的滚动位置（简化处理）
            int sourceScrollPos = GetCurrentScrollPosition(sourceWindowHandle);
            int targetScrollPos = CalculateCrossDisplayScrollPosition(
                sourceDisplayConfig.DisplayId, 
                targetDisplayId, 
                sourceScrollPos);
            
            // 注意：实际应用中需要查找或创建对应的目标窗口
            // 这里仅作为示例，右侧窗口句柄需要根据实际情况获取
            // IntPtr targetWindowHandle = FindOrCreateTargetWindow(targetDisplayId, appConfig);
            // if (targetWindowHandle != IntPtr.Zero)
            // {
            //     SyncScroll(targetWindowHandle, targetScrollPos);
            // }
        }
        
        /// <summary>
        /// 查找目标显示器
        /// </summary>
        /// <param name="sourceConfig">源显示器配置</param>
        /// <param name="direction">滚动方向</param>
        /// <returns>目标显示器ID</returns>
        private string FindTargetDisplay(DisplayConfig sourceConfig, ScrollDirection direction)
        {
            // 简化处理，返回第一个相邻显示器
            return sourceConfig.NeighborDisplayIds.FirstOrDefault();
        }
        
        /// <summary>
        /// 获取当前窗口的滚动位置
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <returns>滚动位置</returns>
        private int GetCurrentScrollPosition(IntPtr windowHandle)
        {
            // 简化处理，返回默认值
            return 0;
        }
        
        /// <summary>
        /// 同步滚动窗口
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <param name="scrollPosition">滚动位置</param>
        private void SyncScroll(IntPtr windowHandle, int scrollPosition)
        {
            // 调用Windows API设置滚动位置
            const int WM_VSCROLL = 0x115;
            const int SB_THUMBPOSITION = 4;
            IntPtr wParam = (IntPtr)(SB_THUMBPOSITION | (scrollPosition << 16));
            SendMessage(windowHandle, WM_VSCROLL, wParam, IntPtr.Zero);
        }

        // Windows API函数声明
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
    }

    #endregion
}