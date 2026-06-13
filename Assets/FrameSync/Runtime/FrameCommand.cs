using System;

namespace FrameSync
{
    /// <summary>
    /// 网络层需要发送的最小命令数据。
    /// </summary>
    [Serializable]
    public struct FrameCommand
    {
        public int Frame;
        public int PlayerId;
        public FrameInput Input;

        public FrameCommand(int frame, int playerId, FrameInput input)
        {
            Frame = frame;
            PlayerId = playerId;
            Input = input;
        }
    }
}
