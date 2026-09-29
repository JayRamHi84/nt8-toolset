# nt8-toolset
# Collection of indicators or ideas
* ** 1- AudioTapeReader

# AudioTapeReader (NinjaTrader 8)

> **Real-time order-flow sonification for NinjaTrader 8 using native Windows MIDI.**

**AudioTapeReader** translates Time & Sales tape prints into dynamic, real-time audio. By leveraging the native Windows Multimedia API (`winmm.dll`), it maps live trade executions directly to MIDI notes and velocities without requiring external audio files, sound packs, or third-party audio engines.

---

### Features

* **Directional Pitch Mapping:**
  * **Upticks:** Play ascending natural notes (white keys) above Middle C ($C_4$ / MIDI 60).
  * **Downticks:** Play descending accidentals (black keys) below Middle C.
  * Tick direction is immediately identifiable by ear through the tonal contrast between diatonic and pentatonic intervals.

* **Logarithmic Volume Dynamics:**
  * **Pitch Spread:** Larger volume trades trigger notes further from the center note (`PitchSensitivity * log2(volume)`).
  * **Dynamic Velocity:** Higher print volume automatically increases strike velocity (volume/loudness) up to the MIDI maximum of 127.

* **Ultra-Low Latency & Zero External Assets:**
  * Direct P/Invoke to `winmm.dll` outputs straight to the default Windows MIDI synthesizer (Microsoft GS Wavetable Synth) with virtually zero audio latency.
  * Only triggers on live trade events (`MarketDataType.Last` in `State.Realtime`).

---

### Parameters

| Parameter | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `CenterNote` | `int` | `60` | Root note (MIDI 60 = Middle C). |
| `PitchSensitivity` | `double` | `2.0` | Multiplier for logarithmic note distance based on volume. |
| `MinVolumeFilter` | `long` | `1` | Filters out prints below this contract/share threshold. |
| `MidiInstrument` | `int` | `12` | General MIDI Program Number (e.g., `12` = Marimba, `13` = Xylophone, `115` = Woodblock). |
| `BaseVelocity` | `int` | `90` | Minimum strike velocity (loudness, 0–127). |

---

### Installation

1. Open NinjaTrader 8.
2. Go to **Tools** > **NinjaScript Editor**.
3. In the right-hand panel, right-click **Indicators** > **New Indicator...** (or paste directly into an existing `.cs` file).
4. Paste the code, press **F5** to compile, and attach **AudioTapeReader** to any chart.
