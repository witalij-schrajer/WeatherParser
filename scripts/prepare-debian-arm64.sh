#!/usr/bin/env bash
# Purpose: Prepare Debian 13 ARM64 target for P1.10 (WeatherParser repository)
#         - User-directory installs only, never run without recorded authorization
# Source URLs:
#   - .NET SDK install: https://learn.microsoft.com/dotnet/core/install/linux-scripted-manual
#   - PowerShell install: https://learn.microsoft.com/powershell/scripting/install/alternate-install-methods

set -euo pipefail

DOTNET_SDK_VERSION="10.0.402"
PWSH_VERSION="7.6.7"
DOTNET_DIR="$HOME/.dotnet"
PWSH_DIR="$HOME/.local/share/powershell/$PWSH_VERSION"

# Check prerequisites
if [[ "$(uname -m)" != "aarch64" ]]; then
  echo "prepare-debian-arm64: Error: This script must run on an ARM64 machine." >&2
  exit 1
fi

if ! grep -q '^ID=debian$' /etc/os-release; then
  echo "prepare-debian-arm64: Error: This script must run on Debian." >&2
  exit 1
fi

VERSION_ID=$(grep "^VERSION_ID=" /etc/os-release | cut -d= -f2- | tr -d '"')
if [[ "${VERSION_ID%%.*}" -lt 13 ]]; then
  echo "prepare-debian-arm64: Error: This script must run on Debian 13 or higher." >&2
  exit 1
fi

for cmd in git curl tar sha256sum iconv; do
  if ! command -v "$cmd" &> /dev/null; then
    echo "prepare-debian-arm64: Error: Required command '$cmd' is not installed." >&2
    exit 1
  fi
done

if ! ls /usr/lib/aarch64-linux-gnu/libicuuc.so.* >/dev/null 2>&1; then
  echo "prepare-debian-arm64: Error: No libicu shared library found in /usr/lib/aarch64-linux-gnu/" >&2
  exit 1
fi

# Helper function to check if SDK is installed
sdk_installed() {
  [ -x "$DOTNET_DIR/dotnet" ] && "$DOTNET_DIR/dotnet" --list-sdks | grep -q "^$DOTNET_SDK_VERSION "
}

# Create temp directory for both installations
TEMP_DIR=$(mktemp -d)
trap 'rm -rf "$TEMP_DIR"' EXIT

# .NET SDK installation
if sdk_installed; then
  echo "prepare-debian-arm64: .NET SDK $DOTNET_SDK_VERSION already installed." >&2
else
  echo "prepare-debian-arm64: Installing .NET SDK $DOTNET_SDK_VERSION..." >&2
  curl -fsSL --retry 3 -o "$TEMP_DIR/dotnet-install.sh" https://dot.net/v1/dotnet-install.sh
  chmod +x "$TEMP_DIR/dotnet-install.sh"
  "$TEMP_DIR/dotnet-install.sh" --version "$DOTNET_SDK_VERSION" --install-dir "$DOTNET_DIR" --no-path

  if ! sdk_installed; then
    echo "prepare-debian-arm64: Error: Failed to install .NET SDK $DOTNET_SDK_VERSION." >&2
    exit 1
  fi
fi

# PowerShell installation
if [ -x "$PWSH_DIR/pwsh" ] && "$PWSH_DIR/pwsh" --version | grep -q "PowerShell $PWSH_VERSION"; then
  echo "prepare-debian-arm64: PowerShell $PWSH_VERSION already installed." >&2
else
  echo "prepare-debian-arm64: Installing PowerShell $PWSH_VERSION..." >&2
  curl -fsSL --retry 3 -o "$TEMP_DIR/powershell-$PWSH_VERSION-linux-arm64.tar.gz" \
    "https://github.com/PowerShell/PowerShell/releases/download/v$PWSH_VERSION/powershell-$PWSH_VERSION-linux-arm64.tar.gz"
  curl -fsSL --retry 3 -o "$TEMP_DIR/hashes.sha256" \
    "https://github.com/PowerShell/PowerShell/releases/download/v$PWSH_VERSION/hashes.sha256"

  # Convert hash file from UTF-16LE with BOM and CRLF to UTF-8 with LF
  iconv -f UTF-16 -t UTF-8 "$TEMP_DIR/hashes.sha256" | tr -d '\r' > "$TEMP_DIR/hashes.txt"
  
  # Extract the hash for the specific file
  EXPECTED_HASH=$(awk -v f="powershell-$PWSH_VERSION-linux-arm64.tar.gz" '$0 ~ "^([a-fA-F0-9]+)[[:space:]]+\\*" {name=$2; sub(/^\*/, "", name); if (name == f) print $1}' "$TEMP_DIR/hashes.txt")
  
  if [ -z "$EXPECTED_HASH" ]; then
    echo "prepare-debian-arm64: Error: Hash for PowerShell archive not found in hashes.sha256." >&2
    exit 1
  fi

  # Verify hash (case-insensitive)
  ACTUAL_HASH=$(sha256sum "$TEMP_DIR/powershell-$PWSH_VERSION-linux-arm64.tar.gz" | cut -d' ' -f1)
  if [ "$(echo "$EXPECTED_HASH" | tr 'A-F' 'a-f')" != "$(echo "$ACTUAL_HASH" | tr 'A-F' 'a-f')" ]; then
    echo "prepare-debian-arm64: Error: PowerShell archive hash mismatch." >&2
    exit 1
  fi

  echo "prepare-debian-arm64: PowerShell archive SHA-256 verified: $EXPECTED_HASH" >&2

  mkdir -p "$PWSH_DIR"
  tar -xzf "$TEMP_DIR/powershell-$PWSH_VERSION-linux-arm64.tar.gz" -C "$PWSH_DIR"

  chmod +x "$PWSH_DIR/pwsh"

  # Create symlink
  mkdir -p "$HOME/.local/bin"
  ln -sf "$PWSH_DIR/pwsh" "$HOME/.local/bin/pwsh"
fi

# Print environment variables for caller to eval
echo 'export DOTNET_ROOT="$HOME/.dotnet"'
echo 'export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$HOME/.local/bin:$PATH"'
echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1'
echo 'export TESTINGPLATFORM_TELEMETRY_OPTOUT=1'

# Print installed versions
echo "prepare-debian-arm64: Installed .NET SDKs:" >&2
"$DOTNET_DIR/dotnet" --list-sdks >&2
echo "prepare-debian-arm64: Installed .NET Runtimes:" >&2
"$DOTNET_DIR/dotnet" --list-runtimes >&2
echo "prepare-debian-arm64: Installed PowerShell version:" >&2
"$PWSH_DIR/pwsh" --version >&2

# Find libicu file
ICU_FILE=$(ls /usr/lib/aarch64-linux-gnu/libicuuc.so.* | head -n1)
echo "prepare-debian-arm64: Found ICU library: $ICU_FILE" >&2
