using System;
using Bpf.Platform;

namespace Bpf.Media
{
    /// <summary>
    /// 画笔基类。应用层类型,被 <see cref="IDrawingContext"/> 用于填充/描边。
    /// 平台后端负责把它物化成平台原生资源(如 D2D1 Brush)。
    /// </summary>
    public abstract class Brush
    {
        /// <summary>
        /// 不透明度 [0,1]。
        /// </summary>
        public double Opacity { get; set; } = 1.0;

        /// <summary>
        /// 把应用层画笔转换成平台原生画笔句柄。
        /// </summary>
        /// <remarks>
        /// 平台后端(Bpf.Windows 等)提供 IPlatformRenderInterface.CreateBrush 的具体实现。
        /// 控件 Render 时把 Brush 传入,由 DrawingContext 物化。
        /// </remarks>
        internal abstract IPlatformBrush ToPlatform(IPlatformRenderInterface render);
    }
}
