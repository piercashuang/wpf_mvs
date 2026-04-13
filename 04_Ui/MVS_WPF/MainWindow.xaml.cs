using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using MVS.Infrastructure;
using MVS_WPF.Views;         // 存放 CameraPropertiesView 的地方
using MVS_WPF.ViewModels;    // 存放 CameraPropertiesViewModel 和 SidebarViewModel 的地方

namespace MVS_WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            LoadCameraPlugins();

            // 核心新增：用代码动态生成下半部分的 UI 和绑定
            SetupMainContentUI();
        }
        private void SetupMainContentUI()
        {
            // ================= 1. 设置列定义 (左边300，中间自适应，右边300) =================
            ContentContainer.ColumnDefinitions.Clear();
            ContentContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
            ContentContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            ContentContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });

            // ================= 2. 构建左侧侧边栏 =================
            Border sidebarBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252526")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46")),
                BorderThickness = new Thickness(0, 0, 1, 0)
            };
            SidebarView sidebar = new SidebarView();
            sidebarBorder.Child = sidebar;
            Grid.SetColumn(sidebarBorder, 0);

            // ================= 3. 构建中间主画面区域 =================
            Grid centerGrid = new Grid { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")) };
            Image cameraDisplay = new Image { Stretch = Stretch.Uniform, Margin = new Thickness(10) };

            // 绑定画面
            Binding imageBinding = new Binding("DataContext.DisplayImage") { Source = sidebar };
            cameraDisplay.SetBinding(Image.SourceProperty, imageBinding);
            centerGrid.Children.Add(cameraDisplay);
            Grid.SetColumn(centerGrid, 1);

            // ================= 4. 构建右侧属性面板 =================
            Border propertiesBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252526")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46")),
                BorderThickness = new Thickness(1, 0, 0, 0)
            };
            CameraPropertiesView propertiesView = new CameraPropertiesView();
            propertiesBorder.Child = propertiesView;
            Grid.SetColumn(propertiesBorder, 2);

            // ================= 5. 【核心逻辑】：建立左右通讯 =================
            // 获取两个 ViewModel
            var sidebarVM = sidebar.DataContext as SidebarViewModel;
            var propVM = propertiesView.DataContext as CameraPropertiesViewModel;

            if (sidebarVM != null && propVM != null)
            {
                // 订阅侧边栏 ViewModel 的属性变化
                sidebarVM.PropertyChanged += (s, e) =>
                {
                    // 假设你在 SidebarViewModel 里增加了一个名为 ConnectedCameraSn 的属性
                    // 或者我们利用连接成功的时机
                    if (e.PropertyName == "ConnectedCameraSn" && !string.IsNullOrEmpty(sidebarVM.ConnectedCameraSn))
                    {
                        // 当左侧连接成功，通知右侧回读参数
                        propVM.LoadParametersFromCamera(sidebarVM.ConnectedCameraSn);
                    }
                };
            }

            // ================= 6. 塞入容器 =================
            ContentContainer.Children.Add(sidebarBorder);
            ContentContainer.Children.Add(centerGrid);
            ContentContainer.Children.Add(propertiesBorder);
        }

        private void LoadCameraPlugins()
        {
            try
            {
                string pluginPath = AppDomain.CurrentDomain.BaseDirectory;
                CameraManager.Instance.InitializePlugins(pluginPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载相机插件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ================= 窗口基础操作 =================

        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                BtnMaximize.Content = "❐";
                BtnMaximize.ToolTip = "向下还原";
            }
            else
            {
                BtnMaximize.Content = "☐";
                BtnMaximize.ToolTip = "最大化";
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            CameraManager.Instance.CloseAllCameras();
        }
    }
}