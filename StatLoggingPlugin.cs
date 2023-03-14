using AssettoServer.Server;
using AssettoServer.Network.Tcp;
using AssettoServer.Server.Configuration;
using Serilog;
using Microsoft.Data.Sqlite;

namespace StatLoggingPlugin;

public class StatLoggingPlugin
{
    public class clientEntryCarData
    {
        public string Guid { get; set; } = "Not Set Yet";
        public string Username { get; set; } = "Not Set Yet";
        public double KmDriven { get; set; } = 0;
        public double TimeSpent { get; set; } = 0;
        public int PlayerCollisions { get; set; } = 0;
        public int TrafficCollisions { get; set; } = 0;
        public int MajorAccidents { get; set; } = 0;
        public int EvnCollisions { get; set; } = 0;
        public int TopSpeed { get; set; } = 0;
    }

    private readonly StatLoggingSessionManager _StatLoggingSessionManager;
    private readonly EntryCarManager _entryCarManager;
    public readonly SessionManager _sessionManager;
    private readonly StatLoggingConfiguration _pluginConfiguration;
    public readonly ACServerConfiguration _serverConfiguration;
    private string dbFilePath = @"Data Source=plugins\StatLoggingPlugin\statlogging.db";

    private Dictionary<string, clientEntryCarData> _entryCarQueueDictionary = new Dictionary<string, clientEntryCarData>();

    public StatLoggingPlugin(EntryCarManager entryCarManager, SessionManager sessionManager, ACServerConfiguration serverConfiguration, StatLoggingConfiguration pluginConfiguration)
    {
        Log.Information("------------------------------------");
        Log.Information("Starting: StatLogging Plugin by maL");
        Log.Information("------Original Code from Yhugi------");
        Log.Information("------------------------------------");

        _pluginConfiguration = pluginConfiguration;
        
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
                        connectedServer text NOT NULL
                    )
                ";
                command.ExecuteNonQuery();

        }

        _entryCarManager = entryCarManager;
        _sessionManager = sessionManager;
        _serverConfiguration = serverConfiguration;
        _StatLoggingSessionManager = new StatLoggingSessionManager(this);

        entryCarManager.ClientConnected += OnClientConnected;
        entryCarManager.ClientDisconnected += OnClientDisconnected;

        var timer1 = new System.Timers.Timer();
        timer1.Elapsed += new System.Timers.ElapsedEventHandler(HandleClientEntryCars);
        timer1.Interval = 500;
        timer1.Start();
    }
    private void OnCollisionEventsReceived(ACTcpClient client, CollisionEventArgs args)
    {
        foreach (var dictSession in _StatLoggingSessionManager.GetSessions())
            {
                var session = dictSession.Value;
                if(session.GetType() == typeof(StatLoggingSession))
                {
                    if (args.TargetCar is null){
                        session._evnCollisions += 1;
                        if (args.Speed >= 150){
                            session._majorAccidents += 1;
                        }
                    }
                    else if (args.TargetCar.AiControlled && args.TargetCar.AiName is not null){
                        session._trafficCollisions += 1;
                        if (args.Speed >= 150){
                            session._majorAccidents += 1;
                        }
                    }
                    else if (!args.TargetCar.AiControlled && args.TargetCar.Client is not null){
                        session._playerCollisions += 1;
                        if (args.Speed >= 150){
                            session._majorAccidents += 1;
                        }
                    }
                }
            }
    }
    private void OnClientConnected(ACTcpClient client, EventArgs args)
    {
        _StatLoggingSessionManager.AddSession(client);
        client.Collision += OnCollisionEventsReceived;
    }

    private void OnClientDisconnected(ACTcpClient client, EventArgs args)
    {
        _StatLoggingSessionManager.RemoveSession(client);
    }

    private void HandleClientEntryCars(object? sender, EventArgs e)
    {
        foreach (var dictSession in _StatLoggingSessionManager.GetSessions())
        {
            var session = dictSession.Value;
            if(session.GetType() == typeof(StatLoggingSession))
            {
                ACTcpClient client = session._client;
                if(client.IsConnected && client.HasSentFirstUpdate)
                {
                    EntryCar car = client.EntryCar;
                    if(car != null)
                    {
                        int speedKmh = ((int) (car.Status.Velocity.Length() * 3.6f));
                        session.AddToAverageSpeed(speedKmh);
                        if(speedKmh > session.GetTopSpeed())
                        {
                            session.SetTopSpeed(speedKmh);
                        }
                    }
                }
            }
        }
    }

    public void CreateClientForEntryCarData(EntryCar car, ACTcpClient client, StatLoggingSession session)
    {
        string model = car.Model;
        string guid = client.Guid.ToString();

        clientEntryCarData newData = new clientEntryCarData(){
            Username = session.getUsername(),
            Guid = session.getGuid(),
            KmDriven = session.CalculateDistanceDriven(),
            TimeSpent = session.CalculateTimeSpent(),
            TopSpeed = session.GetTopSpeed(),
        };
        Log.Information(dbFilePath);
        using (var connection = new SqliteConnection(dbFilePath)) {
            connection.Open();

                var command = connection.CreateCommand();

                command.CommandText =
                @"
                    INSERT INTO statlogging (
                        sessionDate,
                        steamID,
                        userName,
                        carModel,
                        carSkin,
                        kmDriven,
                        timeSpent,
                        topSpeed,
                        evnCollisions,
                        trafficCollisions,
                        playerCollisions,
                        majorAccidents,
                        connectedServer
                    )
                    VALUES (
                        @sessionDate,
                        @steamID,
                        @userName,
                        @carModel,
                        @carSkin,
                        @kmDriven,
                        @timeSpent,
                        @topSpeed,
                        @evnCollisions,
                        @trafficCollisions,
                        @playerCollisions,
                        @majorAccidents,
                        @connectedServer
                    )
                ";
                command.Parameters.Add(new SqliteParameter("@sessionDate", session._creationDate));
                command.Parameters.Add(new SqliteParameter("@steamID", guid));
                command.Parameters.Add(new SqliteParameter("@userName", newData.Username));
                command.Parameters.Add(new SqliteParameter("@carModel", car.Model));
                command.Parameters.Add(new SqliteParameter("@carSkin", car.Skin));
                command.Parameters.Add(new SqliteParameter("@kmDriven", newData.KmDriven));
                command.Parameters.Add(new SqliteParameter("@timeSpent", newData.TimeSpent));
                command.Parameters.Add(new SqliteParameter("@topSpeed", newData.TopSpeed));
                command.Parameters.Add(new SqliteParameter("@evnCollisions", session._evnCollisions));
                command.Parameters.Add(new SqliteParameter("@trafficCollisions", session._trafficCollisions));
                command.Parameters.Add(new SqliteParameter("@playerCollisions", session._playerCollisions));
                command.Parameters.Add(new SqliteParameter("@majorAccidents", session._majorAccidents));
                command.Parameters.Add(new SqliteParameter("@connectedServer", _pluginConfiguration.ServerName));
                command.ExecuteNonQuery();
        }
        Log.Information(string.Format("Session data written to database for {0}", client.Name ?? "Unknown"));

        _entryCarQueueDictionary[$"{model}:{guid}"] = newData;

    }
}
