using AssettoServer.Server.Plugin;
using Autofac;

namespace StatLoggingPlugin;

public class StatLoggingModule : AssettoServerModule
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<StatLoggingPlugin>().AsSelf().AutoActivate().SingleInstance();
    }
}