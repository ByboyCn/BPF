using System.Collections.Generic;

namespace Bpf.Markup.Generator.Model
{
    /// <summary>
    /// markup extension 解析结果。形如 {Binding Name, Mode=TwoWay} 或 {StaticResource Key}。
    /// 第一个 token 是扩展名(Binding/StaticResource/x:Static/x:Null),
    /// 其余是位置参数 + 命名参数。
    /// </summary>
    internal sealed class MarkupExtension
    {
        /// <summary>扩展名(Binding/StaticResource/...)。</summary>
        public string Name { get; set; } = "";

        /// <summary>位置参数(逗号分隔的非命名 token)。Binding 的第一个位置参数是 Path。</summary>
        public List<string> PositionalArgs { get; } = new List<string>();

        /// <summary>命名参数(key=value)。</summary>
        public Dictionary<string, string> NamedArgs { get; } = new Dictionary<string, string>();

        /// <summary>解析 {扩展 参数} 字符串。输入应已去掉外层花括号。</summary>
        public static MarkupExtension Parse(string content)
        {
            var ext = new MarkupExtension();
            var tokens = Tokenize(content);
            if (tokens.Count == 0) return ext;

            ext.Name = tokens[0];
            for (int i = 1; i < tokens.Count; i++)
            {
                var t = tokens[i];
                int eq = t.IndexOf('=');
                if (eq > 0)
                {
                    string key = t.Substring(0, eq).Trim();
                    string val = t.Substring(eq + 1).Trim();
                    ext.NamedArgs[key] = val;
                }
                else
                {
                    ext.PositionalArgs.Add(t.Trim());
                }
            }
            return ext;
        }

        /// <summary>
        /// 简易分词:按空格和逗号分隔,但保留引号内和花括号内的内容。
        /// {Binding Name, Mode=TwoWay, Converter={StaticResource fmt}} 的输入是
        /// "Binding Name, Mode=TwoWay, Converter={StaticResource fmt}"。
        /// </summary>
        private static List<string> Tokenize(string s)
        {
            var result = new List<string>();
            var sb = new System.Text.StringBuilder();
            int braceDepth = 0;
            bool inQuote = false;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '"' ) inQuote = !inQuote;
                if (c == '{') braceDepth++;
                if (c == '}') braceDepth--;

                if (!inQuote && braceDepth == 0 && (c == ' ' || c == ',' || c == '\t'))
                {
                    if (sb.Length > 0) { result.Add(sb.ToString()); sb.Clear(); }
                }
                else
                {
                    sb.Append(c);
                }
            }
            if (sb.Length > 0) result.Add(sb.ToString());
            return result;
        }
    }
}
