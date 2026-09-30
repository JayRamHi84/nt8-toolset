#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript
{
    public enum DisplacementMode
    {
        VolumeOnly,
        PurePhysics,
        HybridMultiplier,
        SweepStutter
    }
}

namespace NinjaTrader.NinjaScript.Indicators
{
    public class AudioTapeReader : Indicator
    {
        #region Win32 MIDI API P/Invoke
        [DllImport("winmm.dll")]
        private static extern int midiOutOpen(out IntPtr lphMidiOut, int uDeviceID, IntPtr dwCallback, IntPtr dwInstance, int dwFlags);

        [DllImport("winmm.dll")]
        private static extern int midiOutShortMsg(IntPtr hMidiOut, int dwMsg);

        [DllImport("winmm.dll")]
        private static extern int midiOutClose(IntPtr hMidiOut);

        private IntPtr midiHandle = IntPtr.Zero;
        #endregion

        // C Blues Scale Base Intervals (relative semitones from Root):
        // C(0), Eb(3), F(5), Gb(6 - Blue Note), G(7), Bb(10)
        private readonly int[] bluesScaleTemplate = new int[] 
        { 
            0, 3, 5, 6, 7, 10, 
            12, 15, 17, 18, 19, 22, 
            24, 27, 29, 30, 31, 34 
        };

        // Data structure to hold each harmonic stage
        private struct HarmonicStage
        {
            public int[] ChordNotes;
            public int RootOffset;

            public HarmonicStage(int[] notes, int offset)
            {
                ChordNotes = notes;
                RootOffset = offset;
            }
        }

        // 13 Harmonic Stages: Index 0 = Stage -6 (Abyss) up to Index 12 = Stage +6 (Cosmic Climax)
        private readonly HarmonicStage[] progressionLadder = new HarmonicStage[]
        {
            // Index 0 (Stage -6): The Abyss -> Sub-Bass Cm7
            new HarmonicStage(new int[] { 36, 39, 43, 46 }, -12),

            // Index 1 (Stage -5): Capitulation / Panic -> Ab7
            new HarmonicStage(new int[] { 44, 48, 51, 54 }, -8),

            // Index 2 (Stage -4): Liquidity Cascade -> Bb7
            new HarmonicStage(new int[] { 46, 50, 53, 56 }, -6),

            // Index 3 (Stage -3): Deep Selling -> Gm7
            new HarmonicStage(new int[] { 43, 46, 50, 53 }, -5),

            // Index 4 (Stage -2): Bearish Momentum -> Fm7
            new HarmonicStage(new int[] { 41, 44, 48, 51 }, -7),

            // Index 5 (Stage -1): Minor Hesitation -> Cm7
            new HarmonicStage(new int[] { 48, 51, 55, 58 }, 0),

            // Index 6 (Stage  0): HOME BASE -> C7 Blues Equilibrium
            new HarmonicStage(new int[] { 60, 64, 67, 70 }, 0),

            // Index 7 (Stage +1): Momentum Lift -> F7
            new HarmonicStage(new int[] { 65, 69, 72, 75 }, 5),

            // Index 8 (Stage +2): Dominant Climax -> G7
            new HarmonicStage(new int[] { 67, 71, 74, 77 }, 7),

            // Index 9 (Stage +3): Breakout Euphoria -> C7 Octave 5
            new HarmonicStage(new int[] { 72, 76, 79, 82 }, 12),

            // Index 10 (Stage +4): High Altitude Push -> F7 Octave 5
            new HarmonicStage(new int[] { 77, 81, 84, 87 }, 17),

            // Index 11 (Stage +5): Stratosphere Screamer -> G7 Octave 5
            new HarmonicStage(new int[] { 79, 83, 86, 89 }, 19),

            // Index 12 (Stage +6): Cosmic Climax -> C7 Octave 6
            new HarmonicStage(new int[] { 84, 88, 91, 94 }, 24)
        };

        private double lastPrice = 0.0;
        private int lastDirection = 0;
        private double anchorPrice = 0.0; // Running EMA harmonic anchor

        #region Indicator Properties
        [NinjaScriptProperty]
        [Display(Name = "Displacement Mode", Description = "Method used to map tick extension to sound", Order = 1, GroupName = "Parameters")]
        public DisplacementMode Mode { get; set; } = DisplacementMode.SweepStutter;

        [NinjaScriptProperty]
        [Range(2, 50)]
        [Display(Name = "Ticks Per Chord Progression", Description = "Number of ticks required to advance to the next chord stage", Order = 2, GroupName = "Parameters")]
        public int TicksPerChord { get; set; } = 8;

        [NinjaScriptProperty]
        [Range(0.001, 0.1)]
        [Display(Name = "Anchor Tracking Speed", Description = "How fast the harmonic anchor follows price (0.01 = smooth/slow, 0.05 = fast)", Order = 3, GroupName = "Parameters")]
        public double AnchorSpeed { get; set; } = 0.01;

        [NinjaScriptProperty]
        [Range(0, 127)]
        [Display(Name = "Center Note", Description = "Base root note (60 = Middle C)", Order = 4, GroupName = "Parameters")]
        public int CenterNote { get; set; } = 60;

        [NinjaScriptProperty]
        [Range(0.1, 10.0)]
        [Display(Name = "Pitch Sensitivity", Description = "Multiplier for pitch displacement scaling", Order = 5, GroupName = "Parameters")]
        public double PitchSensitivity { get; set; } = 1.8;

        [NinjaScriptProperty]
        [Range(1, long.MaxValue)]
        [Display(Name = "Min Volume Filter", Description = "Ignore trade executions below this size", Order = 6, GroupName = "Parameters")]
        public long MinVolumeFilter { get; set; } = 1;

        [NinjaScriptProperty]
        [Range(0, 127)]
        [Display(Name = "MIDI Instrument", Description = "12 = Marimba, 16 = Drawbar Organ, 18 = Rock Organ", Order = 7, GroupName = "Parameters")]
        public int MidiInstrument { get; set; } = 16; // Hammond Organ default

        [NinjaScriptProperty]
        [Range(1, 127)]
        [Display(Name = "Base Velocity", Description = "Base strike loudness (0 - 127)", Order = 8, GroupName = "Parameters")]
        public int BaseVelocity { get; set; } = 85;
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"13-Stage Dynamic Blues Progression Tape Reader.";
                Name = "AudioTapeReader";
                Calculate = Calculate.OnEachTick;
                IsOverlay = true;
            }
            else if (State == State.DataLoaded)
            {
                lastPrice = 0.0;
                lastDirection = 0;
                anchorPrice = 0.0;

                int result = midiOutOpen(out midiHandle, -1, IntPtr.Zero, IntPtr.Zero, 0);
                if (result == 0 && midiHandle != IntPtr.Zero)
                {
                    int programChangeMsg = 0xC0 | (MidiInstrument << 8);
                    midiOutShortMsg(midiHandle, programChangeMsg);
                }
            }
            else if (State == State.Terminated)
            {
                if (midiHandle != IntPtr.Zero)
                {
                    midiOutClose(midiHandle);
                    midiHandle = IntPtr.Zero;
                }
            }
        }

        protected override void OnMarketData(MarketDataEventArgs e)
        {
            if (State != State.Realtime || e.MarketDataType != MarketDataType.Last || midiHandle == IntPtr.Zero)
                return;

            double currentPrice = e.Price;
            long volume = e.Volume;

            if (volume < MinVolumeFilter)
                return;

            double tickSize = (Instrument != null && Instrument.MasterInstrument != null && Instrument.MasterInstrument.TickSize > 0) 
                ? Instrument.MasterInstrument.TickSize 
                : 0.25;

            // 1. Update running harmonic anchor (EMA)
            if (anchorPrice == 0.0)
                anchorPrice = currentPrice;
            else
                anchorPrice = (currentPrice * AnchorSpeed) + (anchorPrice * (1.0 - AnchorSpeed));

            // 2. Calculate tick displacement and direction
            int ticksMoved = 0;
            int direction = 0;

            if (lastPrice != 0.0)
            {
                if (currentPrice > lastPrice)
                {
                    direction = 1;
                    ticksMoved = (int)Math.Max(1, Math.Round((currentPrice - lastPrice) / tickSize));
                }
                else if (currentPrice < lastPrice)
                {
                    direction = -1;
                    ticksMoved = (int)Math.Max(1, Math.Round((lastPrice - currentPrice) / tickSize));
                }
                else
                {
                    direction = lastDirection;
                    ticksMoved = 0;
                }
            }

            lastPrice = currentPrice;
            lastDirection = direction;

            // 3. Resolve active stage on the 13-stage progression ladder (-6 to +6)
            int ticksFromAnchor = (int)Math.Round((currentPrice - anchorPrice) / tickSize);
            int rawStage = (int)Math.Floor(ticksFromAnchor / (double)TicksPerChord);
            
            // Map stage [-6 to +6] into array index [0 to 12]
            int ladderIndex = Math.Max(0, Math.Min(12, rawStage + 6));
            HarmonicStage activeStage = progressionLadder[ladderIndex];

            // 4. Calculate Pitch Step and Dynamic Velocity
            int stepIndex = 0;
            int dynamicVelocity = BaseVelocity;
            double safeVolume = Math.Max(1, volume);

            switch (Mode)
            {
                case DisplacementMode.VolumeOnly:
                    stepIndex = (int)Math.Round(PitchSensitivity * Math.Log(safeVolume, 2));
                    dynamicVelocity = (int)Math.Min(127, BaseVelocity + (Math.Log(safeVolume, 2) * 4));
                    break;

                case DisplacementMode.PurePhysics:
                    stepIndex = (int)Math.Round(PitchSensitivity * ticksMoved);
                    dynamicVelocity = (int)Math.Min(127, BaseVelocity + (Math.Log(safeVolume, 2) * 6));
                    break;

                case DisplacementMode.HybridMultiplier:
                    double extension = (ticksMoved <= 1) ? 1.0 : Math.Pow(ticksMoved, 1.25);
                    stepIndex = (int)Math.Round(PitchSensitivity * Math.Log(safeVolume, 2) * extension);
                    int accent = (ticksMoved > 1) ? 15 : 0;
                    dynamicVelocity = (int)Math.Min(127, BaseVelocity + (Math.Log(safeVolume, 2) * 4) + accent);
                    break;

                case DisplacementMode.SweepStutter:
                    stepIndex = (int)Math.Round(PitchSensitivity * Math.Log(safeVolume, 2));
                    dynamicVelocity = (int)Math.Min(127, BaseVelocity + (Math.Log(safeVolume, 2) * 4));
                    break;
            }

            stepIndex = Math.Max(0, Math.Min(stepIndex, bluesScaleTemplate.Length - 1));

            // 5. Sound Output: Chords on Multi-Tick Sweeps, Transposed Blues Notes on Single Trades
            if (Mode == DisplacementMode.SweepStutter && ticksMoved > 1)
            {
                // Play active chord (2 to 4 notes voiced dynamically)
                int chordSize = Math.Min(ticksMoved, activeStage.ChordNotes.Length);

                for (int i = 0; i < chordSize; i++)
                {
                    int note = activeStage.ChordNotes[i];
                    int noteVelocity = Math.Max(40, dynamicVelocity - (i * 6));
                    int noteMsg = 0x90 | (note << 8) | (noteVelocity << 16);
                    midiOutShortMsg(midiHandle, noteMsg);
                }
            }
            else
            {
                // Play a single note transposed to match the active progression stage
                int baseNote = (direction >= 0) 
                    ? CenterNote + activeStage.RootOffset + bluesScaleTemplate[stepIndex]
                    : CenterNote + activeStage.RootOffset - bluesScaleTemplate[stepIndex];

                baseNote = Math.Max(20, Math.Min(108, baseNote)); // Clamp to standard piano range

                int noteOnMsg = 0x90 | (baseNote << 8) | (dynamicVelocity << 16);
                midiOutShortMsg(midiHandle, noteOnMsg);
            }
        }

        protected override void OnBarUpdate() { }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private AudioTapeReader[] cacheAudioTapeReader;
		public AudioTapeReader AudioTapeReader(DisplacementMode mode, int ticksPerChord, double anchorSpeed, int centerNote, double pitchSensitivity, long minVolumeFilter, int midiInstrument, int baseVelocity)
		{
			return AudioTapeReader(Input, mode, ticksPerChord, anchorSpeed, centerNote, pitchSensitivity, minVolumeFilter, midiInstrument, baseVelocity);
		}

		public AudioTapeReader AudioTapeReader(ISeries<double> input, DisplacementMode mode, int ticksPerChord, double anchorSpeed, int centerNote, double pitchSensitivity, long minVolumeFilter, int midiInstrument, int baseVelocity)
		{
			if (cacheAudioTapeReader != null)
				for (int idx = 0; idx < cacheAudioTapeReader.Length; idx++)
					if (cacheAudioTapeReader[idx] != null && cacheAudioTapeReader[idx].Mode == mode && cacheAudioTapeReader[idx].TicksPerChord == ticksPerChord && cacheAudioTapeReader[idx].AnchorSpeed == anchorSpeed && cacheAudioTapeReader[idx].CenterNote == centerNote && cacheAudioTapeReader[idx].PitchSensitivity == pitchSensitivity && cacheAudioTapeReader[idx].MinVolumeFilter == minVolumeFilter && cacheAudioTapeReader[idx].MidiInstrument == midiInstrument && cacheAudioTapeReader[idx].BaseVelocity == baseVelocity && cacheAudioTapeReader[idx].EqualsInput(input))
						return cacheAudioTapeReader[idx];
			return CacheIndicator<AudioTapeReader>(new AudioTapeReader(){ Mode = mode, TicksPerChord = ticksPerChord, AnchorSpeed = anchorSpeed, CenterNote = centerNote, PitchSensitivity = pitchSensitivity, MinVolumeFilter = minVolumeFilter, MidiInstrument = midiInstrument, BaseVelocity = baseVelocity }, input, ref cacheAudioTapeReader);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.AudioTapeReader AudioTapeReader(DisplacementMode mode, int ticksPerChord, double anchorSpeed, int centerNote, double pitchSensitivity, long minVolumeFilter, int midiInstrument, int baseVelocity)
		{
			return indicator.AudioTapeReader(Input, mode, ticksPerChord, anchorSpeed, centerNote, pitchSensitivity, minVolumeFilter, midiInstrument, baseVelocity);
		}

		public Indicators.AudioTapeReader AudioTapeReader(ISeries<double> input , DisplacementMode mode, int ticksPerChord, double anchorSpeed, int centerNote, double pitchSensitivity, long minVolumeFilter, int midiInstrument, int baseVelocity)
		{
			return indicator.AudioTapeReader(input, mode, ticksPerChord, anchorSpeed, centerNote, pitchSensitivity, minVolumeFilter, midiInstrument, baseVelocity);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.AudioTapeReader AudioTapeReader(DisplacementMode mode, int ticksPerChord, double anchorSpeed, int centerNote, double pitchSensitivity, long minVolumeFilter, int midiInstrument, int baseVelocity)
		{
			return indicator.AudioTapeReader(Input, mode, ticksPerChord, anchorSpeed, centerNote, pitchSensitivity, minVolumeFilter, midiInstrument, baseVelocity);
		}

		public Indicators.AudioTapeReader AudioTapeReader(ISeries<double> input , DisplacementMode mode, int ticksPerChord, double anchorSpeed, int centerNote, double pitchSensitivity, long minVolumeFilter, int midiInstrument, int baseVelocity)
		{
			return indicator.AudioTapeReader(input, mode, ticksPerChord, anchorSpeed, centerNote, pitchSensitivity, minVolumeFilter, midiInstrument, baseVelocity);
		}
	}
}

#endregion
