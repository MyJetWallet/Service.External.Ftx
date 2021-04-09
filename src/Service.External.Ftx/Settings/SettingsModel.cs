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
    }
}
