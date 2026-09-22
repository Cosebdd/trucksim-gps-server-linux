using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Funbit.Ets.Telemetry.Server.Helpers;
using log4net;
using log4net.Config;

namespace Funbit.Ets.Telemetry.Server
{
    static class Program
    {
        static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static void Main()
        {
            XmlConfigurator.Configure(new FileInfo(Path.Combine(AppContext.BaseDirectory, "log4net.config")));

            var config = ServerConfig.Current;
            using (var host = new TelemetryServerHost(config))
            using (var shutdown = new ManualResetEventSlim(false))
            {
                host.Start();
                LogReachableEndpoints(config.Port);

                using (PosixSignalRegistration.Create(PosixSignal.SIGINT, ctx => RequestShutdown(ctx, shutdown)))
                using (PosixSignalRegistration.Create(PosixSignal.SIGTERM, ctx => RequestShutdown(ctx, shutdown)))
                {
                    shutdown.Wait();
                }

                Log.Info("Shutdown requested, stopping server...");
            }
        }

        static void RequestShutdown(PosixSignalContext context, ManualResetEventSlim shutdown)
        {
            context.Cancel = true;
            shutdown.Set();
        }

        static void LogReachableEndpoints(int port)
        {
            try
            {
                foreach (var networkInterface in NetworkHelper.GetAllActiveNetworkInterfaces())
                    Log.InfoFormat("Reachable at http://{0}:{1}", networkInterface.Ip, port);
            }
            catch (Exception ex)
            {
                Log.Warn(ex.Message);
            }
        }
    }
}
