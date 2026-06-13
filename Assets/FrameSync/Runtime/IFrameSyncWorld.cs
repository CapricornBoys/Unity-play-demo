using System.Collections.Generic;

namespace FrameSync
{
    /// <summary>
    /// 所有会影响胜负的游戏逻辑都应在该接口的 Step 中推进。
    /// </summary>
    public interface IFrameSyncWorld
    {
        void Step(int frame, IReadOnlyList<FrameCommand> commands);

        /// <summary>
        /// 返回当前逻辑状态哈希，用于服务器或客户端之间检测不同步。
        /// </summary>
        uint CalculateStateHash();
    }
}
