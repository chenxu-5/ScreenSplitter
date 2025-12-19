using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenSplitter
{
    /// <summary>
    /// 屏幕分屏软件核心功能示例
    /// 实现Windows 11下的屏幕分屏效果
    /// </summary>
    public class ScreenSplitterDemo
    {
        // Windows API常量定义
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOZORDER = 0x0004;
        private const int SWP_SHOWWINDOW = 0x0040;
        private const int WM_HOTKEY = 0x0312;
        private const int MOD_CONTROL = 0x0002;
        private const int MOD_ALT = 0x0001;
        private const int MOD_SHIFT = 0x0004;

        // 分屏快捷键定义
        private const int HOTKEY_LEFT = 1;
        private const int HOTKEY_RIGHT = 2;
        private const int HOTKEY_TOP = 3;
        private const int HOTKEY_BOTTOM = 4;

        // 窗口句柄
        private IntPtr _currentWindowHandle;
        private NotifyIcon _trayIcon;

        /// <summary>
        /// 初始化分屏软件
        /// </summary>
        public void Initialize()
        {
            // 注册全局快捷键
            RegisterHotkeys();
            
            // 创建系统托盘图标
            CreateTrayIcon();
            
            // 开始监听窗口事件
            StartWindowMonitoring();
        }

        /// <summary>
        /// 注册全局快捷键
        /// </summary>
        private void RegisterHotkeys()
        {
            // Ctrl + Alt + Left - 左分屏
            RegisterHotKey(IntPtr.Zero, HOTKEY_LEFT, MOD_CONTROL | MOD_ALT, Keys.Left.GetHashCode());
            
            // Ctrl + Alt + Right - 右分屏
            RegisterHotKey(IntPtr.Zero, HOTKEY_RIGHT, MOD_CONTROL | MOD_ALT, Keys.Right.GetHashCode());
            
            // Ctrl + Alt + Up - 上分屏
            RegisterHotKey(IntPtr.Zero, HOTKEY_TOP, MOD_CONTROL | MOD_ALT, Keys.Up.GetHashCode());
            
            // Ctrl + Alt + Down - 下分屏
            RegisterHotKey(IntPtr.Zero, HOTKEY_BOTTOM, MOD_CONTROL | MOD_ALT, Keys.Down.GetHashCode());
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
                Text = "屏幕分屏工具"
            };

            // 创建上下文菜单
            ContextMenu contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add("配置", OnConfigClick);
            contextMenu.MenuItems.Add("退出", OnExitClick);
            _trayIcon.ContextMenu = contextMenu;
        }

        /// <summary>
        /// 开始监听窗口事件
        /// </summary>
        private void StartWindowMonitoring()
        {
            // 这里可以实现Windows钩子，监听窗口创建、移动、大小变化等事件
            // 示例中简化处理，只通过快捷键触发
        }

        /// <summary>
        /// 处理快捷键消息
        /// </summary>
        /// <param name="hotkeyId">快捷键ID</param>
        public void HandleHotkey(int hotkeyId)
        {
            // 获取当前活动窗口
            _currentWindowHandle = GetForegroundWindow();
            
            if (_currentWindowHandle == IntPtr.Zero) return;

            // 获取屏幕信息
            Rectangle screenBounds = Screen.FromHandle(_currentWindowHandle).Bounds;

            switch (hotkeyId)
            {
                case HOTKEY_LEFT:
                    // 左半屏
                    SetWindowPosition(_currentWindowHandle, screenBounds.Left, screenBounds.Top, 
                                      screenBounds.Width / 2, screenBounds.Height);
                    break;
                case HOTKEY_RIGHT:
                    // 右半屏
                    SetWindowPosition(_currentWindowHandle, screenBounds.Left + screenBounds.Width / 2, screenBounds.Top, 
                                      screenBounds.Width / 2, screenBounds.Height);
                    break;
                case HOTKEY_TOP:
                    // 上半屏
                    SetWindowPosition(_currentWindowHandle, screenBounds.Left, screenBounds.Top, 
                                      screenBounds.Width, screenBounds.Height / 2);
                    break;
                case HOTKEY_BOTTOM:
                    // 下半屏
                    SetWindowPosition(_currentWindowHandle, screenBounds.Left, screenBounds.Top + screenBounds.Height / 2, 
                                      screenBounds.Width, screenBounds.Height / 2);
                    break;
            }
        }

        /// <summary>
        /// 设置窗口位置和大小
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        private void SetWindowPosition(IntPtr hWnd, int x, int y, int width, int height)
        {
            // 调整窗口大小和位置
            MoveWindow(hWnd, x, y, width, height, true);
            
            // 确保窗口处于激活状态
            SetForegroundWindow(hWnd);
        }

        /// <summary>
        /// 配置菜单点击事件
        /// </summary>
        private void OnConfigClick(object sender, EventArgs e)
        {
            // 显示配置窗口
            MessageBox.Show("配置窗口将在此处显示", "屏幕分屏工具");
        }

        /// <summary>
        /// 退出菜单点击事件
        /// </summary>
        private void OnExitClick(object sender, EventArgs e)
        {
            // 清理资源
            UnregisterHotkeys();
            _trayIcon.Dispose();
            Application.Exit();
        }

        /// <summary>
        /// 注销快捷键
        /// </summary>
        private void UnregisterHotkeys()
        {
            UnregisterHotKey(IntPtr.Zero, HOTKEY_LEFT);
            UnregisterHotKey(IntPtr.Zero, HOTKEY_RIGHT);
            UnregisterHotKey(IntPtr.Zero, HOTKEY_TOP);
            UnregisterHotKey(IntPtr.Zero, HOTKEY_BOTTOM);
        }

        // Windows API函数声明
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }

    /// <summary>
    /// 分屏管理类
    /// 负责管理分屏区域和窗口吸附逻辑
    /// </summary>
    public class SplitScreenManager
    {
        /// <summary>
        /// 获取屏幕的分屏区域
        /// </summary>
        /// <param name="screen">屏幕对象</param>
        /// <param name="splitType">分屏类型</param>
        /// <returns>分屏区域数组</returns>
        public static Rectangle[] GetSplitRegions(Screen screen, SplitType splitType)
        {
            Rectangle screenBounds = screen.Bounds;
            
            switch (splitType)
            {
                case SplitType.LeftRight: // 左右分屏
                    return new Rectangle[]
                    {
                        new Rectangle(screenBounds.Left, screenBounds.Top, screenBounds.Width / 2, screenBounds.Height),
                        new Rectangle(screenBounds.Left + screenBounds.Width / 2, screenBounds.Top, screenBounds.Width / 2, screenBounds.Height)
                    };
                case SplitType.TopBottom: // 上下分屏
                    return new Rectangle[]
                    {
                        new Rectangle(screenBounds.Left, screenBounds.Top, screenBounds.Width, screenBounds.Height / 2),
                        new Rectangle(screenBounds.Left, screenBounds.Top + screenBounds.Height / 2, screenBounds.Width, screenBounds.Height / 2)
                    };
                default:
                    return new Rectangle[] { screenBounds };
            }
        }

        /// <summary>
        /// 检查窗口是否应该吸附到分屏区域
        /// </summary>
        /// <param name="windowRect">窗口矩形</param>
        /// <param name="screen">屏幕对象</param>
        /// <returns>应该吸附的分屏区域，如不需要吸附则返回null</returns>
        public static Rectangle? CheckSnapToRegion(Rectangle windowRect, Screen screen)
        {
            Rectangle[] regions = GetSplitRegions(screen, SplitType.LeftRight);
            int snapThreshold = 50; // 吸附阈值（像素）

            // 检查窗口是否接近某个分屏区域
            foreach (Rectangle region in regions)
            {
                // 计算窗口与分屏区域的差异
                int diffX = Math.Abs(windowRect.X - region.X);
                int diffY = Math.Abs(windowRect.Y - region.Y);
                int diffWidth = Math.Abs(windowRect.Width - region.Width);
                int diffHeight = Math.Abs(windowRect.Height - region.Height);

                // 如果差异在阈值内，则吸附到该区域
                if (diffX <= snapThreshold && diffY <= snapThreshold && 
                    diffWidth <= snapThreshold && diffHeight <= snapThreshold)
                {
                    return region;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// 分屏类型枚举
    /// </summary>
    public enum SplitType
    {
        LeftRight,  // 左右分屏
        TopBottom   // 上下分屏
    }
}