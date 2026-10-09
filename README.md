# NT8-Toolset

> **My collection of indicators, tools and ideas for Ninja Trader 8.**

---

## 🛠 Tools Catalog

### Indicators

| Tool | Category | Description | Docs |
| :--- | :--- | :--- | :---: |
| **[AudioTapeReader](./Indicators/AudioTapeReader)** | Order Flow / Audio | Real-time Time & Sales sonification using a 13-stage dynamic blues progression engine via Win32 MIDI. | [View Docs](./Indicators/AudioTapeReader/README.md) |
| **[NumPadTraderV2](./Indicators/NumPadTraderV2)** | Execution / Hotkeys | Low-latency numeric keypad execution console with dynamic OCO brackets, adaptive step management, and Chart Trader sync. | [View Docs](./Indicators/NumPadTraderV2/README.md) |

### Strategies & Utilities

| Tool | Category | Description | Docs |
| :--- | :--- | :--- | :---: |
| **[DataExport](./Strategies/DataExport)** | Utility / Data | Native on-chart UI button to export currently loaded bar series directly to clean, chronological CSV. | [View Docs](./Strategies/DataExport/README.md) |

---

## 📂 Repository Structure

```text
nt8-toolset/
├── README.md               # Main repository catalog
├── LICENSE
├── Indicators/             # Custom NT8 Indicators
│   └── AudioTapeReader/
│       ├── AudioTapeReader.cs
│       └── README.md
└── Strategies/             # Custom NT8 Strategies
    └── DataExport/
        ├── DataExport.cs
        └── README.md
```

---

## 🚀 Global Installation Guide

### Option 1: Direct File Copy (Recommended)
1. Clone or download this repository to your PC.
2. Copy the `.cs` files directly to your NinjaTrader 8 directories:
   * **Indicators:** Copy the `.cs` file from `Indicators/<ToolName>/` to:  
     `Documents\NinjaTrader 8\bin\Custom\Indicators\`
   * **Strategies:** Copy the `.cs` file from `Strategies/<ToolName>/` to:  
     `Documents\NinjaTrader 8\bin\Custom\Strategies\`
3. Open NinjaTrader 8.
4. Open the NinjaScript Editor (**Tools** > **New NinjaScript Editor** or press **F5**) and compile.

### Option 2: Copy-Paste via NinjaScript Editor
1. In NinjaTrader 8, go to **Tools** > **New NinjaScript** > **Indicator** (or **Strategy**).
2. Set the name to match the script (e.g., `DataExport`).
3. Copy the code from the corresponding `.cs` file in this repository and replace the generated template.
4. Press **F5** to compile.

---

## ⚡ Performance Guidelines

All scripts in this repository follow strict performance best practices:
* **Garbage Collection (GC) Safe:** Minimal allocations on `OnBarUpdate()` and `OnMarketData()` to prevent latency spikes during high-volatility events.
* **Thread Marshalling:** UI operations use WPF `Dispatcher`, while data-layer interactions are safely pushed to the data thread via `TriggerCustomEvent()`.
* **Native Win32 Integration:** Hardware and low-level subsystem access (such as MIDI synthesis or disk streaming) utilize native Windows APIs without external wrapper dependencies.

---

## 📄 License

This repository is licensed under the [MIT License](LICENSE). You are free to modify, distribute, and integrate these scripts into your own commercial or private trading setups.

⚠️ Risk & Financial Disclaimer
This software is for educational, research, and entertainment purposes only. 
It does not constitute financial, investment, or trading advice. 
Futures, options, and equities trading involve substantial risk of loss and are not suitable for every investor. 
The author assumes no responsibility or liability for any financial losses incurred from using this indicator.
