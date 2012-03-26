namespace Pushbaby.Server
{
    public interface IEndpointFactory
    {
        Endpoint Create(EndpointSettings settings);
    }
}
