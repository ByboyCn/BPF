using System;
using System.IO;
using System.Linq;
using Bpf.Markup.Generator.Emitter;
using Bpf.Markup.Generator.Parser;
using Microsoft.CodeAnalysis;

namespace Bpf.Markup.Generator
{
    /// <summary>
    /// .bpfaml 增量源生成器入口。
    /// 读取项目中所有 .bpfaml 文件(经 AdditionalFiles 注入),解析为 BpfamlDocument,
    /// 发射成对应的 partial class C# 源,加入编译。
    ///
    /// AOT 纪律:生成的代码全是直接调用,零反射;生成的 partial 类用 global:: 全限定名。
    /// </summary>
    [Generator]
    public sealed class BpfamlGenerator : IIncrementalGenerator
    {
        private const string FileExtension = ".bpfaml";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 1. 选出所有 .bpfaml AdditionalFiles
            var bpfamlFiles = context.AdditionalTextsProvider
                .Where(t => t.Path.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase));

            // 2. 取 (path, text) 对。text 为 null 时(读失败)跳过。
            var docs = bpfamlFiles.Select((text, ct) => (Path: text.Path, Content: text.GetText(ct)?.ToString()));

            // 3. 注册输出。按文件逐个生成。
            context.RegisterSourceOutput(docs, (spc, item) =>
            {
                if (item.Content == null)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        s_errFileRead, Location.None,
                        $"无法读取 .bpfaml 文件:{item.Path}"));
                    return;
                }

                try
                {
                    var doc = new BpfamlParser().Parse(item.Content, item.Path);
                    var (hintName, source) = new BpfamlEmitter().Emit(doc);
                    spc.AddSource(hintName, source);
                }
                catch (BpfamlParseException ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        s_errParse, Location.None,
                        $"解析 .bpfaml 失败:{ex.Message} (文件:{Path.GetFileName(ex.FilePath)} 行:{ex.Line})"));
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        s_errInternal, Location.None,
                        $"生成器内部错误:{ex.Message}"));
                }
            });
        }

        // ── 诊断描述符 ──
        // 注:阶段 A 用 Location.None(不定位到具体行列)。后续可结合 XmlReader 的行号
        // 用 AdditionalTextLocation 精确报错。
        private static readonly DiagnosticDescriptor s_errFileRead = new DiagnosticDescriptor(
            id: "BPFAML0001",
            title: "无法读取 bpfaml 文件",
            messageFormat: "{0}",
            category: "BpfamlGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor s_errParse = new DiagnosticDescriptor(
            id: "BPFAML0002",
            title: "bpfaml 解析错误",
            messageFormat: "{0}",
            category: "BpfamlGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor s_errInternal = new DiagnosticDescriptor(
            id: "BPFAML9999",
            title: "bpfaml 生成器内部错误",
            messageFormat: "{0}",
            category: "BpfamlGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
