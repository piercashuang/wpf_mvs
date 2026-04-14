using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MVS_WPF.DynamicViews
{
    public abstract class ControlBase
    {
        /// <summary>
        /// 核心方法：由子类实现，返回一个完整的 WPF 控件
        /// </summary>
        public abstract FrameworkElement Build(ControlContext ctx);

        /// <summary>
        /// 创建标准的表单行容器 (左边 Label，右边留给子类放 Control)
        /// 使用 Grid 保证所有的 Label 宽度一致，表单对齐
        /// </summary>
        protected Grid CreateRowContainer(string labelText)
        {
            Grid row = new Grid
            {
                Margin = new Thickness(0, 5, 0, 5)
            };

            // 第一列放标签 (固定 120 宽)，第二列放控件 (自适应)
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock label = new TextBlock
            {
                Text = labelText,
                Foreground = Brushes.LightGray,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 10, 0)
            };

            // 将标签放在第一列
            Grid.SetColumn(label, 0);
            row.Children.Add(label);

            return row;
        }
    }
}