using System;
using Bpf.Media;

namespace Bpf.Platform
{
    /// <summary>
    /// 绘图上下文:一帧内所有绘制命令的入口。后端包装 D2D1 DrawingContext。
    /// 控件 <c>Render</c> 方法接收此接口,完全感知不到底层是 D2D1 还是 SK。
    /// </summary>
    public interface IDrawingContext : IDisposable
    {
        /// <summary>清空为指定颜色。</summary>
        void Clear(Color color);

        /// <summary>用画笔填充矩形。</summary>
        void FillRectangle(Rect rect, IPlatformBrush brush);

        /// <summary>
        /// 用画笔填充三角形(三个顶点)。用于自绘图标(如展开/收起指示符),避免 Unicode 符号缺字。
        /// </summary>
        void FillTriangle(Point p1, Point p2, Point p3, IPlatformBrush brush);

        /// <summary>用画笔绘制矩形描边。</summary>
        void DrawRectangle(Rect rect, IPlatformBrush brush, double strokeWidth);

        /// <summary>绘制圆角矩形描边。</summary>
        void DrawRoundedRectangle(Rect rect, double radiusX, double radiusY,
            IPlatformBrush brush, double strokeWidth);

        /// <summary>填充圆角矩形。</summary>
        void FillRoundedRectangle(Rect rect, double radiusX, double radiusY,
            IPlatformBrush brush);

        /// <summary>
        /// 绘制单行文本,左上角对齐到 <paramref name="origin"/>。
        /// </summary>
        void DrawText(Point origin, string text, IPlatformTextFormat format,
            IPlatformBrush foreground);

        /// <summary>测量指定文本格式下、单行文本的自然尺寸。</summary>
        Size MeasureText(string text, IPlatformTextFormat format);

        /// <summary>绘制位图到指定目标矩形(自动缩放)。</summary>
        void DrawImage(IPlatformBitmap bitmap, Rect destRect);

        /// <summary>
        /// 推入裁剪矩形(后续绘制仅在该矩形内可见)。必须与对应 Pop 配对。
        /// </summary>
        void PushClip(Rect clip);

        /// <summary>弹出最近一次 PushClip。</summary>
        void PopClip();

        /// <summary>推入平移变换。必须与对应 Pop 配对。</summary>
        void PushTranslate(Vector offset);

        /// <summary>弹出最近一次 PushTranslate。</summary>
        void PopTransform();
    }
}
