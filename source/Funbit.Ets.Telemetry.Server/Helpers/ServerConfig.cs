using System;

namespace Funbit.Ets.Telemetry.Server.Helpers
{
    public class ServerConfig
    {
        const int DefaultPort = 31377;
        const string DefaultSharedMemoryPath = "/dev/shm/TSGPSTelemetry";
        const int DefaultBroadcastRateSeconds = 10;
        const int MinBroadcastRateSeconds = 1;
        const int MaxBroadcastRateSeconds = 86400;

        public int Port { get; }
        public string SharedMemoryPath { get; }
        public bool UseTestTelemetry { get; }
        public string BroadcastUrl { get; }
        public int BroadcastRateSeconds { get; }
        public string BroadcastUserId { get; }
        public string BroadcastUserPassword { get; }

        ServerConfig()
        {
            Port = ReadInt("TSGPS_PORT", DefaultPort);
            SharedMemoryPath = ReadString("TSGPS_SHM_PATH", DefaultSharedMemoryPath);
            UseTestTelemetry = ReadBool("TSGPS_USE_TEST_TELEMETRY", false);
            BroadcastUrl = ReadString("TSGPS_BROADCAST_URL", "");
            BroadcastRateSeconds = Clamp(
                ReadInt("TSGPS_BROADCAST_RATE", DefaultBroadcastRateSeconds),
                MinBroadcastRateSeconds, MaxBroadcastRateSeconds);
            BroadcastUserId = ReadString("TSGPS_BROADCAST_USER", "");
            BroadcastUserPassword = ReadString("TSGPS_BROADCAST_PASSWORD", "");
        }

        public static ServerConfig Current { get; } = new ServerConfig();

        static string ReadString(string name, string fallback)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        static int ReadInt(string name, int fallback)
        {
            return int.TryParse(Environment.GetEnvironmentVariable(name), out var value) ? value : fallback;
        }

        static bool ReadBool(string name, bool fallback)
        {
            return bool.TryParse(Environment.GetEnvironmentVariable(name), out var value) ? value : fallback;
        }

        static int Clamp(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }
    }
}
