# 这是什么？

./AGENTS.md 是您的长期记忆，每次您被唤醒时都被加载。

如果您发现您花费了相当长的时间来解决某个可能具有通用性的问题，或者您发现自己在不断地重复同样的错误，那么请将这些经验简要地写下来并保存在这里。

# 开发资源

理论上有一个 .reference 目录，里面放了一些参考项目。

另外走到 ~/Documents/MTSII/ ，你可以看到 sts2src 和 sts2res 两个目录，里面分别放了游戏的反编译源代码和资源文件（图片、场景等），还有一些反编译工具。

以及走到上级目录，这里有很多 Mod 项目，其中最重要的：

- BaseLib 项目，是二代的 ModTheSpire+stslib 的合体，里面有一些工具类和一些对游戏的反射封装，
- BaseLib-Wiki 是 BaseLib 的文档，里面有一些使用说明。

拿不准的可以去参考。 （注意参考前最好 ls -l 看看文件的修改时间，如果超过一周了可能就过时了，对于 Mod 项目来说你可以试试 git
pull 来更新一下）

请您优先执行 dotcheck.sh 代替 dotnet build 来构建项目，它不返回 Warning，能加快 check 速度和减少上下文浪费。

# 项目结构

我们有一个 lemonSpire2.Tests 子项目，但是那主要是用来测试 ./Chat/Input @ 等功能的解析的，主项目和杀戮尖塔2的关联太紧密了没法撰写单元测试。

sts2.dll 的启动需要 Godot Runtime ，所以测试主机崩溃是常态，遇到后就不要测试那些依赖游戏内部件的功能了，直接等我去游戏里测试就好了。

目前项目的主要模块：（增加新的记得往这里写一下，或者提醒我）

基础库部分

- SyncReward - 同步战后奖励。
- SyncShop - 同步商店。

独立模块

- QoL - 一些小优化。 目前仅包括一个给看别人牌堆的时候增加 HoverTip 的功能。
- ColorEx - 改变玩家的颜色、玩家鼠标的颜色
- SynergyIndicator - 给玩家头像旁边显示一个小图标来提示当前牌堆中是否有多人协同效果。

相对耦合的模块：

- Chat - 聊天面板，目前包含了一个简单的聊天系统和一些命令解析功能。
- PlayerStateEx - 玩家状态面板，包含了一个新的面板来显示玩家的状态信息，如手牌。
- Tooltips - 提供了一些新的 HoverTip 来显示一些额外的信息。

请注意， Chat/Message/ 下的 Segment 是我为了直接借用游戏内建的 INetMessage 所以有 Serialize / Deserialize 这么一个抽象的方法，
然后为了统一抽象我又给它们了 Render 来保存 url string 和 CreatePreview 这个方法来生成预览图案。
实际上这几块逻辑是拆得很开的， IMsgSegment 完全可以作为文本被 parse 之后的数据类使用。
当然如果你觉得还有更好的设计方案也欢迎提出来。

# 开发经验

你其实有多个实例一起在运行，而且我也会编辑一部分文本，
而您的 replace 工具调用要求一字不差地指定被替换的原始文本。所以如果您发现 replace 工具返回一个错误，请您重读一遍原始文件以正常替换。

# 开发假设

目前这个 mod 不包含任何持久化，所以不用考虑任何数据存储和版本兼容问题，Keep It Shit & Stupid.

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

