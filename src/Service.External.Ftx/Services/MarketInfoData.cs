using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using FtxApi;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.ExternalMarketApi.Models;
using MyJetWallet.Sdk.Service;

namespace Service.External.Ftx.Services
{
    public class MarketInfoData: IStartable
    {
        private readonly FtxRestApi _restApi;
        private readonly ILogger<MarketInfoData> _logger;
        private Dictionary<string, ExchangeMarketInfo> _marketInfoData = new Dictionary<string, ExchangeMarketInfo>();

        public MarketInfoData(FtxRestApi restApi, ILogger<MarketInfoData> logger)
        {
            _restApi = restApi;
            _logger = logger;
        }

        private async Task LoadMarketInfo()
        {
            using var activity = MyTelemetry.StartActivity("Load market info");
            try
            {
                var data = await _restApi.GetMarketsAsync();

                if (!data.Success)
                {
                    throw new Exception($"Error from FTX: {data.Error}");
                }

                var result = new Dictionary<string, ExchangeMarketInfo>();

                foreach (var marketInfo in data.Result.Where(e => e.Type == "spot" && e.Enabled))
                {
                    var resp = new ExchangeMarketInfo()
                    {
                        Market = marketInfo.Name,
                        MinVolume = (double)(marketInfo.MinProvideSize ?? 0),
                        BaseAsset = marketInfo.BaseCurreny,
                        QuoteAsset = marketInfo.QuoteCurrency,
                        
                    };

                    var volumeParams = (marketInfo.SizeIncrement ?? 0m).ToString(CultureInfo.InvariantCulture).Split('.');
                    var priceParams = (marketInfo.PriceIncrement ?? 0m).ToString(CultureInfo.InvariantCulture).Split('.');
                    resp.VolumeAccuracy = volumeParams.Length == 2 ? volumeParams.Length : 0;
                    resp.PriceAccuracy = priceParams.Length == 2 ? priceParams.Length : 0;

                    result[resp.Market] = resp;
                }

                _marketInfoData = result;
            }
            catch (Exception ex)
            {
                ex.FailActivity();
                _logger.LogError(ex, "Cannot get market info from FTX");
            }
        }

        public async Task<List<ExchangeMarketInfo>> GetMarketInfo()
        {
            if (_marketInfoData == null || !_marketInfoData.Any())
            {
                await LoadMarketInfo();
            }

            return _marketInfoData.Values.ToList();
        }


        public void Start()
        {
            LoadMarketInfo().GetAwaiter().GetResult();
        }
    }
}