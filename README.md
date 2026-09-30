# AudioTapeReader (NinjaTrader 8)

> **Real-time Order Flow Sonification with a 13-Stage Dynamic Blues Progression Engine.**

**AudioTapeReader** translates NinjaTrader 8 Time & Sales trade prints into live, adaptive blues music. Using low-latency Win32 MIDI, the indicator sonifies trade size, tick direction, and market displacement in real time.

Instead of outputting static clicks or disjointed beeps, the tape actively improvises over a **13-stage blues progression**. As price consolidates, breaks out, overextends, or collapses, the harmony shifts dynamically—allowing you to hear market **tension and resolution** without looking at the DOM.

---

## Table of Contents
- [How It Works](#how-it-works)
- [The 13-Stage Harmonic Ladder](#the-13-stage-harmonic-ladder)
- [Displacement Modes](#displacement-modes)
- [Parameter Reference](#parameter-reference)
- [General MIDI Instrument Reference](#general-midi-instrument-reference)
- [Instrument Tuning Recommendations](#instrument-tuning-recommendations)
- [Installation](#installation)
- [Architecture & Performance](#architecture--performance)

---

## How It Works

### 1. The Dynamic Harmonic Anchor (Tick-by-Tick EMA)
The indicator maintains a running exponential moving average (`anchorPrice`) on every incoming trade execution. This anchor represents the market's current **center of gravity (fair value)**.
* When price hovers near the anchor, the music stays at **Home Base ($C^7$)**.
* When price pushes away from the anchor, the harmonic stage advances step-by-step into higher tension chords.
* When price pauses or mean-reverts, the anchor catches up, **resolving the music back to the home key**.

### 2. The Unified C Blues Scale
All single-print notes are mapped to a 3-octave **C Blues Scale** (C, E♭, F, F♯/G♭, G, B♭):
* **No Sour Intervals:** Stripping out natural diatonic fourths and sevenths eliminates the harsh tritone dissonances common in standard major scales.
* **The "Blue Note" (F♯ / G♭):** Embedded into every octave (MIDI 30, 42, 54, 66, 78, 90) to give momentum bursts their gritty, acoustic character.
* **Automatic Key Transposition:** When the trend shifts the active chord stage (e.g., from C7 to F7), single-print executions immediately transpose their scale degrees to solo cleanly over the active harmony.

### 3. Native Win32 MIDI Output (`winmm.dll`)
Audio is generated via direct P/Invoke calls into the native Windows Multimedia subsystem. Notes are synthesized by the built-in **Microsoft GS Wavetable Synth** at the hardware level:
* **Zero Audio Files:** No `.wav` or `.mp3` loading.
* **Sub-Millisecond Latency:** Notes trigger instantly on raw `MarketDataType.Last` events.
* **Polyphonic Voicing:** Fast book sweeps strike rich, multi-voice chords simultaneously.

---

## The 13-Stage Harmonic Ladder

Price displacement away from the harmonic anchor is divided into **13 discrete stages** (from `-6` in the sub-bass abyss to `+6` in the cosmic stratosphere):

```text
[ +6 ] +48 Ticks ── Cosmic Climax ── C7 (Octave 6) [High ringing celebration]
[ +5 ] +40 Ticks ── Stratosphere ── G7 (Octave 5) [Screaming dominant tension]
[ +4 ] +32 Ticks ── High Altitude ── F7 (Octave 5) [Ascending breakout power]
[ +3 ] +24 Ticks ── Breakout Euphoria ── C7 (Octave 5) [Triumphant run]
[ +2 ] +16 Ticks ── Dominant Climax ── G7 Dominant [Peak trend extension]
[ +1 ] +8 Ticks ── Momentum Lift ── F7 Subdominant [Bulls taking control]
────────────────────────────────────────────────────────────────────────────────────────
[ 0 ] 0 Ticks ── HOME BASE ── C7 Blues [Value / Equilibrium / Chop]
────────────────────────────────────────────────────────────────────────────────────────
[ -1 ] -8 Ticks ── Minor Hesitation ── Cm7 [First sign of weakness]
[ -2 ] -16 Ticks ── Bearish Momentum ── Fm7 [Sellers pressing]
[ -3 ] -24 Ticks ── Deep Selling ── Gm7 [Heavy momentum drop]
[ -4 ] -32 Ticks ── Liquidity Cascade ── Bb7 [Support collapsing]
[ -5 ] -40 Ticks ── Capitulation ── Ab7 [Tritone panic chord]
[ -6 ] -48 Ticks ── The Abyss ── Cm7 (Sub-Bass) [Rumbling capitulation floor]
```

---

## Displacement Modes

You can customize how tick distance and volume affect audio via the **`Mode`** setting:

| Mode | Pitch Control | Loudness (Velocity) Control | Sweep Behavior ($\ge 2$ Ticks) | Best Used For |
| :--- | :--- | :--- | :--- | :--- |
| **`SweepStutter`** *(Default)* | Volume ($\log_2$) transposed to active stage | Volume ($\log_2$) | Strikes **2 to 4-note polyphonic chords** matching the active stage. | **Full experience:** Turns the tape into an adaptive blues progression. |
| **`HybridMultiplier`** | Volume ($\log_2$) amplified exponentially by $\text{Ticks}^{1.25}$ | Volume ($\log_2$) + 15 velocity sweep bonus | Single-note strike thrown into higher/lower octaves on slippage. | Momentum trading; flags book thinning and slippage instantly. |
| **`PurePhysics`** | Pure tick displacement ($\Delta\text{Ticks}$) | Trade volume ($\log_2 \times 6$) | Decouples mass from distance. Single notes only. | Microstructure purists: absorption sounds loud and low; vacuums sound quiet and high. |
| **`VolumeOnly`** | Trade volume ($\log_2$) | Trade volume ($\log_2$) | Classic audio tape reading. Price movement only dictates direction. | Rhythmic sizing over thick books (e.g., Treasuries). |

---

## Parameter Reference

### Progression & Anchor Settings
* **`TicksPerChord`** *(Default: `8`, Range: `2` to `50`)*
  The distance (in ticks) price must stretch away from the harmonic anchor to advance to the next chord stage.
  * *Example (ES):* A value of `8` means the chord shifts every $2.00$ points ($8 \text{ ticks} \times 0.25$).
* **`AnchorSpeed`** *(Default: `0.01`, Range: `0.001` to `0.1`)*
  The smoothing factor ($\alpha$) of the tick-by-tick harmonic anchor (EMA).
  * **Higher (`0.03 – 0.05`):** Anchor tracks price rapidly. Chords advance only during explosive bursts and quickly reset back to $C^7$ when movement slows. Ideal for micro-scalpers.
  * **Lower (`0.002 – 0.005`):** Anchor has a long memory. Chords remain elevated throughout extended runs. Ideal for trend runners and fast instruments (NQ).

### Musical & Audio Settings
* **`Mode`** *(Default: `SweepStutter`)*
  Selects the calculation model for multi-tick sweeps and pitch mapping.
* **`MidiInstrument`** *(Default: `16` - Drawbar Organ)*
  The General MIDI Program Change number sent to Channel 0.
* **`PitchSensitivity`** *(Default: `1.8`, Range: `0.1` to `10.0`)*
  The multiplier scaling logarithmic volume to note pitch steps.
  * **Higher (`2.5 – 4.0`):** Wider keyboard spread. Useful on thin instruments where lot sizes are small (NQ, Crypto).
  * **Lower (`0.8 – 1.2`):** Compressed keyboard spread. Useful on thick contracts with high lot variance (ES, ZN).
* **`CenterNote`** *(Default: `60`, Range: `0` to `127`)*
  The central root reference note (MIDI `60` = Middle C / $C_4$).
* **`BaseVelocity`** *(Default: `85`, Range: `1` to `127`)*
  The baseline MIDI strike velocity (volume/loudness). Volume-scaling dynamically increases velocity up to the MIDI maximum of `127`.
* **`MinVolumeFilter`** *(Default: `1`, Range: `1` to $\infty$)*
  Filters out trades smaller than this contract size to eliminate tape chatter.

---

## General MIDI Instrument Reference

Change the **`MidiInstrument`** property to swap out sound banks:

| Patch ID | Instrument Name | Acoustic Character |
| :---: | :--- | :--- |
| **`16`** *(Default)* | **Drawbar Organ (Hammond B3)** | Warm, bluesy, natural sustain. Excellent for polyphonic sweep chords. |
| **`18`** | **Rock Organ** | Gritty, aggressive, cuts through background noise easily. |
| **`12`** | **Marimba** | Woody, percussive, fast decay. Zero clutter in ultra-fast order flow. |
| **`13`** | **Xylophone** | Bright, sharp attack, distinct individual tick prints. |
| **`11`** | **Vibraphone** | Mellow, jazzy, clean harmonic sustain. |
| **`115`** | **Woodblock** | Clicky, minimalist, dry percussive tape sonification. |

---

## Instrument Tuning Recommendations

| Market | TicksPerChord | AnchorSpeed | PitchSensitivity | MinVolumeFilter | Preferred Instrument |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **E-mini S&P 500 (ES)** | `8` | `0.010` | `1.8` | `1` | `16` (Drawbar Organ) |
| **Micro E-mini (MES)** | `8` | `0.010` | `1.4` | `5` | `16` (Drawbar Organ) |
| **Nasdaq 100 (NQ)** | `24` | `0.004` | `3.0` | `1` | `12` (Marimba) / `18` (Rock Organ) |
| **Crude Oil (CL)** | `10` | `0.008` | `2.0` | `1` | `16` (Drawbar Organ) |
| **10Y Treasury (ZN)** | `4` | `0.020` | `1.0` | `25` | `12` (Marimba) / `115` (Woodblock) |

---

## Installation

1. Open **NinjaTrader 8**.
2. Navigate to **Tools** > **New NinjaScript** > **Indicator...**
3. In the NinjaScript Editor window, open the newly created indicator (or paste into an existing file inside the `Indicators` folder).
4. Replace the entire contents of the file with the code from [`AudioTapeReader.cs`](./AudioTapeReader.cs).
5. Press **F5** to compile.
6. Open any chart, press **Ctrl + I**, select **AudioTapeReader**, adjust parameters, and click **OK**.

> **Note:** Ensure your chart is connected to a live or simulated real-time market data feed. For safety, audio synthesis will only initialize in `State.Realtime` to avoid playing historical data during chart load.

---

## Architecture & Performance

* **Garbage Collector Friendly:** Avoids allocations in `OnMarketData`. Lookups use pre-allocated value arrays, bitwise operators, and fixed structs.
* **Namespace Isolation:** The `DisplacementMode` enum is placed directly under `NinjaTrader.NinjaScript`, allowing the indicator to compile cleanly alongside NinjaTrader's auto-generated `MarketAnalyzerColumns` and `Strategies` wrapper classes without type-resolution conflicts (`CS0246`).
* **Resource Safety:** Audio handles (`midiHandle`) are cleanly released via `midiOutClose` in `State.Terminated` to prevent Windows unmanaged handle leaks.

---
