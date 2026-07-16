# GioFX ForexWidget

**A real-time Forex session & killzone tracker for Windows — 100% offline-first.**

Always-on-top desktop widget that shows you exactly what's happening in the Forex market right now: which sessions are open, which killzones are active, DST shifts, bank holidays, and upcoming high-impact news — without ever depending on an internet connection to function.



---

## Features

- **Live session timeline** — Sydney, Tokyo, London, New York, always in UTC (or your local time, your choice)
- **Killzone tracking** — configurable high-liquidity windows, with visual highlighting on the timeline
- **DST-aware** — automatically detects and warns about daylight saving transitions that shift session overlaps
- **Weekend close/open logic** — accurate to the minute, no internet required
- **Bank holiday awareness** — pulls from a public economic calendar feed, cached locally so the app never blocks on network
- **High-impact news countdown** — USD/EUR/GBP/JPY events, filtered to what matters
- **System tray alerts** — get notified before killzones start, sessions open/close, or the weekend close approaches
- **Live theme switching** — Dark/Light, applies instantly, no restart
- **100% offline-first core** — the session/killzone/DST engine never touches the network; internet only enriches the experience, it's never a requirement
<a href="https://apps.microsoft.com/detail/ForexWidget/9PGXXRRQMZ0L">
  <img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>


## Screenshots

<img width="312" height="505" alt="Captura de pantalla 2026-07-15 183444" src="https://github.com/user-attachments/assets/0860a4f5-c99d-4cbb-910f-afdbb5842bdf" />
<img width="305" height="496" alt="Captura de pantalla 2026-07-15 183420" src="https://github.com/user-attachments/assets/1b89e33c-bcd9-4d36-a370-b58aba996dc5" />
<img width="312" height="507" alt="Captura de pantalla 2026-07-15 183430" src="https://github.com/user-attachments/assets/19918236-2e5f-4ca9-9640-bbe7c8158681" />



---

## Installation

### Option 1 — Microsoft Store (recommended)

<a href="https://apps.microsoft.com/detail/ForexWidget/9PGXXRRQMZ0L">
  <img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

*Currently propagating through Microsoft's infrastructure post-certification — if the "Get" button isn't active yet, check back shortly, or use the direct download below in the meantime.*

### Option 2 — Direct download (available now)

Download from the [Releases](../../releases) page. You need **both** files:
- `ForexWidget.msix`
- `ForexWidget.appinstaller`

**Open `ForexWidget.appinstaller`** (not the `.msix` directly) to install — this is what enables automatic future updates. If you install the `.msix` directly, the app will work but won't check for updates on its own.

### Option 3 — Run from source (for developers)

Requirements: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), Windows 10/11.

```powershell
git clone https://github.com/Gilocho/GioFX_ForexWidget.git
cd GioFX_ForexWidget
dotnet build
dotnet run --project ForexWidget.App
```

## Architecture

The project follows a layered architecture, designed so the market-logic core never depends on UI, configuration files, or network access:

```
ForexWidget.Domain/          Pure models, enums, interfaces — no dependencies
ForexWidget.Core/            SessionEngine, KillzoneEngine, DstEngine, WeekendEngine,
                              AlertEngine — 100% local, testable in isolation
ForexWidget.Infrastructure/  Config loading, holiday/news providers, caching,
                              notifications, theming — all the "outside world" plumbing
ForexWidget.App/             WPF UI, MVVM (CommunityToolkit.Mvvm)
ForexWidget.Tests/           xUnit test suite
```

**Design principles:**
- The Core engine works entirely in UTC and has zero network dependencies — it will keep functioning correctly even if every external API disappears.
- Configuration lives in JSON, editable without recompiling, and self-heals to sane defaults if a file is missing or corrupted.
- MVVM throughout — no business logic in code-behind.

## Configuration

User settings, killzones, alerts, and cached data live in `%LOCALAPPDATA%\ForexWidget`. Editable directly, or via the in-app Settings window (⚙ button).

Default killzones ship tuned for MMM but are fully customizable:

```json
{
  "Killzones": [
    { "Name": "London Open", "StartUtc": "06:00", "EndUtc": "09:00", "Enabled": true },
    { "Name": "New York Open", "StartUtc": "12:00", "EndUtc": "15:00", "Enabled": true }
  ]
}
```

## Building from source / Contributing

```powershell
dotnet build
dotnet test
```

Contributions are welcome. Since this project is GPLv3 (see [LICENSE](LICENSE)), any distributed fork or derivative must also remain open source under the same license.

---

### ♥ Support this project

ForexWidget is free and always will be. If it has been useful to you, you can support its continued development using the **♥ Support** button inside the app, or directly through any of the options below:

| Method | Network | Link / Address |
| :--- | :--- | :--- |
| ☕ **PayPal** | Direct Link | [paypal.me/Giodow](https://paypal.me/Giodow) |
| 💵 **USDT** | Tron (TRC20) | `TMgpufHnuhQLBZxr4LTF6wY3ZyCibQnhJc` |
| 💵 **USDT** | BSC (BEP20) | `0x2f4fc2f588d6222b34b997fe2d6b9ad14f6bebce` |
| 💵 **USDT** | Ethereum (ERC20) | `0x2f4fc2f588d6222b34b997fe2d6b9ad14f6bebce` |

---

## Disclaimer

ForexWidget is an informational tool only. It does not provide trading signals, financial advice, or recommendations of any kind — it shows market session timing and context, nothing more. Trading Forex carries substantial risk. You are solely responsible for your own trading decisions.

---

## License

GPLv3 — see [LICENSE](LICENSE) for full text.
