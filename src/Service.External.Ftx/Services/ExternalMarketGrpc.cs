using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FtxApi;
using FtxApi.Enums;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.ExternalMarketApi;
using MyJetWallet.Domain.ExternalMarketApi.Dto;
using MyJetWallet.Domain.ExternalMarketApi.Models;
using MyJetWallet.Domain.Orders;
using MyJetWallet.Sdk.Service;
using Newtonsoft.Json;

namespace Service.External.Ftx.Services
{
    public class ExternalMarketGrpc: IExternalMarket
    {

        private readonly ILogger<ExternalMarketGrpc> _logger;
        private readonly FtxRestApi _restApi;
        private readonly BalanceCache _balanceCache;
        private readonly MarketInfoData _marketInfoData;

        public ExternalMarketGrpc(ILogger<ExternalMarketGrpc> logger, FtxRestApi restApi, BalanceCache balanceCache, MarketInfoData marketInfoData)
        {
            _logger = logger;
            _restApi = restApi;
            _balanceCache = balanceCache;
            _marketInfoData = marketInfoData;
        }

        public Task<GetNameResult> GetNameAsync()
        {
            return Task.FromResult(new GetNameResult() { Name = FtxConst.Name  });
        }

        public async Task<GetBalancesResponse> GetBalancesAsync()
        {
            try
            {
                var balance = await _balanceCache.GetBalancesAsync();
                return balance;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Cannot get FTX balance");
                throw;
            }
        }

        public async Task<GetMarketInfoResponse> GetMarketInfoAsync(MarketRequest request)
        {
            try
            {
                var data = await _marketInfoData.GetMarketInfo();
                return new GetMarketInfoResponse()
                {
                    Info = data.FirstOrDefault(e => e.Market == request.Market)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot get FTX GetMarketInfo: {marketText}", request.Market);
                throw;
            }
        }

        public async Task<GetMarketInfoListResponse> GetMarketInfoListAsync()
        {
            try
            {
                var data = await _marketInfoData.GetMarketInfo();
                return new GetMarketInfoListResponse()
                {
                    Infos = data
                };
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

                var resp = await _restApi.PlaceOrderAsync(request.Market,
                    request.Side == OrderSide.Buy ? SideType.buy : SideType.sell, 0, FtxApi.Enums.OrderType.market,
                    (decimal) request.Volume, refId, true);

                resp.AddToActivityAsJsonTag("marketOrder-response");

                if (!resp.Success && resp.Error != "Duplicate client order ID")
                {
                    throw new Exception(
                        $"Cannot place marketOrder. Error: {resp.Error}. Request: {JsonConvert.SerializeObject(request)}. Reference: {refId}");
                }

                action?.AddTag("is-duplicate", resp.Error != "Duplicate client order ID");

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

                var trade = new ExchangeTrade()
                {
                    Id = (tradeData.Result.Id ?? 0).ToString(CultureInfo.InvariantCulture),
                    Market = tradeData.Result.Market,
                    Side = tradeData.Result.Side == "buy" ? OrderSide.Buy : OrderSide.Sell,
                    Price = (double) (tradeData.Result.AvgFillPrice ?? 0),
                    ReferenceId = tradeData.Result.ClientId,
                    Source = FtxConst.Name,
                    Volume = (double) (tradeData.Result.FilledSize ?? 0),
                    Timestamp = tradeData.Result.CreatedAt
                };

                trade.AddToActivityAsJsonTag("response");

                if (resp.Error == "Duplicate client order ID")
                {
                    _logger.LogInformation("Ftx trade is Duplicate. Request: {requestJson}. Trade: {tradeJson}", JsonConvert.SerializeObject(request), JsonConvert.SerializeObject(trade));
                }
                else
                {
                    _logger.LogInformation("Ftx trade is done. Request: {requestJson}. Trade: {tradeJson}", JsonConvert.SerializeObject(request), JsonConvert.SerializeObject(trade));
                }

                return trade;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Cannot execute trade. Request: {requestJson}", JsonConvert.SerializeObject(request));
                throw;
            }
        }
    }
}