using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using MVS.Contract.Camera;
using MvCameraControl; // 【关键引入】：Manager 现在直接依赖海康 SDK 进行全局扫描

namespace MVS.Infrastructure
{
    public sealed class CameraManager : SingletonBase<CameraManager>
    {
        // 1. 私有构造函数：负责所有内部容器的初始化
        private CameraManager()
        {
            _discoveredCameras = new ConcurrentDictionary<string, CameraDescriptor>();
            _activeCameras = new ConcurrentDictionary<string, ICamera>();
            _factories = new List<ICameraFactory>();
        }

        private class CameraDescriptor
        {
            public ICameraFactory Factory { get; set; }
            public CameraMetaInfo MetaInfo { get; set; }
        }

        // 2. 字段声明：这里不需要再 new 了，交给构造函数即可

        private readonly ConcurrentDictionary<string, CameraDescriptor> _discoveredCameras;
        private readonly ConcurrentDictionary<string, ICamera> _activeCameras;
        private readonly List<ICameraFactory> _factories;

        public void InitializePlugins(string pluginDir)
        {
            if (!Directory.Exists(pluginDir)) return;

            _factories.Clear();
            var dllFiles = Directory.GetFiles(pluginDir, "MVS.Camera.*.dll", SearchOption.TopDirectoryOnly);

            foreach (var file in dllFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    foreach (var type in assembly.GetTypes())
                    {
                        if (typeof(ICameraFactory).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                        {
                            var factory = Activator.CreateInstance(type) as ICameraFactory;
                            if (factory != null)
                            {
                                _factories.Add(factory);
                                System.Diagnostics.Debug.WriteLine($"[PluginLoader] 成功加载工厂: {factory.Name}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PluginLoader] 加载 DLL 错误: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 统一在 Manager 中使用海康 SDK 扫描所有相机
        /// </summary>
        public void ScanAllCameras()
        {
            _discoveredCameras.Clear();

            // 设定要扫描的总线类型
            DeviceTLayerType enumTLayerType = DeviceTLayerType.MvGigEDevice | DeviceTLayerType.MvUsbDevice;
            List<IDeviceInfo> deviceInfoList = new List<IDeviceInfo>();

            // 统一调用海康底层 SDK 进行硬件枚举
            int nRet = DeviceEnumerator.EnumDevices(enumTLayerType, out deviceInfoList);

            if (nRet == MvError.MV_OK)
            {
                foreach (var info in deviceInfoList)
                {
                    // 提取信息
                    var metaInfo = new CameraMetaInfo
                    {
                        VendorName = string.IsNullOrEmpty(info.ManufacturerName) ? "Unknown" : info.ManufacturerName,
                        SerialNumber = info.SerialNumber,
                        ModelName = info.ModelName,
                        UserDefinedName = info.UserDefinedName
                    };

                    // 【核心分配逻辑】：扫描到了相机，我们需要决定把它分发给哪个工厂去实例化
                    // 这里通过比较 VendorName 和 插件的 Name 来匹配 (例如：如果扫到 "Hikvision"，就找 Name 包含 "Hik" 的工厂)
                    var matchedFactory = _factories.FirstOrDefault(f =>
                        metaInfo.VendorName.IndexOf("GEV", StringComparison.OrdinalIgnoreCase) >= 0  ||
                        metaInfo.VendorName.IndexOf("Dalsa", StringComparison.OrdinalIgnoreCase) >= 0 && f.Name.Contains("Dalsa")
                    // 如果都没有匹配上，默认给第一个加载的工厂（或者给专门的通用 GigE 工厂）
                    ) ?? _factories.FirstOrDefault();

                    if (!string.IsNullOrEmpty(metaInfo.SerialNumber))
                    {
                        _discoveredCameras.TryAdd(metaInfo.SerialNumber, new CameraDescriptor
                        {
                            Factory = matchedFactory,
                            MetaInfo = metaInfo
                        });

                        System.Diagnostics.Debug.WriteLine($"[Scanner] Manager统一扫描发现设备: {metaInfo.SerialNumber} ({metaInfo.VendorName}) -> 分配给工厂: {(matchedFactory != null ? matchedFactory.Name : "无")}");
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[Scanner] 全局枚举设备失败，错误码: {nRet}");
            }
        }

        public IEnumerable<CameraMetaInfo> GetAllDiscoveredCameraInfos()
        {
            return _discoveredCameras.Values.Select(d => d.MetaInfo);
        }

        public void CloseAllCameras()
        {
            // 遍历所有正在使用的相机
            foreach (var sn in _activeCameras.Keys.ToList())
            {
                if (_activeCameras.TryRemove(sn, out var camera))
                {
                    try
                    {
                        camera.Close();
                        System.Diagnostics.Debug.WriteLine($"[Manager] 已成功释放相机: {sn}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Manager] 释放相机 {sn} 时出错: {ex.Message}");
                    }
                }
            }
        }

        public ICamera CreateAndConnectCamera(string sn)
        {
            if (_activeCameras.TryGetValue(sn, out var activeCamera))
                return activeCamera;

            if (_discoveredCameras.TryGetValue(sn, out var descriptor))
            {
                if (descriptor.Factory == null) return null;

                // 依然是让工厂去 new 对象
                var camera = descriptor.Factory.CreateCamera(descriptor.MetaInfo);

                if (camera != null)
                {
                    _activeCameras.TryAdd(sn, camera);
                    return camera;
                }
            }
            return null;
        }

        public ICamera GetActiveCamera(string sn) => _activeCameras.TryGetValue(sn, out var cam) ? cam : null;
    }
}