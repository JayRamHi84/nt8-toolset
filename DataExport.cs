// =============================================================================
//  DataExport.cs  --  NinjaTrader 8 Chart Data CSV Exporter
// =============================================================================

using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using NinjaTrader.Cbi;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.Strategies
{
    public class DataExport : Strategy
    {
        private Button exportButton;
        private bool isButtonAdded = false;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                 = @"Exports chart bar data to a CSV file via an on-chart Export button.";
                Name                        = "DataExport";
                Calculate                   = Calculate.OnBarClose;
                EntriesPerDirection         = 1;
                EntryHandling               = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds   = 30;
                IsFillLimitOnTouch          = false;
                MaximumBarsLookBack         = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution         = OrderFillResolution.Standard;
                Slippage                    = 0;
                StartBehavior               = StartBehavior.WaitUntilFlat;
                TimeInForce                 = TimeInForce.Gtc;
                TraceOrders                 = false;
                RealtimeErrorHandling       = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling          = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade         = 1;
                IsInstantiatedOnEachOptimizationIteration = false;
            }
            else if (State == State.Historical || State == State.Realtime)
            {
                // Add the button to the chart canvas using NinjaTrader's UserControlCollection
                if (ChartControl != null && !isButtonAdded)
                {
                    ChartControl.Dispatcher.InvokeAsync(AddExportButtonToChart);
                }
            }
            else if (State == State.Terminated)
            {
                RemoveExportButtonFromChart();
            }
        }

        protected override void OnBarUpdate()
        {
            // Fallback: If button hasn't been added yet when bars start processing, ensure it is added
            if (ChartControl != null && !isButtonAdded)
            {
                ChartControl.Dispatcher.InvokeAsync(AddExportButtonToChart);
            }
        }

        // ---------------------------------------------------------------------
        //  UI INTEGRATION (UserControlCollection)
        // ---------------------------------------------------------------------
        private void AddExportButtonToChart()
        {
            if (ChartControl == null || isButtonAdded)
                return;

            if (exportButton != null && UserControlCollection.Contains(exportButton))
                return;

            // Styled button that stands out on both Dark and Light chart backgrounds
            exportButton = new Button
            {
                Content             = "Export CSV",
                Height              = 28,
                Padding             = new Thickness(12, 3, 12, 3),
                Margin              = new Thickness(15, 15, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment   = VerticalAlignment.Top,
                Background          = new SolidColorBrush(Color.FromRgb(45, 125, 210)), // NT Blue
                Foreground          = Brushes.White,
                FontWeight          = FontWeights.SemiBold,
                FontSize            = 12,
                BorderBrush         = Brushes.Transparent,
                Cursor              = System.Windows.Input.Cursors.Hand,
                ToolTip             = "Click to export chart data to a CSV file"
            };

            exportButton.Click += OnExportButtonClick;

            UserControlCollection.Add(exportButton);
            isButtonAdded = true;
            Print("[DataExport] 'Export CSV' button added to chart.");
        }

        private void RemoveExportButtonFromChart()
        {
            if (ChartControl == null) return;

            ChartControl.Dispatcher.InvokeAsync(() =>
            {
                if (exportButton != null)
                {
                    exportButton.Click -= OnExportButtonClick;
                    if (UserControlCollection.Contains(exportButton))
                    {
                        UserControlCollection.Remove(exportButton);
                    }
                    exportButton = null;
                }
                isButtonAdded = false;
            });
        }

        // ---------------------------------------------------------------------
        //  EXPORT LOGIC
        // ---------------------------------------------------------------------
        private void OnExportButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string instrumentName = Instrument != null ? Instrument.MasterInstrument.Name : "Data";
                string timeframe      = BarsPeriod != null ? $"{BarsPeriod.Value}_{BarsPeriod.BarsPeriodType}" : "Series";
                string defaultName    = $"{instrumentName}_{timeframe}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                SaveFileDialog dialog = new SaveFileDialog
                {
                    Title       = "Export Chart Bars to CSV",
                    Filter      = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    DefaultExt  = ".csv",
                    FileName    = defaultName
                };

                bool? result = dialog.ShowDialog();
                if (result == true)
                {
                    string filePath = dialog.FileName;
                    TriggerCustomEvent(o => ExportDataToFile(filePath), null);
                }
            }
            catch (Exception ex)
            {
                Print($"[DataExport] Error displaying file dialog: {ex.Message}");
            }
        }

        private void ExportDataToFile(string filePath)
        {
            try
            {
                if (Bars == null || Bars.Count == 0)
                {
                    Print("[DataExport] No bars loaded on this chart to export.");
                    return;
                }

                int totalBars = Bars.Count;

                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    // CSV Header
                    writer.WriteLine("Date,Open,High,Low,Close,Volume");

                    // Chronological write (0 = oldest loaded bar, Count - 1 = latest bar)
                    for (int i = 0; i < totalBars; i++)
                    {
                        writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                            "{0:yyyy-MM-dd HH:mm:ss},{1},{2},{3},{4},{5}",
                            Bars.GetTime(i),
                            Bars.GetOpen(i),
                            Bars.GetHigh(i),
                            Bars.GetLow(i),
                            Bars.GetClose(i),
                            Bars.GetVolume(i)));
                    }
                }

                Print($"[DataExport] Successfully exported {totalBars} bars to: {filePath}");
            }
            catch (Exception ex)
            {
                Print($"[DataExport] Failed to export data: {ex.Message}");
            }
        }
    }
}
