using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using MyJetWallet.Connector.Ftx.WebSocket;
using MyJetWallet.Domain.ExternalMarketApi.Models;

namespace Service.External.Ftx.Services
{
    public class OrderBookManager: IDisposable
    {
        private readonly List<string> _symbolList;

        private readonly FtxWsOrderBooks _wsFtx;

        public OrderBookManager(ILoggerFactory loggerFactory)
        {
            _symbolList = !string.IsNullOrEmpty(Program.Settings.FtxInstrumentsOriginalSymbolToSymbol)
                ? Program.Settings.FtxInstrumentsOriginalSymbolToSymbol.Split(';').ToList()
                : new List<string>();

            _wsFtx = new FtxWsOrderBooks(loggerFactory.CreateLogger<FtxWsOrderBooks>(), _symbolList.ToArray());
        }

        public List<string> GetSymbols()
        {
            return _symbolList.ToList();
        }

        public bool HasSymbol(string symbol)
        {
            return _symbolList.Contains(symbol);
        }

        public LeOrderBook GetOrderBook(string symbol)
        {
            if (!_symbolList.Contains(symbol))
            {
                return null;
            }

            var data = _wsFtx.GetOrderBookById(symbol);

            if (data == null)
                return null;

            var book = new LeOrderBook()
            {
                Symbol = symbol,
                Timestamp = data.GetTime().UtcDateTime,
                Asks = data.asks.Select(LeOrderBookLevel.Create).Where(e => e != null).ToList(),
                Bids = data.bids.Select(LeOrderBookLevel.Create).Where(e => e != null).ToList(),
                Source = FtxConst.Name
            };

            return book;
        }

        public void Start()
        {
            _wsFtx.Start();
        }

        public void Stop()
        {
            _wsFtx.Stop();
        }


        public void Dispose()
        {
            _wsFtx?.Dispose();
        }
    }
}