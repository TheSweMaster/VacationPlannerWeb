using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VacationPlannerWeb.JsonModels
{
    public partial class SvenskaDagar
    {
        [JsonProperty("cachetid")]
        public string Cachetid { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("uri")]
        public string Uri { get; set; }
        [JsonProperty("startdatum")]
        public string Startdatum { get; set; }
        [JsonProperty("slutdatum")]
        public string Slutdatum { get; set; }
        [JsonProperty("dagar")]
        public List<Dagar> Dagar { get; set; }
    }

    public partial class Dagar
    {
        [JsonProperty("datum")]
        public string Datum { get; set; }
        [JsonProperty("veckodag")]
        public string Veckodag { get; set; }
        [JsonProperty("arbetsfri dag")]
        public string Arbetsfri_dag { get; set; }
        [JsonProperty("r\u00f6d dag")]
        public string Rod_dag { get; set; }
        [JsonProperty("vecka")]
        public string Vecka { get; set; }
        [JsonProperty("dag i vecka")]
        public string Dag_i_vecka { get; set; }
        [JsonProperty("helgdag")]
        public string Helgdag { get; set; }
        [JsonProperty("namnsdag")]
        public List<string> Namnsdag { get; set; }
        [JsonProperty("flaggdag")]
        public string Flaggdag { get; set; }
        [JsonProperty("helgdagsafton")]
        public string Helgdagsafton { get; set; }
        [JsonProperty("dag f\u00f6re arbetsfri helgdag")]
        public string Dag_fore_arbetsfri_helgdag { get; set; }
        [JsonProperty("kl\u00e4mdag")]
        public string Klamdag { get; set; }
    }

    public partial class SvenskaDagar
    {
        public static SvenskaDagar FromJson(string json) => JsonConvert.DeserializeObject<SvenskaDagar>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this SvenskaDagar self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    internal class Converter
    {
        public static readonly JsonSerializerSettings Settings = new()
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

}
