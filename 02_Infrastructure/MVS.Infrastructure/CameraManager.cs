using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MVS.Contract.Camera;

namespace MVS.Infrastructure
{
    public sealed class CameraManager
    {
        private static readonly CameraManager _instance = new CameraManager();
        public static CameraManager Instance => _instance;

        // 存储具体的相机实例
        private readonly ConcurrentDictionary<string, ICamera> _devices = new ConcurrentDictionary<string, ICamera>();

        // 存储加载的工厂实例（如果后续需要按品牌重新扫描，会用到这个集合）
        private readonly List<ICameraFactory> _factories = new List<ICameraFactory>();

        private CameraManager() { }

        public void InitializePlugins(string pluginDir)
        {
            if (!Directory.Exists(pluginDir)) return;

            // 每次扫描前清理旧数据
            _devices.Clear();
            _factories.Clear();

            var dllFiles = Directory.GetFiles(pluginDir, "MVS.Plugins.*.dll", SearchOption.AllDirectories);

            foreach (var file in dllFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    foreach (var type in assembly.GetTypes())
                    {
                        // 【修改点 1】: 这里寻找实现了 ICameraFactory 的类，而不是 ICamera
                        if (typeof(ICameraFactory).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                        {
                            // 1. 创建工厂实例
                            var factory = Activator.CreateInstance(type) as ICameraFactory;
                            if (factory != null)
                            {
                                _factories.Add(factory);

                                // 2. 调用工厂枚举真实硬件，返回元信息列表
                                var discoveredCameras = factory.ScanCameras();

                                if (discoveredCameras != null)
                                {
                                    foreach (var info in discoveredCameras)
                                    {
                                        // 3. 让工厂根据元信息创建具体的相机控制实例
                                        var camera = factory.CreateCamera(info);

                                        // 4. 将创建好的相机实例注册到管理器中
                                        // 【注意】这里假设你的 CameraMetaInfo 类中包含了 SerialNumber 属性
                                        if (camera != null && !string.IsNullOrEmpty(info.SerialNumber))
                                        {
                                            _devices.TryAdd(info.SerialNumber, camera);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    /* 记录加载 DLL 失败的日志 */
                }
            }
        }

        public ICamera GetCamera(string sn) => _devices.TryGetValue(sn, out var cam) ? cam : null;

        public IEnumerable<ICamera> GetAllCameras() => _devices.Values;
    }
}