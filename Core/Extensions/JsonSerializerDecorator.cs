using Newtonsoft.Json;
using SpotifyAPI.Web.Http;

namespace Core.Extensions;

public class JsonSerializerDecorator : IJSONSerializer
{
    private readonly NewtonsoftJSONSerializer jsonSerializer = new();

    public void SerializeRequest(IRequest request)
    {
        jsonSerializer.SerializeRequest(request);
    }

    public IAPIResponse<T> DeserializeResponse<T>(IResponse response)
    {
        try
        {
            return jsonSerializer.DeserializeResponse<T>(response);
        }
        catch (JsonReaderException)
        {
            return new APIResponse<T>(response);
        }
    }
}