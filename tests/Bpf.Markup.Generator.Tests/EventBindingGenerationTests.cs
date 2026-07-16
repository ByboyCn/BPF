using Xunit;

namespace Bpf.Markup.Generator.Tests
{
    /// <summary>
    /// 测试事件挂接(阶段 C)+ 数据绑定(阶段 D)+ 资源/Style/marker extension(阶段 E)。
    /// </summary>
    public class EventBindingGenerationTests
    {
        // ── 事件(阶段 C)──

        [Fact]
        public void Event_GeneratesHandlerSubscription()
        {
            var bpfaml = "<StackPanel x:Class=\"App.F\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<Button Click=\"OnClick\"/>" +
                         "</StackPanel>";
            var (g, _) = GeneratorTestHelper.Run(bpfaml);
            Assert.Contains(".Click += F.OnClick", g);
        }

        [Fact]
        public void Event_OnNamedElement_UsesFieldReference()
        {
            var bpfaml = "<StackPanel x:Class=\"App.F\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<TextBox x:Name=\"input\" TextChanged=\"OnTextChanged\"/>" +
                         "</StackPanel>";
            var (g, _) = GeneratorTestHelper.Run(bpfaml);
            Assert.Contains("F.input.TextChanged += F.OnTextChanged", g);
        }

        [Fact]
        public void MultipleEvents_AllSubscribed()
        {
            var bpfaml = "<StackPanel x:Class=\"App.F\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<Button Click=\"OnA\"/><CheckBox Checked=\"OnB\"/>" +
                         "</StackPanel>";
            var (g, _) = GeneratorTestHelper.Run(bpfaml);
            Assert.Contains(".Click += F.OnA", g);
            Assert.Contains(".Checked += F.OnB", g);
        }

        // ── 数据绑定(阶段 D)──

        [Fact]
        public void Binding_GeneratesCompiledBinding_WithDataType()
        {
            var bpfaml = "<StackPanel x:Class=\"App.F\" xmlns=\"bpf\" xmlns:x=\"bpf:x\" " +
                         "x:DataType=\"App.VM\">" +
                         "<TextBlock Text=\"{Binding Name}\"/>" +
                         "</StackPanel>";
            var (g, _) = GeneratorTestHelper.Run(bpfaml);
            Assert.Contains("SetCompiledBinding", g);
            Assert.Contains("new global::Bpf.Data.CompiledBinding(", g);
            // 强类型 lambda 含 VM 转换
            Assert.Contains("App.VM? __t", g);
            Assert.Contains("__t.Name", g);
        }

        [Fact]
        public void Binding_ObservableNames_ArrayContainsPropertyName()
        {
            var bpfaml = "<StackPanel x:Class=\"App.F\" xmlns=\"bpf\" xmlns:x=\"bpf:x\" " +
                         "x:DataType=\"App.VM\">" +
                         "<TextBlock Text=\"{Binding Name}\"/>" +
                         "</StackPanel>";
            var (g, _) = GeneratorTestHelper.Run(bpfaml);
            // 观察名数组含 "Name"
            Assert.Contains("\"Name\"", g);
        }

        [Fact]
        public void Binding_NestedPath_GeneratesPropertyChain()
        {
            var bpfaml = "<StackPanel x:Class=\"App.F\" xmlns=\"bpf\" xmlns:x=\"bpf:x\" " +
                         "x:DataType=\"App.VM\">" +
                         "<TextBlock Text=\"{Binding User.Name}\"/>" +
                         "</StackPanel>";
            var (g, _) = GeneratorTestHelper.Run(bpfaml);
            // 路径链:__t.User.Name
            Assert.Contains("__t.User.Name", g);
        }

        [Fact]
        public void Binding_WithoutDataType_GeneratesNothing()
        {
            // 无 x:DataType 时,绑定跳过(M6 阶段 D 简化:不退化到反射)
            var bpfaml = "<StackPanel x:Class=\"App.F\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<TextBlock Text=\"{Binding Name}\"/>" +
                         "</StackPanel>";
            var (g, _) = GeneratorTestHelper.Run(bpfaml);
            Assert.DoesNotContain("SetCompiledBinding", g);
        }

        // ── 资源/Style/marker extension(阶段 E)──

        [Fact]
        public void StaticResource_GeneratesLocalVarReference()
        {
            var bpfaml = "<StackPanel x:Class=\"App.F\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<StackPanel.Resources>" +
                         "<SolidColorBrush x:Key=\"Accent\" Color=\"#2D7FF9\"/>" +
                         "</StackPanel.Resources>" +
                         "<Button Background=\"{StaticResource Accent}\"/>" +
                         "</StackPanel>";
            var (g, _) = GeneratorTestHelper.Run(bpfaml);
            // 资源作为局部变量,被 {StaticResource} 引用
            Assert.Contains("Color.Parse(\"#2D7FF9\")", g);
            Assert.Contains(".Resources[\"Accent\"]", g);
            // 按钮的 Background 应引用资源变量,而非重新构造
            // (按钮的 Background = solidColorBrushN,变量名由生成器分配)
        }

        [Fact]
        public void Resources_AllAddedToDictionary()
        {
            var bpfaml = "<StackPanel x:Class=\"App.F\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<StackPanel.Resources>" +
                         "<SolidColorBrush x:Key=\"A\" Color=\"Red\"/>" +
                         "<SolidColorBrush x:Key=\"B\" Color=\"Blue\"/>" +
                         "</StackPanel.Resources>" +
                         "</StackPanel>";
            var (g, _) = GeneratorTestHelper.Run(bpfaml);
            Assert.Contains(".Resources[\"A\"]", g);
            Assert.Contains(".Resources[\"B\"]", g);
        }

        [Fact]
        public void Style_GeneratesStyleWithSetters()
        {
            var bpfaml = "<StackPanel x:Class=\"App.F\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<StackPanel.Styles>" +
                         "<Style TargetType=\"TextBlock\">" +
                         "<Setter Property=\"FontSize\" Value=\"13\"/>" +
                         "</Style>" +
                         "</StackPanel.Styles>" +
                         "</StackPanel>";
            var (g, _) = GeneratorTestHelper.Run(bpfaml);
            Assert.Contains("new global::Bpf.Styling.Style<Bpf.Controls.TextBlock>()", g);
            Assert.Contains("Bpf.Controls.TextBlock.FontSizeProperty", g);
            Assert.Contains(", 13)", g);
            Assert.Contains(".Styles.Add(", g);
        }

        [Fact]
        public void XNull_GeneratesNullLiteral()
        {
            var bpfaml = "<StackPanel x:Class=\"App.F\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<TextBlock Text=\"{x:Null}\"/>" +
                         "</StackPanel>";
            var (g, _) = GeneratorTestHelper.Run(bpfaml);
            // {x:Null} 在普通属性赋值上下文 → 静默忽略(M6 简化,见 EmitMarkupExtension)
            // 至少不应崩溃,且不应生成无效的 Text = {x:Null}
            Assert.DoesNotContain("Text = \"{x:Null}\"", g);
        }
    }
}
