using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using FtxApi;
using FtxApi.Enums;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.ExternalMarketApi;
using MyJetWallet.Domain.ExternalMarketApi.Dto;
using MyJetWallet.Domain.ExternalMarketApi.Models;
using MyJetWallet.Domain.Orders;
using MyJetWallet.Sdk.ExternalMarketsSettings.Settings;
using MyJetWallet.Sdk.Service;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using OrderType = FtxApi.Enums.OrderType;

namespace Service.External.Ftx.Services
{
    public class ExternalMarketGrpc : IExternalMarket
    {
        private readonly ILogger<ExternalMarketGrpc> _logger;
        private readonly FtxRestApi _restApi;
        private readonly BalanceCache _balanceCache;
        private readonly IExternalMarketSettingsAccessor _externalMarketSettingsAccessor;

        public ExternalMarketGrpc(ILogger<ExternalMarketGrpc> logger, FtxRestApi restApi, BalanceCache balanceCache,
            IExternalMarketSettingsAccessor externalMarketSettingsAccessor)
        {
            _logger = logger;
            _restApi = restApi;
            _balanceCache = balanceCache;
            _externalMarketSettingsAccessor = externalMarketSettingsAccessor;
        }

        public Task<GetNameResult> GetNameAsync()
        {
            return Task.FromResult(new GetNameResult() {Name = FtxConst.Name});
        }

        public async Task<GetBalancesResponse> GetBalancesAsync()
        {
            try
            {
                var balance = await _balanceCache.GetBalancesAsync();
                return balance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot get FTX balance");
                throw;
            }
        }

        public Task<GetMarketInfoResponse> GetMarketInfoAsync(MarketRequest request)
        {
            try
            {
                var data = _externalMarketSettingsAccessor.GetExternalMarketSettings(request.Market);
                if (data == null)
                {
                    return new GetMarketInfoResponse().AsTask();
                }

                return new GetMarketInfoResponse
                {
                    Info = new ExchangeMarketInfo()
                    {
                        Market = data.Market,
                        BaseAsset = data.BaseAsset,
                        QuoteAsset = data.QuoteAsset,
                        MinVolume = data.MinVolume,
                        PriceAccuracy = data.PriceAccuracy,
                        VolumeAccuracy = data.VolumeAccuracy
                    }
                }.AsTask();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot get FTX GetMarketInfo: {marketText}", request.Market);
                throw;
            }
        }

        public Task<GetMarketInfoListResponse> GetMarketInfoListAsync()
        {
            try
            {
                var data = _externalMarketSettingsAccessor.GetExternalMarketSettingsList();
                var result =  new GetMarketInfoListResponse
                {
                    Infos = data.Select(e => new ExchangeMarketInfo()
                    {
                        Market = e.Market,
                        BaseAsset = e.BaseAsset,
                        QuoteAsset = e.QuoteAsset,
                        MinVolume = e.MinVolume,
                        PriceAccuracy = e.PriceAccuracy,
                        VolumeAccuracy = e.VolumeAccuracy
                    }).ToList()
                };
                _logger.LogInformation(JsonSerializer.Serialize(result, new JsonSerializerOptions {WriteIndented = true}));
                _logger.LogInformation(JsonSerializer.Serialize(result, new JsonSerializerOptions {WriteIndented = true}).AsTask().Result);
                
                return result.AsTask();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot get FTX GetMarketInfo");
                throw;
            }
        }

        public async Task<ExchangeTrade> MarketTrade(MarketTradeRequest request)
        {
            try
            {
                using var action = MyTelemetry.StartActivity("FTX Market Trade");
                request.AddToActivityAsJsonTag("request");

                var refId = request.ReferenceId ?? Guid.NewGuid().ToString("N");

                refId.AddToActivityAsTag("reference-id");

                var size = (decimal) Math.Abs(request.Volume);

                var resp = await _restApi.PlaceOrderAsync(request.Market,
                    request.Side == OrderSide.Buy ? SideType.buy : SideType.sell, 0, OrderType.market,
                    size, refId, true);

                resp.AddToActivityAsJsonTag("marketOrder-response");

                if (!resp.Success && resp.Error != "Duplicate client order ID")
                {
                    throw new Exception(
                        $"Cannot place marketOrder. Error: {resp.Error}. Request: {JsonConvert.SerializeObject(request)}. Reference: {refId}");
                }

                action?.AddTag("is-duplicate", resp.Error == "Duplicate client order ID");

                var tradeData = await _restApi.GetOrderStatusByClientIdAsync(refId);

                if (!tradeData.Success)
                {
                    throw new Exception(
                        $"Cannot get order state. Error: {resp.Error}. Request: {JsonConvert.SerializeObject(request)}. Reference: {refId}");
                }

                if (tradeData.Result.Status != "closed")
                {
                    await _restApi.CancelOrderByClientIdAsync(refId);
                }

                tradeData = await _restApi.GetOrderStatusByClientIdAsync(refId);

                tradeData.AddToActivityAsJsonTag("order-status-response");

                if (!tradeData.Success)
                {
                    throw new Exception(
                        $"Cannot get second order state. Error: {resp.Error}. Request: {JsonConvert.SerializeObject(request)}. Reference: {refId}");
                }

                size = tradeData.Result.FilledSize ?? 0;
                if (tradeData.Result.Side == "sell")
                    size = size * -1;

                var trade = new ExchangeTrade()
                {
                    Id = (tradeData.Result.Id ?? 0).ToString(CultureInfo.InvariantCulture),
                    Market = tradeData.Result.Market,
                    Side = tradeData.Result.Side == "buy" ? OrderSide.Buy : OrderSide.Sell,
                    Price = (double) (tradeData.Result.AvgFillPrice ?? 0),
                    ReferenceId = tradeData.Result.ClientId,
                    Source = FtxConst.Name,
                    Volume = (double) size,
                    Timestamp = tradeData.Result.CreatedAt
                };

                trade.AddToActivityAsJsonTag("response");

                if (resp.Error == "Duplicate client order ID")
                {
                    _logger.LogInformation("Ftx trade is Duplicate. Request: {requestJson}. Trade: {tradeJson}",
                        JsonConvert.SerializeObject(request), JsonConvert.SerializeObject(trade));
                }
                else
                {
                    _logger.LogInformation("Ftx trade is done. Request: {requestJson}. Trade: {tradeJson}",
                        JsonConvert.SerializeObject(request), JsonConvert.SerializeObject(trade));
                }

                return trade;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot execute trade. Request: {requestJson}",
                    JsonConvert.SerializeObject(request));
                throw;
            }
        }
    }
}