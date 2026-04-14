using MVS_WPF.DynamicViews.Builders;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
// using MVS_WPF.DynamicViews.Builders; // 等你建了 Builders 文件夹后取消注释这行

namespace MVS_WPF.DynamicViews
{
    public static class ControlFactory
    {
        // 控件生成器注册表
        private static readonly Dictionary<int, ControlBase> _builders = new Dictionary<int, ControlBase>();

        /// <summary>
        /// 静态构造函数，程序启动时自动注册所有控件类型
        /// 对应你 C++ 里的 REGISTER_CONTROL 宏
        /// </summary>
        static ControlFactory()
        {
            // 注意：等你写好了具体的 Builder 类，把下面的注释解开

            //_builders.Add(0, new EnumControlBuilder());
            _builders.Add(5, new StringControlBuilder());

            // 注册 Float 控件
            _builders.Add(2, new FloatControlBuilder());
        }

        /// <summary>
        /// 根据上下文生成 WPF 控件
        /// </summary>
        public static FrameworkElement Create(ControlContext ctx)
        {
            // 1. 查找是否有对应的生成器
            if (_builders.TryGetValue(ctx.TypeId, out var builder))
            {
                return builder.Build(ctx);
            }

            // 2. 降级处理：如果没有匹配的类型，返回一个红色的警告字样，防止程序崩溃
            return new TextBlock
            {
                Text = $"[未实现的控件类型: TypeId = {ctx.TypeId}]",
                Foreground = Brushes.Red,
                Margin = new Thickness(0, 5, 0, 5)
            };
        }
    }
}