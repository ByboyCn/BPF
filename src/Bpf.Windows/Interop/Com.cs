using System;
using System.Runtime.InteropServices;

namespace Bpf.Windows.Interop
{
    /// <summary>
    /// COM interop 辅助:在 netstandard2.1 + NativeAOT 下,绕开 [ComImport]
    /// (它在某些动态场景下不被 AOT 喜爱),改用 vtable 槽位直接调用。
    /// </summary>
    internal static unsafe class Com
    {
        /// <summary>
        /// 读取 COM 对象第 N 个 vtable 槽位的函数指针,返回调用委托。
        /// </summary>
        public static T GetVTableMethod<T>(IntPtr comPtr, int slot) where T : Delegate
        {
            // comPtr 指向对象的指针,其首字段是 vtable 指针;
            // vtable 又是函数指针数组(IntPtr*)。直接按 IntPtr* 索引最简单。
            var ppObj = (IntPtr*)comPtr;
            var vtable = *ppObj;
            var fnPtr = ((IntPtr*)vtable)[slot];
            return Marshal.GetDelegateForFunctionPointer<T>(fnPtr);
        }

        /// <summary>调用 IUnknown::AddRef。</summary>
        public static uint AddRef(IntPtr comPtr)
        {
            var addRef = GetVTableMethod<AddRefDelegate>(comPtr, 1);
            return addRef(comPtr);
        }

        /// <summary>调用 IUnknown::Release。返回剩余引用计数。</summary>
        public static uint Release(IntPtr comPtr)
        {
            if (comPtr == IntPtr.Zero) return 0;
            var release = GetVTableMethod<ReleaseDelegate>(comPtr, 2);
            return release(comPtr);
        }

        /// <summary>QueryInterface。</summary>
        public static int QueryInterface(IntPtr comPtr, in Guid iid, out IntPtr ppv)
        {
            var qi = GetVTableMethod<QueryInterfaceDelegate>(comPtr, 0);
            fixed (Guid* p = &iid)
            {
                return qi(comPtr, p, out ppv);
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int QueryInterfaceDelegate(IntPtr thisPtr, Guid* iid, out IntPtr ppv);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate uint AddRefDelegate(IntPtr thisPtr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate uint ReleaseDelegate(IntPtr thisPtr);
    }
}
