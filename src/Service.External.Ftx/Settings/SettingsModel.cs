using SimpleTrading.SettingsReader;

namespace Service.External.Ftx.Settings
{
    [YamlAttributesOnly]
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

        [YamlProperty("ExternalFtx.FtxInstrumentsOriginalSymbolToSymbol")]
        public string FtxInstrumentsOriginalSymbolToSymbol { get; set; }

        
    }
}
