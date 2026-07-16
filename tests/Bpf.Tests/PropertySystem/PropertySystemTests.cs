using Bpf.Controls;
using Bpf.Media;
using Xunit;

namespace Bpf.Tests.PropertySystem
{
    public class PropertySystemTests
    {
        // ── 默认值 ──

        [Fact]
        public void GetValue_ReturnsDefault_WhenNotSet()
        {
            var btn = new Button();
            Assert.Equal("", btn.Content);                         // Content 默认 ""
            Assert.NotNull(btn.Background);                        // Background 有默认画刷
        }

        [Fact]
        public void GetValue_ReturnsDefault_ForCheckBox()
        {
            var cb = new CheckBox();
            Assert.False(cb.IsChecked);                            // IsChecked 默认 false
        }

        // ── SetValue / GetValue 往返 ──

        [Fact]
        public void SetValue_ThenGetValue_Roundtrips()
        {
            var btn = new Button();
            btn.Content = "Hello";
            Assert.Equal("Hello", btn.Content);

            btn.FontSize = 24.0;
            Assert.Equal(24.0, btn.FontSize);
        }

        [Fact]
        public void SetValue_Bool_Roundtrips()
        {
            var cb = new CheckBox();
            cb.IsChecked = true;
            Assert.True(cb.IsChecked);
            cb.IsChecked = false;
            Assert.False(cb.IsChecked);
        }

        // ── SetValue 触发变更(通过 CLR 包装器的副作用间接验证)──
        // Button.Content 是 StyledProperty,affectsMeasure/affectsRender。设值后应标记失效。
        // 这里只验证值确实写入了本地存储(不依赖布局系统)。

        [Fact]
        public void SetValue_OverwritesPrevious()
        {
            var btn = new Button();
            btn.Content = "A";
            btn.Content = "B";
            Assert.Equal("B", btn.Content);
        }

        [Fact]
        public void ClearValue_RestoresDefault()
        {
            var btn = new Button();
            btn.Content = "Changed";
            Assert.Equal("Changed", btn.Content);

            btn.ClearValue(Button.ContentProperty);
            Assert.Equal("", btn.Content);  // 回到默认
        }

        // ── 附加属性(Grid.Row/Column)──

        [Fact]
        public void AttachedProperty_GridRow_SetGet()
        {
            var btn = new Button();
            Assert.Equal(0, Grid.GetRow(btn));      // 默认 0
            Grid.SetRow(btn, 3);
            Assert.Equal(3, Grid.GetRow(btn));
        }

        [Fact]
        public void AttachedProperty_GridColumn_SetGet()
        {
            var btn = new Button();
            Grid.SetColumn(btn, 5);
            Grid.SetColumnSpan(btn, 2);
            Assert.Equal(5, Grid.GetColumn(btn));
            Assert.Equal(2, Grid.GetColumnSpan(btn));
            Assert.Equal(1, Grid.GetRowSpan(btn));  // 默认 1
        }

        [Fact]
        public void AttachedProperty_IndependentFromLocalValue()
        {
            // 附加属性存在子控件上,不影响控件自己的属性
            var btn = new Button();
            btn.Content = "X";
            Grid.SetRow(btn, 2);
            Assert.Equal("X", btn.Content);          // 本地属性不受影响
            Assert.Equal(2, Grid.GetRow(btn));       // 附加属性独立
        }

        // ── 多控件属性独立(验证存储不串)──

        [Fact]
        public void Properties_AreIndependent_BetweenInstances()
        {
            var a = new Button();
            var b = new Button();
            a.Content = "A";
            b.Content = "B";
            Assert.Equal("A", a.Content);
            Assert.Equal("B", b.Content);
        }

        // ── 默认值不可变性(所有实例共享同一默认对象引用,但不被修改)──

        [Fact]
        public void Background_Default_IsConsistent()
        {
            var a = new Button();
            var b = new Button();
            // 默认 Background 是 #DDDDDD 画刷
            var solidA = a.Background as SolidColorBrush;
            var solidB = b.Background as SolidColorBrush;
            Assert.NotNull(solidA);
            Assert.Equal(solidA!.Color, solidB!.Color);
        }
    }
}
