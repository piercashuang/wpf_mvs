using System;
using System.Drawing;
using MVS.Contract; // 确保引入了包含 MvsStatus 的命名空间

namespace MVS.Contract.Camera
{
    public interface ICamera
    {
        string Brand { get; }

        // --- 核心操作接口：现在返回 MvsStatus ---
        MvsStatus Open();
        void Close();
        MvsStatus StartGrabbing();
        MvsStatus StopGrabbing();

        event EventHandler<Bitmap> ImageGrabbed;

        // --- 参数读写接口：现在返回 MvsStatus ---

        MvsStatus SetFloatValue(string key, float value);
        MvsStatus GetFloatValue(string key, out float value);

        MvsStatus SetIntValue(string key, long value);
        MvsStatus GetIntValue(string key, out long value);

        MvsStatus SetBoolValue(string key, bool value);
        MvsStatus GetBoolValue(string key, out bool value);

        // 枚举类型：支持 ID 设置和 字符串设置
        MvsStatus SetEnumValue(string key, uint value);
        MvsStatus GetEnumValue(string key, out uint value);

        MvsStatus SetEnumValueByString(string key, string value); // 修正：设置时不需要 out

        // 在 ICamera.cs 中添加
        MvsStatus GetEnumSymbolic(string key, out string symbolic);
    }
}