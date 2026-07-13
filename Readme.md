# TruckSim GPS Telemetry Server

The open-source companion telemetry server for the [TruckSim GPS](https://trucksimgps.com/) mobile app. It runs in the background while you play **Euro Truck Simulator 2** or **American Truck Simulator**, reading live game data and making it available to the mobile app on your local network.

The telemetry plugin that reads game data is also open source under the MIT license: [trucksim-gps-plugin](https://github.com/TruckSim-GPS/trucksim-gps-plugin).

## System Requirements

- **Linux** (64-bit)
- **.NET 8 runtime** (`dotnet-runtime-8.0`)
- The native Linux telemetry plugin installed into your game — see [trucksim-gps-plugin](https://github.com/TruckSim-GPS/trucksim-gps-plugin)

## How to Use

1. **Install the plugin** into your ETS2/ATS installation as described in the plugin repository. The plugin publishes live game data to a shared-memory segment at `/dev/shm/TSGPSTelemetry`.
2. **Build and run the server** (see *Building from Source* below), or install it as a service (see *Running as a service*).
3. **Keep the server running** while playing ETS2/ATS.
4. **Connect the mobile app** using the server's IP address on your local network. The server logs each reachable `http://<ip>:31377` URL on startup.

### Configuration

The server is configured entirely through environment variables (all optional):

| Variable | Default | Description |
|----------|---------|-------------|
| `TSGPS_PORT` | `31377` | HTTP listening port |
| `TSGPS_SHM_PATH` | `/dev/shm/TSGPSTelemetry` | Shared-memory segment written by the plugin |
| `TSGPS_USE_TEST_TELEMETRY` | `false` | Serve `Ets2TestTelemetry.json` instead of live data |
| `TSGPS_BROADCAST_URL` | (unset) | If set, telemetry is POSTed here every `TSGPS_BROADCAST_RATE` seconds |
| `TSGPS_BROADCAST_RATE` | `10` | Broadcast interval in seconds (1–86400) |
| `TSGPS_BROADCAST_USER` / `TSGPS_BROADCAST_PASSWORD` | (empty) | Sent as base64 `X-UserId` / `X-UserPassword` headers |

### Running as a service

A systemd **user** unit is provided in [`packaging/trucksim-gps-server.service`](packaging/trucksim-gps-server.service). It runs under your user account so it can read your game's shared-memory segment.

```bash
dotnet publish source/Funbit.Ets.Telemetry.Server -c Release -o ~/.local/share/trucksim-gps-server
mkdir -p ~/.config/systemd/user
cp packaging/trucksim-gps-server.service ~/.config/systemd/user/
systemctl --user daemon-reload
systemctl --user enable --now trucksim-gps-server
```

## Troubleshooting

Test connectivity by opening `http://<server-ip>:31377/` in a browser on any device on your network. If it doesn't load, check that your firewall allows inbound TCP on the port and that the device is on the same network.

## Privacy

This application does not collect, store, or transmit any personal user data. The server communicates exclusively over the local network and makes no external network requests. The only outbound request is the optional telemetry broadcast, which is disabled unless you configure `TSGPS_BROADCAST_URL` yourself.

## Building from Source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet build source/Funbit.Ets.Telemetry.Server -c Release
```

Run it directly with:

```bash
dotnet run --project source/Funbit.Ets.Telemetry.Server
```

## Project Links

- **Official Website:** https://trucksimgps.com/
- **Support us on Patreon:** https://www.patreon.com/TruckSimGPS
- **Join our Discord:** https://discord.gg/RdC99Er37U (discussion, support, feature requests)

---

This project is built upon the foundational work of the [Funbit ETS2 Telemetry Server](https://github.com/Funbit/ets2-telemetry-server).
Open source software licensed under **GPL-3.0**. Anyone can read the source code and verify for themselves that it's safe.
