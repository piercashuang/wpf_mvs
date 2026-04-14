using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace MVS_WPF.DynamicViews.Builders
{
    public class StringControlBuilder : ControlBase
    {
        public override FrameworkElement Build(ControlContext ctx)
        {
            // 1. 获取显示名称。如果 XML 里配了 "text" 属性就用它，否则兜底使用参数名 "name"
            string labelText = ctx.Element.Attribute("text")?.Value ?? ctx.Name;

            // 2. 调用基类方法，生成带有 Label 的标准 Grid 行
            Grid row = CreateRowContainer(labelText);

            // 3. 创建 TextBox 并设置你的深色工业风样式
            TextBox textBox = new TextBox
            {
                Height = 26,
                Background = new SolidColorBrush(Color.FromRgb(51, 51, 51)),       // 对应 Qt 的 #333333
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),      // 对应 Qt 的 #555555
                BorderThickness = new Thickness(1),
                Padding = new Thickness(5, 0, 5, 0),
                VerticalContentAlignment = VerticalAlignment.Center
            };

            // 4. 【核心】设置双向数据绑定
            // 这完全等价于 XAML 中的 Text="{Binding Path=xxx, Mode=TwoWay, UpdateSourceTrigger=LostFocus}"
            Binding binding = new Binding(ctx.Name)
            {
                Source = ctx.DataSource,
                Mode = BindingMode.TwoWay,
                // 对于字符串输入，推荐使用 LostFocus（失去焦点时才下发参数），防止用户还在打字就频繁触发底层逻辑
                UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
            };
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, binding);

            // 5. 将输入框放入 Grid 的第二列 (第一列在基类里已经放了 Label)
            Grid.SetColumn(textBox, 1);
            row.Children.Add(textBox);

            return row;
        }
    }
}