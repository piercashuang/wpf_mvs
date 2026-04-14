using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace MVS_WPF.DynamicViews.Builders
{
    public class FloatControlBuilder : ControlBase
    {
        public override FrameworkElement Build(ControlContext ctx)
        {
            // 1. 获取左侧显示的 Label 名称
            string labelText = ctx.Element.Attribute("text")?.Value ?? ctx.Name;
            Grid row = CreateRowContainer(labelText);

            // 2. 创建一个内部 Grid，用来横向排列 Slider 和 TextBox
            Grid inputGrid = new Grid();
            // 第一列放滑动条 (自适应宽度)
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            // 第二列放输入框 (固定 60 像素宽度)
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });

            // 3. 解析 XML 中的最大值和最小值 (参考你 C++ 的逻辑)
            double min = 0;
            double max = 100;
            if (ctx.Element.Element("min") != null) double.TryParse(ctx.Element.Element("min").Value, out min);
            if (ctx.Element.Element("max") != null) double.TryParse(ctx.Element.Element("max").Value, out max);

            // ================== 创建滑动条 (Slider) ==================
            Slider slider = new Slider
            {
                Minimum = min,
                Maximum = max,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0), // 右侧留点间距
                IsSnapToTickEnabled = false          // 允许无级平滑滑动
            };

            // 滑动条实时触发更新 (PropertyChanged)
            Binding sliderBinding = new Binding(ctx.Name)
            {
                Source = ctx.DataSource,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(slider, Slider.ValueProperty, sliderBinding);


            // ================== 创建输入框 (TextBox) ==================
            TextBox textBox = new TextBox
            {
                Height = 26,
                Background = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(5, 0, 5, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };

            // 输入框失去焦点或按回车时才触发更新 (LostFocus)
            // StringFormat="F2" 限制显示两位小数
            Binding textBinding = new Binding(ctx.Name)
            {
                Source = ctx.DataSource,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
                StringFormat = "F2"
            };
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, textBinding);


            // 4. 将两个控件塞入内部 Grid
            Grid.SetColumn(slider, 0);
            Grid.SetColumn(textBox, 1);
            inputGrid.Children.Add(slider);
            inputGrid.Children.Add(textBox);

            // 5. 将内部 Grid 塞入主行的第二列
            Grid.SetColumn(inputGrid, 1);
            row.Children.Add(inputGrid);

            return row;
        }
    }
}