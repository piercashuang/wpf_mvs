using System.Collections.Generic;

namespace MVS.Contract.Camera
{
    /// <summary>
    /// 相机工厂接口：负责枚举和创建特定品牌的相机实例（对应 CameraPlugin.h）
    /// </summary>
    public interface ICameraFactory
    {
        // 插件描述
        string Name { get; }
        string Version { get; }

        /// <summary>
        /// 枚举当前环境下该插件能找到的所有相机
        /// </summary>
        /// <returns>返回相机元信息列表</returns>
        List<CameraMetaInfo> EnumCameras();

        /// <summary>
        /// 根据选定的元信息，创建一个实现了 ICamera 的对象
        /// </summary>
        /// <param name="info">相机元信息</param>
        /// <returns>相机实例</returns>
        ICamera CreateCamera(CameraMetaInfo info);
    }

    public interface ICameraFactory
    {
        /// <summary>
        /// 扫描当前环境下的相机列表
        /// </summary>
        List<CameraMetaInfo> ScanCameras();

        /// <summary>
        /// 根据元信息创建具体的相机控制实例
        /// </summary>
        ICamera CreateCamera(CameraMetaInfo metaInfo);
    }
}