using AssettoServer.Server.Plugin;
using Autofac;

namespace StatLoggingPlugin;

public class StatLoggingPluginModule : AssettoServerModule<StatLoggingConfiguration>
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<StatLoggingPlugin>().AsSelf().AutoActivate().SingleInstance();
    }
}
