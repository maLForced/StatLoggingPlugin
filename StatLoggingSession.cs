using AssettoServer.Network.Tcp;
using AssettoServer.Server;
using Serilog;
using Microsoft.Data.Sqlite;

namespace StatLoggingPlugin;

public sealed class StatLoggingSession
{
    public ACTcpClient _client;
    public string _username;
    public string _guid;
    public string _carModel;
    public string _carSkin;

    private long _sessionLength;
    private System.Timers.Timer _timer;
    public long _creationDate;
    private int _topSpeed = 0;
    private int _speedCounter = 0;
    private long _speedSum = 0;
    public int _playerCollisions { get; set; } = 0;
    public int _trafficCollisions { get; set; } = 0;
    public int _majorAccidents { get; set; } = 0;
    public int _evnCollisions { get; set; } = 0;
    

    public StatLoggingSession(ACTcpClient client)
    {
        Log.Information(string.Format("Session created: {0}", client.Name ?? "Unknown"));
        _client = client;
        _username = client.Name ?? "Unknown";
        _guid = client.Guid.ToString();
        client.Collision += OnCollision;
        _timer = new System.Timers.Timer();
        _timer.Elapsed += new System.Timers.ElapsedEventHandler(GetCarSpeed);
        _timer.Interval = 500;
        _timer.Start();
        _creationDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _carModel = client.EntryCar.Model;
        _carSkin = client.EntryCar.Skin;
    }
    public void GetCarSpeed(object? sender, EventArgs e)
    {
        int speedKmh = ((int) (_client.EntryCar.Status.Velocity.Length() * 3.6f));
        _speedSum += speedKmh;
        if(speedKmh > _topSpeed)
        {
            _topSpeed = speedKmh;
        }
        _speedCounter += 1;
    }
    private void OnCollision(ACTcpClient client, CollisionEventArgs args)
    {
        if (args.TargetCar is null){
            _evnCollisions += 1;
            if (args.Speed >= 150){
                _majorAccidents += 1;
            }
        }
        else if (args.TargetCar.AiControlled && args.TargetCar.AiName is not null){
            _trafficCollisions += 1;
            if (args.Speed >= 150){
                _majorAccidents += 1;
            }
        }
        else if (!args.TargetCar.AiControlled && args.TargetCar.Client is not null){
            _playerCollisions += 1;
            if (args.Speed >= 150){
                _majorAccidents += 1;
            }
        }
    }
    public void LogData(StatLoggingSession _statsSession, String dbFilePath, String serverName, String trackName)
    {
        _sessionLength = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _creationDate;
        double hours = TimeSpan.FromSeconds(_sessionLength).TotalHours;
        long avgSpeed = ((_speedSum) / ((long)_speedCounter));
        double kmDriven = avgSpeed * hours;
        Log.Information(string.Format("Session closed: {0}", _statsSession._username ?? "Unknown"));
        _timer.Stop();
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
                        connectedServer,
                        track
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
                        @connectedServer,
                        @track
                    )
                ";
                command.Parameters.Add(new SqliteParameter("@sessionDate", _statsSession._creationDate));
                command.Parameters.Add(new SqliteParameter("@steamID", _statsSession._guid));
                command.Parameters.Add(new SqliteParameter("@userName", _statsSession._username));
                command.Parameters.Add(new SqliteParameter("@carModel", _carModel));
                command.Parameters.Add(new SqliteParameter("@carSkin", _carSkin));
                command.Parameters.Add(new SqliteParameter("@kmDriven", kmDriven));
                command.Parameters.Add(new SqliteParameter("@timeSpent", _sessionLength));
                command.Parameters.Add(new SqliteParameter("@topSpeed", _topSpeed));
                command.Parameters.Add(new SqliteParameter("@evnCollisions", _statsSession._evnCollisions));
                command.Parameters.Add(new SqliteParameter("@trafficCollisions", _statsSession._trafficCollisions));
                command.Parameters.Add(new SqliteParameter("@playerCollisions", _statsSession._playerCollisions));
                command.Parameters.Add(new SqliteParameter("@majorAccidents", _statsSession._majorAccidents));
                command.Parameters.Add(new SqliteParameter("@connectedServer", serverName));
                command.Parameters.Add(new SqliteParameter("@track", trackName));
                command.ExecuteNonQuery();
        }

    }
}
