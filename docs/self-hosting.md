# Self-hosting the FoodUs relay

This is the complete setup path for standing up your own relay: provision a host, harden it,
put Caddy in front of it, point a name at it, install the relay as a systemd service, deploy,
and check it works.

Everything here uses placeholder values. `relay.example.com` is not a real host — substitute
your own name everywhere it appears. Your host name, IP address, SSH key, and runtime
configuration are yours; they belong on your machine and your server, never in a repository.
That rule is the project's [secrets policy](../context/wiki/secrets.md), and it is why this
document reads like it was written for a stranger: it was.

## What you end up with

```text
phone  ──HTTPS──>  Caddy (TLS, Let's Encrypt)  ──127.0.0.1:5000──>  relay  ──>  SQLite
```

The relay speaks plain HTTP and binds to loopback only. It is never reachable from the
internet directly; Caddy is the only thing listening on a public port.

## Before you start

You need:

- A server you can SSH into. These instructions assume Ubuntu 24.04 LTS.
- A domain (or subdomain) you control, so you can create a DNS record for it. The phones are
  configured with the name, never a raw IP, so changing servers later never touches a phone.
- A Windows machine with PowerShell 7+, the [.NET SDK](https://dotnet.microsoft.com/download),
  and OpenSSH (`ssh` and `scp` on `PATH`) to run the publish script from. On Linux or macOS,
  run the equivalent commands by hand — see [Deploying from a non-Windows machine](#deploying-from-a-non-windows-machine).

You do **not** need .NET on the server. The publish step produces a self-contained bundle that
carries its own runtime — publish, don't build on the server.

---

## 1. Provision the server

The reference host is a DigitalOcean droplet, but any Ubuntu 24.04 LTS server works.

| Setting | Value | Why |
| --- | --- | --- |
| Region | Nearest to your household (the reference instance uses SYD1) | Latency; nothing else depends on it |
| Image | Ubuntu 24.04 LTS | What these instructions assume |
| Plan | 1 GB RAM (about $6 USD/month) advised | Headroom for Ubuntu + the relay + Caddy |
| Authentication | SSH key, added at creation | Key-only from first boot; never enable password auth |
| Monitoring | On | Free memory and CPU graphs |
| Backups | Off | Relay data is transient ciphertext, swept after 30 days |
| Managed database | Off | The relay uses a single local SQLite file |
| Networking | IPv4 and IPv6 | Both, so either address family reaches you |

**The 512 MB budget alternative.** The smaller plan (about $4 USD/month) also runs this
workload, but only with swap. If you choose it, add a swap file during hardening:

```bash
sudo fallocate -l 1G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

Watch the memory graph after your first deploy either way.

## 2. Harden the server

A box with a public IP is being probed within minutes of first boot. Do this before anything
else is installed.

**Confirm password authentication is off.** It should already be, if you added an SSH key at
creation. Check `/etc/ssh/sshd_config` and any file in `/etc/ssh/sshd_config.d/`:

```bash
sudo sshd -T | grep -E 'passwordauthentication|permitrootlogin'
```

Both should read `no`. If not, set `PasswordAuthentication no` and `PermitRootLogin no`, then
`sudo systemctl restart ssh`.

**Allow only SSH, HTTP, and HTTPS.**

```bash
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp     # required: Let's Encrypt validates over HTTP
sudo ufw allow 443/tcp
sudo ufw enable
sudo ufw status verbose
```

Port 80 must stay open even though the relay is HTTPS-only — Caddy needs it for certificate
issuance and renewal, and it serves nothing but a redirect to HTTPS.

Nothing opens the relay's own port. It listens on `127.0.0.1` and must never be reachable from
outside.

**Create the service account.** A dedicated, non-root, non-login system user owns the install
and runs the service:

```bash
sudo adduser --system --group --no-create-home --shell /usr/sbin/nologin foodus-relay
```

**Enable unattended security updates.**

```bash
sudo apt update && sudo apt install -y unattended-upgrades
sudo dpkg-reconfigure --priority=low unattended-upgrades
```

**Create the deploy account (optional but recommended).** The publish script connects as a
normal user and uses `sudo` for the service operations. You can use your existing admin
account, or make one dedicated to deploys with narrow sudo rights:

```bash
sudo adduser --disabled-password --gecos "" deploy
sudo mkdir -p /home/deploy/.ssh
sudo cp ~/.ssh/authorized_keys /home/deploy/.ssh/authorized_keys
sudo chown -R deploy:deploy /home/deploy/.ssh
sudo chmod 700 /home/deploy/.ssh && sudo chmod 600 /home/deploy/.ssh/authorized_keys
```

Give it passwordless sudo for exactly what the deploy needs, via
`sudo visudo -f /etc/sudoers.d/foodus-relay-deploy`:

```text
deploy ALL=(root) NOPASSWD: /usr/bin/systemctl start foodus-relay, \
                            /usr/bin/systemctl stop foodus-relay, \
                            /usr/bin/systemctl is-active foodus-relay, \
                            /usr/bin/mkdir -p /opt/foodus-relay, \
                            /usr/bin/tar -xzf /tmp/foodus-relay-deploy/foodus-relay.tar.gz -C /opt/foodus-relay, \
                            /usr/bin/chown -R foodus-relay\:foodus-relay /opt/foodus-relay, \
                            /usr/bin/chmod +x /opt/foodus-relay/FoodUsRelay
```

## 3. Install Caddy

From Caddy's official apt repository:

```bash
sudo apt install -y debian-keyring debian-archive-keyring apt-transport-https curl
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' \
  | sudo gpg --dearmor -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' \
  | sudo tee /etc/apt/sources.list.d/caddy-stable.list
sudo apt update && sudo apt install -y caddy
```

Copy [`deploy/Caddyfile.template`](../deploy/Caddyfile.template) to `/etc/caddy/Caddyfile` and
replace `relay.example.com` with your own host name. The `reverse_proxy` target must match the
relay's configured port (`127.0.0.1:5000` by default).

Do not reload Caddy yet. Certificate issuance fails until DNS points at this server.

## 4. Point your domain at the server

At your DNS provider, create records for the name you chose:

- an `A` record to the server's IPv4 address;
- an `AAAA` record to its IPv6 address, if you enabled IPv6.

Wait for it to resolve (`dig +short relay.example.com`, using your own name), then let Caddy
issue the certificate:

```bash
sudo systemctl reload caddy
sudo journalctl -u caddy -n 50 --no-pager
```

The log should show a certificate obtained for your host. If it does not, the usual causes are
DNS not yet propagated or port 80 blocked.

## 5. Install the relay service

**Runtime configuration.** The relay reads, in increasing order of precedence: the committed
`appsettings.json` defaults, then `appsettings.Production.json` beside the executable, then
environment variables. There is no custom loader — this is ASP.NET's standard override chain.
Two keys matter:

| Key | Environment variable | Example |
| --- | --- | --- |
| `Kestrel:Endpoints:Http:Url` | `Kestrel__Endpoints__Http__Url` | `http://127.0.0.1:5000` |
| `Relay:DatabasePath` | `Relay__DatabasePath` | `/var/lib/foodus-relay/relay.db` |

The URL must stay a loopback address in every environment. That is a constitutional rule, not
a preference: the relay is never internet-facing.

Pick one of the two placements:

*Either* copy `source/FoodUsRelay/appsettings.Production.template.json` from this repository to
`/opt/foodus-relay/appsettings.Production.json` on the server and fill in the two blank values
— deploys unpack over the install directory and never ship a file of that name, so your copy
survives, but it does live among replaceable build output;

*or* put the same keys in `/etc/foodus-relay/foodus-relay.env`, which survives deploys:

```bash
sudo mkdir -p /etc/foodus-relay /var/lib/foodus-relay
sudo chown foodus-relay:foodus-relay /var/lib/foodus-relay
sudo tee /etc/foodus-relay/foodus-relay.env >/dev/null <<'EOF'
Kestrel__Endpoints__Http__Url=http://127.0.0.1:5000
Relay__DatabasePath=/var/lib/foodus-relay/relay.db
EOF
sudo chown root:foodus-relay /etc/foodus-relay/foodus-relay.env
sudo chmod 640 /etc/foodus-relay/foodus-relay.env
```

Neither file is ever committed. `appsettings.Production.json` is git-ignored precisely so an
accident cannot become a commit.

**The unit.** Copy [`deploy/foodus-relay.service.template`](../deploy/foodus-relay.service.template)
to `/etc/systemd/system/foodus-relay.service`, adjust the paths if your layout differs, then:

```bash
sudo systemctl daemon-reload
sudo systemctl enable foodus-relay
```

Leave it stopped — the first deploy starts it.

## 6. Deploy

From your Windows machine, in a clone of this repository, create the git-ignored settings file
`scripts/publish.local.psd1` with your own values:

```powershell
@{
    RelayHost    = 'relay.example.com'
    SshUser      = 'deploy'
    IdentityFile = 'C:\Users\you\.ssh\id_ed25519'
}
```

Then run:

```powershell
./scripts/publish.ps1
```

The script publishes a self-contained Release bundle, packs it, copies it over SSH, unpacks it
into `/opt/foodus-relay`, and restarts the service. Every value it needs can also be passed as
a parameter instead (`-RelayHost`, `-SshUser`, `-IdentityFile`, `-RemoteInstallPath`, …); run
`Get-Help ./scripts/publish.ps1 -Detailed` for the full list.

This is the only deployment path. Do not hand-copy a build "just this once" — an unproven
deploy script is a deploy script that fails when you need it.

**A note on the .NET SDK.** The project targets `net10.0`. Publishing with a preview 10.x SDK
emits the informational `NETSDK1057` notice; it is harmless, and installing a GA 10.x SDK
clears it. Because the publish is self-contained, the runtime travels inside the bundle and
the server needs no .NET installation at all — but do confirm your SDK bundles a GA 10.x
runtime rather than a preview one before shipping to a server you care about.

### Deploying from a non-Windows machine

The script is PowerShell only because that is what the reference setup runs. The steps are
four commands; run them by hand on Linux or macOS:

```bash
dotnet publish source/FoodUsRelay/FoodUsRelay.csproj -c Release -r linux-x64 --self-contained true -o ./out
tar -czf foodus-relay.tar.gz -C ./out .
scp -i ~/.ssh/id_ed25519 foodus-relay.tar.gz deploy@relay.example.com:/tmp/
ssh -i ~/.ssh/id_ed25519 deploy@relay.example.com \
  'sudo systemctl stop foodus-relay && sudo tar -xzf /tmp/foodus-relay.tar.gz -C /opt/foodus-relay && sudo chown -R foodus-relay:foodus-relay /opt/foodus-relay && sudo chmod +x /opt/foodus-relay/FoodUsRelay && sudo systemctl start foodus-relay'
```

## 7. Check it works

Run all six. The first is the liveness proof; the rest are the ones people skip and regret.

1. **Capability endpoint over HTTPS, from outside your network.** A phone on mobile data is the
   honest check — a request from your own LAN can pass for reasons that have nothing to do with
   your server. `https://relay.example.com/v1/capabilities` returns a JSON body describing the
   contract version and supported capabilities, with a valid certificate.
2. **Plain HTTP is never served.** `curl -sI http://relay.example.com/v1/capabilities` returns a
   redirect to HTTPS, not content.
3. **The relay's own port is not exposed.** From another machine,
   `curl --max-time 5 http://<your-server-ip>:5000/v1/capabilities` must fail to connect. If it
   succeeds, the relay is bound to a public interface or the firewall is open — stop and fix it
   before going further.
4. **Reboot survival.** `sudo reboot`, wait, then re-run check 1. Both Caddy and the relay must
   come back with no manual intervention (`systemctl is-enabled foodus-relay caddy` should read
   `enabled` for both).
5. **The deploy is repeatable.** Run the publish script a second time and confirm the service is
   healthy afterwards. A deploy that only works once is not a deploy.
6. **Monitoring is reporting.** Your provider's memory and CPU graphs should be populating.

If something is wrong, the logs are `sudo journalctl -u foodus-relay -n 100 --no-pager` for the
relay and the same with `-u caddy` for the front door.

## Keeping your instance private

The relay's endpoint address is itself part of its security. Authentication is the real
protection, but obscurity is a free extra layer, so:

- Never publish your host name or server IP — not in a repository, an issue, a commit message,
  or a screenshot.
- Type the host name into each phone's relay-URL setting by hand.
- Keep the SSH private key on your machine and out of every working tree.

## Related documents

- [Wire contract v1](wire-contract-v1.md) — the API the relay implements.
- [Secrets policy](../context/wiki/secrets.md) — what lives where, and what must never be
  committed.
