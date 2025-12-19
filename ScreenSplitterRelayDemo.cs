using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;

namespace ScreenSplitter
{
    /// <summary>
    /// 竖屏接力分屏软件核心实现
    /// 实现屏幕左右分屏的内容连续滚动效果
    /// </summary>
    public class ScreenSplitterRelayDemo : IDisposable
    {
        // Windows API常量定义
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOZORDER = 0x0004;
        private const int SWP_SHOWWINDOW = 0x0040;
        private const int WM_HOTKEY = 0x0312;
        private const int WM_VSCROLL = 0x115;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WH_CALLWNDPROC = 4;
        private const int WH_GETMESSAGE = 3;
        private const int MOD_CONTROL = 0x0002;
        private const int MOD_ALT = 0x0001;

        // 分屏快捷键定义
        private const int HOTKEY_ENABLE_RELAY = 1;
        private const int HOTKEY_DISABLE_RELAY = 2;

        // 窗口句柄
        private IntPtr _leftWindowHandle;
        private IntPtr _rightWindowHandle;
        private NotifyIcon _trayIcon;
        private bool _isRelayEnabled = false;

        // 钩子相关
        private IntPtr _hookId = IntPtr.Zero;
        private HookProc _hookProc;

        // 滚动状态
        private int _leftScrollPosition = 0;
        private int _rightScrollPosition = 0;
        private int _contentHeight = 0;
        private int _overlapHeight = 50; // 重叠区域高度（像素）

        /// <summary>
        /// 滚动方向枚举
        /// </summary>
        private enum ScrollDirection
        {
            Up,
            Down
        }

        /// <summary>
        /// 初始化竖屏接力分屏软件
        /// </summary>
        public void Initialize()
        {
            // 注册全局快捷键
            RegisterHotkeys();
            
            // 创建系统托盘图标
            CreateTrayIcon();
            
            // 初始化钩子服务
            InitializeHook();
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
                Text = "竖屏接力分屏工具"
            };

            // 创建上下文菜单
            ContextMenu contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add("启用竖屏接力", OnEnableRelayClick);
            contextMenu.MenuItems.Add("禁用竖屏接力", OnDisableRelayClick);
            contextMenu.MenuItems.Add("退出", OnExitClick);
            _trayIcon.ContextMenu = contextMenu;
        }

        /// <summary>
        /// 初始化Windows钩子
        /// </summary>
        private void InitializeHook()
        {
            _hookProc = new HookProc(HookCallback);
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
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
            // 判断是左侧还是右侧窗口
            bool isLeftWindow = hWnd == _leftWindowHandle;
            bool isRightWindow = hWnd == _rightWindowHandle;
            
            if (!isLeftWindow && !isRightWindow) return;

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

            // 执行内容接力逻辑
            PerformContentRelay(hWnd, direction, scrollOffset);
        }

        /// <summary>
        /// 执行内容接力逻辑
        /// </summary>
        /// <param name="sourceWnd">源窗口句柄</param>
        /// <param name="direction">滚动方向</param>
        /// <param name="offset">滚动偏移量</param>
        private void PerformContentRelay(IntPtr sourceWnd, ScrollDirection direction, int offset)
        {
            bool isLeftWindow = sourceWnd == _leftWindowHandle;
            
            if (isLeftWindow)
            {
                // 左侧窗口滚动
                if (direction == ScrollDirection.Down)
                {
                    // 向下滚动
                    _leftScrollPosition += offset;
                    
                    // 检查是否到达左侧窗口底部
                    if (IsAtBottom(_leftWindowHandle))
                    {
                        // 右侧窗口开始显示后续内容
                        _rightScrollPosition = _leftScrollPosition - GetWindowClientHeight(_leftWindowHandle) + _overlapHeight;
                        SyncScroll(_rightWindowHandle, _rightScrollPosition);
                    }
                }
                else
                {
                    // 向上滚动
                    _leftScrollPosition -= offset;
                    if (_leftScrollPosition < 0) _leftScrollPosition = 0;
                }
            }
            else
            {
                // 右侧窗口滚动
                if (direction == ScrollDirection.Up)
                {
                    // 向上滚动
                    _rightScrollPosition -= offset;
                    
                    // 检查是否到达右侧窗口顶部
                    if (IsAtTop(_rightWindowHandle))
                    {
                        // 左侧窗口显示前面的内容
                        _leftScrollPosition = _rightScrollPosition - GetWindowClientHeight(_rightWindowHandle) + _overlapHeight;
                        if (_leftScrollPosition < 0) _leftScrollPosition = 0;
                        SyncScroll(_leftWindowHandle, _leftScrollPosition);
                    }
                }
                else
                {
                    // 向下滚动
                    _rightScrollPosition += offset;
                }
            }
        }

        /// <summary>
        /// 同步滚动指定窗口到目标位置
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="scrollPosition">滚动位置</param>
        private void SyncScroll(IntPtr hWnd, int scrollPosition)
        {
            // 使用Windows API设置滚动位置
            SendMessage(hWnd, WM_VSCROLL, (IntPtr)(SB_THUMBPOSITION | (scrollPosition << 16)), IntPtr.Zero);
        }

        /// <summary>
        /// 检查窗口是否滚动到底部
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>是否到达底部</returns>
        private bool IsAtBottom(IntPtr hWnd)
        {
            SCROLLINFO scrollInfo = new SCROLLINFO();
            scrollInfo.cbSize = (uint)Marshal.SizeOf(scrollInfo);
            scrollInfo.fMask = SIF_ALL;
            
            if (GetScrollInfo(hWnd, SB_VERT, ref scrollInfo))
            {
                // 计算是否接近底部（阈值：50像素）
                int bottomThreshold = 50;
                int maxScroll = scrollInfo.nMax - scrollInfo.nPage;
                return scrollInfo.nPos >= maxScroll - bottomThreshold;
            }
            return false;
        }

        /// <summary>
        /// 检查窗口是否滚动到顶部
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>是否到达顶部</returns>
        private bool IsAtTop(IntPtr hWnd)
        {
            SCROLLINFO scrollInfo = new SCROLLINFO();
            scrollInfo.cbSize = (uint)Marshal.SizeOf(scrollInfo);
            scrollInfo.fMask = SIF_POS;
            
            if (GetScrollInfo(hWnd, SB_VERT, ref scrollInfo))
            {
                // 计算是否接近顶部（阈值：50像素）
                int topThreshold = 50;
                return scrollInfo.nPos <= topThreshold;
            }
            return false;
        }

        /// <summary>
        /// 获取窗口客户端高度
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>客户端高度</returns>
        private int GetWindowClientHeight(IntPtr hWnd)
        {
            RECT clientRect = new RECT();
            if (GetClientRect(hWnd, ref clientRect))
            {
                return clientRect.bottom - clientRect.top;
            }
            return 0;
        }

        /// <summary>
        /// 启用竖屏接力模式
        /// </summary>
        public void EnableRelayMode()
        {
            // 获取当前活动窗口
            IntPtr activeWnd = GetForegroundWindow();
            if (activeWnd == IntPtr.Zero) return;

            // 获取屏幕信息
            Rectangle screenBounds = Screen.FromHandle(activeWnd).Bounds;
            
            // 创建左右两个分屏窗口（实际应用中应该是复制或分割现有窗口）
            // 这里简化处理，直接将当前窗口作为左侧窗口，创建一个新窗口作为右侧窗口
            _leftWindowHandle = activeWnd;
            
            // 设置左侧窗口为屏幕左半部分
            SetWindowPosition(_leftWindowHandle, screenBounds.Left, screenBounds.Top, 
                              screenBounds.Width / 2, screenBounds.Height);
            
            // 注意：实际应用中需要创建或找到对应的右侧窗口
            // 这里仅作为示例，右侧窗口句柄需要根据实际情况获取
            _rightWindowHandle = IntPtr.Zero; // 临时设置为无效值
            
            _isRelayEnabled = true;
            _trayIcon.Text = "竖屏接力分屏工具 - 已启用";
            MessageBox.Show("竖屏接力模式已启用\n使用Ctrl+Alt+R启用，Ctrl+Alt+F禁用", "竖屏接力分屏工具");
        }

        /// <summary>
        /// 禁用竖屏接力模式
        /// </summary>
        public void DisableRelayMode()
        {
            _isRelayEnabled = false;
            _trayIcon.Text = "竖屏接力分屏工具 - 已禁用";
            MessageBox.Show("竖屏接力模式已禁用", "竖屏接力分屏工具");
        }

        /// <summary>
        /// 设置窗口位置和大小
        /// </summary>
        private void SetWindowPosition(IntPtr hWnd, int x, int y, int width, int height)
        {
            MoveWindow(hWnd, x, y, width, height, true);
            SetForegroundWindow(hWnd);
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

        [StructLayout(LayoutKind.Sequential)]
        private struct SCROLLINFO
        {
            public uint cbSize;
            public uint fMask;
            public int nMin;
            public int nMax;
            public uint nPage;
            public int nPos;
            public int nTrackPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        // Windows API常量补充
        private const int SB_VERT = 1;
        private const int SB_LINEDOWN = 1;
        private const int SB_LINEUP = 0;
        private const int SB_THUMBPOSITION = 4;
        private const int SIF_POS = 0x0004;
        private const int SIF_RANGE = 0x0001;
        private const int SIF_PAGE = 0x0002;
        private const int SIF_ALL = SIF_RANGE | SIF_PAGE | SIF_POS;

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
        private static extern bool GetScrollInfo(IntPtr hWnd, int fnBar, ref SCROLLINFO lpsi);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, ref RECT lpRect);

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
    }

    /// <summary>
    /// 内容接力服务
    /// 实现左右屏幕内容的连续滚动逻辑
    /// </summary>
    public class ContentRelayService
    {
        private int _overlapHeight = 50; // 重叠区域高度
        private int _scrollThreshold = 10; // 滚动触发阈值

        /// <summary>
        /// 计算右侧窗口的滚动位置
        /// </summary>
        /// <param name="leftScrollPos">左侧窗口滚动位置</param>
        /// <param name="leftWindowHeight">左侧窗口高度</param>
        /// <returns>右侧窗口应该显示的滚动位置</returns>
        public int CalculateRightScrollPosition(int leftScrollPos, int leftWindowHeight)
        {
            // 右侧窗口起始位置 = 左侧窗口滚动位置 - 左侧窗口高度 + 重叠区域高度
            int rightScrollPos = leftScrollPos - leftWindowHeight + _overlapHeight;
            
            // 确保滚动位置不小于0
            return Math.Max(0, rightScrollPos);
        }

        /// <summary>
        /// 计算左侧窗口的滚动位置
        /// </summary>
        /// <param name="rightScrollPos">右侧窗口滚动位置</param>
        /// <param name="rightWindowHeight">右侧窗口高度</param>
        /// <returns>左侧窗口应该显示的滚动位置</returns>
        public int CalculateLeftScrollPosition(int rightScrollPos, int rightWindowHeight)
        {
            // 左侧窗口起始位置 = 右侧窗口滚动位置 - 右侧窗口高度 + 重叠区域高度
            int leftScrollPos = rightScrollPos - rightWindowHeight + _overlapHeight;
            
            // 确保滚动位置不小于0
            return Math.Max(0, leftScrollPos);
        }

        /// <summary>
        /// 检查是否需要触发内容接力
        /// </summary>
        /// <param name="scrollPosition">当前窗口滚动位置</param>
        /// <param name="windowHeight">窗口高度</param>
        /// <param name="contentHeight">内容总高度</param>
        /// <param name="direction">滚动方向</param>
        /// <returns>是否需要触发接力</returns>
        public bool ShouldTriggerRelay(int scrollPosition, int windowHeight, int contentHeight, ScrollDirection direction)
        {
            if (direction == ScrollDirection.Down)
            {
                // 向下滚动时，检查是否接近内容底部
                int bottomPosition = contentHeight - windowHeight;
                return scrollPosition >= bottomPosition - _scrollThreshold;
            }
            else
            {
                // 向上滚动时，检查是否接近内容顶部
                return scrollPosition <= _scrollThreshold;
            }
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
}