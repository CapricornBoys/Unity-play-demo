using System;

namespace FrameSync
{
    /// <summary>
    /// 简单的三位小数定点数。
    /// 帧同步逻辑中避免使用 float，降低不同平台浮点误差导致状态不一致的风险。
    /// </summary>
    [Serializable]
    public readonly struct Fixed64 : IComparable<Fixed64>, IEquatable<Fixed64>
    {
        public const long Scale = 1000L;

        public static readonly Fixed64 Zero = new Fixed64(0L);
        public static readonly Fixed64 One = new Fixed64(Scale);

        public long RawValue { get; }

        private Fixed64(long rawValue)
        {
            RawValue = rawValue;
        }

        public static Fixed64 FromRaw(long rawValue)
        {
            return new Fixed64(rawValue);
        }

        public static Fixed64 FromInt(int value)
        {
            return new Fixed64(value * Scale);
        }

        public static Fixed64 FromRatio(long numerator, long denominator)
        {
            if (denominator == 0)
            {
                throw new DivideByZeroException();
            }

            return new Fixed64(numerator * Scale / denominator);
        }

        /// <summary>
        /// 只允许表现层读取 float，不要把结果写回帧同步逻辑。
        /// </summary>
        public float ToFloat()
        {
            return RawValue / (float)Scale;
        }

        public int CompareTo(Fixed64 other)
        {
            return RawValue.CompareTo(other.RawValue);
        }

        public bool Equals(Fixed64 other)
        {
            return RawValue == other.RawValue;
        }

        public override bool Equals(object obj)
        {
            return obj is Fixed64 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return RawValue.GetHashCode();
        }

        public override string ToString()
        {
            return (RawValue / (double)Scale).ToString("0.###");
        }

        public static Fixed64 operator +(Fixed64 left, Fixed64 right)
        {
            return FromRaw(left.RawValue + right.RawValue);
        }

        public static Fixed64 operator -(Fixed64 left, Fixed64 right)
        {
            return FromRaw(left.RawValue - right.RawValue);
        }

        public static Fixed64 operator *(Fixed64 left, Fixed64 right)
        {
            return FromRaw(left.RawValue * right.RawValue / Scale);
        }

        public static Fixed64 operator /(Fixed64 left, Fixed64 right)
        {
            if (right.RawValue == 0)
            {
                throw new DivideByZeroException();
            }

            return FromRaw(left.RawValue * Scale / right.RawValue);
        }

        public static bool operator ==(Fixed64 left, Fixed64 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Fixed64 left, Fixed64 right)
        {
            return !left.Equals(right);
        }
    }
}
