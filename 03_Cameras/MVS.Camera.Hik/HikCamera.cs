using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using MvCameraControl;
using MVS.Contract.Camera;

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

        // 构造函数：记住自己的序列号
        public HikCamera(string serialNumber)
        {
            _serialNumber = serialNumber;
        }

        public bool Open()
        {
            if (_device != null) return true; // 已经打开了

            // 1. 根据序列号，在底层重新找回这个特定的设备信息
            DeviceTLayerType enumTLayerType = DeviceTLayerType.MvGigEDevice | DeviceTLayerType.MvUsbDevice;
            List<IDeviceInfo> deviceInfoList = new List<IDeviceInfo>();
            int nRet = DeviceEnumerator.EnumDevices(enumTLayerType, out deviceInfoList);

            if (nRet != MvError.MV_OK) return false;

            IDeviceInfo targetInfo = null;
            foreach (var info in deviceInfoList)
            {
                if (info.SerialNumber == _serialNumber)
                {
                    targetInfo = info;
                    break;
                }
            }

            if (targetInfo == null) return false; // 没找到该序列号的相机

            // 2. 创建并打开设备
            try
            {
                _device = DeviceFactory.CreateDevice(targetInfo);
                nRet = _device.Open();

                if (nRet != MvError.MV_OK)
                {
                    _device.Dispose();
                    _device = null;
                    return false;
                }

                // 探测网络最佳包大小 (如果是 GigE 相机)
                if (_device is IGigEDevice gigEDevice)
                {
                    gigEDevice.GetOptimalPacketSize(out int packetSize);
                    _device.Parameters.SetIntValue("GevSCPSPacketSize", packetSize);
                }

                // 默认设置为连续采图模式
                _device.Parameters.SetEnumValueByString("AcquisitionMode", "Continuous");
                _device.Parameters.SetEnumValueByString("TriggerMode", "Off");

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool StartGrabbing()
        {
            if (_device == null || _isGrabbing) return false;

            int nRet = _device.StreamGrabber.StartGrabbing();
            if (nRet != MvError.MV_OK) return false;

            _isGrabbing = true;

            // 开启后台独立线程取图（完全照搬你提供的官方 Demo 逻辑）
            _grabThread = new Thread(ReceiveThreadProcess)
            {
                IsBackground = true // 设为后台线程，主程序退出时自动销毁
            };
            _grabThread.Start();

            return true;
        }

        public bool StopGrabbing()
        {
            if (_device == null || !_isGrabbing) return false;

            _isGrabbing = false; // 1. 改变标志位，后台 while 循环会退出

            // 2. 等待线程结束（最多等1秒），防止程序关闭时线程还在访问已释放的资源
            if (_grabThread != null && _grabThread.IsAlive)
            {
                _grabThread.Join(1000);
            }

            int nRet = _device.StreamGrabber.StopGrabbing();
            return nRet == MvError.MV_OK;
        }

        public void Close()
        {
            if (_isGrabbing)
            {
                StopGrabbing();
            }

            if (_device != null)
            {
                // 3. 彻底释放底层句柄
                _device.Close();
                _device.Dispose();
                _device = null;
            }
        }


        // 取图后台线程
        private void ReceiveThreadProcess()
        {
            while (_isGrabbing)
            {
                // 设置 1000ms 超时获取一帧图像
                int nRet = _device.StreamGrabber.GetImageBuffer(1000, out IFrameOut frameOut);

                if (nRet == MvError.MV_OK && frameOut != null)
                {
                    try
                    {
                        // 将海康底层的图像数据转换为标准的 C# Bitmap
                        // 注意：ToBitmap() 是内部深拷贝，转换完之后底层 Buffer 就可以释放了
                        Bitmap bitmap = frameOut.Image.ToBitmap();

                        if (bitmap != null)
                        {
                            bitmap.Save(@"D:\123test_frame.bmp");
                            // 触发事件，将图像甩给主 UI 层
                            ImageGrabbed?.Invoke(this, bitmap);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[HikCamera] 图像转换失败: {ex.Message}");
                    }
                    finally
                    {
                        // 【极其重要】：用完必须释放底层 Buffer，否则会导致相机内存爆满卡死！
                        _device.StreamGrabber.FreeImageBuffer(frameOut);
                    }
                }
                else
                {
                    // 没取到图（可能是触发模式没给信号），稍微歇一下防止 CPU 占用 100%
                    Thread.Sleep(5);
                }
            }
        }
    }
}