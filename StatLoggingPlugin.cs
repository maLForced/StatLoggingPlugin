using AssettoServer.Server;
using AssettoServer.Network.Tcp;
using AssettoServer.Server.Configuration;
using Serilog;
using Microsoft.Data.Sqlite;

namespace StatLoggingPlugin;

public class StatLoggingPlugin
{
    private readonly StatLoggingConfiguration _pluginConfiguration;
    public readonly ACServerConfiguration _serverConfiguration;
    public string dbFilePath = @"Data Source=plugins\StatLoggingPlugin\statlogging.db";
    public string _serverName;
    private Dictionary<ulong, StatLoggingSession> _sessions = new Dictionary<ulong, StatLoggingSession>();
    public StatLoggingPlugin(EntryCarManager entryCarManager, ACServerConfiguration serverConfiguration, StatLoggingConfiguration pluginConfiguration)
    {
        Log.Information("------------------------------------");
        Log.Information("Starting: StatLogging Plugin by maL");
        Log.Information("------Original Code from Yhugi------");
        Log.Information("------------------------------------");

        _pluginConfiguration = pluginConfiguration;

        if (pluginConfiguration.ServerName is null){
            _serverName = serverConfiguration.Server.Name;
        }
        else{
            _serverName = pluginConfiguration.ServerName;
        }
        
        if (_pluginConfiguration.CommonDB == true && _pluginConfiguration.CommonDBFileLocation is not null){
            Log.Information("CommonDB Selected");
            Log.Information(_pluginConfiguration.CommonDBFileLocation);
            dbFilePath = "Data Source=" + _pluginConfiguration.CommonDBFileLocation + @"statlogging.db";
        }
        using (var connection = new SqliteConnection(dbFilePath)) {
            connection.Open();

                var command = connection.CreateCommand();

                command.CommandText =
                @"
                    CREATE TABLE IF NOT EXISTS statlogging (
                        sessionDate INTEGER NOT NULL,
                        steamID TEXT NOT NULL,
                        userName TEXT NOT NULL,
                        carModel TEXT NOT NULL,
                        carSkin TEXT NOT NULL,
                        kmDriven REAL NOT NULL,
                        timeSpent INTEGER NOT NULL,
                        topSpeed INTEGER NOT NULL,
                        evnCollisions INTEGER NOT NULL,
                        trafficCollisions INTEGER NOT NULL,
                        playerCollisions INTEGER NOT NULL,
                        majorAccidents INTEGER NOT NULL,
                        connectedServer text NOT NULL,
                        track text NOT NULL
                    )
                ";
                command.ExecuteNonQuery();

        }
        _serverConfiguration = serverConfiguration;

        entryCarManager.ClientConnected += OnClientConnected;
        entryCarManager.ClientDisconnected += OnClientDisconnected;
    }
    private void OnClientConnected(ACTcpClient client, EventArgs args)
    {
        if(client is null)
        {
            return;
        }
        StatLoggingSession _statLogginSession = new StatLoggingSession(client);
        _sessions[client.Guid] = _statLogginSession;
    }

    private void OnClientDisconnected(ACTcpClient client, EventArgs args)
    {
        _sessions[client.Guid].LogData(_sessions[client.Guid], dbFilePath, _serverName, _serverConfiguration.Server.Track);
        _sessions.Remove(client.Guid);
    }
}