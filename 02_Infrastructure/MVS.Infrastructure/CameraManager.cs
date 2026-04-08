using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using MVS.Contract.Camera;

namespace MVS.Infrastructure
{
    public sealed class CameraManager
    {
        private static readonly CameraManager _instance = new CameraManager();
        public static CameraManager Instance => _instance;

        // 记录“扫到的信息”和“对应的工厂”
        private class CameraDescriptor
        {
            public ICameraFactory Factory { get; set; }
            public CameraMetaInfo MetaInfo { get; set; }
        }

        // 字典 1：只存储相机的“档案”（SN -> 档案），不分配物理资源
        private readonly ConcurrentDictionary<string, CameraDescriptor> _discoveredCameras = new ConcurrentDictionary<string, CameraDescriptor>();

        // 字典 2：存储真正被用户“打开/连接”的活动相机实例
        private readonly ConcurrentDictionary<string, ICamera> _activeCameras = new ConcurrentDictionary<string, ICamera>();

        // 列表：存储已加载的工厂实例（内存常驻）
        private readonly List<ICameraFactory> _factories = new List<ICameraFactory>();

        private CameraManager() { }

        /// <summary>
        /// 1. 插件加载接口：只负责从硬盘加载 DLL，提取工厂类。
        /// 【建议调用时机】：主程序启动时（App 启动或 MainWindow 初始化时）只调用一次。
        /// </summary>
        public void InitializePlugins(string pluginDir)
        {
            if (!Directory.Exists(pluginDir)) return;

            _factories.Clear();

            // 精确匹配 "MVS.Camera.*.dll"
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
                                // 只存工厂，不再这里进行硬件扫描
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
        /// 2. 硬件扫描接口：不需要加载 DLL，直接指挥已有的工厂去找设备。
        /// 【建议调用时机】：界面上的“刷新列表”按钮点击时。
        /// </summary>
        public void ScanAllCameras()
        {
            // 每次扫描前清空旧的设备档案
            _discoveredCameras.Clear();

            // 让所有已加载的工厂去干活
            foreach (var factory in _factories)
            {
                try
                {
                    var discoveredCameras = factory.ScanCameras();

                    if (discoveredCameras != null)
                    {
                        foreach (var info in discoveredCameras)
                        {
                            if (!string.IsNullOrEmpty(info.SerialNumber))
                            {
                                _discoveredCameras.TryAdd(info.SerialNumber, new CameraDescriptor
                                {
                                    Factory = factory,
                                    MetaInfo = info
                                });

                                System.Diagnostics.Debug.WriteLine($"[Scanner] 发现设备: {info.SerialNumber} ({info.VendorName})");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Scanner] {factory.Name} 扫描失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 提供给 UI 的菜单：只返回纯文本的元数据列表
        /// </summary>
        public IEnumerable<CameraMetaInfo> GetAllDiscoveredCameraInfos()
        {
            return _discoveredCameras.Values.Select(d => d.MetaInfo);
        }

        /// <summary>
        /// 当用户真正点击“连接”时，才调用此方法实例化相机
        /// </summary>
        public ICamera CreateAndConnectCamera(string sn)
        {
            // 如果已经实例化过，直接返回
            if (_activeCameras.TryGetValue(sn, out var activeCamera))
            {
                return activeCamera;
            }

            // 如果字典里有这台相机的档案
            if (_discoveredCameras.TryGetValue(sn, out var descriptor))
            {
                // 【延迟加载】：让对应的工厂去 new 一个 ICamera 实例
                var camera = descriptor.Factory.CreateCamera(descriptor.MetaInfo);

                if (camera != null)
                {
                    _activeCameras.TryAdd(sn, camera);
                    return camera;
                }
            }

            return null; // 没找到该 SN 的相机
        }

        /// <summary>
        /// 获取当前已经连接/实例化的相机
        /// </summary>
        public ICamera GetActiveCamera(string sn) => _activeCameras.TryGetValue(sn, out var cam) ? cam : null;
    }
}