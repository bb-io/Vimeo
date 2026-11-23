using Newtonsoft.Json;

namespace Apps.Vimeo.Models.Dtos;

internal class VimeoPage<T>
{
    [JsonProperty("data")]
    public List<T> Data { get; set; } = [];

    [JsonProperty("paging")]
    public VimeoPaging? Paging { get; set; }
}
