using System;
using Bpf.Platform;
using Bpf.Windows.Interop;

namespace Bpf.Windows.Render
{
    /// <summary>
    /// 包装 ID2D1SolidColorBrush,实现 IPlatformBrush。
    /// </summary>
    internal sealed class D2D1Brush : IPlatformBrush
    {
        private IntPtr _native;

        public IntPtr NativePtr => _native;

        public D2D1Brush(IntPtr nativeBrush)
        {
            _native = nativeBrush;
        }

        public void Dispose()
        {
            if (_native != IntPtr.Zero)
            {
                Com.Release(_native);
                _native = IntPtr.Zero;
            }
        }
    }
}
