using Bpf.Controls;
using Xunit;

namespace Bpf.Tests.Controls
{
    public class LogicalTreeTests
    {
        [Fact]
        public void AddChild_SetsParent()
        {
            var panel = new StackPanel();
            var btn = new Button();
            panel.AddChild(btn);
            Assert.Same(panel, btn.Parent);
        }

        [Fact]
        public void AddChild_AppearsInChildren()
        {
            var panel = new StackPanel();
            var a = new Button();
            var b = new Button();
            panel.AddChild(a);
            panel.AddChild(b);
            Assert.Equal(2, panel.Children.Count);
            Assert.Same(a, panel.Children[0]);
            Assert.Same(b, panel.Children[1]);
        }

        [Fact]
        public void RemoveChild_ClearsParent()
        {
            var panel = new StackPanel();
            var btn = new Button();
            panel.AddChild(btn);
            panel.RemoveChild(btn);
            Assert.Empty(panel.Children);
            Assert.Null(btn.Parent);
        }

        [Fact]
        public void Grid_IsPanel_AcceptsChildren()
        {
            var grid = new Grid();
            var btn = new Button();
            grid.AddChild(btn);
            Assert.Single(grid.Children);
            Assert.Same(grid, btn.Parent);
        }

        [Fact]
        public void NestedPanels_ParentChain()
        {
            var outer = new StackPanel();
            var inner = new StackPanel();
            var btn = new Button();
            outer.AddChild(inner);
            inner.AddChild(btn);
            Assert.Same(inner, btn.Parent);
            Assert.Same(outer, inner.Parent);
        }

        [Fact]
        public void Grid_AttachedProps_TrackPerChild()
        {
            var grid = new Grid();
            var a = new Button();
            var b = new Button();
            grid.AddChild(a);
            grid.AddChild(b);
            Grid.SetRow(a, 0);
            Grid.SetRow(b, 1);
            Assert.Equal(0, Grid.GetRow(a));
            Assert.Equal(1, Grid.GetRow(b));  // 各自独立
        }
    }
}
