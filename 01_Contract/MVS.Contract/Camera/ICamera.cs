using System;
using System.Drawing; // 必须引用 System.Drawing

namespace MVS.Contract.Camera
{
    public interface ICamera
    {
        /// <summary>
        /// 相机品牌
        /// </summary>
        string Brand { get; }

        /// <summary>
        /// 打开相机 (无需再传 SN，由具体类的构造函数负责接收)
        /// </summary>
        bool Open();

        /// <summary>
        /// 关闭相机并释放资源
        /// </summary>
        void Close();

        /// <summary>
        /// 开始取流 (返回 bool 以便外层判断是否成功)
        /// </summary>
        bool StartGrabbing();

        /// <summary>
        /// 停止取流
        /// </summary>
        bool StopGrabbing();

        /// <summary>
        /// 【核心事件】：图像抓取完成后的回调事件
        /// 第一个参数是发送者(相机本身)，第二个参数是转换好的 Bitmap 图像
        /// </summary>
        event EventHandler<Bitmap> ImageGrabbed;
    }
}