using System;
using System.Reflection;
using System.Threading;

namespace MVS.Infrastructure
{
    /// <summary>
    /// 线程安全的泛型单例基类。
    ///
    /// 为什么它是线程安全的：
    /// 1. Instance 的真实创建由 Lazy&lt;T&gt; 接管，而不是手写 if (instance == null)。
    /// 2. LazyThreadSafetyMode.ExecutionAndPublication 保证多线程同时首次访问时，
    ///    只有一个线程真正执行 CreateInstance()。
    /// 3. 其它线程会等待首次创建完成，然后拿到同一个实例引用。
    /// 4. 因此不会出现“创建出多个单例对象，再相互覆盖”的问题。
    ///
    /// 用法：
    /// public sealed class CameraManager : SingletonBase&lt;CameraManager&gt;
    /// {
    ///     private CameraManager() { }
    /// }
    ///
    /// 然后直接通过 CameraManager.Instance 访问。
    /// </summary>
    /// <typeparam name="T">派生类本身</typeparam>
    public abstract class SingletonBase<T> where T : class
    {
        // Lazy<T> 会把“实例何时创建”和“并发时如何创建”统一管理掉。
        // ExecutionAndPublication 是最常用的线程安全模式：
        // - Execution: 只允许一个线程执行工厂方法 CreateInstance()
        // - Publication: 创建完成后，把同一个实例发布给所有线程
        private static readonly Lazy<T> _instance =
            new Lazy<T>(CreateInstance, LazyThreadSafetyMode.ExecutionAndPublication);

        // 第一次访问 Instance 时才会真正创建对象，属于延迟初始化。
        // 后续访问不会重复创建，而是直接返回已经缓存好的同一个实例。
        public static T Instance => _instance.Value;

        protected SingletonBase()
        {
        }

        private static T CreateInstance()
        {
            var type = typeof(T);

            if (type.IsAbstract)
            {
                throw new InvalidOperationException(type.FullName + " 不能是抽象类型。");
            }

            // 允许派生类把构造函数写成 private 或 protected，
            // 这样外部无法随意 new，只能通过 Instance 获取对象。
            var ctor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);

            if (ctor == null)
            {
                throw new InvalidOperationException(
                    type.FullName + " 必须提供无参构造函数，建议使用 protected 构造函数。");
            }

            // 这里不会因为多线程而重复执行多次。
            // 重复执行的控制已经由上面的 Lazy<T> 负责。
            return (T)ctor.Invoke(null);
        }
    }
}
