using System;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Funbit.Ets.Telemetry.Server.Data;
using Funbit.Ets.Telemetry.Server.Helpers;
using Newtonsoft.Json;

namespace Funbit.Ets.Telemetry.Server
{
    public class TelemetryServerHost : IDisposable
    {
        static readonly log4net.ILog Log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        static readonly Encoding Utf8 = new UTF8Encoding(false);

        readonly ServerConfig _config;
        readonly HttpClient _broadcastHttpClient = new HttpClient();

        MinimalHttpServer _server;
        Timer _broadcastTimer;
        TimeSpan _broadcastPeriod;

        public TelemetryServerHost(ServerConfig config)
        {
            _config = config;
        }

        public void Start()
        {
            Log.Info("=== Server Start Begin ===");
            Log.InfoFormat("Current Date/Time: {0}", DateTime.Now);
            Log.InfoFormat("Runtime Version: {0}", Environment.Version);
            Log.InfoFormat("OS Version: {0}", Environment.OSVersion);
            Log.InfoFormat("Port: {0}", _config.Port);

            _server = new MinimalHttpServer(_config.Port);
            _server.Start();

            if (!string.IsNullOrEmpty(_config.BroadcastUrl))
                StartBroadcasting();
        }

        public void Stop()
        {
            _broadcastTimer?.Dispose();
            _broadcastTimer = null;
            _server?.Stop();
            _server?.Dispose();
            _server = null;
        }

        void StartBroadcasting()
        {
            _broadcastHttpClient.DefaultRequestHeaders.Add("X-UserId", ToBase64(_config.BroadcastUserId));
            _broadcastHttpClient.DefaultRequestHeaders.Add("X-UserPassword", ToBase64(_config.BroadcastUserPassword));

            _broadcastPeriod = TimeSpan.FromSeconds(_config.BroadcastRateSeconds);
            _broadcastTimer = new Timer(OnBroadcastTimer, null, _broadcastPeriod, Timeout.InfiniteTimeSpan);
        }

        void OnBroadcastTimer(object state)
        {
            BroadcastAsync().GetAwaiter().GetResult();
            try
            {
                _broadcastTimer?.Change(_broadcastPeriod, Timeout.InfiniteTimeSpan);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        async Task BroadcastAsync()
        {
            try
            {
                var json = JsonConvert.SerializeObject(ScsTelemetryDataReader.Instance.Read(), JsonHelper.RestSettings);
                using (var content = new StringContent(json, Utf8, "application/json"))
                    await _broadcastHttpClient.PostAsync(_config.BroadcastUrl, content);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        static string ToBase64(string value)
        {
            return Convert.ToBase64String(Utf8.GetBytes(value ?? ""));
        }

        public void Dispose()
        {
            Stop();
            _broadcastHttpClient.Dispose();
        }
    }
}
