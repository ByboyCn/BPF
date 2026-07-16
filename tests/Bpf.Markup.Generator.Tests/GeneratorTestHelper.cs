using System.Text;
using Bpf.Markup.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Bpf.Markup.Generator.Tests
{
    /// <summary>
    /// 源生成器测试辅助。封装 CSharpGeneratorDriver 驱动 + .bpfaml 文件注入 + 结果断言。
    /// </summary>
    internal static class GeneratorTestHelper
    {
        /// <summary>运行生成器,返回生成的所有源代码(合并成一个字符串,便于断言)。</summary>
        public static (string generatedSource, System.Collections.Immutable.ImmutableArray<Diagnostic> diagnostics)
            Run(string bpfamlContent, string fileName = "TestForm.bpfaml")
        {
            var additionalText = new TestAdditionalText(fileName, bpfamlContent);

            // 最小编译单元(空程序集)。生成器只读 AdditionalFiles,不需要 Bpf 框架引用。
            var compilation = CSharpCompilation.Create(
                assemblyName: "Tests",
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText("// 空测试程序集") },
                references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

            var generator = new BpfamlGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator)
                .AddAdditionalTexts(System.Collections.Immutable.ImmutableArray.Create<AdditionalText>(additionalText));

            driver = driver.RunGenerators(compilation);
            var result = driver.GetRunResult();

            // 合并所有生成的源
            var sb = new StringBuilder();
            foreach (var tree in result.GeneratedTrees)
                sb.AppendLine(tree.GetText().ToString());

            return (sb.ToString(), result.Diagnostics);
        }

        /// <summary>断言生成的源代码包含指定子串。</summary>
        public static void AssertContains(string generated, string expected)
            => Assert.Contains(expected, generated);
    }

    /// <summary>测试用的 AdditionalText 桩:模拟一个 .bpfaml 文件。</summary>
    internal sealed class TestAdditionalText : AdditionalText
    {
        private readonly SourceText _text;
        public TestAdditionalText(string path, string content)
        {
            Path = path;
            _text = SourceText.From(content, Encoding.UTF8);
        }
        public override string Path { get; }
        public override SourceText? GetText(System.Threading.CancellationToken cancellationToken = default)
            => _text;
    }
}
