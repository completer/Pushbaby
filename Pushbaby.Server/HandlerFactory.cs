namespace Pushbaby.Server
{
    public interface IHandlerFactory
    {
        Handler Create(EndpointSettings settings, IContext context);
    }
}
