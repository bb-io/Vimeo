using Apps.Vimeo.Constants;
using Apps.Vimeo.Models.Response;
using Blackbird.Applications.Sdk.Utils.Extensions.String;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.Vimeo.Api;

public class VimeoClient : BlackBirdRestClient
{
    protected override JsonSerializerSettings? JsonSettings => JsonConfig.JsonSettings;

    public VimeoClient() : base(new()
    {
        BaseUrl = "https://api.vimeo.com".ToUri()
    })
    {
    }

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        var error = JsonConvert.DeserializeObject<ErrorResponse>(response.Content);
        return new(error.Error);
    }
}