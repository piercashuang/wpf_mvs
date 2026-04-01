using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MVS.Contract
{
    /// <summary>
    /// 全局统一样式状态类 (强类型枚举模式)
    /// </summary>
    public class MvsStatus
    {
        // --- 属性定义 ---
        public int Code { get; }            // 错误十六进制码
        public string Name { get; }         // 内部变量名
        public string MessageEn { get; }    // 英文描述
        public string MessageCn { get; }    // 中文描述

        // --- 私有构造函数 ---
        private MvsStatus(int code, string name, string messageEn, string messageCn)
        {
            Code = code;
            Name = name;
            MessageEn = messageEn;
            MessageCn = messageCn;
        }

        #region 0x0000 成功状态
        public static readonly MvsStatus Ok = new MvsStatus(0x0000, "Ok", "Run Success", "运行成功");
        #endregion

        #region 0x1000 - 0x1FFF 相机硬件/驱动相关
        public static readonly MvsStatus ConnectError = new MvsStatus(0x1001, "ConnectError", "Camera Connect failed", "连接相机失败");
        public static readonly MvsStatus DisconnectError = new MvsStatus(0x1002, "DisconnectError", "Camera DisConnect failed", "断开相机失败");
        public static readonly MvsStatus StartGrabError = new MvsStatus(0x1003, "StartGrabError", "Camera StartGrabbing failed", "开启拉流失败");
        public static readonly MvsStatus StopGrabError = new MvsStatus(0x1004, "StopGrabError", "Camera StopGrabbing failed", "停止拉流失败");
        public static readonly MvsStatus NoCameraError = new MvsStatus(0x1005, "NoCameraError", "Not Find Any Camera", "未找到相机");
        public static readonly MvsStatus InvalidHandle = new MvsStatus(0x1007, "InvalidHandle", "The camera handle is invalid", "相机句柄无效");
        public static readonly MvsStatus NotConnected = new MvsStatus(0x100A, "NotConnected", "Camera not connected", "相机未连接");
        public static readonly MvsStatus AcquireFailed = new MvsStatus(0x100B, "AcquireFailed", "Camera creation failure", "相机创建失败");
        public static readonly MvsStatus GetImageTimeout = new MvsStatus(0x100E, "GetImageTimeout", "Get Image TimeOut", "采图超时");
        public static readonly MvsStatus DeviceUnaccessible = new MvsStatus(0x100F, "DeviceUnaccessible", "The device is UnAccessible", "设备被占用不可达");
        #endregion

        #region 0x3000 - 0x3FFF 系统配置/输入相关
        public static readonly MvsStatus InvalidInput = new MvsStatus(0x3001, "InvalidInput", "Invalid Input Parameter", "输入参数无效");
        public static readonly MvsStatus WriteParamFailed = new MvsStatus(0x3002, "WriteParamFailed", "Write Param failed", "参数设置失败");
        public static readonly MvsStatus ReadParamFailed = new MvsStatus(0x3003, "ReadParamFailed", "Read Param failed", "参数读取失败");
        public static readonly MvsStatus ConfigSaveFailed = new MvsStatus(0x300C, "ConfigSaveFailed", "Failed to export config", "配置文件导出失败");
        public static readonly MvsStatus ConfigLoadFailed = new MvsStatus(0x300D, "ConfigLoadFailed", "Failed to import config", "配置文件导入失败");
        #endregion

        #region 0x4000 - 0x4FFF UI/逻辑处理相关
        public static readonly MvsStatus ViewNotReady = new MvsStatus(0x4001, "ViewNotReady", "UI View is not ready", "界面窗口未就绪");
        #endregion

        #region 特殊状态
        public static readonly MvsStatus UnknownError = new MvsStatus(-1, "UnknownError", "Unknown Error", "未知错误");
        #endregion

        // --- 逻辑判断属性 ---
        public bool IsOk => Code == 0;
        public bool IsCameraError => Code >= 0x1000 && Code <= 0x1FFF;
        public bool IsConfigError => Code >= 0x3000 && Code <= 0x3FFF;
        public bool IsUiError => Code >= 0x4000 && Code <= 0x4FFF;

        // --- 静态辅助方法 ---

        /// <summary>
        /// 获取当前类定义的所有 MvsStatus 实例
        /// </summary>
        public static IEnumerable<MvsStatus> GetAll()
        {
            return typeof(MvsStatus)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(f => f.FieldType == typeof(MvsStatus))
                .Select(f => (MvsStatus)f.GetValue(null));
        }

        /// <summary>
        /// 根据错误码返回对应的状态对象
        /// </summary>
        public static MvsStatus FromCode(int code)
        {
            return GetAll().FirstOrDefault(s => s.Code == code) ?? UnknownError;
        }

        // 重写 ToString 方便调试和 UI 直接绑定显示
        public override string ToString() => $"[0x{Code:X4}] {MessageCn}";
    }
}