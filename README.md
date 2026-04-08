# MVS_WPF
学习WPF 复刻MVS主要功能
https://github.com/piercashuang/MVS_WPF.git

# 工具版本
visual studio 2019 
.net 4.8
hik dll...

# 项目架构MVC分层结构
MVS_WPF (解决方案根目录)
├── 01_Contract (核心契约层 - 新增)
│   ├── ICamera.cs        // 所有相机必须实现的接口
│   ├── IPlugin.cs        // 插件加载元数据接口
│   └── CameraStatus.cs   // 强类型枚举、状态码
│
├── 02_Infrastructure (原 Foundation)
│   ├── MVS.Langs          // 多语言
│   ├── MVS.Logs           // Serilog/NLog 异步日志封装
│   └── MVS.Utils          // 常用工具类
│
├── 03_Plugins (相机插件层)
│   ├── MVS.Plugins.Hik    // 引用海康 DLL
│   ├── MVS.Plugins.Dalsa  // 引用 Dalsa DLL
│   └── MVS.Plugins.Virtual// 虚拟相机
│
└── 04_Ui (WPF 主程序)
    ├── App.xaml
    ├── ViewModels         // MVVM 的核心逻辑
    ├── Views              // 采集显示视图
    ├── Controls           // 自定义渲染控件
    └── MvsGlobal.cs       // 全局单例数据 (通过 get/set 封装)

# 技术方案概述
1相机硬件用插件式 核心机制：MEF (Managed Extensibility Framework)
2日志用开源保证支持高并发IO
3状态码强类型枚举类
4全局数据访问 get/set封装 支持多线程的读写
5Interfaces实现相机接口
6图像文件很大 考虑用池技术优化

