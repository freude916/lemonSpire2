# Lemon Spire 2 / 柠檬塔2

受薄荷尖塔启发的模组，主要面向杀戮尖塔2（尤其是多人模式）提供一些便利功能。

A mod inspired by Mint Spire, mainly providing some QoL for Slay the Spire 2 (especially multiplayer mode).

由于我使用了 Alchyr 的 [BaseLib](https://github.com/Alchyr/BaseLib-StS2/releases) 来解决 Linux 兼容性问题，因此似乎不安装 BaseLib 就无法正常使用这个模组了。

Because I used Alchyr's [BaseLib](https://github.com/Alchyr/BaseLib-StS2/releases) to solve Linux compatibility issues, it seems that the mod cannot be used properly without installing BaseLib.

目前包含功能：

- 多人模式下显示每个玩家的伤害贡献、buff 贡献
- 显示别的玩家是否有多人专属牌（一般是仅帮助队友的效果），显示为一个握手小图标
- 多人聊天
- 

- - 在多人聊天中，使用 Alt + 左键点击 可以发送遗物、牌、buff 的信息到聊天中作为 Tooltip 供队友查看
- - 这一功能的 Vibe Codebase 感谢 [sts2_typing by Shiroim](https://github.com/Shiroim/sts2_typing), MIT License
-
- - Alt + 右键点击 可以发送 “当前的 HoverTip”，适合发送事件显示的即将塞入的牌、遗物，或者牌上带的buff 等信息
- - 这一功能在读取卡牌上是准确的，但是事件和卡牌的文本本身存储得比较混乱，我还在偷懒。
- - 目前采用非常粗暴的读取 HoverTip 的方式实现， 不能在之后提取出对象信息，可拓展性不佳且破坏 i18n

    
Includes:

- Show each player's damage and buff contribution in multiplayer mode
- Show whether other players have multiplayer-exclusive cards (generally effects that only help teammates), displayed as a handshake icon
- Multiplayer Chat
- - In multiplayer chat, use Alt + Click to send information about relics, cards, and buffs to the chat
- - This feature's Vibe Codebase is thanks to [sts2_typing by Shiroim](https://github.com/Shiroim/sts2_typing), MIT License
- - 
- - Alt + Right Click can send the "current HoverTip", suitable for sending information about cards that are about to be inserted, relics, or buffs on cards displayed by events.
- - This feature is accurate when reading cards, but the text of events and cards is stored quite messy, and I'm still being lazy.
- - Currently implemented in a very crude way by reading the HoverTip Text, which cannot extract object information later, has poor extensibility, and breaks i18n

