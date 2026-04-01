using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVS.Infrastructure.Log
{
    public static class Logger
    {
        public static void Info(string message)
        {
            // 暂时先输出到控制台，以后可以改写成存入文件
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INFO] {message}");
        }
    }
}