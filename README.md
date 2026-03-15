# Lemon Spire 2

[English](./README.md) / [中文](./README_zh.md)

A mod inspired by Minty Spire, mainly designed to provide Quality of Life (QoL) features for Slay the Spire 2 (
especially for multiplayer mode).

Because I used [:Alchyr/BaseLib](https://github.com/Alchyr/BaseLib-StS2/releases) to resolve Linux compatibility issues,
it seems this mod cannot function properly without BaseLib installed.

## Features

### Simple Teammate Synergy Indicator

Visually displays on the UI whether teammates have drawn "multiplayer-exclusive cards" or have cards that apply
team-assisting buffs.

- Regular buffs will directly display as their corresponding buff icons. Currently, only Vulnerable, Frail, and Choke
  are supported.
- Multiplayer-exclusive cards will uniformly display as a "Handshake" icon.
- I plan to improve extensibility in the future. Currently, specialized icons cannot hide the general handshake icon. If
  you wanted to create a specialized icon for a specific card (like "Flank"), you would need to modify the handshake
  icon's logic, which is highly inconvenient.

Read `lemonSpire2.SynergyIndicator.Models.IndicatorProvider`.

### Complete Multiplayer Chat

Press the `Tab` key to expand or collapse the chat box. Dragging the title bar with your mouse is supported.

#### Send Anything

Use `Alt + Left Click` to send information about relics, cards, and buffs to the multiplayer chat, generating a hover
tooltip for teammates to view.

For the Vibe Codebase of this feature, thanks to [sts2_typing by Shiroim](https://github.com/Shiroim/sts2_typing), MIT
License.

`Alt + Right Click` sends the "current HoverTip", which is perfect for sharing upcoming cards and relics forced upon you
during events, or buffs attached to cards in your hand.

- The right-click function is very accurate when reading cards, but the game's underlying storage for events and card
  text is slightly complex, so I took a lazy shortcut here.
- The current implementation is very brute-force—it directly reads the text inside the HoverTip. This approach makes it
  impossible to extract specific object data later, resulting in extremely poor extensibility, and it also breaks i18n.
- Additionally, this thing currently only reads 1 HoverTip at a time. I need to think about a better way to handle this.

### Simple Contribution Stats

In multiplayer mode, when you hover your mouse over a character's status on the left, a HoverTip will display the
player's damage contribution, buff contribution, and "extra damage" contribution (i.e., increased damage from
Vulnerable, Frail, etc. Tracing back to the exact buff source is a bit difficult, so I tracked this instead).

- Actually, tracing the source of a Buff isn't hard itself. The problem is that when identical Buffs stack, the game
  logic doesn't update the "buff source" attribute. This leads to subsequent players who stack the same Buff missing out
  on their contribution points, which is very unfair. So, I just gave up on that part.
- You can't expect me to build a whole separate History system just for this.

The HoverTip mechanism here is also open for extension. If you want to know how to add new hover stat information,
please check `NPlayerState.ITooltipProvider`.

Existing contribution stat code is located under the `StatsTracker` namespace, feel free to refer to it.

### In-Combat Status Hover Panel

In the vanilla game, clicking the character status on the left directly pops up a full-screen panel showing their
complete deck and relic list—which is actually of little direct help during a single combat.

Therefore, we changed the "click character status" function to bring up a small hover panel that directly displays your
teammates' hand for the current turn and their available potions.
(The original full-screen panel can now be opened via **Double-Click** or **Right-Click**).

In shop, hover panel will show the gold amount and the shop items.

This feature also supports extensions, please check `lemonSpire2.NPlayerState.Panel` to learn how to shove new
information into the panel.

By the way, items mentioned above inside this hover panel also support `Alt + Click` to send to chat.

## TODOs

### Ui

- The UI is hideous right now.

### Better BBCode

The current chat box **does not escape user input at all (What?!)**, so please keep your teammates in check and don't
let them send unclosed tags that will turn the entire chat box styling into a total mess.

Later on, I really want to make a tag feature that displays the corresponding Tooltip by doing a reverse i18n lookup.
For example, typing `[card:Flank]` would directly display the hover tooltip for "Flank" in the chat. This way you can "
Discover" cards you don't even have in your hand yet, and tell your teammates to quickly find some for you.

### Color / Drawing Related

Plan to support modifying the color of player names on the left side, modifying the screen drawing color, and syncing
this custom color to the names shown in chat messages. I personally really love making these kinds of integrated
features.

### Command System

- Plan to add console commands: for example, typing `/w text` to send private messages, `/ping` to test latency, `/help`
  to show help, `/about` to display about info, `/disable` to stop your itchy-handed friends from opening the console
  and typing random commands, etc.
- Considering that adding more advanced commands (like `/gift [gold] [id]` to send teammates money, or `/cardsend` to
  enable interactive card-giving) would directly impact the original Gameplay...
- ...this doesn't quite fit the QoL theme of this mod. Therefore, I might make a separate mod dedicated to more
  multiplayer commands in the future.
