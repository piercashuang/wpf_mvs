using System;
using System.Collections.Generic;
using MVS.Contract.Camera; // 引入契约接口
using MvCameraControl;     // 引入海康 SDK 底层库

namespace MVS.Camera.Hik
{
    // 【关键修复】：必须显式声明继承 ICameraFactory
    public class HikCameraFactory : ICameraFactory
    {
        // 1. 实现接口的描述属性
        public string Name => "Hikvision Camera Factory";
        public string Version => "1.0.0";

        // 2. 实现扫描相机的核心逻辑
        public List<CameraMetaInfo> ScanCameras()
        {
            var discoveredList = new List<CameraMetaInfo>();

            // 设定要扫描的总线类型（千兆网口和 USB）
            DeviceTLayerType enumTLayerType = DeviceTLayerType.MvGigEDevice | DeviceTLayerType.MvUsbDevice;
            List<IDeviceInfo> deviceInfoList = new List<IDeviceInfo>();

            // 调用海康底层 SDK 进行硬件枚举
            int nRet = DeviceEnumerator.EnumDevices(enumTLayerType, out deviceInfoList);

            if (nRet == MvError.MV_OK)
            {
                foreach (var info in deviceInfoList)
                {
                    // 将海康底层的 IDeviceInfo 转换为我们跨品牌的 CameraMetaInfo 契约
                    var metaInfo = new CameraMetaInfo
                    {
                        VendorName = "Hikvision",
                        SerialNumber = info.SerialNumber,
                        IpAddress = info.ModelName,
                        UserDefinedName = info.UserDefinedName
                    };

                    discoveredList.Add(metaInfo);
                }
            }

            return discoveredList;
        }

        // 3. 实现创建具体相机控制类的逻辑 (稍后我们再细写这个内部的 HikCamera 类)
        public ICamera CreateCamera(CameraMetaInfo info)
        {
            // 当 UI 点击连接时，这里会真正 new 一个相机的控制实例返回
            // 例如：return new HikCamera(info.SerialNumber);

            throw new NotImplementedException("我们稍后来写具体的 HikCamera 控制类！");
        }
    }
}