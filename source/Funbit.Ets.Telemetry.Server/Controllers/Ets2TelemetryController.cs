using System;
using System.IO;
using System.Text;
using Funbit.Ets.Telemetry.Server.Data;
using Funbit.Ets.Telemetry.Server.Helpers;
using Newtonsoft.Json;

namespace Funbit.Ets.Telemetry.Server.Controllers
{
    public static class Ets2TelemetryController
    {
        public const string TelemetryApiUriPath = "/api/ets2/telemetry";
        const string TestTelemetryJsonFileName = "Ets2TestTelemetry.json";

        static readonly bool UseTestTelemetryData = ServerConfig.Current.UseTestTelemetry;

        public static string GetEts2TelemetryJson()
        {
            // if we have test data defined in the app.config then use it
            if (UseTestTelemetryData)
            {
                using (var file = File.Open(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TestTelemetryJsonFileName), 
                        FileMode.Open,
                        FileAccess.Read, 
                        FileShare.ReadWrite))
                using (var reader = new StreamReader(file, Encoding.UTF8))
                    return reader.ReadToEnd();
            }

            // otherwise return real data from the simulator using SCS SDK reader and REST v1 schema
            var v1 = ScsTelemetryDataReader.Instance.Read();
            return JsonConvert.SerializeObject(v1, JsonHelper.RestSettings);
        }
    }
}
