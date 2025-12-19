# 竖屏接力分屏软件 (Vertical Relay Screen Splitter)

## 项目简介

竖屏接力分屏软件是一款专为Windows 11设计的屏幕管理工具，能够将单个或多个显示器划分为独立区域，并实现内容连续滚动的竖屏接力效果。当左侧区域滚动到底部时，右侧区域自动显示后续内容，形成类似超长竖屏的阅读体验。

## 核心功能

### 1. 基础分屏功能
- ✅ 左右分屏显示
- ✅ 上下分屏显示
- ✅ 支持自定义分屏比例
- ✅ 窗口自动吸附

### 2. 竖屏接力效果
- ✅ 左侧滚动到底部时，右侧显示后续内容
- ✅ 右侧滚动到顶部时，左侧显示前面内容
- ✅ 无缝过渡，50像素重叠区域
- ✅ 保持滚动速度一致

### 3. 多显示器支持
- ✅ 动态检测所有连接的显示器
- ✅ 每个显示器独立配置
- ✅ 跨显示器内容连续滚动
- ✅ 支持不同分辨率和方向

### 4. 多应用支持
- ✅ 自动识别应用程序类型
- ✅ 为不同应用提供个性化配置
- ✅ 内置常见应用默认配置
- ✅ 支持应用白名单/黑名单

### 5. 快捷键支持
- `Ctrl+Alt+R`：启用竖屏接力模式
- `Ctrl+Alt+F`：禁用竖屏接力模式
- `Ctrl+Alt+D`：刷新显示器配置

## 系统要求

- **操作系统**：Windows 11
- **.NET Framework**：.NET 6.0 或更高版本
- **开发环境**：Visual Studio 2022 或更高版本
- **硬件**：支持多显示器（可选）

## 安装与运行

### 方法1：从源代码编译

1. 克隆或下载项目代码
2. 使用Visual Studio 2022打开解决方案
3. 选择Release配置，编译项目
4. 运行生成的可执行文件

### 方法2：直接运行

（待发布预编译版本）

## 使用指南

### 1. 基本使用

1. **启动程序**：运行编译后的可执行文件
2. **启用分屏**：按下`Ctrl+Alt+R`启用竖屏接力模式
3. **使用分屏**：在支持的应用中滚动，体验竖屏接力效果
4. **调整配置**：通过系统托盘菜单调整设置

### 2. 支持的应用程序

软件内置了对以下应用的默认支持：
- Google Chrome
- Microsoft Edge
- Visual Studio Code
- Adobe Acrobat Reader
- Notepad

### 3. 多显示器使用

1. 确保连接了多个显示器
2. 按下`Ctrl+Alt+D`刷新显示器配置
3. 软件会自动检测所有显示器和相邻关系
4. 在任意显示器上启用分屏，体验跨显示器接力效果

## 配置说明

### 全局配置

- **默认重叠区域**：50像素
- **默认分屏类型**：左右分屏
- **默认分屏比例**：50:50
- **滚动触发阈值**：50像素

### 显示器配置

每个显示器可以独立配置：
- 分屏类型
- 分屏比例
- 重叠区域高度
- 是否启用接力

### 应用程序配置

每个应用程序可以独立配置：
- 是否启用接力
- 首选分屏类型
- 首选分屏比例
- 应用程序类型

## 项目结构

```
ScreenSplitter/
├── ScreenSplitterRelayDemo.cs       # 核心实现（单显示器版本）
├── ScreenSplitterRelayDemo_MultiDisplayMultiApp.cs  # 多显示器多应用扩展版本
├── ScreenSplitter_VerticalRelay.md  # 竖屏接力设计方案
├── ScreenSplitter_MultiDisplayMultiApp.md  # 多显示器多应用扩展方案
└── README.md                        # 项目使用文档
```

## 开发说明

### 技术栈

- **开发语言**：C# 10.0+
- **框架**：.NET 6.0 WPF
- **API**：Windows User32.dll
- **开发工具**：Visual Studio 2022

### 核心模块

1. **DisplayManager**：显示器管理和配置
2. **ApplicationManager**：应用程序配置管理
3. **CrossDisplayRelayService**：跨显示器接力服务
4. **ScrollManager**：滚动事件管理
5. **ContentRelayService**：内容接力逻辑
6. **HookService**：Windows钩子服务

### 扩展开发

项目采用模块化设计，便于扩展新功能：

1. **添加新的分屏类型**：扩展`SplitType`枚举
2. **支持新的应用程序**：在`ApplicationManager`中添加默认配置
3. **实现新的接力算法**：扩展`CrossDisplayRelayService`

## 快捷键列表

| 快捷键组合 | 功能描述 |
|------------|----------|
| Ctrl+Alt+R | 启用竖屏接力模式 |
| Ctrl+Alt+F | 禁用竖屏接力模式 |
| Ctrl+Alt+D | 刷新显示器配置 |

## 常见问题

### Q: 软件支持哪些应用程序？
A: 软件内置支持Chrome、Edge、VS Code、Acrobat等常见应用，同时支持自定义添加其他应用。

### Q: 如何调整分屏比例？
A: 当前版本通过配置文件调整分屏比例，后续版本将添加可视化配置界面。

### Q: 支持多少个显示器？
A: 理论上支持无限多个显示器，实际取决于系统资源和显示器配置。

### Q: 软件会影响系统性能吗？
A: 软件采用高效的事件过滤和异步处理机制，对系统性能影响很小。

## 更新日志

### v1.0.0 (2025-12-19)
- 初始版本发布
- 支持单显示器竖屏接力
- 支持基础分屏功能
- 支持快捷键操作

### v1.1.0 (2025-12-19)
- 新增多显示器支持
- 新增多应用支持
- 新增跨显示器接力功能
- 新增动态显示器检测

## 许可证

本项目采用 MIT 许可证，详见 LICENSE 文件。

## 贡献指南

欢迎提交 Issue 和 Pull Request！

1. Fork 本项目
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开 Pull Request

## 联系方式

如有问题或建议，欢迎通过以下方式联系：

- GitHub Issues：[项目Issues页面](https://github.com/yourusername/ScreenSplitter/issues)
- 邮件：your.email@example.com

## 致谢

感谢所有为项目做出贡献的开发者！

## 版权信息

© 2025 Vertical Relay Screen Splitter. All rights reserved.
