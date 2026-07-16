using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Bpf.Markup.Generator.Model;

namespace Bpf.Markup.Generator.Parser
{
    /// <summary>
    /// .bpfaml 文本 → BpfamlDocument。
    /// 用 XmlReader 流式解析(轻量、单向,匹配 UI 树自上而下构造的天然结构)。
    /// 仅识别 .bpfaml 子集:元素、属性、属性元素(Owner.Property)、x:Name/x:Class 指令。
    /// </summary>
    internal sealed class BpfamlParser
    {
        /// <summary>解析 .bpfaml 文本。失败抛 BpfamlParseException(由生成器转成诊断)。</summary>
        public BpfamlDocument Parse(string text, string filePath)
        {
            var doc = new BpfamlDocument();
            var settings = new XmlReaderSettings
            {
                XmlResolver = null,
                DtdProcessing = DtdProcessing.Ignore,
                IgnoreComments = true,
                IgnoreWhitespace = true,
            };

            using (var reader = XmlReader.Create(new StringReader(text), settings, filePath))
            {
                // 进到根元素
                while (reader.Read() && reader.NodeType != XmlNodeType.Element) { }
                if (reader.NodeType != XmlNodeType.Element)
                    throw new BpfamlParseException("文档为空,缺少根元素。", filePath, 0, 0);

                doc.Root = ParseElement(reader, doc, filePath);
            }

            // 根元素的 x:Class 必填
            var classAttr = FindAttribute(doc.Root.Attributes, "x:Class")
                         ?? FindAttribute(doc.Root.Attributes, "Class");
            if (classAttr == null)
                throw new BpfamlParseException(
                    $"根元素必须声明 x:Class=\"命名空间.类名\"。文件:{filePath}", filePath, 0, 0);
            doc.ClassFullName = classAttr.Value;

            // 收集 xmlns 前缀映射(prefix → 命名空间)。支持 "using:N" 和 "clr-namespace:N;assembly:A" 两种。
            // XmlReader 把 xmlns 当成名为 "xmlns" 或 "xmlns:prefix" 的属性。
            var nsMap = new Dictionary<string, string>();
            foreach (var attr in doc.Root.Attributes)
            {
                if (attr.Name == "xmlns") continue; // 默认命名空间,本版固定映射到 Bpf.Controls
                if (attr.Name.StartsWith("xmlns:"))
                {
                    string prefix = attr.Name.Substring("xmlns:".Length);
                    string ns = attr.Value;
                    // using:N → N;clr-namespace:N;assembly:A → N
                    if (ns.StartsWith("using:")) ns = ns.Substring("using:".Length);
                    else if (ns.StartsWith("clr-namespace:"))
                    {
                        int semi = ns.IndexOf(';');
                        ns = semi > 0 ? ns.Substring("clr-namespace:".Length, semi - "clr-namespace:".Length)
                                      : ns.Substring("clr-namespace:".Length);
                    }
                    nsMap[prefix] = ns;
                }
            }

            // x:DataType:解析为全限定名(支持 "vm:MainViewModel" 前缀形式)
            var dataTypeAttr = FindAttribute(doc.Root.Attributes, "x:DataType")
                            ?? FindAttribute(doc.Root.Attributes, "DataType");
            if (dataTypeAttr != null)
                doc.DataTypeFullName = ResolveTypeName(dataTypeAttr.Value, nsMap);

            // 从 doc.Root.Attributes 移除 xmlns、x:Class、x:DataType(都不是普通属性)
            doc.Root.Attributes.RemoveAll(IsSpecialRootAttr);

            return doc;
        }

        private static bool IsSpecialRootAttr(BpfamlAttribute a)
        {
            if (a.Name == "xmlns" || a.Name.StartsWith("xmlns:")) return true;
            if (a.Name == "x:Class" || a.Name == "Class") return true;
            if (a.Name == "x:DataType" || a.Name == "DataType") return true;
            return false;
        }

        /// <summary>把带前缀的类型名(如 "vm:MainViewModel")解析为全限定名。</summary>
        private static string ResolveTypeName(string typeName, Dictionary<string, string> nsMap)
        {
            int colon = typeName.IndexOf(':');
            if (colon <= 0) return typeName; // 无前缀,原样
            string prefix = typeName.Substring(0, colon);
            string local = typeName.Substring(colon + 1);
            return nsMap.TryGetValue(prefix, out var ns) ? ns + "." + local : typeName;
        }

        private static bool IsClassAttr(BpfamlAttribute a) => a.Name == "x:Class" || a.Name == "Class";

        /// <summary>递归解析当前 reader 所在的元素。reader 定位在 Element 起始。</summary>
        private BpfamlElement ParseElement(XmlReader reader, BpfamlDocument doc, string filePath)
        {
            string name = reader.LocalName;
            var element = new BpfamlElement { LocalName = name };

            if (!KnownTypes.TryResolve(name, out var fullName))
            {
                throw new BpfamlParseException(
                    $"未知元素 '{name}'。默认命名空间下未注册该类型。",
                    filePath, LineOf(reader), ColOf(reader));
            }
            element.FullTypeName = fullName;

            // 读取属性(特性)
            bool isEmpty = reader.IsEmptyElement;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    var attr = new BpfamlAttribute { Name = reader.Name, Value = reader.Value };
                    // x:Name 登记
                    if (reader.LocalName == "Name" && reader.Prefix == "x")
                        doc.NamedElements.Add((reader.Value, fullName));
                    element.Attributes.Add(attr);
                }
                reader.MoveToElement();
            }

            if (isEmpty)
                return element;

            // 读取内容:子元素、属性元素、文本
            int parseDepth = reader.Depth;
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement)
                {
                    if (reader.Depth <= parseDepth) break;
                    continue;
                }

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName.IndexOf('.') > 0)
                    {
                        // 属性元素 Owner.Property
                        var propNode = ParsePropertyElement(reader, doc, filePath);
                        element.Children.Add(propNode);
                    }
                    else
                    {
                        var child = ParseElement(reader, doc, filePath);
                        element.Children.Add(new BpfamlElementNode { Element = child });
                    }
                }
                else if (reader.NodeType == XmlNodeType.Text)
                {
                    var text = reader.Value.Trim();
                    if (text.Length > 0)
                        element.Attributes.Add(new BpfamlAttribute { Name = "__textContent", Value = text });
                }
            }

            return element;
        }

        /// <summary>解析属性元素 &lt;Owner.Property&gt;。reader 在 Element 起始。</summary>
        private BpfamlPropertyElementNode ParsePropertyElement(XmlReader reader, BpfamlDocument doc, string filePath)
        {
            int dot = reader.LocalName.IndexOf('.');
            string owner = reader.LocalName.Substring(0, dot);
            string prop = reader.LocalName.Substring(dot + 1);

            var node = new BpfamlPropertyElementNode { OwnerLocalName = owner, PropertyName = prop };

            bool isEmpty = reader.IsEmptyElement;
            if (isEmpty) return node;

            int depth = reader.Depth;
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement)
                {
                    if (reader.Depth <= depth) break;
                    continue;
                }
                if (reader.NodeType == XmlNodeType.Element)
                {
                    var child = ParseElement(reader, doc, filePath);
                    if (node.ValueElement == null)
                        node.ValueElement = child;
                    else
                        node.CollectionItems.Add(child);
                }
            }
            return node;
        }

        private static int LineOf(XmlReader r) => ((IXmlLineInfo)r).LineNumber;
        private static int ColOf(XmlReader r) => ((IXmlLineInfo)r).LinePosition;

        private static BpfamlAttribute? FindAttribute(List<BpfamlAttribute> attrs, string name)
        {
            for (int i = 0; i < attrs.Count; i++)
                if (attrs[i].Name == name) return attrs[i];
            return null;
        }
    }

    internal sealed class BpfamlParseException : Exception
    {
        public string FilePath { get; }
        public int Line { get; }
        public int Column { get; }

        public BpfamlParseException(string message, string filePath, int line, int column)
            : base(message)
        {
            FilePath = filePath;
            Line = line;
            Column = column;
        }
    }
}
