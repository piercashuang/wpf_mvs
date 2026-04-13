using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using MVS.Infrastructure;
using MVS_WPF.Views; // 引入侧边栏的命名空间

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
            // 1. 设置列定义 (左侧 300，右侧自适应)
            ContentContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
            ContentContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // ================= 2. 构建左侧侧边栏 =================
            Border sidebarBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252526")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46")),
                BorderThickness = new Thickness(0, 0, 1, 0)
            };

            // 实例化侧边栏
            SidebarView sidebar = new SidebarView();
            sidebarBorder.Child = sidebar;
            Grid.SetColumn(sidebarBorder, 0);

            // ================= 3. 构建右侧主画面区域 =================
            Grid rightGrid = new Grid
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"))
            };

            TextBlock placeholderText = new TextBlock
            {
                Text = "主画面显示区域",
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 30
            };

            Image cameraDisplay = new Image
            {
                Stretch = Stretch.Uniform,
                Margin = new Thickness(10)
            };

            // 【最核心的绑定代码】：相当于 XAML 里的 Source="{Binding DataContext.DisplayImage, ElementName=SidebarCtrl}"
            Binding imageBinding = new Binding("DataContext.DisplayImage")
            {
                // 直接将绑定的源头指向我们上面刚 new 出来的 sidebar 对象
                Source = sidebar
            };
            cameraDisplay.SetBinding(Image.SourceProperty, imageBinding);

            // 组装右侧
            rightGrid.Children.Add(placeholderText);
            rightGrid.Children.Add(cameraDisplay);
            Grid.SetColumn(rightGrid, 1);

            // ================= 4. 将左右两部分塞进主容器 =================
            ContentContainer.Children.Add(sidebarBorder);
            ContentContainer.Children.Add(rightGrid);
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