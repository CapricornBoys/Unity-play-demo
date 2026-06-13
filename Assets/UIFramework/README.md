# Unity uGUI UI Framework

## 核心功能

- Addressables 异步加载和实例释放
- 固定 UI 层级、缓存、返回栈和模态遮罩
- 相同界面的并发加载合并
- 视图 Prefab 与界面控制器分离
- 控制器在打开界面时动态挂载
- UI 组件按节点名称或相对路径自动注册
- 支持运行时动态节点的索引重建
- 参考分辨率缩放和不同宽高比适配
- 手机刘海、圆角和系统手势区域的 Safe Area 适配

## 分辨率适配

框架默认使用 `1920 x 1080` 参考分辨率和 `Expand` 策略：

- 比 16:9 更窄时按宽度缩放
- 比 16:9 更宽时按高度缩放
- 背景层铺满完整屏幕
- Normal、Popup、Tips、Top 层自动限制在 Safe Area 内

可以在启动时修改：

```csharp
UIManager.Instance.ConfigureAdaptation(
    new Vector2(1920f, 1080f),
    UIScaleMode.Expand,
    safeAreaEnabled: true);
```

其他策略包括 `FixedWidth`、`FixedHeight` 和 `Match`。使用 `Match` 时第三个参数
控制宽高匹配值，`0` 表示宽度，`1` 表示高度。

## 预制体要求

Prefab 只保存 `RectTransform`、`Image`、`Button`、`Text` 等视图组件，
不要挂载任何 `UIPanel` 派生脚本。

在 Addressables 中设置唯一地址，例如 `UI/BagPanel`。

## 注册界面

注册时同时指定 Addressable 地址和控制器类型：

```csharp
UIManager.Instance.Register<BagPanel>(
    "Bag",
    "UI/BagPanel",
    UILayer.Normal,
    cache: true,
    layout: UIPanelLayout.FullScreen);
```

Addressables 实例化完成后，`UIManager` 会动态挂载 `BagPanel`。

`FullScreen` 会强制根节点铺满所属 UI 层；弹窗应使用：

```csharp
layout: UIPanelLayout.ContentSize
```

## 绑定组件

节点名称唯一时直接使用名称：

```csharp
public sealed class BagPanel : UIPanel
{
    protected override void OnInitialize()
    {
        Button closeButton = GetUI<Button>("CloseButton");
        closeButton.onClick.AddListener(Close);
    }
}
```

名称重复时使用相对路径：

```csharp
Button closeButton = GetUI<Button>("Header/CloseButton");
```

还可以使用：

```csharp
TryGetUI<Button>("CloseButton", out Button button);
Button[] buttons = GetAllUI<Button>("ButtonRoot");
RebuildUIRegistry(); // 运行时增删节点后刷新索引
```

## 打开和关闭

```csharp
BagPanel panel = await UIManager.Instance.OpenAsync<BagPanel>("Bag", playerBagData);
UIManager.Instance.Close("Bag");
UIManager.Instance.CloseTop();
UIManager.Instance.CloseAll();
UIManager.Instance.Unload("Bag");
```

## 演示

打开 `Assets/UIFramework/Demo/UIFrameworkDemo.unity`。

测试 Prefab 位于 `Assets/UIFramework/Demo/Prefabs`，可通过以下菜单重新生成：

`Tools > UI Framework > Rebuild Demo Prefabs`

演示还包含 `UI/InventoryPanel`：

- 200 个背包槽位
- 循环网格只创建可见格子
- 离屏格子进入对象池复用
- 点击物品查看详情
- 长按物品后拖到其他槽位交换位置
- 普通滑动优先，不会误触物品点击

演示包含 `UI/Match3Panel` 三消游戏：

- 8 x 8 棋盘和 6 种棋子
- 点击两个相邻棋子进行交换
- 无匹配时自动复原
- 横向、纵向匹配检测
- 消除、下落、补充和连锁计分
