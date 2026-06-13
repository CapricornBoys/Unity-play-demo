using System.Collections.Generic;
using UnityEngine;

namespace FrameSync.Demo
{
    /// <summary>
    /// 本地双玩家帧同步演示。
    /// 挂到空物体后运行：玩家 1 使用 WASD，玩家 2 自动移动。
    /// </summary>
    public sealed class FrameSyncDemo : MonoBehaviour, IFrameSyncWorld
    {
        [Header("逻辑参数")]
        [SerializeField, Min(1)] private int logicFps = 20;
        [SerializeField, Min(0)] private int inputDelay = 2;
        [SerializeField, Min(1)] private int maxCatchUpFrames = 5;
        [SerializeField] private int moveUnitsPerFrame = 1;

        [Header("表现参数")]
        [SerializeField] private Transform playerOneView;
        [SerializeField] private Transform playerTwoView;
        [SerializeField] private float renderScale = 0.1f;

        private readonly Dictionary<int, LogicPlayer> players = new Dictionary<int, LogicPlayer>();
        private FrameSyncSession session;
        private float accumulator;

        private void Awake()
        {
            players.Add(1, new LogicPlayer(1, Fixed64.FromInt(-10), Fixed64.Zero));
            players.Add(2, new LogicPlayer(2, Fixed64.FromInt(10), Fixed64.Zero));

            session = new FrameSyncSession(this, new[] { 1, 2 }, inputDelay);
            session.SeedInitialFrames();
        }

        private void Update()
        {
            float frameDuration = 1f / logicFps;
            accumulator += Time.unscaledDeltaTime;

            // 提前提交若干帧输入。真实网络项目应将 FrameCommand 序列化后发送给房间服务器。
            FrameInput playerInput = ReadPlayerInput();
            for (int offset = 0; offset < maxCatchUpFrames; offset++)
            {
                int targetFrame = session.CurrentFrame + inputDelay + offset;
                session.ReceiveCommand(new FrameCommand(targetFrame, 1, playerInput));
                session.ReceiveCommand(new FrameCommand(targetFrame, 2, ReadBotInput(targetFrame)));
            }

            int executedFrames = 0;
            while (accumulator >= frameDuration && executedFrames < maxCatchUpFrames)
            {
                // 未收齐输入时保留累积时间并等待，不擅自预测远端输入。
                if (!session.TryAdvance())
                {
                    break;
                }

                accumulator -= frameDuration;
                executedFrames++;
            }

            UpdateViews();
        }

        public void Step(int frame, IReadOnlyList<FrameCommand> commands)
        {
            for (int i = 0; i < commands.Count; i++)
            {
                FrameCommand command = commands[i];
                LogicPlayer player = players[command.PlayerId];

                player.X += Fixed64.FromInt(command.Input.Horizontal * moveUnitsPerFrame);
                player.Z += Fixed64.FromInt(command.Input.Vertical * moveUnitsPerFrame);
            }
        }

        public uint CalculateStateHash()
        {
            // FNV-1a 哈希足够用于快速发现状态分歧，但不能用于安全校验。
            uint hash = 2166136261u;
            HashPlayer(ref hash, players[1]);
            HashPlayer(ref hash, players[2]);
            return hash;
        }

        private static void HashPlayer(ref uint hash, LogicPlayer player)
        {
            unchecked
            {
                hash = (hash ^ (uint)player.Id) * 16777619u;
                hash = (hash ^ (uint)player.X.RawValue) * 16777619u;
                hash = (hash ^ (uint)(player.X.RawValue >> 32)) * 16777619u;
                hash = (hash ^ (uint)player.Z.RawValue) * 16777619u;
                hash = (hash ^ (uint)(player.Z.RawValue >> 32)) * 16777619u;
            }
        }

        private static FrameInput ReadPlayerInput()
        {
            sbyte horizontal = 0;
            sbyte vertical = 0;

            if (Input.GetKey(KeyCode.A)) horizontal--;
            if (Input.GetKey(KeyCode.D)) horizontal++;
            if (Input.GetKey(KeyCode.S)) vertical--;
            if (Input.GetKey(KeyCode.W)) vertical++;

            return new FrameInput(horizontal, vertical, 0);
        }

        private static FrameInput ReadBotInput(int frame)
        {
            // 机器人输入只依赖逻辑帧号，因此每台机器都会得到完全相同的结果。
            sbyte horizontal = (sbyte)((frame / 40) % 2 == 0 ? -1 : 1);
            return new FrameInput(horizontal, 0, 0);
        }

        private void UpdateViews()
        {
            ApplyView(playerOneView, players[1]);
            ApplyView(playerTwoView, players[2]);
        }

        private void ApplyView(Transform view, LogicPlayer player)
        {
            if (view == null)
            {
                return;
            }

            // Transform 仅负责显示，不参与逻辑计算。
            Vector3 target = new Vector3(player.X.ToFloat(), 0f, player.Z.ToFloat()) * renderScale;
            view.position = Vector3.Lerp(view.position, target, 0.35f);
        }

        private sealed class LogicPlayer
        {
            public readonly int Id;
            public Fixed64 X;
            public Fixed64 Z;

            public LogicPlayer(int id, Fixed64 x, Fixed64 z)
            {
                Id = id;
                X = x;
                Z = z;
            }
        }
    }
}
