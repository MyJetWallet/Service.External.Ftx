using MyJetWallet.Sdk.Service;
using MyYamlParser;

namespace Service.External.Ftx.Settings
{
    public class SettingsModel
    {
        [YamlProperty("ExternalFtx.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("ExternalFtx.ZipkinUrl")]
        public string ZipkinUrl { get; set; }

        [YamlProperty("ExternalFtx.ApiKey")]
        public string ApiKey { get; set; }

        [YamlProperty("ExternalFtx.ApiSecret")]
        public string ApiSecret { get; set; }

        [YamlProperty("ExternalFtx.ElkLogs")]
        public LogElkSettings ElkLogs { get; set; }
        
        [YamlProperty("ExternalFtx.MyNoSqlWriterUrl")]
        public string MyNoSqlWriterUrl { get; set; }

        [YamlProperty("ExternalFtx.ServiceBusHostPort")]
        public string ServiceBusHostPort { get; set; }
        
        [YamlProperty("ExternalFtx.SubAccount")]
        public string SubAccount { get; set; }
    }
}
