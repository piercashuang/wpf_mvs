using System.Xml.Linq;

namespace MVS_WPF.DynamicViews
{
    public class ControlContext
    {
        /// <summary>
        /// 当前参数对应的 XML 节点
        /// </summary>
        public XElement Element { get; set; }

        /// <summary>
        /// 参数的内部名称 (如 "ExposureTime")
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 控件的类型 ID (决定使用哪个 Builder)
        /// </summary>
        public int TypeId { get; set; }

        /// <summary>
        /// 数据源 (通常是你的 ViewModel，包含具体的属性)
        /// </summary>
        public object DataSource { get; set; }
    }
}