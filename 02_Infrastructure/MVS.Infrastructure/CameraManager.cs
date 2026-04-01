using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MVS.Contract;

namespace MVS.Infrastructure
{
    /// <summary>
    /// 需要调用的相机接口
    /// <summary>
    public sealed class CameraManager
    {
        // 饿汉式单例 (线程安全)
        private static readonly CameraManager _instance = new CameraManager();
        public static CameraManager Instance => _instance;

        // 已加载的相机实例 (Key: SerialNumber)
        private readonly ConcurrentDictionary<string, ICamera> _devices = new ConcurrentDictionary<string, ICamera>();

        private CameraManager() { }

        /// <summary>
        /// 扫描并加载 03_Plugins 目录下的所有插件
        /// </summary>
        public void InitializePlugins(string pluginDir)
        {
            if (!Directory.Exists(pluginDir)) return;

            // 递归搜索 dll
            var dllFiles = Directory.GetFiles(pluginDir, "MVS.Plugins.*.dll", SearchOption.AllDirectories);

            foreach (var file in dllFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    foreach (var type in assembly.GetTypes())
                    {
                        // 寻找实现了 ICamera 且可实例化的类
                        if (typeof(ICamera).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                        {
                            // 动态创建实例
                            var device = Activator.CreateInstance(type) as ICamera;
                            if (device != null)
                            {
                                // 此时还不知道 SN，通常插件初始化后会去枚举硬件
                                // 这里可以先存入待发现列表
                            }
                        }
                    }
                }
                catch { /* 记录日志 */ }
            }
        }

        // 统一调用入口
        public ICamera GetCamera(string sn) => _devices.TryGetValue(sn, out var cam) ? cam : null;

        public IEnumerable<ICamera> GetAllCameras() => _devices.Values;
    }
}