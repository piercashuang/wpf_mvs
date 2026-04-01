namespace MVS.Contract.Camera
{
    /// <summary>
    /// 相机元信息（对应 CMCameraMetaInfo.h）
    /// </summary>
    public class CameraMetaInfo
    {
        public string SerialNumber { get; set; }     // 序列号 (唯一标识)
        public string UserDefinedName { get; set; }  // 用户定义名称
        public string VendorName { get; set; }       // 厂商名称 (Hik, Dalsa等)
        public string IpAddress { get; set; }        // IP地址 (网口相机)
        public object RawInfo { get; set; }          // 预留：存放 SDK 原生的驱动信息对象

        // 重写 Equals 方便去重
        public override bool Equals(object obj)
        {
            if (obj is CameraMetaInfo info)
                return SerialNumber == info.SerialNumber;
            return false;
        }

        public override int GetHashCode() => SerialNumber?.GetHashCode() ?? 0;
    }
}