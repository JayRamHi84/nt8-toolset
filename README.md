# NT8-Toolset

> **A curated collection of open-source NinjaTrader 8 indicators, strategies, and execution utilities.**

`nt8-toolset` provides production-ready, high-performance NinjaScript tools designed for order flow traders, algorithmic developers, and quantitative researchers. All tools are built with memory-safe design, zero external bloat, and clean C# architecture.

---

## 🛠 Tools Catalog

### Indicators

| Tool | Category | Description | Docs |
| :--- | :--- | :--- | :---: |
| **[AudioTapeReader](./Indicators/AudioTapeReader)** | Order Flow / Audio | Real-time Time & Sales sonification using a 13-stage dynamic blues progression engine via Win32 MIDI. | [View Docs](./Indicators/AudioTapeReader/README.md) |

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