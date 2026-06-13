# Unity 帧同步基础框架

## 快速运行

1. 在场景中新建空物体 `FrameSyncDemo`。
2. 挂载 `FrameSync.Demo.FrameSyncDemo`。
3. 新建两个 Cube，分别拖给 `Player One View` 和 `Player Two View`。
4. 运行场景，使用 WASD 控制玩家 1；玩家 2 会自动左右移动。

## 正式联网时的接入点

- 客户端采集输入，构造 `FrameCommand` 并发送给房间服务器。
- 服务器按逻辑帧收集所有玩家输入，再广播完整命令集合。
- 客户端收到广播后调用 `FrameSyncSession.ReceiveCommand`。
- 只有 `TryAdvance` 返回 `true` 时，才推进一次逻辑世界。
- 定期比较 `LastStateHash`；不一致时执行快照恢复或重连。

## 确定性约束

- 核心逻辑不要使用 `Transform`、`Rigidbody`、`Time.deltaTime` 或随机的 `Dictionary` 遍历顺序。
- 数值计算使用整数或定点数；Unity 的 float 只用于画面插值。
- 随机数必须使用统一种子和自研确定性随机数生成器。
- 所有命令必须采用固定排序规则。
- 生产项目还需要断线重连、状态快照、丢包重发、超时策略和作弊校验。
