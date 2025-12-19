# 多显示器多应用竖屏接力分屏软件扩展方案

## 1. 数据模型扩展

### 1.1 显示器配置模型

```csharp
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
```

### 1.2 应用程序配置模型

```csharp
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
}
```

## 2. 核心模块扩展

### 2.1 多显示器管理模块

```csharp
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
                NeighborDisplayIds = FindNeighborDisplays(screen),
                Orientation = screen.Bounds.Width > screen.Bounds.Height ? 
                    DisplayOrientation.Horizontal : DisplayOrientation.Vertical
            };
            
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
```

### 2.2 多应用管理模块

```csharp
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
        using (var process = System.Diagnostics.Process.GetProcessById((int)processId))
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
}
```

### 2.3 跨显示器内容接力算法

```csharp
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
        
        // 计算源显示器的分屏区域
        Rectangle sourceSplitRegion = GetSplitRegion(sourceConfig, sourceConfig.SplitType, sourceConfig.SplitRatio);
        
        // 计算目标显示器的分屏区域
        Rectangle targetSplitRegion = GetSplitRegion(targetConfig, targetConfig.SplitType, targetConfig.SplitRatio);
        
        // 计算跨显示器的滚动偏移量
        // 这里实现了简化的水平方向接力算法
        int targetScrollPos = sourceScrollPos - sourceSplitRegion.Height + _relayConfig.DefaultOverlapHeight;
        
        return Math.Max(0, targetScrollPos);
    }
    
    /// <summary>
    /// 获取分屏区域
    /// </summary>
    /// <param name="displayConfig">显示器配置</param>
    /// <param name="splitType">分屏类型</param>
    /// <param name="splitRatio">分屏比例</param>
    /// <returns>分屏区域</returns>
    private Rectangle GetSplitRegion(DisplayConfig displayConfig, SplitType splitType, double splitRatio)
    {
        Rectangle bounds = displayConfig.Bounds;
        
        switch (splitType)
        {
            case SplitType.LeftRight:
                return new Rectangle(
                    bounds.Left,
                    bounds.Top,
                    (int)(bounds.Width * splitRatio),
                    bounds.Height
                );
            case SplitType.TopBottom:
                return new Rectangle(
                    bounds.Left,
                    bounds.Top,
                    bounds.Width,
                    (int)(bounds.Height * splitRatio)
                );
            default:
                return bounds;
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
        
        // 计算目标窗口的滚动位置
        int sourceScrollPos = GetCurrentScrollPosition(sourceWindowHandle);
        int targetScrollPos = CalculateCrossDisplayScrollPosition(
            sourceDisplayConfig.DisplayId, 
            targetDisplayId, 
            sourceScrollPos);
        
        // 查找或创建目标窗口（实际应用中需要实现）
        IntPtr targetWindowHandle = FindOrCreateTargetWindow(targetDisplayId, appConfig);
        if (targetWindowHandle == IntPtr.Zero)
            return;
        
        // 同步滚动目标窗口
        SyncScroll(targetWindowHandle, targetScrollPos);
    }
    
    /// <summary>
    /// 查找目标显示器
    /// </summary>
    /// <param name="sourceConfig">源显示器配置</param>
    /// <param name="direction">滚动方向</param>
    /// <returns>目标显示器ID</returns>
    private string FindTargetDisplay(DisplayConfig sourceConfig, ScrollDirection direction)
    {
        // 实际应用中需要根据滚动方向和显示器布局查找目标显示器
        // 这里简化处理，返回第一个相邻显示器
        return sourceConfig.NeighborDisplayIds.FirstOrDefault();
    }
    
    /// <summary>
    /// 获取当前窗口的滚动位置
    /// </summary>
    /// <param name="windowHandle">窗口句柄</param>
    /// <returns>滚动位置</returns>
    private int GetCurrentScrollPosition(IntPtr windowHandle)
    {
        // 实际应用中需要调用Windows API获取滚动位置
        return 0;
    }
    
    /// <summary>
    /// 查找或创建目标窗口
    /// </summary>
    /// <param name="targetDisplayId">目标显示器ID</param>
    /// <param name="appConfig">应用程序配置</param>
    /// <returns>目标窗口句柄</returns>
    private IntPtr FindOrCreateTargetWindow(string targetDisplayId, ApplicationConfig appConfig)
    {
        // 实际应用中需要实现查找或创建目标窗口的逻辑
        return IntPtr.Zero;
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
}
```

## 3. 系统架构扩展

### 3.1 扩展后的系统架构

```
Core/
├── Services/
│   ├── DisplayManager.cs           # 多显示器管理
│   ├── ApplicationManager.cs       # 多应用管理
│   ├── CrossDisplayRelayService.cs # 跨显示器接力服务
│   ├── ScrollManager.cs            # 滚动事件管理
│   ├── ContentRelayService.cs      # 内容接力逻辑
│   └── HookService.cs              # Windows钩子服务
└── Models/
    ├── DisplayConfig.cs            # 显示器配置
    ├── ApplicationConfig.cs        # 应用程序配置
    ├── RelayConfig.cs              # 接力配置
    ├── ScrollEvent.cs              # 滚动事件数据
    └── SplitType.cs                # 分屏类型枚举
```

### 3.2 事件处理流程

```
1. 用户在应用程序中滚动
2. Windows发送滚动消息（WM_VSCROLL/WM_MOUSEWHEEL）
3. HookService捕获滚动消息
4. ScrollManager解析滚动事件
5. 确定当前窗口所在的显示器和应用程序
6. 获取显示器配置和应用程序配置
7. 检查是否启用了分屏接力
8. 如果是单显示器内接力：调用ContentRelayService
9. 如果是跨显示器接力：调用CrossDisplayRelayService
10. 计算目标窗口的滚动位置
11. 同步滚动目标窗口
12. 更新滚动状态
```

## 4. 实现建议

### 4.1 开发顺序

1. 实现数据模型扩展
2. 开发DisplayManager和ApplicationManager
3. 实现CrossDisplayRelayService
4. 扩展ScrollManager以支持跨显示器滚动
5. 更新HookService以处理多显示器场景
6. 开发配置管理界面

### 4.2 性能优化

1. **事件过滤**：仅处理激活窗口和支持分屏接力的应用程序的滚动事件
2. **异步处理**：跨显示器滚动计算和窗口操作使用异步线程
3. **缓存机制**：缓存显示器布局和应用程序配置，减少重复计算
4. **批量处理**：合并连续的滚动事件，减少窗口操作次数
5. **智能检测**：仅在滚动接近边界时才进行跨显示器接力计算

### 4.3 兼容性考虑

1. **不同分辨率**：处理不同分辨率显示器间的内容映射
2. **不同刷新率**：确保滚动平滑，避免不同刷新率导致的视觉问题
3. **不同DPI设置**：处理高DPI显示器的缩放问题
4. **不同方向**：支持水平和垂直方向的显示器
5. **应用程序差异**：针对不同类型应用程序采用不同的滚动处理策略

## 5. 测试建议

### 5.1 功能测试

1. **多显示器检测**：测试显示器添加/移除时的动态响应
2. **跨显示器接力**：测试不同方向、分辨率显示器间的内容连续滚动
3. **多应用支持**：测试不同类型应用程序的分屏接力效果
4. **配置管理**：测试配置保存和加载功能
5. **边界情况**：测试滚动边界、显示器边界等特殊情况

### 5.2 性能测试

1. **滚动流畅度**：测试连续滚动时的流畅度
2. **资源占用**：测试CPU和内存占用情况
3. **响应时间**：测试滚动事件到窗口更新的延迟
4. **多应用并发**：测试多个应用程序同时使用时的性能

## 6. 总结

通过扩展数据模型、核心模块和算法，竖屏接力分屏软件可以支持多显示器场景和多应用程序，为用户提供更加灵活和强大的分屏体验。扩展后的软件将能够：

1. 在多个显示器间实现无缝的内容连续滚动
2. 为不同应用程序提供个性化的分屏配置
3. 适应不同分辨率、刷新率和方向的显示器
4. 支持动态添加和移除显示器
5. 提供灵活的配置选项，满足不同用户需求

这将显著提高用户在处理长文档、浏览网页、编辑代码等场景下的工作效率，为用户提供全新的屏幕使用体验。