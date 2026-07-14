using System;

namespace Bpf.Layout
{
    /// <summary>
    /// Grid 行/列尺寸单位类型。
    /// </summary>
    public enum GridUnitType
    {
        /// <summary>固定像素值(如 "100")。</summary>
        Pixel,

        /// <summary>自动:按子控件自然尺寸(如 "auto")。</summary>
        Auto,

        /// <summary>按比例瓜分剩余空间(如 "*" 或 "2*")。</summary>
        Star,
    }

    /// <summary>
    /// Grid 行/列的长度定义。值类型,不可变。
    /// 三种模式:Pixel(固定)、Auto(自适应内容)、Star(按比例)。
    /// </summary>
    public readonly struct GridLength : IEquatable<GridLength>
    {
        public GridUnitType UnitType { get; }
        public double Value { get; }

        public GridLength(double value, GridUnitType unitType)
        {
            if (value < 0 || double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentException("GridLength.Value 必须为有限非负数。", nameof(value));
            UnitType = unitType;
            Value = value;
        }

        public GridLength(double pixels) : this(pixels, GridUnitType.Pixel) { }

        /// <summary>是否为 Auto 单位。</summary>
        public bool IsAuto => UnitType == GridUnitType.Auto;

        /// <summary>是否为 Star 单位。</summary>
        public bool IsStar => UnitType == GridUnitType.Star;

        /// <summary>是否为 Pixel 单位。</summary>
        public bool IsAbsolute => UnitType == GridUnitType.Pixel;

        // 预定义
        public static GridLength Auto => new GridLength(0, GridUnitType.Auto);
        public static GridLength Star => new GridLength(1, GridUnitType.Star);
        public static GridLength Zero => new GridLength(0, GridUnitType.Pixel);

        public bool Equals(GridLength other) =>
            UnitType == other.UnitType && Value.Equals(other.Value);

        public override bool Equals(object? obj) => obj is GridLength g && Equals(g);
        public override int GetHashCode() => HashCode.Combine(UnitType, Value);

        public static bool operator ==(GridLength left, GridLength right) => left.Equals(right);
        public static bool operator !=(GridLength left, GridLength right) => !left.Equals(right);

        public override string ToString()
        {
            return UnitType switch
            {
                GridUnitType.Auto => "Auto",
                GridUnitType.Star => Value == 1 ? "*" : Value.ToString("G") + "*",
                GridUnitType.Pixel => Value.ToString("G"),
                _ => Value.ToString("G"),
            };
        }

        /// <summary>
        /// 从单个 token 解析(如 "auto"、"*"、"2*"、"100"、"1.5*")。
        /// </summary>
        public static GridLength Parse(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return Auto;

            token = token.Trim();
            if (token.Equals("auto", StringComparison.OrdinalIgnoreCase))
                return Auto;

            if (token == "*")
                return Star;

            if (token.EndsWith("*"))
            {
                var coefStr = token.Substring(0, token.Length - 1).Trim();
                if (string.IsNullOrEmpty(coefStr)) return Star;
                if (double.TryParse(coefStr, out var coef) && coef > 0)
                    return new GridLength(coef, GridUnitType.Star);
                return Star;
            }

            if (double.TryParse(token, out var px) && px >= 0)
                return new GridLength(px, GridUnitType.Pixel);

            // 无法解析,回落到 Auto
            return Auto;
        }

        /// <summary>
        /// 从逗号分隔字符串解析多个长度(如 "auto,*,100,2*" → 4 个 GridLength)。
        /// </summary>
        public static GridLength[] ParseAll(string definition)
        {
            if (string.IsNullOrWhiteSpace(definition))
                return Array.Empty<GridLength>();

            var parts = definition.Split(',');
            var result = new GridLength[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                result[i] = Parse(parts[i]);
            return result;
        }
    }
}
