using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;
using MVS_WPF.DynamicViews; // 引入你写的动态 UI 引擎命名空间

namespace MVS_WPF.Views
{
    /// <summary>
    /// CameraPropertiesView.xaml 的交互逻辑
    /// </summary>
    public partial class CameraPropertiesView : UserControl
    {
        public CameraPropertiesView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 根据相机品牌加载对应的 XML 配置，并动态生成 UI
        /// </summary>
        /// <param name="brandName">相机品牌名 (对应 xml 文件名，如 "Hikvision")</param>
        /// <param name="viewModelDataSource">数据源 (你的 CameraPropertiesViewModel 对象)</param>
        public void LoadCameraConfig(string brandName, object viewModelDataSource)
        {
            // 每次加载新相机前，清空旧的面板控件
            DynamicStack.Children.Clear();

            if (string.IsNullOrEmpty(brandName)) return;

            // 1. 拼装配置文件的绝对路径
            // 路径指向：运行目录/DynamicViews/CameraConfigs/Hikvision.xml
            string xmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DynamicViews", "CameraConfigs", $"{brandName}.xml");

            // 2. 文件存在性校验
            if (!File.Exists(xmlPath))
            {
                DynamicStack.Children.Add(new TextBlock
                {
                    Text = $"找不到配置文件:\n{xmlPath}",
                    Foreground = Brushes.Red,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(5)
                });
                return;
            }

            try
            {
                // 3. 读取 XML 文档
                XDocument doc = XDocument.Load(xmlPath);
                var inputNode = doc.Root.Element("input");

                if (inputNode != null)
                {
                    // 4. 遍历所有的 <param> 节点
                    foreach (var node in inputNode.Elements())
                    {
                        // 安全读取属性
                        int typeId = 0;
                        if (node.Attribute("typeId") != null)
                            int.TryParse(node.Attribute("typeId").Value, out typeId);

                        string name = node.Attribute("name")?.Value ?? "Unknown";

                        // 5. 组装上下文
                        ControlContext ctx = new ControlContext
                        {
                            Element = node,
                            TypeId = typeId,
                            Name = name,
                            DataSource = viewModelDataSource
                        };

                        // 6. 召唤工厂生产控件！
                        FrameworkElement widget = ControlFactory.Create(ctx);

                        if (widget != null)
                        {
                            // 7. 将生成的控件（比如你写的 Slider + TextBox）塞入界面
                            DynamicStack.Children.Add(widget);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 解析报错（如 XML 格式写错了）时给个提示
                DynamicStack.Children.Add(new TextBlock
                {
                    Text = $"解析配置失败: {ex.Message}",
                    Foreground = Brushes.Red,
                    TextWrapping = TextWrapping.Wrap
                });
            }
        }
    }
}