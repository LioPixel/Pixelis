# Pixelis Relay Multiplayer

Pixelis online hosting uses your Ubuntu server as a relay and room server.

Flow:

1. Host clicks `Host -> Online`.
2. The game connects to the Ubuntu relay and creates a room.
3. The host gets a room code in chat.
4. Other players enter that room code in `Join`.
5. Nobody needs port forwarding, Tailscale, or router changes.

## Build/Publish

Publish the server project for Linux:

```powershell
dotnet publish Pixelis.Server/Pixelis.Server.csproj -c Release -r linux-x64 --self-contained false
```

Copy the publish folder to your Ubuntu server, for example to:

```text
/opt/pixelis-server
```

## Ubuntu Setup

Install the .NET runtime matching the project target, then open UDP port `7777`.

With UFW:

```bash
sudo ufw allow 7777/udp
sudo ufw reload
```

Also open UDP `7777` in your hosting provider firewall/security group if it has one.

## Run Relay

```bash
cd /opt/pixelis-server
dotnet Pixelis.Server.dll --relay 7777 256
```

Arguments:

```text
--relay <udp-port> <max-clients>
```

Example:

```bash
dotnet Pixelis.Server.dll --relay 7777 256
```

## systemd Service

Create `/etc/systemd/system/pixelis-relay.service`:

```ini
[Unit]
Description=Pixelis Relay Server
After=network-online.target
Wants=network-online.target

[Service]
WorkingDirectory=/opt/pixelis-server
ExecStart=/usr/bin/dotnet /opt/pixelis-server/Pixelis.Server.dll --relay 7777 256
Restart=always
RestartSec=5
User=pixelis

[Install]
WantedBy=multi-user.target
```

Create the user and start it:

```bash
sudo useradd --system --no-create-home --shell /usr/sbin/nologin pixelis
sudo chown -R pixelis:pixelis /opt/pixelis-server
sudo systemctl daemon-reload
sudo systemctl enable --now pixelis-relay
sudo systemctl status pixelis-relay
```

## Client Endpoint

By default the client uses:

```text
5.231.148.1:7777
```

For another server, set this before starting the game:

```powershell
$env:PIXELIS_ONLINE_SERVER="YOUR_SERVER_IP:7777"
```

On Linux:

```bash
export PIXELIS_ONLINE_SERVER="YOUR_SERVER_IP:7777"
```
