using System;
using System.Collections.Generic;
using System.Linq;

namespace MVS.Contract
{
    public class MvsStatus
    {
        // --- 属性定义 ---
        public int Code { get; }            // 错误十六进制码
        public string Name { get; }        // 内部名称
        public string MessageEn { get; }   // 英文描述 (对应 getErrorInfoEn)
        public string MessageCn { get; }   // 中文描述

        // --- 私有构造函数 ---
        private MvsStatus(int code, string name, string messageEn, string messageCn)
        {
            Code = code;
            Name = name;
            MessageEn = messageEn;
            MessageCn = messageCn;
        }

        // --- 静态实例 (直接对应你的宏定义) ---
        public static readonly MvsStatus Ok = new MvsStatus(0x0000, "Ok", "Run Success", "运行成功");
        public static readonly MvsStatus ConnectError = new MvsStatus(0x0001, "ConnectError", "Camera Connect failed", "连接相机失败");
        public static readonly MvsStatus DisconnectError = new MvsStatus(0x0002, "DisconnectError", "Camera DisConnect failed", "断开相机失败");
        public static readonly MvsStatus StartGrabError = new MvsStatus(0x0003, "StartGrabError", "Camera StartGrabbing failed", "开启拉流失败");
        public static readonly MvsStatus StopGrabError = new MvsStatus(0x0004, "StopGrabError", "Camera StopGrabbing failed", "停止拉流失败");
        public static readonly MvsStatus NoCameraError = new MvsStatus(0x0005, "NoCameraError", "Not Find Any Camera", "未找到相机");
        public static readonly MvsStatus InvalidInput = new MvsStatus(0x0006, "InvalidInput", "Invalid Input Parameter", "输入参数无效");
        public static readonly MvsStatus InvalidHandle = new MvsStatus(0x0007, "InvalidHandle", "The camera handle is invalid", "相机句柄无效");
        public static readonly MvsStatus WriteParamFailed = new MvsStatus(0x0008, "WriteParamFailed", "Write Param failed", "参数设置失败");
        public static readonly MvsStatus ReadParamFailed = new MvsStatus(0x0009, "ReadParamFailed", "Read Param failed", "参数读取失败");
        public static readonly MvsStatus NotConnected = new MvsStatus(0x000A, "NotConnected", "Camera not connected", "相机未连接");
        public static readonly MvsStatus AcquireFailed = new MvsStatus(0x000B, "AcquireFailed", "Camera creation failure", "相机创建失败");
        public static readonly MvsStatus ConfigSaveFailed = new MvsStatus(0x000C, "ConfigSaveFailed", "Failed to export config", "配置文件导出失败");
        public static readonly MvsStatus ConfigLoadFailed = new MvsStatus(0x000D, "ConfigLoadFailed", "Failed to import config", "配置文件导入失败");
        public static readonly MvsStatus GetImageTimeout = new MvsStatus(0x000E, "GetImageTimeout", "Get Image TimeOut", "采图超时");
        public static readonly MvsStatus DeviceUnaccessible = new MvsStatus(0x000F, "DeviceUnaccessible", "The device is UnAccessible", "设备被占用不可达");

        public static readonly MvsStatus UnknownError = new MvsStatus(-1, "UnknownError", "Unknown Error", "未知错误");

        // --- 逻辑判断与转换 ---
        public bool IsOk => this.Code == 0;

        public static IEnumerable<MvsStatus> GetAll()
        {
            return typeof(MvsStatus).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                                    .Select(f => f.GetValue(null))
                                    .Cast<MvsStatus>();
        }

        public static MvsStatus FromCode(int code)
        {
            return GetAll().FirstOrDefault(s => s.Code == code) ?? UnknownError;
        }

        // 这里的 ToString 相当于你之前的 getErrorInfoEn
        public override string ToString() => $"{MessageEn} (0x{Code:X4})";
    }
}