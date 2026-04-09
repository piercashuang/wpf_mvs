using System;
using MVS.Contract.Camera;

namespace MVS.Camera.Hik
{
    public class HikCameraFactory : ICameraFactory
    {
        public string Name => "Hikvision Camera Factory";
        public string Version => "1.0.0";

        // 已经不需要在这里写扫描逻辑了

        public ICamera CreateCamera(CameraMetaInfo info)
        {
            // 确保这里是 return new 对象，而不是 throw 报错
            return new HikCamera(info.SerialNumber);
        }
    }
}