using AssettoServer.Network.Tcp;
using Serilog;

namespace StatLoggingPlugin;

public class StatLoggingSessionManager
{
    private StatLoggingPlugin _plugin;
    private Dictionary<ulong, StatLoggingSession> _sessions = new Dictionary<ulong, StatLoggingSession>();

    public StatLoggingSessionManager(StatLoggingPlugin plugin)
    {
        _plugin = plugin;
    }

    public Dictionary<ulong, StatLoggingSession> GetSessions()
    {
        return _sessions;
    }

    public StatLoggingSession? GetSession(ACTcpClient client)
    {
        if(_sessions.ContainsKey(client.Guid))
        {
            return _sessions[client.Guid];
        }

        return null;
    }

    public StatLoggingSession? AddSession(ACTcpClient client)
    {
        if(GetSession(client) != null)
        {
            return null;
        }
        StatLoggingSession newSession = new StatLoggingSession(client, _plugin);
        _sessions[client.Guid] = newSession;
        newSession.OnCreation();

        return newSession;
    }

    public void RemoveSession(ACTcpClient client)
    {
        StatLoggingSession? session = GetSession(client);
        if(session == null)
        {
            return;
        }

        session.OnRemove();
        _sessions.Remove(client.Guid);
    }
}