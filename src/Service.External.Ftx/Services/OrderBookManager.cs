using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Connector.Ftx.WebSocket;
using MyJetWallet.Domain.ExternalMarketApi.Models;
using MyJetWallet.Sdk.ExternalMarketsSettings.Settings;

namespace Service.External.Ftx.Services
{
    public class OrderBookManager : IDisposable
    {
        private readonly FtxWsOrderBooks _wsFtx;
        private readonly IExternalMarketSettingsAccessor _externalMarketSettingsAccessor;

        public OrderBookManager(IExternalMarketSettingsAccessor externalMarketSettingsAccessor,
            ILoggerFactory loggerFactory)
        {
            _externalMarketSettingsAccessor = externalMarketSettingsAccessor;

            _wsFtx = new FtxWsOrderBooks(loggerFactory.CreateLogger<FtxWsOrderBooks>(),
                _externalMarketSettingsAccessor.GetExternalMarketSettingsList().Select(e => e.Market).ToArray());
            _wsFtx.ReceiveUpdates += book => Task.CompletedTask;
        }

        public List<string> GetSymbols()
        {
            return _externalMarketSettingsAccessor.GetExternalMarketSettingsList().Select(e => e.Market).ToList();
        }

        public bool HasSymbol(string symbol)
        {
            return _externalMarketSettingsAccessor.GetExternalMarketSettingsList().Find(e => e.Market == symbol) !=
                   null;
        }

        public async Task Resubscribe(string symbol)
        {
            await _wsFtx.Reset(symbol);
        }

        public async Task Subscribe(string symbol)
        {
            await _wsFtx.Subscribe(symbol);
        }

        public async Task Unsubscribe(string symbol)
        {
            await _wsFtx.Unsubscribe(symbol);
        }

        public LeOrderBook GetOrderBook(string symbol)
        {
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