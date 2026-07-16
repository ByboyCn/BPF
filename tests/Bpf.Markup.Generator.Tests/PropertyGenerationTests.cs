using Xunit;

namespace Bpf.Markup.Generator.Tests
{
    /// <summary>
    /// 测试属性转换器 + 附加属性 + x:Name(阶段 B)。
    /// 验证:FontSize(double)、Background(Brush)、Grid.Column(附加)、x:Name(字段)的代码生成。
    /// </summary>
    public class PropertyGenerationTests
    {
        [Fact]
        public void StringProperty_GeneratesLiteralAssignment()
        {
            var bpfaml = "<StackPanel x:Class=\"App.F\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<Label Text=\"你好\"/>" +
                         "</StackPanel>";
            var (g, _) = GeneratorTestHelper.Run(bpfaml);
            Assert.Contains(".Text = \"你好\"", g);
        }

        [Fact]
        public void DoubleProperty_GeneratesNumericLiteral()
        {
            var bpfaml = "<StackPanel x:Class=\"App.F\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<TextBlock FontSize=\"16\"/>" +
                         "</StackPanel>";
            var (g, _) = GeneratorTestHelper.Run(bpfaml);
            Assert.Contains(".FontSize = 16", g);
        }

        [Fact]
        public void BoolProperty_GeneratesBoolLiteral()
        {
            var bpfaml = "<StackPanel x:Class=\"App.F\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<CheckBox IsChecked=\"true\"/>" +
                         "</StackPanel>";
            var (g, _) = GeneratorTestHelper.Run(bpfaml);
            Assert.Contains(".IsChecked = true", g);
        }

        [Fact]
        public void BrushProperty_NamedColor_GeneratesSolidColorBrush()
        {
            var bpfaml = "<StackPanel x:Class=\"App.F\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<Button Background=\"Red\"/>" +
                         "</StackPanel>";
            var (g, _) = GeneratorTestHelper.Run(bpfaml);
            Assert.Contains("new global::Bpf.Media.SolidColorBrush(global::Bpf.Media.Color.Parse(\"Red\"))", g);
        }

        [Fact]
        public void BrushProperty_HexColor_GeneratesParse()
        {
            var bpfaml = "<StackPanel x:Class=\"App.F\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<Button Background=\"#FF8800\"/>" +
                         "</StackPanel>";
            var (g, _) = GeneratorTestHelper.Run(bpfaml);
            Assert.Contains("Color.Parse(\"#FF8800\")", g);
        }

        [Fact]
        public void EnumProperty_GeneratesEnumMember()
        {
            var bpfaml = "<StackPanel x:Class=\"App.F\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<StackPanel Orientation=\"Horizontal\"/>" +
                         "</StackPanel>";
            var (g, _) = GeneratorTestHelper.Run(bpfaml);
            Assert.Contains("global::Bpf.Controls.Orientation.Horizontal", g);
        }

        [Fact]
        public void AttachedProperty_GridColumn_GeneratesSetColumn()
        {
            var bpfaml = "<StackPanel x:Class=\"App.F\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<Button Grid.Column=\"2\"/>" +
                         "</StackPanel>";
            var (g, _) = GeneratorTestHelper.Run(bpfaml);
            Assert.Contains("global::Bpf.Controls.Grid.SetColumn(", g);
            Assert.Contains(", 2)", g);
        }

        [Fact]
        public void AttachedProperty_GridRow_GeneratesSetRow()
        {
            var bpfaml = "<StackPanel x:Class=\"App.F\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<Button Grid.Row=\"3\" Grid.ColumnSpan=\"2\"/>" +
                         "</StackPanel>";
            var (g, _) = GeneratorTestHelper.Run(bpfaml);
            Assert.Contains("Grid.SetRow(", g);
            Assert.Contains("Grid.SetColumnSpan(", g);
        }

        [Fact]
        public void XName_GeneratesStaticField()
        {
            var bpfaml = "<StackPanel x:Class=\"App.F\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<Button x:Name=\"myButton\"/>" +
                         "</StackPanel>";
            var (g, _) = GeneratorTestHelper.Run(bpfaml);
            // 静态字段声明
            Assert.Contains("internal static global::Bpf.Controls.Button? myButton;", g);
            // Build 内赋值(类名 F,在 namespace App 内)
            Assert.Contains("F.myButton = new global::Bpf.Controls.Button()", g);
        }

        [Fact]
        public void XName_UsedAsVariableReference()
        {
            var bpfaml = "<StackPanel x:Class=\"App.F\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<Button x:Name=\"btn\" Content=\"OK\"/>" +
                         "</StackPanel>";
            var (g, _) = GeneratorTestHelper.Run(bpfaml);
            // 命名元素的属性设置用 F.btn 而非 var btn1
            Assert.Contains("F.btn.Content = \"OK\"", g);
        }

        [Fact]
        public void GridLengthString_RowsColumns_PreservedAsLiteral()
        {
            var bpfaml = "<StackPanel x:Class=\"App.F\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<Grid Columns=\"200,*\" Rows=\"Auto,100\"/>" +
                         "</StackPanel>";
            var (g, _) = GeneratorTestHelper.Run(bpfaml);
            Assert.Contains(".Columns = \"200,*\"", g);
            Assert.Contains(".Rows = \"Auto,100\"", g);
        }
    }
}
