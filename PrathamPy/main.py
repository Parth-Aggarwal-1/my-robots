from AlgorithmImports import *
from System.ComponentModel import VersionConverter
from datetime import timedelta


class PrathamPy(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2018, 1, 1)  # Set Start Date
        self.SetEndDate(2021, 1, 1)  # Set End Date
        self.SetCash(100000)  # Set Strategy Cash
        self.eurusd = self.AddForex("EURUSD", Resolution.Minute, Market.Oanda, leverage=1.0)
        self.SetBrokerageModel(BrokerageName.OandaBrokerage)

        self.fiveMinuteWindow = RollingWindow[QuoteBar](5)
        self.fiveMinuteEmas = self.GenerateEmas()

        self.Consolidate(self.eurusd.Symbol, timedelta(minutes=5), self.FiveMinuteHandler)

    def OnData(self, data):
        """OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
            Arguments:
                data: Slice object keyed by symbol containing the stock data
        """
        if not self.Portfolio.Invested:
            self.SetHoldings("BHE", 1)
            self.Debug("Purchased Stock")

    def FiveMinuteHandler(self, bar):
        self.fiveMinuteWindow.Add(bar)
