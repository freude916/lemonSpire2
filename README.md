# Lemon Spire 2

[English](./README.md) / [中文](./README_zh.md)

A mod inspired by Minty Spire, mainly focused on providing Quality of Life (QoL) features for Slay the Spire 2,
especially in multiplayer mode.

Because I used Alchyr's [BaseLib](https://github.com/Alchyr/BaseLib-StS2/releases) to resolve Linux compatibility
issues and provide Mod Config, this mod currently seems unable to function correctly without BaseLib installed.

## Features

### Color / Drawing Related

Change your color.

This affects the cursor color, map drawing color, player name color in the left-side info panel, and the name color in
chat messages.

(TODO) This currently feels a bit too eye-catching and hurts readability. I probably need a way to change the inner
color without affecting the outer stroke.

### Simple Teammate Synergy Indicator

Visually shows whether your teammates have drawn "multiplayer-exclusive cards", or hold cards that can apply buffs that
help the team.

- Regular buffs are displayed directly as their corresponding buff icons. Currently supported: Vulnerable, Frail,
  Choke, ally Strength, ally Block, ally Energy, and ally Draw.
- Multiplayer-exclusive cards are displayed with a generic "Handshake" icon.
- I want to improve extensibility later. Right now, specialized icons cannot directly override the generic handshake
  icon. If you want a specific multiplayer card to suppress the handshake icon, you have to modify the handshake
  icon's logic itself, which is inconvenient.

Click your own small icon to make it appear dimmed for the whole team, which means "I can't play into this" and can be
used to signal teammates. This interaction is not great, but I have not come up with anything better yet.

See `lemonSpire2.SynergyIndicator.Models.IIndicatorProvider`.

### Full Multiplayer Chat

Press `Tab` to expand or collapse the chat box. Mouse dragging at title bar is supported.

There is safe BBCode support, so you can use tags like `[b]` and `[color=red]` to style text. Unclosed tags are
automatically closed at the end. The closer logic lives in `lemonSpire2.util.BBCodeUtils`.

Note that I did not distinguish which tags are self-closing, so there may still be edge cases. I was too lazy to fix
that properly.

Under the hood, there is a reasonably usable segmented message system. Custom message styling should be feasible,
although I have not built much on top of it yet.

See `lemonSpire2.Chat` if you want to add new message segment types or adjust the rendering of existing ones.

You can also try registering new intents into `ChatStore` for additional chat behavior. Or help me split up the
`TextSubmit` intent into smaller pieces.

#### Send Anything - Mouse

Use `Alt + Left Click` to send relics, cards, and buffs into multiplayer chat, generating hover tooltips that your
teammates can inspect.

For the vibe codebase of this feature, thanks to [sts2_typing by Shiroim](https://github.com/Shiroim/sts2_typing)
(MIT License).

Use `Alt + Right Click` to send the "current HoverTip", which is useful for sharing cards or relics that events are
about to force on you, or buffs attached to cards in your hand.

- The right-click path is very accurate when reading cards, but the game's internal storage for events and card text is
  a bit messy, so I cut corners here.
- The current implementation is extremely brute-force: it literally reads the text from the HoverTip. That means you
  cannot recover the original object data later, extensibility is terrible, and it also breaks i18n.
- Right now this thing only reads a single HoverTip at a time. I still need to think through a better design.

#### Send Anything - Keyboard

There is also a shorthand input syntax.

Currently supported:

- `@playerName` creates a mention.
- `<card:cardId>` creates a card link.
- `<relic:relicId>` creates a relic link.
- `<potion:potionId>` creates a potion link.

There is also a slash command system using `/`, but since this mod is meant to stay in the QoL space, there are not
many good built-in commands that do not interfere with gameplay.

Currently built-in commands:

- `/help` reads help information for all commands from the registry and displays it.
- `/about` shows information about this mod.
- `/ping` reads the current ping from the game's `NetService` and displays it.
- `/w @playerName text` sends a private message.

For the shorthand input internals, see `lemonSpire2.Chat.Input` if you want to add new input types or commands.

For quickly registering a new shorthand input, see `lemonSpire2.ChatServices` and copy the existing registration code.
It is somewhat coupled at the moment.

For registering a new slash command, see `lemonSpire2.ChatServices.Command`.

### Simple Contribution Stats

In multiplayer mode, hovering a character status on the left shows a HoverTip with the player's damage contribution,
buff contribution, and "extra damage" contribution (damage gained from effects such as Vulnerable and Frail, because
accurately tracing the original buff source is awkward).

- Tracing a buff source is not hard by itself. The real problem is that when identical buffs stack, the game logic does
  not update the "buff source" attribute. That means players who apply the same buff later do not get contribution
  credit, which is obviously unfair, so I gave up on that.
- You cannot seriously expect me to build a separate History system just for this.

This HoverTip mechanism is also open for extension. If you want to add new hover stat information, see
`PlayerStateEx.PanelProvider.ITooltipProvider`.

Existing contribution stat code lives under the `StatsTracker` namespace.

### In-Combat Status Hover Panel

In the vanilla game, clicking the character status on the left opens a full-screen panel showing the complete deck and
relic list. That is not especially helpful during a single combat, and opening and closing that panel is a bit
annoying.

So I changed "click character status" into a small hover panel that directly shows your teammates' current-turn hand
and their available potions.

The original full-screen panel can now be opened with **Double-Click** or **Right-Click**.

It can also show the card rewards your teammates are currently facing. That includes post-combat rewards, rewards from
relics like the Orrery, and even special rewards such as the Bonfire Key or taking back stolen cards.

In shops, the hover panel shows your teammates' gold and the shop items they are looking at. This only becomes truly
useful together with other mods that let players gift cards or items. We only provide the display panel.

This feature is also extensible. See `PlayerStateEx.PanelProvider` to learn how to insert new information into the
panel.

By the way, the items currently shown inside this hover panel also support `Alt + Click` sending.

#### Pointing Fingers

Now, pressing `Alt + Left Click` can make your teammate's hand cards, potions, or shop items flash green, telling them
to play or buy that thing.

Extremely loud newcomer-coaching energy, fully deployed!

This part has not been tested properly yet. If you can help fix it, that would be ideal! see
`PlayerStateEx.RemoteFlash`.

## TODOs

### UI

- The UI is currently hideous. I really do not have much visual sense, so if you want to help redesign the
  borders, colors, layout, or anything else, that would be great.
- Most of the current UI is basically AI-stacked code. Do you think that was ever going to look good?

### More Commands

- To be honest, I think there are still some hidden information in the vanilla game that can be displayed, but I can't
  think of them right now, so suggestions are welcome!
- But please note that any command that directly affects gameplay is not very suitable for this mod. In the future,
  there may be a separate auxiliary mod with more multiplayer commands that utilize the hooks here
