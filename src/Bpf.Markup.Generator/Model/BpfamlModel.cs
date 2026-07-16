using System.Collections.Generic;

namespace Bpf.Markup.Generator.Model
{
    /// <summary>
    /// .bpfaml 文档的中间表示(IR)。解析阶段产出,发射阶段消费。
    /// 刻意与 XML、与 Roslyn 解耦,方便单测和后续扩展。
    /// </summary>
    internal sealed class BpfamlDocument
    {
        /// <summary>x:Class 指定的全限定类名(如 "Bpf.Samples.HelloWorld.MainWindow")。必需。</summary>
        public string ClassFullName { get; set; } = "";

        /// <summary>
        /// x:DataType 指定的 DataContext 类型全名(如 "Bpf.Samples.HelloWorld.MainViewModel")。
        /// 启用编译式 {Binding}:源生成器据此类型生成 vm =&gt; vm.Path 强类型 lambda。
        /// 为空时,{Binding} 会退化为无类型(运行时反射,M6 暂不支持,会发诊断)。
        /// </summary>
        public string DataTypeFullName { get; set; } = "";

        /// <summary>根元素(Window 等)。</summary>
        public BpfamlElement Root { get; set; } = new BpfamlElement();

        /// <summary>文件中所有 x:Name 元素(name → 全限定类型名),用于生成强类型字段。</summary>
        public List<(string Name, string FullTypeName)> NamedElements { get; } = new List<(string, string)>();
    }

    /// <summary>元素节点:一个控件实例。</summary>
    internal sealed class BpfamlElement
    {
        /// <summary>未带前缀的元素本地名(如 "Button"),命名空间已解析为全限定类型名。</summary>
        public string LocalName { get; set; } = "";

        /// <summary>解析后的全限定类型名(如 "Bpf.Controls.Button")。未知则为空。</summary>
        public string FullTypeName { get; set; } = "";

        /// <summary>属性列表(含附加属性、x:Name 等),保持出现顺序。</summary>
        public List<BpfamlAttribute> Attributes { get; } = new List<BpfamlAttribute>();

        /// <summary>
        /// 子内容元素。语义由发射器根据父类型决定:
        /// ContentProperty(Panel→AddChild / Border→Child / Button→Content)、
        /// 属性元素(Owner.Property 语法)、Resources / DataContext / Style 等特殊节点。
        /// </summary>
        public List<BpfamlNode> Children { get; } = new List<BpfamlNode>();
    }

    /// <summary>属性节点:元素特性或属性元素的值。</summary>
    internal sealed class BpfamlAttribute
    {
        /// <summary>属性名(如 "Content"、"FontSize"、或附加属性 "Grid.Column")。</summary>
        public string Name { get; set; } = "";

        /// <summary>原始字符串值(未转换)。如 "OK"、"14"、"{Binding Name}"。</summary>
        public string Value { get; set; } = "";

        /// <summary>值是否是 markup extension(花括号开头),发射时特殊处理。</summary>
        public bool IsMarkupExtension => Value != null && Value.Length > 0 && Value[0] == '{';
    }

    /// <summary>子节点抽象:可能是元素(子控件)或属性元素(Owner.Property)。</summary>
    internal abstract class BpfamlNode { }

    /// <summary>子元素节点(子控件)。</summary>
    internal sealed class BpfamlElementNode : BpfamlNode
    {
        public BpfamlElement Element { get; set; } = new BpfamlElement();
    }

    /// <summary>
    /// 属性元素节点:形如 &lt;Button.Background&gt;...&lt;/Button.Background&gt;,
    /// 内部通常包含一个对象元素作为复杂属性值。
    /// </summary>
    internal sealed class BpfamlPropertyElementNode : BpfamlNode
    {
        /// <summary>属主类型本地名(如 "Button")。</summary>
        public string OwnerLocalName { get; set; } = "";
        /// <summary>属性名(如 "Background")。</summary>
        public string PropertyName { get; set; } = "";
        /// <summary>属性值(通常是一个对象元素,如 SolidColorBrush)。可能为 null(空属性元素)。</summary>
        public BpfamlElement? ValueElement { get; set; }
        /// <summary>若属性元素含多个子元素(如 Resources 字典、Children 集合),用此列表。</summary>
        public List<BpfamlElement> CollectionItems { get; } = new List<BpfamlElement>();
    }
}
