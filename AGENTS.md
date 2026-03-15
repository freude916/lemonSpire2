# 这是什么？

./AGENTS.md 是您的长期记忆，每次您 (iflow) 被唤醒时都被加载。

如果您发现您花费了相当长的时间来解决某个可能具有通用性的问题，或者您发现自己在不断地重复同样的错误，那么请将这些经验简要地写下来并保存在这里。

# 开发工具

请您总是执行 dotcheck.sh 代替 dotnet build 来构建项目，能加快 check 速度和减少上下文浪费。

---

## StS 2 处理

INetMessage 的 broadcast 实际上是提交到 host ，然后 host 执行广播，意味着 host 自己 broadcast 的时候 host 收不到（client 广播
client 能）。

永远不要更新本地。永远在发送之后立刻执行 OnReceiveMessage

### Log

Sts2 Mod 的 Log 没有

## Godot 开发经验

### 节点 `_Ready()` 时序

节点的 `_Ready()` 只有在**加入场景树后**才会执行。如果需要访问 `_Ready()` 中初始化的字段，必须确保节点已在场景树中：

```csharp
// 错误：container 不在场景树中，holder._Ready() 不会执行
var container = new VBoxContainer();
container.AddChild(holder);
holder.AddPotion(nPotion);  // NRE! _emptyIcon 未初始化

// 正确：先让 container 加入场景树，再添加子节点
row.AddChild(container);     // container 在场景树中
container.AddChild(holder);  // holder._Ready() 会执行
holder.AddPotion(nPotion);   // 正常工作
```

### 获取尺寸：用 `GetMinimumSize()` 而非 `Size`

- `Size` 是**上一帧渲染后的实际尺寸**，动态添加/删除节点后立即读取会得到旧值
- `GetMinimumSize()` 是**实时计算所需最小尺寸**，推荐用于布局计算

### `CustomMinimumSize` 语义

- 语义是**"保底限制"**，不是"当前实际尺寸"
- 设大后再改小，外层 `Size` 不会自动缩小
- 解决方法：将父级容器 `Size = Vector2.Zero`，让 Godot 重新计算

### 缩放与 `PivotOffset`

- `PivotOffset` 决定缩放/旋转的基准点
- NPotion 在 `AddPotion()` 中设置了 `PivotOffset = Size * 0.5f`（中心点）
- NRelic **没有设置**，默认左上角
- 缩放时：中心点缩放位置不变，左上角缩放会导致内容偏移
- 解决：

```csharp
nRelic.PivotOffset = nRelic.Size * 0.5f;  // 设置中心点
nRelic.Scale = Vector2.One * scale;
nRelic.Position = Vector2.Zero;  // 重置位置修正偏移
```

---

## TODO

