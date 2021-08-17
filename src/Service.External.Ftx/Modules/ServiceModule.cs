using Autofac;
using FtxApi;
using MyJetWallet.Connector.Ftx.Rest;
using MyJetWallet.Domain.Prices;
using MyJetWallet.Sdk.ExternalMarketsSettings.NoSql;
using MyJetWallet.Sdk.ExternalMarketsSettings.Services;
using MyJetWallet.Sdk.ExternalMarketsSettings.Settings;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.ServiceBus;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataWriter;
using Service.External.Ftx.Services;

namespace Service.External.Ftx.Modules
{
    public class ServiceModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            FtxRestApi ftxRestClient = FtxRestApiFactory.CreateClient(Program.Settings.ApiKey, Program.Settings.ApiSecret);
            builder.RegisterInstance(ftxRestClient).AsSelf().SingleInstance();

            builder.RegisterType<BalanceCache>().As<IStartable>().AutoActivate().AsSelf().SingleInstance();
            builder.RegisterType<OrderBookManager>().AsSelf().SingleInstance();

            builder
                .RegisterType<ExternalMarketSettingsManager>()
                .WithParameter("name", FtxConst.Name)
                .As<IExternalMarketSettingsManager>()
                .As<IExternalMarketSettingsAccessor>()
                .AsSelf()
                .SingleInstance();


            RegisterMyNoSqlWriter<ExternalMarketSettingsNoSql>(builder, ExternalMarketSettingsNoSql.TableName);

            var serviceBusClient = builder.RegisterMyServiceBusTcpClient(() => Program.Settings.ServiceBusHostPort, ApplicationEnvironment.HostName, Program.LogFactory);

            builder.RegisterMyServiceBusPublisher<BidAsk>(serviceBusClient, "jetwallet-external-prices", false);
        }

        private void RegisterMyNoSqlWriter<TEntity>(ContainerBuilder builder, string table)
            where TEntity : IMyNoSqlDbEntity, new()
        {
            builder.Register(ctx =>
                    new MyNoSqlServerDataWriter<TEntity>(
                        Program.ReloadedSettings(e => e.MyNoSqlWriterUrl), table, true))
                .As<IMyNoSqlServerDataWriter<TEntity>>()
                .SingleInstance();
        }
    }
}