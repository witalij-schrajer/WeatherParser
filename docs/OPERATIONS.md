# WeatherParser Operations

**Status: draft of P9.07 (2026-10-31): the deployment section only; P9.08 completes the guide (configuration and secrets, scheduling, exit codes, logs, request volume, broker set-up, freshness, limitations).** This guide covers the example CLI `WeatherParser` on Debian 13 ARM64 (`linux-arm64`, for example the NanoPi R76S). The library itself is consumed as a NuGet package and needs no deployment of its own.

## 1. Deployment (RL-07 A)

All paths, the user and the group below are documented examples, not defaults: the CLI reads no configuration on its own and needs `--Config <path>` or `WEATHERPARSER_CONFIG` for every command (CL-04, REQ-064).

### Runtime

The CLI is a framework-dependent `linux-arm64` publish and needs the .NET runtime 10.0.14 or newer (`Microsoft.NETCore.App`; the ASP.NET Core runtime is not needed). Install it from the Microsoft package feed for Debian 13 and keep it current with the system updates:

```bash
sudo apt-get install dotnet-runtime-10.0
dotnet --list-runtimes
```

Follow Microsoft's instructions for adding the package feed on Debian 13 first. A runtime installed by other means (for example the `dotnet-install` script into a user folder) works as well when `DOTNET_ROOT` points to it; the installer and the update path are then the operator's responsibility.

### Layout, user and permissions

The CLI acceptance artifact `WeatherParser.Cli.<version>.linux-arm64.zip` of the release build is verified against its `SHA256SUMS` line and unpacked into a versioned folder; a stable symlink names the current version, so an update is an unpack plus a symlink change and a rollback is the reverse:

```bash
sha256sum -c --ignore-missing SHA256SUMS
sudo useradd --system --no-create-home --shell /usr/sbin/nologin weatherparser
sudo install -d -m 0755 /opt/weatherparser/<version>
sudo python3 -c "import zipfile,sys; zipfile.ZipFile(sys.argv[1]).extractall(sys.argv[2])" WeatherParser.Cli.<version>.linux-arm64.zip /opt/weatherparser/<version>
sudo chmod 0755 /opt/weatherparser/<version>/WeatherParser
sudo ln -sfn /opt/weatherparser/<version> /opt/weatherparser/current
sudo install -d -m 0750 -o root -g weatherparser /etc/weatherparser
```

(`unzip -d` works the same way where it is installed; `zipfile` keeps no permission bits, hence the `chmod` of the app host.)

- Configuration files under `/etc/weatherparser/`: owner `root`, group `weatherparser`, mode `0640`. Copy `examples/weatherparser.example.json` and replace the placeholder endpoints; never overwrite an existing configuration.
- Broker credential file (MQTT mode only): owned by `weatherparser`, mode `0600` (CL-06); the CLI rejects a file that another user owns or that the group or others can access, with `CLI-CREDENTIALS-INVALID` and exit code 2, before any request.

  First installation only; the command refuses to replace an existing file, so repeating it during an update never truncates working credentials:

```bash
sudo test ! -e /etc/weatherparser/broker.credentials.json && sudo install -m 0600 -o weatherparser -g weatherparser /dev/null /etc/weatherparser/broker.credentials.json
```

  Then write the members `Username` and `Password` into it with an editor run as root; never put credentials on a command line or into the configuration file.

### Updates

An update unpacks the new version into its own folder and moves the symlink (`sudo ln -sfn /opt/weatherparser/<new version> /opt/weatherparser/current`); it does not touch `/etc/weatherparser/`. Configuration files and the credential file are created only at the first installation and are never copied over an existing file; compare a new example configuration with the existing one by hand.

### Invocation

Every invocation runs exactly one command and exits; there is no service, timer or daemon to install. A scheduler (cron, n8n) starts it as the service user without a terminal, for example:

```bash
sudo -u weatherparser /opt/weatherparser/current/WeatherParser --ParseDWD --Output json --Config /etc/weatherparser/weatherparser.json < /dev/null
```

The document goes to standard output (or `--OutputFile`), logs go to standard error, and the exit code reports the result (README section "Example CLI"). Scheduling examples follow in P9.08.

### Verification on the target (P9.07)

The deployment was checked on the NanoPi R76S on 2026-10-31 without root, in a disposable folder that mirrored this layout and as the login user instead of `weatherparser` ([P9 verification record](p9/P9_VERIFICATION.md) section 8): checksum, `--Help`, a usage error, the CL-06 mode check, `--SelectDWD` and `--ParseOstHessen` against the offline fixture host, all without a terminal. The service user, the system paths, the CL-06 owner check and the runtime 10.0.14 were not verified on the device.
