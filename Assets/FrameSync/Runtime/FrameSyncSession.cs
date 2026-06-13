using System;
using System.Collections.Generic;

namespace FrameSync
{
    /// <summary>
    /// 与 Unity 渲染帧无关的帧同步会话核心。
    /// 收齐当前逻辑帧所有玩家命令后，才允许世界向前推进。
    /// </summary>
    public sealed class FrameSyncSession
    {
        private readonly IFrameSyncWorld world;
        private readonly List<int> playerIds;
        private readonly Dictionary<int, Dictionary<int, FrameCommand>> commandBuffer =
            new Dictionary<int, Dictionary<int, FrameCommand>>();
        private readonly List<FrameCommand> orderedCommands = new List<FrameCommand>();

        public int CurrentFrame { get; private set; }
        public int InputDelay { get; }
        public uint LastStateHash { get; private set; }

        public event Action<int, uint> FrameAdvanced;

        public FrameSyncSession(IFrameSyncWorld world, IEnumerable<int> playerIds, int inputDelay)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.playerIds = new List<int>(playerIds ?? throw new ArgumentNullException(nameof(playerIds)));

            if (this.playerIds.Count == 0)
            {
                throw new ArgumentException("至少需要一个玩家。", nameof(playerIds));
            }

            this.playerIds.Sort();
            InputDelay = Math.Max(0, inputDelay);
        }

        /// <summary>
        /// 将本地输入转换为未来帧命令，预留网络传输时间。
        /// </summary>
        public FrameCommand CreateLocalCommand(int playerId, FrameInput input)
        {
            return new FrameCommand(CurrentFrame + InputDelay, playerId, input);
        }

        /// <summary>
        /// 网络层收到命令后调用。相同玩家、相同帧的命令只保留第一份，防止重复包改变历史。
        /// </summary>
        public bool ReceiveCommand(FrameCommand command)
        {
            if (command.Frame < CurrentFrame || playerIds.BinarySearch(command.PlayerId) < 0)
            {
                return false;
            }

            if (!commandBuffer.TryGetValue(command.Frame, out Dictionary<int, FrameCommand> frameCommands))
            {
                frameCommands = new Dictionary<int, FrameCommand>();
                commandBuffer.Add(command.Frame, frameCommands);
            }

            if (frameCommands.ContainsKey(command.PlayerId))
            {
                return false;
            }

            frameCommands.Add(command.PlayerId, command);
            return true;
        }

        /// <summary>
        /// 为输入延迟造成的开局空白帧填充中立输入。
        /// 正式项目中应由房间服务器统一调用，保证所有客户端起点一致。
        /// </summary>
        public void SeedInitialFrames()
        {
            for (int frame = 0; frame < InputDelay; frame++)
            {
                for (int i = 0; i < playerIds.Count; i++)
                {
                    ReceiveCommand(new FrameCommand(frame, playerIds[i], FrameInput.Neutral));
                }
            }
        }

        public bool CanAdvance()
        {
            if (!commandBuffer.TryGetValue(CurrentFrame, out Dictionary<int, FrameCommand> frameCommands))
            {
                return false;
            }

            for (int i = 0; i < playerIds.Count; i++)
            {
                if (!frameCommands.ContainsKey(playerIds[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 尝试执行一个逻辑帧。命令始终按 PlayerId 排序，避免字典遍历顺序影响结果。
        /// </summary>
        public bool TryAdvance()
        {
            if (!CanAdvance())
            {
                return false;
            }

            Dictionary<int, FrameCommand> frameCommands = commandBuffer[CurrentFrame];
            orderedCommands.Clear();

            for (int i = 0; i < playerIds.Count; i++)
            {
                orderedCommands.Add(frameCommands[playerIds[i]]);
            }

            world.Step(CurrentFrame, orderedCommands);
            LastStateHash = world.CalculateStateHash();
            commandBuffer.Remove(CurrentFrame);

            int completedFrame = CurrentFrame;
            CurrentFrame++;
            FrameAdvanced?.Invoke(completedFrame, LastStateHash);
            return true;
        }
    }
}
