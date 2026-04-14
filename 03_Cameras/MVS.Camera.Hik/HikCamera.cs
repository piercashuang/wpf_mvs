using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using MvCameraControl;
using MVS.Contract.Camera;
using MVS.Contract;

namespace MVS.Camera.Hik
{
    public class HikCamera : ICamera
    {
        public string Brand => "Hikvision";

        private readonly string _serialNumber;
        private IDevice _device = null;

        private bool _isGrabbing = false;
        private Thread _grabThread = null;

        // 对外暴露的图像事件
        public event EventHandler<Bitmap> ImageGrabbed;

        // 构造函数
        public HikCamera(string serialNumber)
        {
            _serialNumber = serialNumber;
        }

        public MvsStatus Open()
        {
            if (_device != null) return MvsStatus.Ok;

            // 1. 枚举设备找目标
            DeviceTLayerType enumTLayerType = DeviceTLayerType.MvGigEDevice | DeviceTLayerType.MvUsbDevice;
            List<IDeviceInfo> deviceInfoList = new List<IDeviceInfo>();
            int nRet = DeviceEnumerator.EnumDevices(enumTLayerType, out deviceInfoList);

            if (nRet != MvError.MV_OK) return MvsStatus.NoCameraError;

            IDeviceInfo targetInfo = null;
            foreach (var info in deviceInfoList)
            {
                if (info.SerialNumber == _serialNumber)
                {
                    targetInfo = info;
                    break;
                }
            }

            if (targetInfo == null) return MvsStatus.NoCameraError;

            // 2. 创建并打开
            try
            {
                _device = DeviceFactory.CreateDevice(targetInfo);
                nRet = _device.Open();

                if (nRet != MvError.MV_OK)
                {
                    _device.Dispose();
                    _device = null;
                    return MapNativeError(nRet);
                }

                // GigE 优化
                if (_device is IGigEDevice gigEDevice)
                {
                    gigEDevice.GetOptimalPacketSize(out int packetSize);
                    _device.Parameters.SetIntValue("GevSCPSPacketSize", packetSize);
                }

                _device.Parameters.SetEnumValueByString("AcquisitionMode", "Continuous");
                _device.Parameters.SetEnumValueByString("TriggerMode", "Off");

                return MvsStatus.Ok;
            }
            catch
            {
                return MvsStatus.AcquireFailed;
            }
        }

        public MvsStatus StartGrabbing()
        {
            if (_device == null) return MvsStatus.NotConnected;
            if (_isGrabbing) return MvsStatus.Ok;

            int nRet = _device.StreamGrabber.StartGrabbing();
            if (nRet != MvError.MV_OK) return MapNativeError(nRet);

            _isGrabbing = true;
            _grabThread = new Thread(ReceiveThreadProcess)
            {
                IsBackground = true
            };
            _grabThread.Start();

            return MvsStatus.Ok;
        }

        public MvsStatus StopGrabbing()
        {
            if (_device == null) return MvsStatus.NotConnected;
            if (!_isGrabbing) return MvsStatus.Ok;

            _isGrabbing = false;
            if (_grabThread != null && _grabThread.IsAlive)
            {
                _grabThread.Join(1000);
            }

            int nRet = _device.StreamGrabber.StopGrabbing();
            return nRet == MvError.MV_OK ? MvsStatus.Ok : MvsStatus.StopGrabError;
        }

        public void Close()
        {
            if (_isGrabbing) StopGrabbing();

            if (_device != null)
            {
                _device.Close();
                _device.Dispose();
                _device = null;
            }
        }

        private void ReceiveThreadProcess()
        {
            while (_isGrabbing)
            {
                // 1. 防御性检查：防止主线程调用 Close() 后 _device 变为空引发异常
                if (_device == null || _device.StreamGrabber == null)
                {
                    break;
                }

                int nRet = _device.StreamGrabber.GetImageBuffer(1000, out IFrameOut frameOut);

                if (nRet == MvError.MV_OK && frameOut != null)
                {
                    try
                    {
                        // 2. 【核心修复】：必须检查 frameOut.Image 是否为空！
                        // 海康SDK在网络丢包或残帧时，Image 属性可能为 null
                        if (frameOut.Image != null)
                        {
                            Bitmap bitmap = frameOut.Image.ToBitmap();
                            if (bitmap != null)
                            {
                                ImageGrabbed?.Invoke(this, bitmap);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[HikCamera] Image convert error: {ex.Message}");
                    }
                    finally
                    {
                        // 3. 再次确保底层没被释放，才去释放图像缓存
                        if (_device != null && _device.StreamGrabber != null)
                        {
                            _device.StreamGrabber.FreeImageBuffer(frameOut);
                        }
                    }
                }
                else
                {
                    Thread.Sleep(5);
                }
            }
        }

        // ================= 参数设置实现 (返回 MvsStatus) =================

        public MvsStatus GetFloatValue(string key, out float value)
        {
            value = 0f;
            if (_device == null) return MvsStatus.NotConnected;
            int nRet = _device.Parameters.GetFloatValue(key, out var stParam);
            if (nRet == MvError.MV_OK) value = stParam.CurValue;
            return MapNativeError(nRet);
        }

        public MvsStatus SetFloatValue(string key, float value)
        {
            if (_device == null) return MvsStatus.NotConnected;
            return MapNativeError(_device.Parameters.SetFloatValue(key, value));
        }

        public MvsStatus GetIntValue(string key, out long value)
        {
            value = 0;
            if (_device == null) return MvsStatus.NotConnected;
            int nRet = _device.Parameters.GetIntValue(key, out var stParam);
            if (nRet == MvError.MV_OK) value = stParam.CurValue;
            return MapNativeError(nRet);
        }

        public MvsStatus SetIntValue(string key, long value)
        {
            if (_device == null) return MvsStatus.NotConnected;
            return MapNativeError(_device.Parameters.SetIntValue(key, value));
        }

        public MvsStatus GetBoolValue(string key, out bool value)
        {
            value = false;
            if (_device == null) return MvsStatus.NotConnected;
            return MapNativeError(_device.Parameters.GetBoolValue(key, out value));
        }

        public MvsStatus SetBoolValue(string key, bool value)
        {
            if (_device == null) return MvsStatus.NotConnected;
            return MapNativeError(_device.Parameters.SetBoolValue(key, value));
        }

        public MvsStatus GetEnumValue(string key, out uint value)
        {
            value = 0;
            //if (_device == null) return MvsStatus.NotConnected;

            //// 海康 SDK 返回 IEnumValue 接口
            //int nRet = _device.Parameters.GetEnumValue(key, out IEnumValue stParam);
            //if (nRet == MvError.MV_OK)
            //{
            //    // 修正点：使用 .CurValue
            //    value = (uint)stParam.CurValue;
            //    return MvsStatus.Ok;
            //}
            return MvsStatus.ReadParamFailed;
        }
        public MvsStatus GetEnumSymbolic(string key, out string symbolic)
        {
            symbolic = "";
            if (_device == null) return MvsStatus.NotConnected;

            int nRet = _device.Parameters.GetEnumValue(key, out IEnumValue stParam);
            if (nRet == MvError.MV_OK)
            {
                // 获取当前选中的枚举项的符号名（如 "On", "Off"）
                symbolic = stParam.CurEnumEntry.Symbolic;
                return MvsStatus.Ok;
            }
            return MapNativeError(nRet);
        }
        public MvsStatus SetEnumValueByString(string key, string value)
        {
            if (_device == null) return MvsStatus.NotConnected;
            // 直接调用海康的字符串设置接口
            int nRet = _device.Parameters.SetEnumValueByString(key, value);
            return MapNativeError(nRet);
        }
        public MvsStatus SetEnumValue(string key, uint value)
        {
            if (_device == null) return MvsStatus.NotConnected;
            return MapNativeError(_device.Parameters.SetEnumValue(key, value));
        }

        private MvsStatus MapNativeError(int nRet)
        {
            if (nRet == MvError.MV_OK) return MvsStatus.Ok;

            switch (nRet)
            {
                case MvError.MV_E_HANDLE: return MvsStatus.InvalidHandle;
                case MvError.MV_E_GC_ACCESS: return MvsStatus.DeviceUnaccessible;
                case MvError.MV_E_PARAMETER: return MvsStatus.InvalidInput;
                case MvError.MV_E_NODATA: return MvsStatus.GetImageTimeout;
                default:
                    // 如果是写入操作失败通常返回此
                    return MvsStatus.WriteParamFailed;
            }
        }
    }
}