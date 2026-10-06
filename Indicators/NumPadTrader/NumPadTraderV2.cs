#region Using declarations
using System;
using System.Linq;
using System.Windows.Input;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Cbi;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class NumPadTraderV2 : Indicator
    {
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Default Quantity", GroupName = "1. Order Parameters", Order = 1)]
        public int Quantity { get; set; }

        [NinjaScriptProperty]
        [Range(0.25, double.MaxValue)]
        [Display(Name = "Take Profit 1 (Points)", GroupName = "1. Order Parameters", Order = 2)]
        public double TakeProfitPoints { get; set; }

        [NinjaScriptProperty]
        [Range(0.25, double.MaxValue)]
        [Display(Name = "TP2 Offset (Points beyond TP1)", GroupName = "1. Order Parameters", Order = 3)]
        public double TP2OffsetPoints { get; set; }

        [NinjaScriptProperty]
        [Range(0.25, double.MaxValue)]
        [Display(Name = "Stop Loss (Points)", GroupName = "1. Order Parameters", Order = 4)]
        public double StopLossPoints { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Split Into 2 Targets (if Qty >= 2)?", GroupName = "1. Order Parameters", Order = 5)]
        public bool SplitTargets { get; set; }

        [NinjaScriptProperty]
        [Range(0.25, double.MaxValue)]
        [Display(Name = "Stop Coarse Step (Points)", Description = "Standard points to move SL per press of Num 9", GroupName = "2. Adjustments", Order = 1)]
        public double StopAdjustStepPoints { get; set; }

        [NinjaScriptProperty]
        [Range(0.25, double.MaxValue)]
        [Display(Name = "TP Coarse Step (Points)", Description = "Standard points to move TP per press of Keypad +", GroupName = "2. Adjustments", Order = 2)]
        public double TPAdjustStepPoints { get; set; }

        [NinjaScriptProperty]
        [Range(0.25, double.MaxValue)]
        [Display(Name = "Fine Step (Points)", Description = "Points to move per press once within the limit zone", GroupName = "2. Adjustments", Order = 3)]
        public double FineAdjustStepPoints { get; set; }

        [NinjaScriptProperty]
        [Range(0.25, double.MaxValue)]
        [Display(Name = "Min Buffer From Price (Points)", Description = "Closest allowed distance to live market price", GroupName = "2. Adjustments", Order = 4)]
        public double MinBufferPoints { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Calc From Entry Price?", Description = "True = Entry fill price, False = Current market price", GroupName = "1. Order Parameters", Order = 6)]
        public bool CalcFromEntryPrice { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Cancel Existing Exits First?", Description = "Cancels old resting orders before placing new SL/TP", GroupName = "1. Order Parameters", Order = 7)]
        public bool CancelOldExitsFirst { get; set; }

        [TypeConverter(typeof(NinjaTrader.NinjaScript.AccountNameConverter))]
        [Display(Name = "Fallback Account", Description = "Used only if Chart Trader is completely disabled or hidden", GroupName = "1. Order Parameters", Order = 8)]
        public string FallbackAccount { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                 = "Hotkeys: 4=Buy Mkt, 5=Sell Mkt, 7=Buy Bid, 8=Sell Ask, 0=SL/TP, 9=Adjust SL, +=Adjust TP (Innermost), Enter=Close All (Follows Chart Trader Account)";
                Name                        = "NumPadTraderV2";
                Calculate                   = Calculate.OnEachTick;
                IsOverlay                   = true;
                Quantity                    = 2;
                TakeProfitPoints            = 5.0;
                TP2OffsetPoints             = 5.0;
                StopLossPoints              = 5.0;
                SplitTargets                = true;
                StopAdjustStepPoints        = 2.0;
                TPAdjustStepPoints          = 2.0;
                FineAdjustStepPoints        = 1.0;
                MinBufferPoints             = 1.0;
                CalcFromEntryPrice          = true;
                CancelOldExitsFirst         = true;
                FallbackAccount             = "Sim101";
            }
            else if (State == State.Historical)
            {
                if (ChartControl != null)
                {
                    ChartControl.Dispatcher.InvokeAsync(() =>
                    {
                        ChartControl.PreviewKeyDown += OnChartKeyDown;
                    });
                }
            }
            else if (State == State.Terminated)
            {
                if (ChartControl != null)
                {
                    ChartControl.Dispatcher.InvokeAsync(() =>
                    {
                        ChartControl.PreviewKeyDown -= OnChartKeyDown;
                    });
                }
            }
        }

        protected override void OnBarUpdate() { }

        // Dynamically retrieves the active account from Chart Trader
        private Account GetActiveAccount()
        {
            Account account = null;

            try
            {
                if (ChartControl != null && ChartControl.OwnerChart != null && ChartControl.OwnerChart.ChartTrader != null)
                {
                    account = ChartControl.OwnerChart.ChartTrader.Account;
                }
            }
            catch { }

            if (account == null && !string.IsNullOrEmpty(FallbackAccount))
            {
                account = Account.All.FirstOrDefault(a => a.Name == FallbackAccount);
            }

            return account;
        }

        private void OnChartKeyDown(object sender, KeyEventArgs e)
        {
            if (Instrument == null) return;

            Account account = GetActiveAccount();
            if (account == null)
            {
                Print($"[NumPadTraderV2] No account detected! Make sure Chart Trader is enabled on your chart or set a Fallback Account.");
                return;
            }

            bool isShiftOrCtrl = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0;

            // Keypad 4: Buy Market
            if (e.Key == Key.NumPad4 || (!Keyboard.IsKeyToggled(Key.NumLock) && e.Key == Key.Left))
            {
                e.Handled = true;
                SubmitMarketOrder(account, OrderAction.Buy);
            }
            // Keypad 5: Sell Market
            else if (e.Key == Key.NumPad5 || (!Keyboard.IsKeyToggled(Key.NumLock) && e.Key == Key.Clear))
            {
                e.Handled = true;
                SubmitMarketOrder(account, OrderAction.Sell);
            }
            // Keypad 7: Buy Limit at Bid (Long)
            else if (e.Key == Key.NumPad7 || (!Keyboard.IsKeyToggled(Key.NumLock) && e.Key == Key.Home))
            {
                e.Handled = true;
                SubmitLimitOrder(account, OrderAction.Buy, GetCurrentBid());
            }
            // Keypad 8: Sell Limit at Ask (Short)
            else if (e.Key == Key.NumPad8 || (!Keyboard.IsKeyToggled(Key.NumLock) && e.Key == Key.Up))
            {
                e.Handled = true;
                SubmitLimitOrder(account, OrderAction.Sell, GetCurrentAsk());
            }
            // Keypad 0: Enter Stop Loss & Take Profit Bracket (OCO)
            else if (e.Key == Key.NumPad0 || e.Key == Key.Insert)
            {
                e.Handled = true;
                SubmitBracketOrders(account);
            }
            // Keypad 9: Adjust ALL Stop Losses Closer / Further
            else if (e.Key == Key.NumPad9 || e.Key == Key.Prior || e.Key == Key.PageUp)
            {
                e.Handled = true;
                if (isShiftOrCtrl)
                    AdjustStopLoss(account, moveCloser: false);
                else
                    AdjustStopLoss(account, moveCloser: true);
            }
            // Keypad +: Adjust Innermost Take Profit Closer / Further
            else if (e.Key == Key.Add || e.Key == Key.OemPlus)
            {
                e.Handled = true;
                if (isShiftOrCtrl)
                    AdjustTakeProfit(account, moveCloser: false);
                else
                    AdjustTakeProfit(account, moveCloser: true);
            }
            // Enter Key: Close / Flatten & Cancel All
            else if (e.Key == Key.Return)
            {
                e.Handled = true;
                CloseAllOrdersAndPositions(account);
            }
        }

        private void SubmitMarketOrder(Account account, OrderAction action)
        {
            Order order = account.CreateOrder(
                Instrument,
                action,
                OrderType.Market,
                OrderEntry.Manual,
                TimeInForce.Day,
                Quantity,
                0,
                0,
                string.Empty,
                action == OrderAction.Buy ? "HotkeyBuyMarket" : "HotkeySellMarket",
                DateTime.MaxValue,
                null
            );

            account.Submit(new[] { order });
            Print($"[NumPadTraderV2] [{account.Name}] Sent {action} Market order ({Quantity} contract(s)) on {Instrument.FullName}");
        }

        private void SubmitLimitOrder(Account account, OrderAction action, double limitPrice)
        {
            if (limitPrice <= 0)
            {
                Print($"[NumPadTraderV2] [{account.Name}] Unable to get a valid price for the limit order. Current price returned {limitPrice}.");
                return;
            }

            Order order = account.CreateOrder(
                Instrument,
                action,
                OrderType.Limit,
                OrderEntry.Manual,
                TimeInForce.Day,
                Quantity,
                limitPrice,
                0,
                string.Empty,
                action == OrderAction.Buy ? "HotkeyBuyBid" : "HotkeySellAsk",
                DateTime.MaxValue,
                null
            );

            account.Submit(new[] { order });
            Print($"[NumPadTraderV2] [{account.Name}] Sent {action} Limit order at {limitPrice} ({Quantity} contract(s)) on {Instrument.FullName}");
        }

        private void SubmitBracketOrders(Account account)
        {
            Position pos = account.Positions.FirstOrDefault(p => p.Instrument.FullName == Instrument.FullName);
            Order lastWorkingOrder = account.Orders.LastOrDefault(o => o.Instrument.FullName == Instrument.FullName && (o.OrderState == OrderState.Working || o.OrderState == OrderState.Accepted));

            OrderAction exitAction;
            double basePrice;
            int totalQty;

            if (pos != null && pos.MarketPosition != MarketPosition.Flat)
            {
                totalQty = pos.Quantity;
                basePrice = (CalcFromEntryPrice && pos.AveragePrice > 0) 
                    ? pos.AveragePrice 
                    : (Close.Count > 0 ? Close[0] : pos.AveragePrice);

                if (CancelOldExitsFirst)
                {
                    var workingOrders = account.Orders
                        .Where(o => o.Instrument.FullName == Instrument.FullName && (o.OrderState == OrderState.Working || o.OrderState == OrderState.Accepted))
                        .ToList();

                    if (workingOrders.Count > 0)
                        account.Cancel(workingOrders);
                }

                exitAction = pos.MarketPosition == MarketPosition.Long ? OrderAction.Sell : OrderAction.Buy;
            }
            else if (lastWorkingOrder != null)
            {
                totalQty = lastWorkingOrder.Quantity;
                basePrice = lastWorkingOrder.LimitPrice > 0 ? lastWorkingOrder.LimitPrice : (Close.Count > 0 ? Close[0] : 0);
                exitAction = (lastWorkingOrder.OrderAction == OrderAction.Buy || lastWorkingOrder.OrderAction == OrderAction.BuyToCover) ? OrderAction.Sell : OrderAction.Buy;
            }
            else
            {
                Print($"[NumPadTraderV2] [{account.Name}] No open position or working orders found to attach SL/TP.");
                return;
            }

            bool isLong = exitAction == OrderAction.Sell;
            double slPrice = isLong 
                ? Instrument.MasterInstrument.RoundToTickSize(basePrice - StopLossPoints) 
                : Instrument.MasterInstrument.RoundToTickSize(basePrice + StopLossPoints);

            List<Order> ordersToSubmit = new List<Order>();

            if (totalQty >= 2 && SplitTargets)
            {
                int qty1 = totalQty / 2;
                int qty2 = totalQty - qty1;

                double tp1Price = isLong 
                    ? Instrument.MasterInstrument.RoundToTickSize(basePrice + TakeProfitPoints) 
                    : Instrument.MasterInstrument.RoundToTickSize(basePrice - TakeProfitPoints);

                double tp2Price = isLong 
                    ? Instrument.MasterInstrument.RoundToTickSize(basePrice + TakeProfitPoints + TP2OffsetPoints) 
                    : Instrument.MasterInstrument.RoundToTickSize(basePrice - (TakeProfitPoints + TP2OffsetPoints));

                string oco1 = "OCO1_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                string oco2 = "OCO2_" + Guid.NewGuid().ToString("N").Substring(0, 8);

                ordersToSubmit.Add(account.CreateOrder(Instrument, exitAction, OrderType.Limit, OrderEntry.Manual, TimeInForce.Day, qty1, tp1Price, 0, oco1, "HotkeyTP1", DateTime.MaxValue, null));
                ordersToSubmit.Add(account.CreateOrder(Instrument, exitAction, OrderType.StopMarket, OrderEntry.Manual, TimeInForce.Day, qty1, 0, slPrice, oco1, "HotkeySL1", DateTime.MaxValue, null));

                ordersToSubmit.Add(account.CreateOrder(Instrument, exitAction, OrderType.Limit, OrderEntry.Manual, TimeInForce.Day, qty2, tp2Price, 0, oco2, "HotkeyTP2", DateTime.MaxValue, null));
                ordersToSubmit.Add(account.CreateOrder(Instrument, exitAction, OrderType.StopMarket, OrderEntry.Manual, TimeInForce.Day, qty2, 0, slPrice, oco2, "HotkeySL2", DateTime.MaxValue, null));

                Print($"[NumPadTraderV2] [{account.Name}] Split Brackets Placed: TP1 @ {tp1Price} ({qty1}ct), TP2 @ {tp2Price} ({qty2}ct), Stops @ {slPrice} ({totalQty}ct total).");
            }
            else
            {
                double tpPrice = isLong 
                    ? Instrument.MasterInstrument.RoundToTickSize(basePrice + TakeProfitPoints) 
                    : Instrument.MasterInstrument.RoundToTickSize(basePrice - TakeProfitPoints);

                string oco = "OCO_" + Guid.NewGuid().ToString("N").Substring(0, 8);

                ordersToSubmit.Add(account.CreateOrder(Instrument, exitAction, OrderType.Limit, OrderEntry.Manual, TimeInForce.Day, totalQty, tpPrice, 0, oco, "HotkeyTP", DateTime.MaxValue, null));
                ordersToSubmit.Add(account.CreateOrder(Instrument, exitAction, OrderType.StopMarket, OrderEntry.Manual, TimeInForce.Day, totalQty, 0, slPrice, oco, "HotkeySL", DateTime.MaxValue, null));

                Print($"[NumPadTraderV2] [{account.Name}] Single Bracket Placed: TP @ {tpPrice}, SL @ {slPrice} ({totalQty}ct).");
            }

            account.Submit(ordersToSubmit);
        }

        private void AdjustStopLoss(Account account, bool moveCloser)
        {
            Position pos = account.Positions.FirstOrDefault(p => p.Instrument.FullName == Instrument.FullName);
            if (pos == null || pos.MarketPosition == MarketPosition.Flat)
            {
                Print($"[NumPadTraderV2] [{account.Name}] Cannot adjust stop: No open position found for {Instrument.FullName}.");
                return;
            }

            var slOrders = account.Orders.Where(o =>
                o.Instrument.FullName == Instrument.FullName &&
                (o.OrderState == OrderState.Working || o.OrderState == OrderState.Accepted) &&
                (o.OrderType == OrderType.StopMarket || o.OrderType == OrderType.StopLimit)).ToList();

            if (slOrders.Count == 0)
            {
                Print($"[NumPadTraderV2] [{account.Name}] Cannot adjust: No active Stop Loss order found for {Instrument.FullName}.");
                return;
            }

            double currentStop = slOrders[0].StopPrice;
            double newStop = currentStop;

            double bid = GetCurrentBid();
            double ask = GetCurrentAsk();
            double marketPrice = pos.MarketPosition == MarketPosition.Long 
                ? (bid > 0 ? bid : (Close.Count > 0 ? Close[0] : 0)) 
                : (ask > 0 ? ask : (Close.Count > 0 ? Close[0] : 0));

            if (marketPrice <= 0) return;

            double currentDistance = pos.MarketPosition == MarketPosition.Long 
                ? (marketPrice - currentStop) 
                : (currentStop - marketPrice);

            double stepToUse;
            if (moveCloser)
            {
                if (currentDistance <= StopAdjustStepPoints || (currentDistance - StopAdjustStepPoints) < MinBufferPoints)
                    stepToUse = FineAdjustStepPoints;
                else
                    stepToUse = StopAdjustStepPoints;
            }
            else
            {
                stepToUse = StopAdjustStepPoints;
            }

            if (pos.MarketPosition == MarketPosition.Long)
            {
                newStop = moveCloser ? (currentStop + stepToUse) : (currentStop - stepToUse);
                if (moveCloser && (marketPrice - newStop) < MinBufferPoints)
                {
                    Print($"[NumPadTraderV2] [{account.Name}] Stop Loss is at minimum limit ({MinBufferPoints} pt) from market.");
                    return;
                }
            }
            else if (pos.MarketPosition == MarketPosition.Short)
            {
                newStop = moveCloser ? (currentStop - stepToUse) : (currentStop + stepToUse);
                if (moveCloser && (newStop - marketPrice) < MinBufferPoints)
                {
                    Print($"[NumPadTraderV2] [{account.Name}] Stop Loss is at minimum limit ({MinBufferPoints} pt) from market.");
                    return;
                }
            }

            newStop = Instrument.MasterInstrument.RoundToTickSize(newStop);

            if (Math.Abs(newStop - currentStop) < (Instrument.MasterInstrument.TickSize / 2.0))
                return;

            foreach (var order in slOrders)
            {
                order.StopPriceChanged = newStop;
                order.QuantityChanged  = order.Quantity;

                if (order.OrderType == OrderType.StopLimit)
                {
                    double delta = newStop - currentStop;
                    order.LimitPriceChanged = Instrument.MasterInstrument.RoundToTickSize(order.LimitPrice + delta);
                }
            }

            account.Change(slOrders);

            string dir = moveCloser ? "CLOSER" : "FURTHER";
            string mode = (stepToUse == FineAdjustStepPoints && moveCloser) ? " [FINE 1-PT STEP]" : "";
            Print($"[NumPadTraderV2] [{account.Name}] All Stop Losses ({slOrders.Count} order(s)) moved {dir}{mode} by {stepToUse} pts: {currentStop} -> {newStop}");
        }

        private void AdjustTakeProfit(Account account, bool moveCloser)
        {
            Position pos = account.Positions.FirstOrDefault(p => p.Instrument.FullName == Instrument.FullName);
            if (pos == null || pos.MarketPosition == MarketPosition.Flat)
            {
                Print($"[NumPadTraderV2] [{account.Name}] Cannot adjust TP: No open position found for {Instrument.FullName}.");
                return;
            }

            var tpOrders = account.Orders.Where(o =>
                o.Instrument.FullName == Instrument.FullName &&
                (o.OrderState == OrderState.Working || o.OrderState == OrderState.Accepted) &&
                o.OrderType == OrderType.Limit).ToList();

            if (tpOrders.Count == 0)
            {
                Print($"[NumPadTraderV2] [{account.Name}] Cannot adjust: No active Take Profit Limit orders found for {Instrument.FullName}.");
                return;
            }

            Order targetTP = pos.MarketPosition == MarketPosition.Long
                ? tpOrders.OrderBy(o => o.LimitPrice).First()
                : tpOrders.OrderByDescending(o => o.LimitPrice).First();

            double currentTP = targetTP.LimitPrice;
            double newTP = currentTP;

            double bid = GetCurrentBid();
            double ask = GetCurrentAsk();
            double marketPrice = pos.MarketPosition == MarketPosition.Long 
                ? (bid > 0 ? bid : (Close.Count > 0 ? Close[0] : 0)) 
                : (ask > 0 ? ask : (Close.Count > 0 ? Close[0] : 0));

            if (marketPrice <= 0) return;

            double currentDistance = pos.MarketPosition == MarketPosition.Long 
                ? (currentTP - marketPrice) 
                : (marketPrice - currentTP);

            double stepToUse;
            if (moveCloser)
            {
                if (currentDistance <= TPAdjustStepPoints || (currentDistance - TPAdjustStepPoints) < MinBufferPoints)
                    stepToUse = FineAdjustStepPoints;
                else
                    stepToUse = TPAdjustStepPoints;
            }
            else
            {
                stepToUse = TPAdjustStepPoints;
            }

            if (pos.MarketPosition == MarketPosition.Long)
            {
                newTP = moveCloser ? (currentTP - stepToUse) : (currentTP + stepToUse);
                if (moveCloser && (newTP - marketPrice) < MinBufferPoints)
                {
                    Print($"[NumPadTraderV2] [{account.Name}] Take Profit is at minimum limit ({MinBufferPoints} pt) from market.");
                    return;
                }
            }
            else if (pos.MarketPosition == MarketPosition.Short)
            {
                newTP = moveCloser ? (currentTP + stepToUse) : (currentTP - stepToUse);
                if (moveCloser && (marketPrice - newTP) < MinBufferPoints)
                {
                    Print($"[NumPadTraderV2] [{account.Name}] Take Profit is at minimum limit ({MinBufferPoints} pt) from market.");
                    return;
                }
            }

            newTP = Instrument.MasterInstrument.RoundToTickSize(newTP);

            if (Math.Abs(newTP - currentTP) < (Instrument.MasterInstrument.TickSize / 2.0))
                return;

            targetTP.LimitPriceChanged = newTP;
            targetTP.QuantityChanged   = targetTP.Quantity;

            account.Change(new[] { targetTP });

            string dir = moveCloser ? "CLOSER" : "FURTHER";
            string mode = (stepToUse == FineAdjustStepPoints && moveCloser) ? " [FINE 1-PT STEP]" : "";
            Print($"[NumPadTraderV2] [{account.Name}] Innermost TP ({targetTP.Name}) moved {dir}{mode} by {stepToUse} pts: {currentTP} -> {newTP}");
        }

        private void CloseAllOrdersAndPositions(Account account)
        {
            account.CancelAllOrders(Instrument);
            account.Flatten(new[] { Instrument });
            Print($"[NumPadTraderV2] [{account.Name}] ENTER pressed -> CANCEL ALL & FLATTEN executed for {Instrument.FullName}.");
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private NumPadTraderV2[] cacheNumPadTraderV2;
		public NumPadTraderV2 NumPadTraderV2(int quantity, double takeProfitPoints, double tP2OffsetPoints, double stopLossPoints, bool splitTargets, double stopAdjustStepPoints, double tPAdjustStepPoints, double fineAdjustStepPoints, double minBufferPoints, bool calcFromEntryPrice, bool cancelOldExitsFirst)
		{
			return NumPadTraderV2(Input, quantity, takeProfitPoints, tP2OffsetPoints, stopLossPoints, splitTargets, stopAdjustStepPoints, tPAdjustStepPoints, fineAdjustStepPoints, minBufferPoints, calcFromEntryPrice, cancelOldExitsFirst);
		}

		public NumPadTraderV2 NumPadTraderV2(ISeries<double> input, int quantity, double takeProfitPoints, double tP2OffsetPoints, double stopLossPoints, bool splitTargets, double stopAdjustStepPoints, double tPAdjustStepPoints, double fineAdjustStepPoints, double minBufferPoints, bool calcFromEntryPrice, bool cancelOldExitsFirst)
		{
			if (cacheNumPadTraderV2 != null)
				for (int idx = 0; idx < cacheNumPadTraderV2.Length; idx++)
					if (cacheNumPadTraderV2[idx] != null && cacheNumPadTraderV2[idx].Quantity == quantity && cacheNumPadTraderV2[idx].TakeProfitPoints == takeProfitPoints && cacheNumPadTraderV2[idx].TP2OffsetPoints == tP2OffsetPoints && cacheNumPadTraderV2[idx].StopLossPoints == stopLossPoints && cacheNumPadTraderV2[idx].SplitTargets == splitTargets && cacheNumPadTraderV2[idx].StopAdjustStepPoints == stopAdjustStepPoints && cacheNumPadTraderV2[idx].TPAdjustStepPoints == tPAdjustStepPoints && cacheNumPadTraderV2[idx].FineAdjustStepPoints == fineAdjustStepPoints && cacheNumPadTraderV2[idx].MinBufferPoints == minBufferPoints && cacheNumPadTraderV2[idx].CalcFromEntryPrice == calcFromEntryPrice && cacheNumPadTraderV2[idx].CancelOldExitsFirst == cancelOldExitsFirst && cacheNumPadTraderV2[idx].EqualsInput(input))
						return cacheNumPadTraderV2[idx];
			return CacheIndicator<NumPadTraderV2>(new NumPadTraderV2(){ Quantity = quantity, TakeProfitPoints = takeProfitPoints, TP2OffsetPoints = tP2OffsetPoints, StopLossPoints = stopLossPoints, SplitTargets = splitTargets, StopAdjustStepPoints = stopAdjustStepPoints, TPAdjustStepPoints = tPAdjustStepPoints, FineAdjustStepPoints = fineAdjustStepPoints, MinBufferPoints = minBufferPoints, CalcFromEntryPrice = calcFromEntryPrice, CancelOldExitsFirst = cancelOldExitsFirst }, input, ref cacheNumPadTraderV2);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.NumPadTraderV2 NumPadTraderV2(int quantity, double takeProfitPoints, double tP2OffsetPoints, double stopLossPoints, bool splitTargets, double stopAdjustStepPoints, double tPAdjustStepPoints, double fineAdjustStepPoints, double minBufferPoints, bool calcFromEntryPrice, bool cancelOldExitsFirst)
		{
			return indicator.NumPadTraderV2(Input, quantity, takeProfitPoints, tP2OffsetPoints, stopLossPoints, splitTargets, stopAdjustStepPoints, tPAdjustStepPoints, fineAdjustStepPoints, minBufferPoints, calcFromEntryPrice, cancelOldExitsFirst);
		}

		public Indicators.NumPadTraderV2 NumPadTraderV2(ISeries<double> input , int quantity, double takeProfitPoints, double tP2OffsetPoints, double stopLossPoints, bool splitTargets, double stopAdjustStepPoints, double tPAdjustStepPoints, double fineAdjustStepPoints, double minBufferPoints, bool calcFromEntryPrice, bool cancelOldExitsFirst)
		{
			return indicator.NumPadTraderV2(input, quantity, takeProfitPoints, tP2OffsetPoints, stopLossPoints, splitTargets, stopAdjustStepPoints, tPAdjustStepPoints, fineAdjustStepPoints, minBufferPoints, calcFromEntryPrice, cancelOldExitsFirst);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.NumPadTraderV2 NumPadTraderV2(int quantity, double takeProfitPoints, double tP2OffsetPoints, double stopLossPoints, bool splitTargets, double stopAdjustStepPoints, double tPAdjustStepPoints, double fineAdjustStepPoints, double minBufferPoints, bool calcFromEntryPrice, bool cancelOldExitsFirst)
		{
			return indicator.NumPadTraderV2(Input, quantity, takeProfitPoints, tP2OffsetPoints, stopLossPoints, splitTargets, stopAdjustStepPoints, tPAdjustStepPoints, fineAdjustStepPoints, minBufferPoints, calcFromEntryPrice, cancelOldExitsFirst);
		}

		public Indicators.NumPadTraderV2 NumPadTraderV2(ISeries<double> input , int quantity, double takeProfitPoints, double tP2OffsetPoints, double stopLossPoints, bool splitTargets, double stopAdjustStepPoints, double tPAdjustStepPoints, double fineAdjustStepPoints, double minBufferPoints, bool calcFromEntryPrice, bool cancelOldExitsFirst)
		{
			return indicator.NumPadTraderV2(input, quantity, takeProfitPoints, tP2OffsetPoints, stopLossPoints, splitTargets, stopAdjustStepPoints, tPAdjustStepPoints, fineAdjustStepPoints, minBufferPoints, calcFromEntryPrice, cancelOldExitsFirst);
		}
	}
}

#endregion
