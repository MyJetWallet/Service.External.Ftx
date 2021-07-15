using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using FtxApi;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain.ExternalMarketApi.Dto;
using MyJetWallet.Domain.ExternalMarketApi.Models;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.Service.Tools;

namespace Service.External.Ftx.Services
{
    public class BalanceCache: IStartable
    {
        private readonly FtxRestApi _restApi;
        private readonly ILogger<BalanceCache> _logger;

        private GetBalancesResponse _response = null;
        private DateTime _lastUpdate = DateTime.MinValue;
        private SemaphoreSlim _slim = new SemaphoreSlim(1);
        

        public BalanceCache(FtxRestApi restApi, ILogger<BalanceCache> logger)
        {
            _restApi = restApi;
            _logger = logger;
        }

        public async Task<GetBalancesResponse> GetBalancesAsync()
        {
            await _slim.WaitAsync();
            try
            {
                if (_response == null || (DateTime.UtcNow - _lastUpdate).TotalSeconds > 1)
                {
                    await RefreshBalancesAsync();
                }

                return _response;
            }
            finally
            {
                _slim.Release();
            }
        }

        private async Task<GetBalancesResponse> RefreshBalancesAsync()
        {
            using var activity = MyTelemetry.StartActivity("Load balance info");

            var data = await _restApi.GetBalancesAsync();

            if (data.Success)
            {
                _response = new GetBalancesResponse()
                {
                    Balances = data.Result.Select(e => new ExchangeBalance()
                        { Symbol = e.Coin, Balance = e.Total, Free = e.Free }).ToList()
                };
                _lastUpdate = DateTime.UtcNow;
            }
            else
            {
                throw new Exception($"Cannot get balance, error: {data.Error}");
            }

            _response.AddToActivityAsJsonTag("balance");

            _logger.LogDebug("Balance refreshed");

            return _response;
            
        }

        public void Start()
        {
            RefreshBalancesAsync().GetAwaiter().GetResult();
        }
    }
}