---

# DataExport (NinjaTrader 8)

> **One-Click Native Chart Data Exporter to Clean, Chronological CSV.**

**DataExport** is a lightweight NinjaTrader 8 strategy utility that embeds an interactive **"Export CSV"** button directly onto your active chart canvas. With a single click, it captures all bars currently loaded in your chart series and prompts a native Windows file dialog to save them to a formatted `.csv` file.

---

## Table of Contents
- [Features](#features)
- [How It Works](#how-it-works)
- [CSV Output Schema](#csv-output-schema)
- [Installation](#installation)

---

## Features

* **Zero External Dependencies:** Completely standalone C# script. No Python backends, sockets, or auxiliary processes required.
* **On-Chart Canvas Integration:** Injects a clean, styled WPF button into the top-left corner of your chart via NinjaTrader's native `UserControlCollection`.
* **Standard Windows Save Dialog:** Pre-populates the export file path with intelligent defaults: `[Instrument]_[Timeframe]_[Timestamp].csv`.
* **Chronological Absolute Indexing:** Pulls historical bar data using `Bars.Get*()` methods, preserving true chronological order (oldest bar to newest bar).
* **Locale Agnostic:** Formats all decimal fields (prices and volumes) using `CultureInfo.InvariantCulture` (`.` decimal separator) to ensure compatibility with Python/Pandas, R, Excel, and backtesting platforms worldwide.

---

## How It Works

### 1. Canvas-Level UI Integration (`UserControlCollection`)
Rather than injecting buttons into fragile, workspace-dependent toolbars (`Chart.MainMenu`), the script uses NinjaTrader's recommended `UserControlCollection`. 
* Avoids visual tree race conditions when loading workspaces.
* Anchors seamlessly to the active chart panel without bleeding over into other chart tabs or floating windows.

### 2. Thread-Safe Bar Iteration
When the button is clicked on the WPF UI thread, file writing is dispatched to NinjaTrader's data thread using `TriggerCustomEvent()`. 
* Prevents concurrency locks or race conditions with incoming market ticks.
* Reads directly from loaded chart memory rather than querying raw database archives, ensuring the export matches **exactly what you see on the chart screen**.

---

## CSV Output Schema

The exported file uses standard comma-delimited columns formatted as follows:

```text
Date,Open,High,Low,Close,Volume
2026-03-30 08:30:00,5120.25,5122.50,5119.75,5121.00,1425
2026-03-30 08:31:00,5121.00,5124.00,5120.50,5123.75,980
2026-03-30 08:32:00,5123.50,5125.00,5122.25,5124.50,1104
...
```

| Field | Format / Type | Description |
| :--- | :--- | :--- |
| **`Date`** | `yyyy-MM-dd HH:mm:ss` | Bar opening timestamp. |
| **`Open`** | Invariant `double` | Bar open price. |
| **`High`** | Invariant `double` | Bar high price. |
| **`Low`** | Invariant `double` | Bar low price. |
| **`Close`** | Invariant `double` | Bar close price. |
| **`Volume`** | Invariant `double` | Total traded volume across the bar. |

---

## Installation

1. Open **NinjaTrader 8**.
2. Press **F5** (or open the NinjaScript Editor via **Tools** > **New NinjaScript** > **Strategy...**).
3. In the NinjaScript Editor tree, right-click **Strategies** > **New Strategy** (or place [`DataExport.cs`](./DataExport.cs) directly inside `Documents\NinjaTrader 8\bin\Custom\Strategies\`).
4. Replace the contents with the code from `DataExport.cs`.
5. Press **F5** to compile.

### Adding to a Chart
1. Open any NinjaTrader chart with the instrument, timeframe, and days of history you want to export.
2. Right-click the chart and select **Strategies** (`Ctrl + S`).
3. Select **DataExport** from the top-left list and click **Add**.
4. In the right-hand properties panel, locate **General** and verify that **`Enabled`** is set to **`True`** *(required for NT8 strategies to initialize)*.
5. Click **OK**. The blue **"Export CSV"** button will immediately appear in the top-left corner of the chart canvas.