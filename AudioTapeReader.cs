#region Using declarations
using System;
using System.Runtime.InteropServices;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
#endregion

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

        // Up notes: White keys ascending above Middle C (60)
        private readonly int[] upNotes = new int[] 
        { 
            62, 64, 65, 67, 69, 71, 72, 74, 76, 77, 79, 81, 83, 84, 86, 88, 89, 91, 93, 96 
        };

        // Down notes: Black keys descending below Middle C (60)
        private readonly int[] downNotes = new int[] 
        { 
            58, 56, 54, 51, 49, 46, 44, 42, 39, 37, 34, 32, 30, 27, 25, 22 
        };

        private double lastPrice = 0.0;
        private int lastDirection = 0;

        #region Indicator Properties
        [NinjaScriptProperty]
        public int CenterNote { get; set; } = 60; // Middle C

        [NinjaScriptProperty]
        public double PitchSensitivity { get; set; } = 2.0;

        [NinjaScriptProperty]
        public long MinVolumeFilter { get; set; } = 1;

        // 12 = Marimba, 115 = Woodblock, 13 = Xylophone
        [NinjaScriptProperty]
        public int MidiInstrument { get; set; } = 12;

        [NinjaScriptProperty]
        public int BaseVelocity { get; set; } = 90;
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Audio tape reader: Whites for upticks, Blacks for downticks.";
                Name = "AudioTapeReader";
                Calculate = Calculate.OnEachTick;
                IsOverlay = true;
            }
            else if (State == State.DataLoaded)
            {
                lastPrice = 0.0;
                lastDirection = 0;

                int result = midiOutOpen(out midiHandle, -1, IntPtr.Zero, IntPtr.Zero, 0);
                if (result == 0 && midiHandle != IntPtr.Zero)
                {
                    // Select instrument on Channel 0
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
            // Guard: Only process true live trade executions
            if (State != State.Realtime)
                return;

            if (e.MarketDataType != MarketDataType.Last)
                return;

            if (midiHandle == IntPtr.Zero)
                return;

            double currentPrice = e.Price;
            long volume = e.Volume;

            if (volume < MinVolumeFilter)
                return;

            // Determine tick direction
            int direction = 0;
            if (lastPrice != 0.0)
            {
                if (currentPrice > lastPrice)
                    direction = 1;
                else if (currentPrice < lastPrice)
                    direction = -1;
                else
                    direction = lastDirection;
            }

            lastPrice = currentPrice;
            lastDirection = direction;

            // Compute index step based on volume (logarithmic)
            int stepIndex = (int)Math.Round(PitchSensitivity * Math.Log(volume, 2));

            int targetNote = CenterNote;

            if (direction > 0)
            {
                // Select from Natural notes (no sharps)
                stepIndex = Math.Max(0, Math.Min(stepIndex, upNotes.Length - 1));
                targetNote = upNotes[stepIndex];
            }
            else if (direction < 0)
            {
                // Select from Sharps/Flats (black keys only)
                stepIndex = Math.Max(0, Math.Min(stepIndex, downNotes.Length - 1));
                targetNote = downNotes[stepIndex];
            }

            int dynamicVelocity = (int)Math.Min(127, BaseVelocity + (Math.Log(volume, 2) * 4));

            // Fire Note On message
            int noteOnMsg = 0x90 | (targetNote << 8) | (dynamicVelocity << 16);
            midiOutShortMsg(midiHandle, noteOnMsg);
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
		public AudioTapeReader AudioTapeReader(int centerNote, double pitchSensitivity, long minVolumeFilter, int midiInstrument, int baseVelocity)
		{
			return AudioTapeReader(Input, centerNote, pitchSensitivity, minVolumeFilter, midiInstrument, baseVelocity);
		}

		public AudioTapeReader AudioTapeReader(ISeries<double> input, int centerNote, double pitchSensitivity, long minVolumeFilter, int midiInstrument, int baseVelocity)
		{
			if (cacheAudioTapeReader != null)
				for (int idx = 0; idx < cacheAudioTapeReader.Length; idx++)
					if (cacheAudioTapeReader[idx] != null && cacheAudioTapeReader[idx].CenterNote == centerNote && cacheAudioTapeReader[idx].PitchSensitivity == pitchSensitivity && cacheAudioTapeReader[idx].MinVolumeFilter == minVolumeFilter && cacheAudioTapeReader[idx].MidiInstrument == midiInstrument && cacheAudioTapeReader[idx].BaseVelocity == baseVelocity && cacheAudioTapeReader[idx].EqualsInput(input))
						return cacheAudioTapeReader[idx];
			return CacheIndicator<AudioTapeReader>(new AudioTapeReader(){ CenterNote = centerNote, PitchSensitivity = pitchSensitivity, MinVolumeFilter = minVolumeFilter, MidiInstrument = midiInstrument, BaseVelocity = baseVelocity }, input, ref cacheAudioTapeReader);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.AudioTapeReader AudioTapeReader(int centerNote, double pitchSensitivity, long minVolumeFilter, int midiInstrument, int baseVelocity)
		{
			return indicator.AudioTapeReader(Input, centerNote, pitchSensitivity, minVolumeFilter, midiInstrument, baseVelocity);
		}

		public Indicators.AudioTapeReader AudioTapeReader(ISeries<double> input , int centerNote, double pitchSensitivity, long minVolumeFilter, int midiInstrument, int baseVelocity)
		{
			return indicator.AudioTapeReader(input, centerNote, pitchSensitivity, minVolumeFilter, midiInstrument, baseVelocity);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.AudioTapeReader AudioTapeReader(int centerNote, double pitchSensitivity, long minVolumeFilter, int midiInstrument, int baseVelocity)
		{
			return indicator.AudioTapeReader(Input, centerNote, pitchSensitivity, minVolumeFilter, midiInstrument, baseVelocity);
		}

		public Indicators.AudioTapeReader AudioTapeReader(ISeries<double> input , int centerNote, double pitchSensitivity, long minVolumeFilter, int midiInstrument, int baseVelocity)
		{
			return indicator.AudioTapeReader(input, centerNote, pitchSensitivity, minVolumeFilter, midiInstrument, baseVelocity);
		}
	}
}

#endregion
