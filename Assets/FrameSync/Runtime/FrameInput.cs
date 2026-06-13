using System;

namespace FrameSync
{
    /// <summary>
    /// 单个玩家在一个逻辑帧内的输入。
    /// 只传离散整数，不直接同步 Transform、速度或 Unity 物理状态。
    /// </summary>
    [Serializable]
    public struct FrameInput : IEquatable<FrameInput>
    {
        public static readonly FrameInput Neutral = new FrameInput(0, 0, 0);

        public sbyte Horizontal;
        public sbyte Vertical;
        public byte Buttons;

        public FrameInput(sbyte horizontal, sbyte vertical, byte buttons)
        {
            Horizontal = horizontal;
            Vertical = vertical;
            Buttons = buttons;
        }

        public bool Equals(FrameInput other)
        {
            return Horizontal == other.Horizontal
                   && Vertical == other.Vertical
                   && Buttons == other.Buttons;
        }

        public override bool Equals(object obj)
        {
            return obj is FrameInput other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Horizontal;
                hash = (hash * 397) ^ Vertical;
                hash = (hash * 397) ^ Buttons;
                return hash;
            }
        }
    }
}
