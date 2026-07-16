using Xunit;

namespace Bpf.Markup.Generator.Tests
{
    /// <summary>
    /// 测试控件树实例化 + ContentProperty(阶段 A)。
    /// 验证:<StackPanel><Button/></StackPanel> 生成正确的 new + AddChild 调用。
    /// </summary>
    public class ControlTreeGenerationTests
    {
        [Fact]
        public void SimpleStackPanel_WithButton_GeneratesNewAndAddChild()
        {
            var bpfaml = "<StackPanel x:Class=\"App.Form\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<Button Content=\"OK\"/>" +
                         "</StackPanel>";

            var (generated, _) = GeneratorTestHelper.Run(bpfaml);

            GeneratorTestHelper.AssertContains(generated, "new global::Bpf.Controls.StackPanel()");
            GeneratorTestHelper.AssertContains(generated, "new global::Bpf.Controls.Button()");
            GeneratorTestHelper.AssertContains(generated, ".AddChild(");
        }

        [Fact]
        public void RootElement_GeneratesBuildMethod_ReturningRootType()
        {
            var bpfaml = "<StackPanel x:Class=\"App.Form\" xmlns=\"bpf\" xmlns:x=\"bpf:x\"></StackPanel>";
            var (generated, _) = GeneratorTestHelper.Run(bpfaml);

            GeneratorTestHelper.AssertContains(generated, "public static global::Bpf.Controls.StackPanel Build()");
        }

        [Fact]
        public void MultipleChildren_AllAddedToParent()
        {
            var bpfaml = "<StackPanel x:Class=\"App.Form\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<Label/><TextBlock/><Button/>" +
                         "</StackPanel>";

            var (generated, _) = GeneratorTestHelper.Run(bpfaml);

            Assert.Contains("new global::Bpf.Controls.Label()", generated);
            Assert.Contains("new global::Bpf.Controls.TextBlock()", generated);
            Assert.Contains("new global::Bpf.Controls.Button()", generated);
            // 三个 AddChild
            int addCount = CountOccurrences(generated, ".AddChild(");
            Assert.Equal(3, addCount);
        }

        [Fact]
        public void Border_Child_SetViaChildProperty()
        {
            var bpfaml = "<Border x:Class=\"App.Form\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<Button/>" +
                         "</Border>";

            var (generated, _) = GeneratorTestHelper.Run(bpfaml);

            // Border 是单子容器,用 .Child = 而非 AddChild
            Assert.Contains(".Child = ", generated);
            Assert.DoesNotContain(".AddChild(", generated);
        }

        [Fact]
        public void NestedStackPanel_GeneratesRecursiveTree()
        {
            var bpfaml = "<StackPanel x:Class=\"App.Form\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<StackPanel><Button/></StackPanel>" +
                         "</StackPanel>";

            var (generated, _) = GeneratorTestHelper.Run(bpfaml);

            // 应有两个 StackPanel(外层 + 内层)
            Assert.Equal(2, CountOccurrences(generated, "new global::Bpf.Controls.StackPanel()"));
            Assert.Equal(1, CountOccurrences(generated, "new global::Bpf.Controls.Button()"));
        }

        [Fact]
        public void Button_TextContent_SetsContentProperty()
        {
            var bpfaml = "<StackPanel x:Class=\"App.Form\" xmlns=\"bpf\" xmlns:x=\"bpf:x\">" +
                         "<Button>点击我</Button>" +
                         "</StackPanel>";

            var (generated, _) = GeneratorTestHelper.Run(bpfaml);

            Assert.Contains(".Content = \"点击我\"", generated);
        }

        [Fact]
        public void XClass_DeterminesNamespaceAndClassName()
        {
            var bpfaml = "<StackPanel x:Class=\"MyApp.Views.LoginForm\" xmlns=\"bpf\" xmlns:x=\"bpf:x\"></StackPanel>";
            var (generated, _) = GeneratorTestHelper.Run(bpfaml);

            Assert.Contains("namespace MyApp.Views", generated);
            Assert.Contains("public partial class LoginForm", generated);
        }

        [Fact]
        public void MissingXClass_ReportsDiagnostic()
        {
            var bpfaml = "<StackPanel xmlns=\"bpf\"></StackPanel>";
            var (_, diagnostics) = GeneratorTestHelper.Run(bpfaml);

            // 应报 BPFAML0002(解析错误,缺 x:Class)
            Assert.Contains(diagnostics, d => d.Id == "BPFAML0002");
        }

        private static int CountOccurrences(string haystack, string needle)
        {
            int count = 0, i = 0;
            while ((i = haystack.IndexOf(needle, i)) >= 0) { count++; i += needle.Length; }
            return count;
        }
    }
}
