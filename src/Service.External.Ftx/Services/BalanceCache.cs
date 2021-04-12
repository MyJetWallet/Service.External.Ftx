using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using FtxApi;
using MyJetWallet.Domain.ExternalMarketApi.Dto;
using MyJetWallet.Domain.ExternalMarketApi.Models;
using MyJetWallet.Sdk.Service;

namespace Service.External.Ftx.Services
{
    public class BalanceCache: IStartable
    {
        private readonly FtxRestApi _restApi;

        private GetBalancesResponse _response = null;
        private DateTime _lastUpdate = DateTime.MinValue;
        private SemaphoreSlim _slim = new SemaphoreSlim(1);

        public BalanceCache(FtxRestApi restApi)
        {
            _restApi = restApi;
        }

        public async Task<GetBalancesResponse> GetBalancesAsync()
        {
            await _slim.WaitAsync();
            try
            {
                if (_response == null || (DateTime.UtcNow - _lastUpdate).TotalSeconds > 1)
                {
                    var data = await _restApi.GetBalancesAsync();

                    if (data.Success)
                    {
                        _response = new GetBalancesResponse()
                        {
                            Balances = data.Result.Select(e => new ExchangeBalance()
                                {Symbol = e.Coin, Balance = e.Total, Free = e.Free}).ToList()
                        };
                        _lastUpdate = DateTime.UtcNow;
                    }
                    else
                    {
                        throw new Exception($"Cannot get balance, error: {data.Error}");
                    }
                }

                return _response;
            }
            finally
            {
                _slim.Release();
            }
        }

        public async Task<GetBalancesResponse> RefreshBalancesAsync()
        {
            await _slim.WaitAsync();
            try
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

                return _response;
            }
            finally
            {
                _slim.Release();
            }
        }

        public void Start()
        {
            RefreshBalancesAsync().GetAwaiter().GetResult();
        }
    }
}