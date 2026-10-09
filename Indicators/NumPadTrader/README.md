# NumPadTraderV2 (NinjaTrader 8)

> **Use Kaypad as shortcuts for entry and exit orders and SL TP bracket management, good for scalping.**

---

## Table of Contents
- [Features](#features)
- [Complete Hotkey Reference](#complete-hotkey-reference)
- [Bracket & Target Architecture](#bracket--target-architecture)
- [Adaptive Step Adjustment Engine](#adaptive-step-adjustment-engine)
- [Chart Trader Account Synchronization](#chart-trader-account-synchronization)
- [Parameter Reference](#parameter-reference)
- [Installation](#installation)

---

## Features

* **Sub-Millisecond Order Entry:** Direct market (`Buy Mkt`, `Sell Mkt`) and passive liquidity limit orders (`Buy Bid`, `Sell Ask`) sent directly to the broker via hotkeys.
* **Smart OCO Brackets:** One-key bracket placement that dynamically detects open position average price, calculates tick-rounded SL/TP boundaries, and optionally splits into dual targets (`TP1` and `TP2`).
* **Adaptive Stepping (Coarse / Fine):** Move active stops closer to price on the fly. Automatically switches from coarse point steps to a 1.0-point fine step once within the threshold buffer to prevent accidental order rejections.
* **Chart Trader Synchronization:** Dynamically tracks the account currently selected in your active chart's Chart Trader window, eliminating account mismatch errors.
* **NumLock Agnostic:** Built-in fallback mapping for keypad navigation keys (`Insert`, `Home`, `Page Up`, `Left`, `Clear`, `Up`) so hotkeys execute whether NumLock is enabled or disabled.
* **Panic Flatten:** Instant Cancel All Orders + Flatten Position execution on `Enter`.

---

## Complete Hotkey Reference

| Key (NumLock ON) | Key (NumLock OFF) | Action | Modifiers | Behavior |
| :---: | :---: | :--- | :---: | :--- |
| **`Num 4`** | `Left Arrow` | **Buy Market** | None | Sends a Market Buy order for configured quantity. |
| **`Num 5`** | `Clear` (Center 5) | **Sell Market** | None | Sends a Market Sell order for configured quantity. |
| **`Num 7`** | `Home` | **Buy Bid (Limit)** | None | Places passive Buy Limit at current inside Bid. |
| **`Num 8`** | `Up Arrow` | **Sell Ask (Limit)** | None | Places passive Sell Limit at current inside Ask. |
| **`Num 0`** | `Insert` | **Attach Brackets** | None | Attaches OCO SL/TP bracket to current position or working order. |
| **`Num 9`** | `Page Up` | **Tighten Stop Loss** | None | Moves all active Stop Loss orders closer to market price. |
| **`Num 9`** | `Page Up` | **Widen Stop Loss** | `Shift` or `Ctrl` | Moves all active Stop Loss orders further away from market price. |
| **`Num +`** | `+` (Plus) | **Bring TP Closer** | None | Moves innermost Take Profit limit order closer to current price. |
| **`Num +`** | `+` (Plus) | **Extend TP Further** | `Shift` or `Ctrl` | Moves innermost Take Profit limit order further away into trend. |
| **`Enter`** | `Enter` | **Panic Flatten** | None | Cancels all working orders and flattens active position immediately. |

---

## Bracket & Target Architecture

Pressing **`Num 0`** inspects your active position or working orders and builds standard NinjaTrader OCO (One-Cancels-Other) groups:

```text
                     ▲ +TakeProfitPoints + TP2OffsetPoints ──► [ TP2 Limit (Qty / 2) ] (OCO #2)
                     │
                     ▲ +TakeProfitPoints ───────────────────► [ TP1 Limit (Qty / 2) ] (OCO #1)
                     │
  [ Entry Price ] ───┼──────────────────────────────────────────────────────────────────────────
                     │
                     ▼ -StopLossPoints ─────────────────────► [ SL Stop Market (All Qty) ]
```

* **Dual-Target Splitting:** When `SplitTargets = True` and position quantity $\ge 2$, the exit splits evenly between runner and scalp targets.
* **Cleanup on Entry:** If `CancelOldExitsFirst = True`, any existing un-filled working orders on the contract are purged before placing new brackets, avoiding accidental position flips.

---

## Adaptive Step Adjustment

When adjusting orders dynamically during fast price action (via **`Num 9`** or **`Num +`**), orders scale automatically between coarse and fine steps:

```text
[ Live Market Price ]
         ▲
         │   [ MinBufferPoints Zone ]  ──► Movement BLOCKED (Safety limit)
         ├──────────────────────────────
         │   Within Limit Zone         ──► Fine Step (Default: 1.0 Point)
         ├──────────────────────────────
         │   Standard Distance         ──► Coarse Step (Default: 2.0 Points)
         ▼
[ Active Stop Loss Order ]
```

* **`MinBufferPoints`:** Protects against moving a stop order too close to live bid/ask, preventing exchange rejection errors.
* **Stop-Limit Preservation:** If using Stop-Limit orders, the offset delta between Stop and Limit triggers is automatically preserved when shifted.

---

## Chart Trader Account Synchronization

`NumPadTraderV2` attaches directly to the WPF chart control:

1. **Auto Detection:** Reads `ChartControl.OwnerChart.ChartTrader.Account` dynamically. When you switch accounts in the Chart Trader drop-down (e.g., from `Sim101` to an evaluation/live account), hotkeys immediately route orders to the newly selected account.
2. **Fallback Safe:** If Chart Trader is hidden or turned off on the chart, execution safely falls back to the configured `FallbackAccount` property.

---

## Parameter Reference

### 1. Order Parameters
| Parameter | Default | Description |
| :--- | :---: | :--- |
| **`Quantity`** | `2` | Default order size for entry keys (`Num 4`, `5`, `7`, `8`). |
| **`TakeProfitPoints`** | `5.0` | Target distance (in index/instrument points) from entry. |
| **`TP2OffsetPoints`** | `5.0` | Additional points beyond TP1 for the runner target. |
| **`StopLossPoints`** | `5.0` | Stop loss distance (in points) from entry. |
| **`SplitTargets`** | `True` | Automatically divides brackets into TP1 and TP2 if Quantity $\ge 2$. |
| **`CalcFromEntryPrice`** | `True` | Calculates bracket offsets from position average price (`True`) or current market price (`False`). |
| **`CancelOldExitsFirst`**| `True` | Cancels pre-existing working orders on the symbol before submitting new brackets. |
| **`FallbackAccount`** | `"Sim101"` | Account used if Chart Trader is disabled or hidden on the chart window. |

### 2. Adjustments
| Parameter | Default | Description |
| :--- | :---: | :--- |
| **`StopAdjustStepPoints`** | `2.0` | Standard points shifted per press of `Num 9`. |
| **`TPAdjustStepPoints`** | `2.0` | Standard points shifted per press of `Num +`. |
| **`FineAdjustStepPoints`** | `1.0` | Reduced step applied when orders are close to the price threshold. |
| **`MinBufferPoints`** | `1.0` | Minimum allowed distance from the live market to prevent order rejection. |

---

## Installation

1. Place [`NumPadTraderV2.cs`](./NumPadTraderV2.cs) into:  
   `Documents\NinjaTrader 8\bin\Custom\Indicators\`
2. Open NinjaTrader 8 and press **F5** in the NinjaScript Editor to compile.
3. Open any chart, press **Ctrl + I**, select **NumPadTraderV2**, configure your point parameters, and click **OK**.
4. Click once on the chart canvas to ensure the chart window has keyboard focus.

⚠️ Risk & Financial Disclaimer
This software is for educational, research, and entertainment purposes only. 
It does not constitute financial, investment, or trading advice. 
Futures, options, and equities trading involve substantial risk of loss and are not suitable for every investor. 
The author assumes no responsibility or liability for any financial losses incurred from using this indicator.