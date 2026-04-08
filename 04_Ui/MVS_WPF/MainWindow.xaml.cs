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

namespace MVS_WPF
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 【新增】在 UI 初始化完成后，立刻去扫描并加载相机插件
            MVS.Infrastructure.CameraManager.Instance.ScanAllCameras();
            LoadCameraPlugins();
        }


        /// <summary>
        /// 【新增】调用底层 CameraManager 扫描根目录的相机 DLL
        /// </summary>
        private void LoadCameraPlugins()
        {
            try
            {
                // 获取当前 MVS_WPF.exe 所在的运行目录 (即 bin\Debug\)
                string pluginPath = AppDomain.CurrentDomain.BaseDirectory;

                // 触发底层工厂扫描
                MVS.Infrastructure.CameraManager.Instance.InitializePlugins(pluginPath);

                // 测试：获取已加载的相机，在输出窗口打印一下看看是否成功
              //  var loadedCameras = CameraManager.Instance.GetAllCameras();
              //  System.Diagnostics.Debug.WriteLine($"[系统提示] 成功加载了 {loadedCameras.Count()} 个相机实例！");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载相机插件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
    }



}
