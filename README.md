# Unity Play Demo

一个基于 Unity 2022 LTS 的功能演示项目，包含可复用的 uGUI UI 框架、多个交互 Demo，以及基础帧同步实现。

## 项目内容

### UI Framework

- 使用 Addressables 异步加载和释放 UI Prefab
- 支持 UI 分层、缓存、返回栈和模态遮罩
- UI 视图与控制器分离，控制器在界面打开时动态挂载
- 按节点名称或相对路径自动注册 UI 组件
- 支持参考分辨率、宽高比和 Safe Area 适配

UI 演示场景包含：

- 三消游戏：8 x 8 棋盘、交换检测、消除、下落、补充和连锁计分
- 合成数字：相同数字合并玩法
- 虚拟背包：大量格子的虚拟化显示、对象池、拖放交换和详情查看
- 设置窗口、消息弹窗及 UI 层级管理

### Frame Sync

- 基于逻辑帧推进游戏状态
- 使用确定性命令和状态哈希
- 提供双玩家本地演示
- 展示正式联网时的命令收集、广播和同步接入方式

## 环境要求

- Unity `2022.3.43f1c1`
- Universal Render Pipeline `14.0.11`
- Addressables `1.29.0`
- TextMeshPro `3.0.6`

建议使用与项目一致的 Unity 版本打开，避免资源或序列化格式发生不必要的升级。

## 快速开始

1. 克隆仓库：

   ```bash
   git clone https://github.com/CapricornBoys/Unity-play-demo.git
   ```

2. 使用 Unity Hub 添加并打开项目。
3. 等待 Package Manager 和 Addressables 资源导入完成。
4. 打开 UI 演示场景：

   ```text
   Assets/UIFramework/Demo/UIFrameworkDemo.unity
   ```

5. 点击 Unity 编辑器的 Play 按钮运行。

如需重新生成 UI 演示 Prefab，可使用菜单：

```text
Tools > UI Framework > Rebuild Demo Prefabs
```

## 目录结构

```text
Assets/
├── FrameSync/                 # 帧同步基础框架与演示
└── UIFramework/
    ├── Runtime/               # UI 框架核心代码
    ├── Editor/                # Demo Prefab 构建工具
    ├── Demo/                  # 演示场景与公共界面
    ├── Inventory/             # 虚拟背包 Demo
    ├── Match3/                # 三消 Demo
    └── MergeTwo/              # 合成数字 Demo
```

## UI Framework 基本用法

注册界面：

```csharp
UIManager.Instance.Register<MyPanel>(
    "MyPanel",
    "UI/MyPanel",
    UILayer.Normal,
    cache: true,
    layout: UIPanelLayout.FullScreen);
```

打开和关闭界面：

```csharp
MyPanel panel = await UIManager.Instance.OpenAsync<MyPanel>("MyPanel");
UIManager.Instance.Close("MyPanel");
```

在 `UIPanel` 控制器中获取组件：

```csharp
Button closeButton = GetUI<Button>("CloseButton");
closeButton.onClick.AddListener(Close);
```

更完整的 UI 框架说明请查看 [`Assets/UIFramework/README.md`](Assets/UIFramework/README.md)，帧同步说明请查看 [`Assets/FrameSync/README.md`](Assets/FrameSync/README.md)。

## 注意事项

- `Library`、`Temp`、`Obj`、`Logs` 等 Unity 生成目录不会提交到仓库。
- UI Prefab 需要配置对应的 Addressables 地址。
- 帧同步核心逻辑应避免依赖 `Transform`、`Rigidbody`、`Time.deltaTime` 和非确定性随机数。

