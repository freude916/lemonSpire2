# 这是什么？

./AGENTS.md 是您的长期记忆，每次您 (iflow) 被唤醒时都被加载。

如果您发现您花费了相当长的时间来解决某个可能具有通用性的问题，或者您发现自己在不断地重复同样的错误，那么请将这些经验简要地写下来并保存在这里。

# 开发工具

请您总是执行 dotcheck.sh 代替 dotnet build 来构建项目，能加快 check 速度和减少上下文浪费。

---

## StS 2 处理

INetMessage 的 broadcast 实际上是提交到 host ，然后 host 执行广播，意味着 host 自己 broadcast 的时候 host 收不到（client 广播 client 能）。

## Godot 开发经验

### 节点 `_Ready()` 时序
节点的 `_Ready()` 只有在**加入场景树后**才会执行。如果需要访问 `_Ready()` 中初始化的字段，必须确保节点已在场景树中：
```csharp
container.AddChild(holder);  // 先加入场景树
holder.AddPotion(nPotion);   // 现在 holder._Ready() 已执行，_emptyIcon 已初始化
```

### 复用游戏 Ui： Scale 偏移问题
缩放节点时，如果节点有 `PivotOffset`（如 NPotion 设置了中心点），缩放会导致位置偏移。解决方法：
```csharp
nPotion.Scale = Vector2.One * scale;
nPotion.Position = Vector2.Zero;  // 关键：重置位置修正偏移
```

---

## TODO

