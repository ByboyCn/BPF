using System;
using Bpf.Controls.Routing;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 颜色选择器:点击弹出预设颜色网格,选一种颜色。
    /// SelectedColor 属性持有当前颜色,Changed 事件通知变化。
    /// </summary>
    public sealed class ColorPicker : Control
    {
        public static readonly StyledProperty<Color> SelectedColorProperty =
            StyledProperty<Color>.Register<ColorPicker>(nameof(SelectedColor),
                Color.FromRgb(0xFF, 0xFF, 0xFF), affectsRender: true);
        public Color SelectedColor
        {
            get => GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public static readonly StyledProperty<Brush> BorderBrushProperty =
            StyledProperty<Brush>.Register<ColorPicker>(nameof(BorderBrush),
                new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)), affectsRender: true);
        public Brush BorderBrush
        {
            get => GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        /// <summary>颜色变化时触发。</summary>
        public event EventHandler<RoutedEventArgs>? ColorChanged
        {
            add => AddHandler(ColorChangedEvent, value!);
            remove => RemoveHandler(ColorChangedEvent, value!);
        }
        public static readonly RoutedEvent<RoutedEventArgs> ColorChangedEvent =
            RoutedEvent<RoutedEventArgs>.Register<ColorPicker>(nameof(ColorChanged), RoutingStrategies.Bubble);

        private bool _isOpen;
        private int _hoverCell = -1;
        private IPlatformTextFormat? _format;

        // 预设调色板(8 列 × 5 行 = 40 色)
        private static readonly Color[] Palette = BuildPalette();
        private const double SwatchSize = 18;
        private const double SwatchGap = 2;
        private const double BoxH = 24;
        private const int PaletteCols = 8;

        private static Color[] BuildPalette()
        {
            var list = new System.Collections.Generic.List<Color>();
            // 标准色:黑/白/灰系 + 红橙黄绿青蓝紫粉棕
            byte[][] rows = {
                new byte[] { 0x00, 0x40, 0x80, 0xC0, 0xFF }, // 灰阶
            };
            // 用 HSL 生成常见颜色
            uint[] presets = {
                0x000000, 0x404040, 0x808080, 0xC0C0C0, 0xFFFFFF, 0xFF0000, 0x800000, 0xFFFF00,
                0x808000, 0x00FF00, 0x008000, 0x00FFFF, 0x008080, 0x0000FF, 0x000080, 0xFF00FF,
                0x800080, 0xFFA500, 0x8B4513, 0xA52A2A, 0xD2691E, 0xCD5C5C, 0xF08080, 0xFA8072,
                0x90EE90, 0x98FB98, 0x87CEEB, 0xADD8E6, 0xDDA0DD, 0xEE82EE, 0xFFB6C1, 0xFFC0CB,
                0x2D7FF9, 0x4A9CFF, 0x3399FF, 0x0070C0, 0xE53935, 0x43A047, 0xFBC02D, 0x8E24AA,
            };
            var arr = new Color[presets.Length];
            for (int i = 0; i < presets.Length; i++)
                arr[i] = Color.FromRgb((byte)((presets[i] >> 16) & 0xFF), (byte)((presets[i] >> 8) & 0xFF), (byte)(presets[i] & 0xFF));
            return arr;
        }

        public ColorPicker() { }

        protected override Size MeasureCore(Size availableSize)
        {
            return new Size(60, BoxH);
        }

        protected override void ArrangeCore(Rect finalRect) => Bounds = finalRect;

        public override void Render(IDrawingContext context)
        {
            var render = Bpf.Application.Application.Current.RenderInterface;

            // 色块 + 边框
            var sel = new SolidColorBrush(SelectedColor).ToPlatform(render);
            var border = BorderBrush.ToPlatform(render);
            try
            {
                context.FillRectangle(new Rect(0, 0, Bounds.Width, BoxH), sel);
                context.DrawRectangle(new Rect(0.5, 0.5, Bounds.Width - 1, BoxH - 1), border, 1.0);
            }
            finally { sel.Dispose(); border.Dispose(); }

            // 下拉调色板
            if (_isOpen)
            {
                int rows = (Palette.Length + PaletteCols - 1) / PaletteCols;
                double palW = PaletteCols * (SwatchSize + SwatchGap) + SwatchGap;
                double palH = rows * (SwatchSize + SwatchGap) + SwatchGap;
                context.PushTranslate(new Vector(0, BoxH + 2));
                try
                {
                    var bg = new SolidColorBrush(Color.White).ToPlatform(render);
                    var bd = BorderBrush.ToPlatform(render);
                    try
                    {
                        context.FillRectangle(new Rect(0, 0, palW, palH), bg);
                        context.DrawRectangle(new Rect(0.5, 0.5, palW - 1, palH - 1), bd, 1.0);
                    }
                    finally { bg.Dispose(); bd.Dispose(); }

                    for (int i = 0; i < Palette.Length; i++)
                    {
                        int col = i % PaletteCols, row = i / PaletteCols;
                        double x = SwatchGap + col * (SwatchSize + SwatchGap);
                        double y = SwatchGap + row * (SwatchSize + SwatchGap);
                        var swatch = new SolidColorBrush(Palette[i]).ToPlatform(render);
                        try
                        {
                            context.FillRectangle(new Rect(x, y, SwatchSize, SwatchSize), swatch);
                            if (i == _hoverCell)
                                context.DrawRectangle(new Rect(x + 0.5, y + 0.5, SwatchSize - 1, SwatchSize - 1), border, 1.5);
                        }
                        finally { swatch.Dispose(); }
                    }
                }
                finally { context.PopTransform(); }
            }
        }

        public override void OnPointerPressed(PointerEventArgs e)
        {
            if (!_isOpen && e.Position.Y < BoxH)
            {
                _isOpen = true;
                RenderOnTop = true; // 展开调色板时浮在最上层
                InvalidateVisual();
                e.Handled = true;
                return;
            }
            if (_isOpen)
            {
                // 点击调色板
                double relY = e.Position.Y - BoxH - 2;
                double relX = e.Position.X;
                if (relY >= 0)
                {
                    int col = (int)((relX - SwatchGap) / (SwatchSize + SwatchGap));
                    int row = (int)((relY - SwatchGap) / (SwatchSize + SwatchGap));
                    if (col >= 0 && col < PaletteCols)
                    {
                        int idx = row * PaletteCols + col;
                        if (idx >= 0 && idx < Palette.Length)
                        {
                            SelectedColor = Palette[idx];
                            RaiseEvent(ColorChangedEvent, new RoutedEventArgs());
                        }
                    }
                }
                _isOpen = false;
                RenderOnTop = false;
                InvalidateVisual();
                e.Handled = true;
            }
        }

        public override void OnPointerMoved(PointerEventArgs e)
        {
            if (!_isOpen) return;
            double relY = e.Position.Y - BoxH - 2;
            int col = (int)((e.Position.X - SwatchGap) / (SwatchSize + SwatchGap));
            int row = (int)((relY - SwatchGap) / (SwatchSize + SwatchGap));
            int idx = (col >= 0 && col < PaletteCols && row >= 0) ? row * PaletteCols + col : -1;
            if (idx != _hoverCell) { _hoverCell = idx; InvalidateVisual(); }
        }

        protected internal override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            if (_hoverCell != -1) { _hoverCell = -1; InvalidateVisual(); }
        }

        public override bool HitTest(Point point) => IsVisible && Bounds.Contains(point);

        /// <summary>扩展命中:调色板打开时,下拉区域也算命中。</summary>
        public override bool HitTestExtended(Point windowPoint)
        {
            if (!IsVisible) return false;
            if (Bounds.Contains(windowPoint)) return true;
            if (_isOpen)
            {
                int rows = (Palette.Length + PaletteCols - 1) / PaletteCols;
                double palW = PaletteCols * (SwatchSize + SwatchGap) + SwatchGap;
                double palH = rows * (SwatchSize + SwatchGap) + SwatchGap;
                var palRect = new Rect(Bounds.X, Bounds.Y + BoxH + 2, palW, palH);
                if (palRect.Contains(windowPoint)) return true;
            }
            return false;
        }
    }
}
