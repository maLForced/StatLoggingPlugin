using AssettoServer.Network.Tcp;
using Serilog;

namespace StatLoggingPlugin;

public sealed class StatLoggingSession
{
    public ACTcpClient _client;
    private StatLoggingPlugin _plugin;
    public string _username;
    public string _guid;

    private Queue<int> _speedList = new Queue<int>();
    private long _creationTime;

    private int _topSpeed = 0;
    private int _speedCounter = 0;
    private long _speedSum = 0;
    public int _playerCollisions { get; set; } = 0;
    public int _trafficCollisions { get; set; } = 0;
    public int _majorAccidents { get; set; } = 0;
    public int _evnCollisions { get; set; } = 0;

    public StatLoggingSession(ACTcpClient client, StatLoggingPlugin plugin)
    {
        Log.Information(string.Format("Session created: {0}", client.Name ?? "Unknown"));

        _client = client;
        _plugin = plugin;
        _username = client.Name ?? "Unknown";
        _guid = client.Guid.ToString();
    }

    public void OnCreation()
    {
        _creationTime = _plugin._sessionManager.ServerTimeMilliseconds;
    }

    public void OnRemove()
    {
        if(!_client.HasSentFirstUpdate || !_client.HasPassedChecksum)
        {
            return;
        }

        _plugin.CreateClientForEntryCarData(_client.EntryCar, _client, this);
        Log.Information(string.Format("Session closed: {0}", _client.Name ?? "Unknown"));
    }

    public string getUsername()
    {
        return _username;
    }

        public string getGuid()
    {
        return _guid;
    }

    public int GetAverageSpeed()
    {
        if(_speedCounter < 1)
        {
            return 0;
        }
        return ((int)_speedSum) / _speedCounter;
    }
    public void AddToAverageSpeed(int speed)
    {
        _speedCounter += 1;
        _speedSum += speed;
    }

    public double CalculateDistanceDriven()
    {
        double hours = TimeSpan.FromMilliseconds(CalculateTimeSpent()).TotalHours;
        long avgSpeed = GetAverageSpeed();
        return avgSpeed * hours;
    }

    public int GetTopSpeed()
    {
        return _topSpeed;
    }

    public void SetTopSpeed(int speed)
    {
        _topSpeed = speed;
    }

    public long CalculateTimeSpent()
    {
        return _plugin._sessionManager.ServerTimeMilliseconds - _creationTime;
    }
}
