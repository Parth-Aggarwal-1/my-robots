using System;
using System.Collections.Generic;
using System.Linq;
using Accord;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Orders.Slippage;

namespace QuantConnect.Algorithm.CSharp
{
    public class Pratham : QCAlgorithm
    {
        private Symbol _symbol;
        private RollingWindow<QuoteBar> _slowWindow = new RollingWindow<QuoteBar>(5);
        private RollingWindow<QuoteBar> _fastWindow = new RollingWindow<QuoteBar>(5);
        private readonly ExponentialMovingAverage _slow21Ema = new ExponentialMovingAverage(21);
        private readonly ExponentialMovingAverage _slow8Ema = new ExponentialMovingAverage(8);
        private readonly ExponentialMovingAverage _fast21Ema = new ExponentialMovingAverage(21);
        private readonly ExponentialMovingAverage _fast13Ema = new ExponentialMovingAverage(13);
        private readonly ExponentialMovingAverage _fast8Ema = new ExponentialMovingAverage(8);
        private readonly RollingWindow<IndicatorDataPoint> _slow21EmaRollingWindow = new RollingWindow<IndicatorDataPoint>(5);
        private readonly RollingWindow<IndicatorDataPoint> _slow8EmaRollingWindow = new RollingWindow<IndicatorDataPoint>(5);
        private readonly RollingWindow<IndicatorDataPoint> _fast21EmaRollingWindow = new RollingWindow<IndicatorDataPoint>(5);
        private readonly RollingWindow<IndicatorDataPoint> _fast13EmaRollingWindow = new RollingWindow<IndicatorDataPoint>(5);
        private readonly RollingWindow<IndicatorDataPoint> _fast8EmaRollingWindow = new RollingWindow<IndicatorDataPoint>(5);
        private readonly List<OrderTicket> _orderTickets = new List<OrderTicket>(){};
        private decimal _risk;
        private decimal _trailingStopLossHigh;


        public override void Initialize()
        {
            Debug("Initializing");
            
            SetStartDate(2020, 11, 10);
            SetEndDate(2020, 11, 30);
            SetCash(100000); // Set Strategy Cash
            SetBrokerageModel(BrokerageName.OandaBrokerage, AccountType.Margin);

            // Pair
            _symbol = AddForex("EURUSD", Resolution.Minute, Market.Oanda, true, 1m).Symbol;

            //Candles
            _slowWindow = new RollingWindow<QuoteBar>(5);
            _fastWindow = new RollingWindow<QuoteBar>(5);
            var fiveMinuteConsolidator = new QuoteBarConsolidator(TimeSpan.FromMinutes(5));
            SubscriptionManager.AddConsolidator(_symbol.Value, fiveMinuteConsolidator);
            var oneHourConsolidator = new QuoteBarConsolidator(TimeSpan.FromMinutes(60));
            SubscriptionManager.AddConsolidator(_symbol.Value, oneHourConsolidator);
            
            Consolidate<QuoteBar>(_symbol, TimeSpan.FromMinutes(5), FiveMinutelyDataHandler);
            Consolidate<QuoteBar>(_symbol, TimeSpan.FromHours(1), HourlyDataHandler);

            //Indicators
            RegisterIndicator(_symbol, _slow21Ema, oneHourConsolidator);
            RegisterIndicator(_symbol, _slow8Ema, oneHourConsolidator);
            RegisterIndicator(_symbol, _fast21Ema, fiveMinuteConsolidator);
            RegisterIndicator(_symbol, _fast13Ema, fiveMinuteConsolidator);
            RegisterIndicator(_symbol, _fast8Ema, fiveMinuteConsolidator);
            
            SetWarmup(1320);
            // var minutelyQuoteBarHistory = History<QuoteBar>(_symbol, TimeSpan.FromHours(3), Resolution.Minute);
            // var fiveMinutelyQuoteBarHistory = minutelyQuoteBarHistory.Where(x => x.EndTime.Minute % 5 == 0);
            // var hourlyQuoteBarHistory = History<QuoteBar>(_symbol, TimeSpan.FromHours(30), Resolution.Hour);
            // foreach (var quoteBar in fiveMinutelyQuoteBarHistory)
            // {
            //     _fast21Ema.Update(quoteBar.EndTime, quoteBar.Close);
            //     _fast13Ema.Update(quoteBar.EndTime, quoteBar.Close);
            //     _fast8Ema.Update(quoteBar.EndTime, quoteBar.Close);
            //     _fast21EmaRollingWindow.Add(_fast21Ema.Current);
            //     _fast13EmaRollingWindow.Add(_fast13Ema.Current);
            //     _fast8EmaRollingWindow.Add(_fast8Ema.Current);
            // }
            // foreach (var quoteBar in hourlyQuoteBarHistory)
            // {
            //     _slow21Ema.Update(quoteBar.EndTime, quoteBar.Close);
            //     _slow8Ema.Update(quoteBar.EndTime, quoteBar.Close);
            //     _slow21EmaRollingWindow.Add(_slow21Ema.Current);
            //     _slow8EmaRollingWindow.Add(_slow8Ema.Current);
            // }
            var window = new RollingWindow<QuoteBar>(2);
        }


        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// Slice object keyed by symbol containing the stock data
        public override void OnData(Slice data)
        {
            Debug("OnData");

            if (IsWarmingUp) return;

            if (!_slow21Ema.IsReady || !_fast21Ema.IsReady)
            {
                throw new Exception("Indicators not ready despite warm up");
            }
            
            if (AnchorChartIsTrendingUp() && 
                FasterChartIsTrendingUp() &&
                _orderTickets.Count == 0 &&
                IsBuyTrigger())
            {
                var buyStopPrice = _fastWindow.Select(x => x.High).Max() + 0.0003m;
                var stopLossPrice = _fastWindow[0].Low - 0.0003m;
                _risk = buyStopPrice - stopLossPrice;
                
                var buyStopTicket = StopMarketOrder(
                    _symbol,
                    1m,
                    buyStopPrice,
                    "buystop");
                
                _orderTickets.Add(buyStopTicket);
            }
            
            if (_orderTickets.Any(o => o.Tag == "takeprofit1" && o.Status == OrderStatus.Filled))
            {
                var trailingStopLoss = _orderTickets.Find(o => o.Tag == "stoploss");
                if (Securities[_symbol].Price > _trailingStopLossHigh)
                {
                    _trailingStopLossHigh = Securities[_symbol].Price;
                    trailingStopLoss.Update(new UpdateOrderFields() {StopPrice = _trailingStopLossHigh - 0.0003m});
                }
            }
        }

        private bool IsBuyTrigger()
        {
            Debug("BuyTrigger");
            return _fastWindow[0].Low < _fast8EmaRollingWindow[0];
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug("Order Event");
            var orderTicket = _orderTickets.First(ot => ot.OrderId == orderEvent.Id);
            
            if (orderEvent.Status == OrderStatus.Filled)
            {
                var stopLossPrice = orderTicket.AverageFillPrice - _risk;
                
                if (orderTicket.Tag == "buystop")
                {
                    Debug("Buy Stop Filled");
                    var stopLossTicket = StopMarketOrder(
                        _symbol,
                        -orderEvent.Quantity,
                        stopLossPrice,
                        "stoploss");
                    Debug("Placed StopLoss");

                    var takeProfit1Ticket = LimitOrder(
                        _symbol,
                        -0.5m * orderEvent.Quantity,
                        orderTicket.AverageFillPrice + _risk,
                        "takeprofit1");
                    Debug("placed tp1");
                    
                    _orderTickets.Add(stopLossTicket);
                    _orderTickets.Add(takeProfit1Ticket);
                }

                if (orderTicket.Tag == "takeprofit1")
                {
                    _trailingStopLossHigh = orderTicket.AverageFillPrice;
                    var stopLossTicket = _orderTickets.FirstOrDefault(o => o.Tag == "stoploss");
                    stopLossTicket.Update(new UpdateOrderFields() {StopPrice = _trailingStopLossHigh - 0.0003m});
                }

                if (orderTicket.Tag == "stoploss")
                {
                    foreach (var ticket in _orderTickets.Where(o => o.Status != OrderStatus.Filled))
                    {
                        ticket.Cancel();
                    }

                    _orderTickets.Clear();
                }
            }
            base.OnOrderEvent(orderEvent);
        }

        private bool FasterChartIsTrendingUp()
        {
            var trends = new List<bool>();

            for (int i = 0; i < 2; i++)
            {
                var isUp = _fast21EmaRollingWindow[i] > _fast21EmaRollingWindow[i + 1] &&
                           _fast13EmaRollingWindow[i] > _fast13EmaRollingWindow[i + 1] &&
                           _fast8EmaRollingWindow[i] > _fast8EmaRollingWindow[i + 1] &&
                           _fast13EmaRollingWindow[i] > _fast21EmaRollingWindow[i] &&
                           _fast8EmaRollingWindow[i] > _fast13EmaRollingWindow[i] &&
                           _fast13EmaRollingWindow[i+1] > _fast21EmaRollingWindow[i+1] &&
                           _fast8EmaRollingWindow[i+1] > _fast13EmaRollingWindow[i+1] &&
                           _fastWindow[i].Low > _fast8EmaRollingWindow[i] &&
                           _fastWindow[i + 1].Low > _fast8EmaRollingWindow[i + 1];
                
                trends.Add(isUp);
            }

            return trends.All(t => t == true);
        }

        private bool AnchorChartIsTrendingUp()
        {
            var trends = new List<bool>();

            for (int i = 0; i < 2; i++)
            {
                var isUp = _slow21EmaRollingWindow[i] > _slow21EmaRollingWindow[i + 1] &&
                           _slow8EmaRollingWindow[i] > _slow8EmaRollingWindow[i + 1] &&
                           _slow8EmaRollingWindow[i] > _slow21EmaRollingWindow[i] &&
                           _slow8EmaRollingWindow[i + 1] > _slow21EmaRollingWindow[i + 1] &&
                           _slowWindow[i].Low > _slow8EmaRollingWindow[i] &&
                           _slowWindow[i + 1].Low > _slow8EmaRollingWindow[i + 1];
                
                trends.Add(isUp);
            }

            return trends.All(t => t);
        }

        private void HourlyDataHandler(QuoteBar obj)
        {
            _slowWindow.Add(obj);
            _slow21EmaRollingWindow.Add(_slow21Ema.Current);
            _slow8EmaRollingWindow.Add(_slow8Ema.Current);
        }

        private void FiveMinutelyDataHandler(QuoteBar obj)
        {
            _fastWindow.Add(obj);
            _fast21EmaRollingWindow.Add(_fast21Ema.Current);
            _fast13EmaRollingWindow.Add(_fast13Ema.Current);
            _fast8EmaRollingWindow.Add(_fast8Ema.Current);
        }
    }
}
