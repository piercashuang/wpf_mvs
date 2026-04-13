using MVS.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MVS.Infrastructure;
using MVS.Contract.Camera;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MVS_WPF
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        // 保持一个相机的全局引用，防止被垃圾回收
        private ICamera _testCamera = null;

        public MainWindow()
        {
            InitializeComponent();

            LoadCameraPlugins();

            // 扫描完毕后，我们立刻执行一次取图测试
            MVS.Infrastructure.CameraManager.Instance.ScanAllCameras();
            TestGrabFirstCamera();
        }

        private void LoadCameraPlugins()
        {
            try
            {
                string pluginPath = AppDomain.CurrentDomain.BaseDirectory;
                MVS.Infrastructure.CameraManager.Instance.InitializePlugins(pluginPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载相机插件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 【新增测试方法】：连接第一台扫到的相机并取图
        /// </summary>
        private void TestGrabFirstCamera()
        {
            // 1. 获取刚刚扫到的所有相机情报
            var discoveredInfos = CameraManager.Instance.GetAllDiscoveredCameraInfos().ToList();

            if (discoveredInfos.Count == 0)
            {
                MessageBox.Show("没有扫描到任何相机，请检查连接！");
                return;
            }

            // 2. 拿出第一台相机的序列号
            string firstSN = discoveredInfos[0].SerialNumber;
            System.Diagnostics.Debug.WriteLine($"[测试] 准备连接相机: {firstSN}");

            // 3. 让管理器造出这台相机
            _testCamera = CameraManager.Instance.CreateAndConnectCamera(firstSN);

            if (_testCamera != null)
            {
                // 4. 打开相机
                if (_testCamera.Open())
                {
                    // 5. 订阅图像抓取事件！
                    _testCamera.ImageGrabbed += OnCameraImageGrabbed;

                    // 6. 开始取流
                    bool startOk = _testCamera.StartGrabbing();
                    System.Diagnostics.Debug.WriteLine($"[测试] 取流启动状态: {startOk}");
                }
                else
                {
                    MessageBox.Show("相机打开失败！");
                }
            }
        }

        /// <summary>
        /// 【事件响应】：每当相机底层取到一张图，就会触发这里
        /// </summary>
        private void OnCameraImageGrabbed(object sender, Bitmap bmp)
        {
            // 注意：这个事件是在后台取图线程里触发的！
            // WPF 规定：非 UI 线程绝对不能直接修改 UI 控件，必须通过 Dispatcher 封送回主线程
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // 1. 将 System.Drawing.Bitmap 转换为 WPF 认识的 BitmapImage
                    BitmapImage wpfImage = BitmapToBitmapImage(bmp);

                    // 2. 显示到界面上 (假设你的 xaml 里有一个名叫 CameraDisplay 的 Image 控件)
                    if (CameraDisplay != null)
                    {
                        CameraDisplay.Source = wpfImage;
                    }

                    // 可以在输出窗口看取图帧率感受一下
                    System.Diagnostics.Debug.WriteLine($"成功刷新一帧！尺寸: {bmp.Width} x {bmp.Height}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UI 刷新图像报错: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 【核心转换器】：将 GDI+ 的 Bitmap 存入内存流，再被 WPF 读取
        /// </summary>
        private BitmapImage BitmapToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                // 工业相机图通常是 Bmp 格式，直接原样存入内存
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // 必须加上这个，否则内存流释放后图片会消失
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // 冻结对象，提升 WPF 渲染性能，跨线程安全

                return bitmapImage;
            }
        }

 
        private void TopBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 如果是鼠标左键按下，就允许拖动整个窗口
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove(); // 这一句是 WPF 提供的魔法，直接实现窗口拖拽
            }
        }

        // 最小化
        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // 最大化/还原
        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }

        // 关闭
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // ================= 添加状态改变事件 =================
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                // 如果当前是最大化，图标变成双框（还原），并修改提示文字
                BtnMaximize.Content = "❐";
                BtnMaximize.ToolTip = "向下还原";
            }
            else
            {
                // 如果当前是普通状态，图标变成单框（最大化）
                BtnMaximize.Content = "☐";
                BtnMaximize.ToolTip = "最大化";
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // 1. 取消事件订阅，防止在关闭过程中 UI 还在尝试刷新
            if (_testCamera != null)
            {
                _testCamera.ImageGrabbed -= OnCameraImageGrabbed;
            }

            // 2. 调用 Manager 释放所有相机资源
            CameraManager.Instance.CloseAllCameras();

            // 3. 强制结束当前进程（可选，确保所有后台线程彻底消失）
            // Environment.Exit(0); 
        }
    }



}
