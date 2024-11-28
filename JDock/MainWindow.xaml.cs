using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;


namespace JDock {

    public class ListItem {
        public int Pid { get; set; }
        public string InfoText { get; set; }
        public BitmapSource Icon { get; set; }
        public double ItemMargin { get; set; } = 10;
        public double ItemWidth { get; set; } = 45;
    }
    public class Win_User32 {
        public static readonly uint SPI_SETDESKWALLPAPER = 0x0014;
        public static readonly uint SPIF_UPDATEINIFILE = 0x0001;

        public static readonly uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
        public static readonly uint WINEVENT_OUTOFCONTEXT = 0x0000; // Events are ASYNC
        public static readonly uint WINEVENT_SKIPOWNPROCESS = 0x0002; // Don't call back for events on installer's process

        public static readonly uint EVENT_OBJECT_CREATE = 0x8000;
        public static readonly uint EVENT_OBJECT_DESTROY = 0x8001;

        public static readonly uint OBJID_WINDOW = 0x00000000; // 表示窗口对象本身


        public static readonly uint SW_SHOWMAXIMIZED = 3;

        public delegate void Wineventproc(IntPtr hWinEventHook, uint eventId, IntPtr hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime);


        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            Wineventproc pfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("User32.dll")]
        public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    }

    public class SHOW_WINDOWS_MODER {
        public static readonly int MINIMIZE = 6;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int mCmdShow);
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);


        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowVisible(IntPtr handle);


        [DllImport("user32.dll")]
        static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn,
            IntPtr lParam);

        delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        public ObservableCollection<ListItem> Items { get; set; }

        public ConcurrentDictionary<int, Process> LinkProcess = new ConcurrentDictionary<int, Process>();

        public ConcurrentDictionary<int, ListItem> LinkListItem = new ConcurrentDictionary<int, ListItem>();
        private Process Cur_Process;

        //private void LstOnSelectionChanged(object sender, SelectionChangedEventArgs e) {
        private void LstOnSelectionChanged(object sender, RoutedEventArgs e) {
            ListItem? item = ButAsListItem(sender);
            if (item != null) {
                Trace.WriteLine($"Process {item.InfoText} " +
                       $"is shown on the taskbar.");

                LinkProcess.TryGetValue(item.Pid, out Process process);

                if (process == null)
                    return;

                List<IntPtr> handles = GetIntPtrs(process);

                if (handles.Count > 0) {
                    int show_mode = 1;
                    if (process != Cur_Process) {
                        Cur_Process = process;
                    } else {
                        show_mode = SHOW_WINDOWS_MODER.MINIMIZE;
                    }
                    foreach (IntPtr handle in handles) {
                        ShowWindows(handle, show_mode);
                    }
                    if (show_mode != 1) {
                        Cur_Process = null;
                    }
                } else {
                    //得重新执行 exe
                    if (!process.HasExited) {
                        Process.Start(process.MainModule.FileName);
                    }
                }
            }
        }

        private static List<IntPtr> GetIntPtrs(Process process) {
            List<IntPtr> handles = new List<IntPtr>(5);

            foreach (ProcessThread thread in process.Threads)
                EnumThreadWindows(thread.Id,
                    (wnd, lParam) => {
                        int textLength = GetWindowTextLength(wnd);
                        StringBuilder outText = new StringBuilder(textLength + 1);
                        int a = GetWindowText(wnd, outText, outText.Capacity);
                        if (wnd != IntPtr.Zero && IsWindowVisible(wnd) && textLength > 0) {
                            Trace.WriteLine($"text {outText} " +
                                        $"is shown on the taskbar.");
                            handles.Add(wnd);
                        }
                        return true;
                    }, IntPtr.Zero);
            return handles;
        }

        private ListItem? ButAsListItem(object sender) {
            Button? btn = sender as Button;
            return btn?.DataContext as ListItem;
        }

        private void ShowWindows(IntPtr handle, int show_mode) {
            if (IsIconic(handle) || show_mode != 1) {
                ShowWindow(handle, show_mode);
            } else {
                SetForegroundWindow(handle);
            }
        }

        /*
         * 菜单
         */
        private ContextMenu _contextMenu;
        private void CreateContextMenu() {
            // 定义 ContextMenu
            _contextMenu = new ContextMenu();

            // 添加菜单项
            MenuItem option1 = new MenuItem { Header = "选项 1" };
            option1.Click += (s, e) => MessageBox.Show("你选择了选项 1");

            MenuItem option2 = new MenuItem { Header = "选项 2" };
            option2.Click += (s, e) => MessageBox.Show("你选择了选项 2");

            MenuItem exit = new MenuItem { Header = "不显示" };
            exit.Click += (s, e) => {
                //Application.Current.Shutdown();
                if (Cur_Process != null) {
                    removeLinkProceById(Cur_Process.Id);
                    Cur_Process = null;
                }
            };
            MenuItem w_refresh = new MenuItem { Header = "刷新" };
            w_refresh.Click += (s, e) => {
                remove_all_process();
                ProcessesInfo();
            };

            MenuItem w_cu_size_local = new MenuItem { Header = "自由" };
            w_cu_size_local.Click += (s, e) => {
                move = true;
            };
            MenuItem w_re_size_local = new MenuItem { Header = "复原" };
            w_re_size_local.Click += (s, e) => {
                move = false;
                WindowLocationAndSizeScreen();
            };

            // 添加到 ContextMenu
            _contextMenu.Items.Add(option1);
            _contextMenu.Items.Add(option2);
            _contextMenu.Items.Add(new Separator());
            _contextMenu.Items.Add(exit);
            _contextMenu.Items.Add(w_refresh);
            _contextMenu.Items.Add(new Separator());
            _contextMenu.Items.Add(w_cu_size_local);
            _contextMenu.Items.Add(w_re_size_local);
        }

        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            // 获取鼠标点击的位置
            var mousePosition = e.GetPosition(this);


            // 显示右键菜单
            _contextMenu.PlacementTarget = this; // 菜单相对于整个窗口
            _contextMenu.PlacementRectangle = new Rect(mousePosition, new System.Windows.Size(0, 0)); // 菜单显示在鼠标点击点
            _contextMenu.IsOpen = true;

            ListItem? item = ButAsListItem(sender);
            if (item != null) {
                LinkProcess.TryGetValue(item.Pid, out Process process);

                if (process != null) {
                    Cur_Process = process;
                }
            }
        }

        // 鼠标悬停事件
        private void HoverButton_MouseEnter(object sender, MouseEventArgs e) {

        }

        // 鼠标离开事件
        private void HoverButton_MouseLeave(object sender, MouseEventArgs e) {

        }

        // 鼠标双击
        private void Window_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
         
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            // 创建滑动动画（向下）
            var slideDownAnimation = new DoubleAnimation {
                From = 0,
                To = screenHeight - this.Top, // 滑出屏幕
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase() // 平滑过渡
            };

            // 创建透明度动画（渐隐）
            var fadeOutAnimation = new DoubleAnimation {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(500)
            };

            // 动画完成后显示条形按钮窗口并隐藏当前窗口
            slideDownAnimation.Completed += (s, _) => {
                // 双击主窗口隐藏并显示底部按钮窗口
                HideButton bottomBar = new HideButton(this);
                bottomBar.Show();
                this.Hide();
            };

            // 应用动画
            RenderTransform.BeginAnimation(TranslateTransform.YProperty, slideDownAnimation);
            this.BeginAnimation(OpacityProperty, fadeOutAnimation);
        }

        private ConcurrentDictionary<int, byte> concur_event_procs_set = new ConcurrentDictionary<int, byte>();

        private void EventProcs_destroy(IntPtr hWinEventHook, uint eventId, IntPtr wnd,
            int idObject, int idChild, uint idEventThread, uint dwmsEventTime) {

            int textLength = GetWindowTextLength(wnd);
            if (textLength == 0 || wnd == IntPtr.Zero
                 || Win_User32.EVENT_OBJECT_DESTROY != eventId
                 || idObject != Win_User32.OBJID_WINDOW
                 || !Win_User32.IsWindow(wnd)
                )
                return;

            Win_User32.GetWindowThreadProcessId(wnd, out uint pid);

            if (!concur_event_procs_set.ContainsKey((int)pid)) {
                if (!concur_event_procs_set.TryAdd((int)pid, 0)) {
                    return;
                }
            } else {
                return;
            }

            Trace.WriteLine($"EventProcs_destroy id: {(int)pid} " +
                     $".");

            if (LinkProcess.ContainsKey((int)pid)) {

                LinkListItem.TryGetValue((int)pid, out ListItem item);
                LinkProcess.TryGetValue((int)pid, out Process process);

                Trace.WriteLine($"EventProcs_destroy is run:{IsWindowVisible(process.MainWindowHandle)} " +
                  $".");

                if (item != null && !IsWindowVisible(process.MainWindowHandle)) {
                    Trace.WriteLine($"EventProcs_destroy Process name:{item.InfoText} " +
                        $".");

                    Application.Current.Dispatcher.Invoke((() => {
                        removeLinkProceById((int)pid);
                    }));
                }
            }
            concur_event_procs_set.TryRemove((int)pid, out _);
        }

        private void EventProcs_Create(IntPtr hWinEventHook, uint eventId, IntPtr wnd,
          int idObject, int idChild, uint idEventThread, uint dwmsEventTime) {


            int textLength = GetWindowTextLength(wnd);
            if (textLength == 0 || wnd == IntPtr.Zero
               || !Win_User32.IsWindow(wnd)
                || idObject != Win_User32.OBJID_WINDOW
                 || Win_User32.EVENT_OBJECT_CREATE != eventId
                )
                return;

            Win_User32.GetWindowThreadProcessId(wnd, out uint pid);

            if (!concur_event_procs_set.ContainsKey((int)pid)) {
                if (!concur_event_procs_set.TryAdd((int)pid, 0)) {
                    return;
                }
            } else {
                return;
            }

            if (!LinkProcess.ContainsKey((int)pid)) {
                Trace.WriteLine($"EventProcs_Create -> Process id {(int)pid} is EVENT_OBJECT_CREATE.");
                //判断当前进程
                try {
                    Process process = Process.GetProcessById((int)pid);
                    Trace.WriteLine($"EventProcs_Create {process.MainModule.FileName} " +
                       $"is AddProcessListInfo.");
                    StringBuilder outText = new StringBuilder(textLength + 1);
                    GetWindowText(wnd, outText, outText.Capacity);
                    Trace.WriteLine($"EventProcs_Create title: {outText} " +
                     $".");
                    if (filter_process_create.ContainsKey(process.ProcessName)) {
                        return;
                    }
                    AddProcessListInfo(process);
                } catch {
                    return;
                }
            }
            concur_event_procs_set.TryRemove((int)pid, out _);
        }


        private IntPtr g_eventhook_destroy, g_eventhook_create;
        private Win_User32.Wineventproc Wineventproc_destroy, Wineventproc_Create;
        private void SetWinEventHook() {
            Wineventproc_destroy = new Win_User32.Wineventproc(EventProcs_destroy);
            Wineventproc_Create = new Win_User32.Wineventproc(EventProcs_Create);

            g_eventhook_create = Win_User32.SetWinEventHook(
                Win_User32.EVENT_OBJECT_CREATE,
                Win_User32.EVENT_OBJECT_CREATE,
                 IntPtr.Zero,
                 Wineventproc_Create, 0, 0,
                 Win_User32.WINEVENT_OUTOFCONTEXT | Win_User32.WINEVENT_SKIPOWNPROCESS);

            g_eventhook_destroy = Win_User32.SetWinEventHook(
                Win_User32.EVENT_OBJECT_DESTROY,
                Win_User32.EVENT_OBJECT_DESTROY,
                 IntPtr.Zero,
                 Wineventproc_destroy, 0, 0,
                 Win_User32.WINEVENT_OUTOFCONTEXT | Win_User32.WINEVENT_SKIPOWNPROCESS);
        }

        void WaitForProcess() {
            //ManagementEventWatcher startWatch = new ManagementEventWatcher(
            //  new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
            //startWatch.EventArrived
            //                    += new EventArrivedEventHandler(startWatch_EventArrived);
            //startWatch.Start();

            ManagementEventWatcher stoptWatch = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace "));
            stoptWatch.EventArrived
                                += new EventArrivedEventHandler(stopWatch_EventArrived);
            stoptWatch.Start();
        }

        private void stopWatch_EventArrived(object sender, EventArrivedEventArgs e) {

            string processName = e.NewEvent["ProcessName"]?.ToString();
            uint pid = (uint)e.NewEvent["ProcessID"];
            //string parentProcessId = e.NewEvent["ParentProcessID"]?.ToString();

            //Trace.WriteLine("Process stop:");
            //Trace.WriteLine($"    Name: {processName}");
            //Trace.WriteLine($"    PID: {pid}");

            if (LinkProcess.ContainsKey((int)pid)) {
                //Trace.WriteLine($"EventProcs_destroy {pid} " +
                //        $"is EVENT_OBJECT_DESTROY.");

                LinkListItem.TryGetValue((int)pid, out ListItem item);

                if (item != null) {
                    //Trace.WriteLine($"Process stop: {item.InfoText} " +
                    //    $"is EVENT_OBJECT_DESTROY.");

                    Application.Current.Dispatcher.Invoke((() => {
                        removeLinkProceById((int)pid);
                    }));

                }
            }
        }


        //public MainViewModelDC modelDC { get; set; }
        public DockItem dockItem { get; set; }

        public MainWindow() {

            DataContext = this;

            Items = new ObservableCollection<ListItem>();

            dockItem = new DockItem();

            ProcessesInfo();

            InitializeComponent();

            CreateContextMenu();

            WindowLocationAndSizeScreen();

            // 监听数据源的更新
            Items.CollectionChanged += OnDockItemsChanged;

            SetWinEventHook();
            //WaitForProcess();
        }

        // 当 DockItems 更新时重新计算宽度
        private void OnDockItemsChanged(object sender, NotifyCollectionChangedEventArgs e) {
            WindowLocationAndSizeScreen();
        }

        void DataWindow_Closing(object sender, CancelEventArgs e) {
            Trace.WriteLine("DataWindow_Closing =====> .");

            Win_User32.UnhookWinEvent(g_eventhook_destroy);
            Win_User32.UnhookWinEvent(g_eventhook_create);

            LinkProcess.Clear();
        }

        private void MainWindow_OnStateChanged(object sender, EventArgs e) {
            var window = (Window)sender;

            if (window.WindowState == WindowState.Minimized)
                window.WindowState = WindowState.Normal;
        }

        private void ProcessesInfo() {
            Process[] processes = Process.GetProcesses();

            foreach (Process process in processes) {

                AddProcessListInfo(process);

            }
        }

        private static readonly ConcurrentDictionary<string, byte> filter_process_name = new ConcurrentDictionary<string, byte>(
            new Dictionary<string, byte>() {
                { "TextInputHost",0},
                { "JDock",0},
            }
            );
        private static readonly ConcurrentDictionary<string, byte> filter_process_create = new ConcurrentDictionary<string, byte>(
            new Dictionary<string, byte>() {
                { "explorer",0},
            }
            );

        private void AddProcessListInfo(Process process) {

            if (filter_process_name.ContainsKey(process.ProcessName))
                return;

            try {
                if (process == null || process.HasExited)
                    return;
            } catch {
                return;
            }

            IntPtr mainWindowHandle = process.MainWindowHandle;

            if (mainWindowHandle != IntPtr.Zero &&
                !LinkProcess.ContainsKey(process.Id)) {


                Trace.WriteLine($"Process {process.MainModule?.FileName} " +
                    $"is shown on the taskbar.");

                Icon? icon = System.Drawing.Icon
                    .ExtractAssociatedIcon(process.MainModule?.FileName);

                if (icon != null) {
                    LinkProcess.TryAdd(process.Id, process);
                    ListItem listItem = new ListItem();

                    listItem.Pid = process.Id;
                    listItem.InfoText = process.ProcessName;

                    listItem.Icon = Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

                    LinkListItem.TryAdd(process.Id, listItem);
                    Items.Add(listItem);
                }
            }
        }

        private void removeLinkProceById(int pid) {
            LinkListItem.TryGetValue(pid, out ListItem listItem);
            if (listItem != null) {
                Items.Remove(listItem);
                LinkListItem.TryRemove(pid, out _);
                LinkProcess.TryRemove(pid, out _);
            }
        }

        private void remove_all_process() {
            Items.Clear();
            LinkListItem.Clear();
            LinkProcess.Clear();
            Cur_Process = null;
        }


        /*
         *  窗口大小系数
         *      百分比
         */
        public float heightCoefficient = 0.07f;
        public float widthCoefficient = 0.25f;
        private bool move = false;


        /*
         *  设置窗口位置和大小
         */
        private void WindowLocationAndSizeScreen() {
            //this.Width = (SystemParameters.PrimaryScreenWidth * widthCoefficient);

            if (!move) {
                windowsSizeScreen();

                windowsLocation();
            }
        }


        private void windowsSizeScreen() {
            //大小，百分比
            this.Height = (SystemParameters.PrimaryScreenHeight * heightCoefficient);
            this.Width = Items.Count * (dockItem.ItemWidth + (dockItem.ItemMargin * 2)); // 减去最后一个多余的间距
        }

        private void windowsLocation() {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;

            // 水平居中
            this.Left = (screenWidth / 2) - (windowWidth / 2);
            // 底部
            this.Top = (screenHeight - windowHeight) - 5;
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            if (!move) {
                return;
            }
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) {
                this.DragMove();
            }
        }
    }
}
