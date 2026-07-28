<#
.SYNOPSIS
    Publishes the FoodUs relay and deploys it to a host over SSH.

.DESCRIPTION
    The one deployment path for this repository: publish a self-contained bundle locally,
    copy it to the host, unpack it, and restart the systemd service. "Publish, don't build
    on the server" — the host needs no .NET SDK and no .NET runtime.

    Public mechanism, private values (context/wiki/secrets.md). This script contains steps,
    never credentials, never a host name, never a key path. Those three values come from
    parameters, or from a git-ignored local settings file beside this script:

        scripts/publish.local.psd1

        @{
            RelayHost    = 'relay.example.com'
            SshUser      = 'deploy'
            IdentityFile = 'C:\path\to\your\ssh\key'
        }

    Any key in that file supplies the default for the parameter of the same name; an
    explicitly passed parameter always wins. Remote paths and service identity also have
    defaults that can be overridden the same way.

    The remote account needs passwordless sudo for `systemctl` on the relay unit and for
    unpacking into the install directory. See docs/self-hosting.md.

.EXAMPLE
    ./scripts/publish.ps1 -RelayHost relay.example.com -SshUser deploy -IdentityFile ~/.ssh/id_ed25519

.EXAMPLE
    ./scripts/publish.ps1
    Uses every value from scripts/publish.local.psd1.
#>

[CmdletBinding()]
param(
    # Host to deploy to. Private value: pass it, or put it in publish.local.psd1.
    [string] $RelayHost,

    # SSH account used for the copy and the restart.
    [string] $SshUser,

    # Path to the SSH private key used for authentication.
    [string] $IdentityFile,

    # Writable scratch directory on the host; the bundle lands here before it is unpacked.
    [string] $RemoteStagingPath,

    # Directory the service runs from on the host.
    [string] $RemoteInstallPath,

    # systemd unit name, without the .service suffix.
    [string] $ServiceName,

    # Non-root account the service runs as; owns the install directory.
    [string] $ServiceUser,

    # Runtime identifier for the self-contained publish.
    [string] $RuntimeIdentifier
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
$repositoryRoot = Split-Path -Parent $scriptRoot
$projectPath = Join-Path $repositoryRoot 'source/FoodUsRelay/FoodUsRelay.csproj'
$localSettingsPath = Join-Path $scriptRoot 'publish.local.psd1'

# Mechanism-only defaults. They describe the layout this repository's templates assume, so a
# reader with a stock setup needs to supply nothing but the three private values.
$defaults = @{
    RemoteStagingPath = '/tmp/foodus-relay-deploy'
    RemoteInstallPath = '/opt/foodus-relay'
    ServiceName       = 'foodus-relay'
    ServiceUser       = 'foodus-relay'
    RuntimeIdentifier = 'linux-x64'
}

function Import-LocalSettings
{
    param([string] $Path)

    if (-not (Test-Path -LiteralPath $Path))
    {
        return @{}
    }

    return Import-PowerShellDataFile -LiteralPath $Path
}

function Resolve-Setting
{
    param(
        [string] $Name,
        [string] $Value,
        [hashtable] $LocalSettings,
        [switch] $Required
    )

    if (-not [string]::IsNullOrWhiteSpace($Value))
    {
        return $Value
    }

    if ($LocalSettings.ContainsKey($Name) -and -not [string]::IsNullOrWhiteSpace($LocalSettings[$Name]))
    {
        return [string] $LocalSettings[$Name]
    }

    if ($defaults.ContainsKey($Name))
    {
        return [string] $defaults[$Name]
    }

    if ($Required)
    {
        throw "No value for '$Name'. Pass -$Name, or set it in $localSettingsPath."
    }

    return $null
}

function Invoke-Native
{
    param(
        [string] $Description,
        [string] $FilePath,
        [string[]] $Arguments
    )

    Write-Host "==> $Description"
    & $FilePath @Arguments

    if ($LASTEXITCODE -ne 0)
    {
        throw "$Description failed with exit code $LASTEXITCODE."
    }
}

$localSettings = Import-LocalSettings -Path $localSettingsPath

$RelayHost = Resolve-Setting -Name 'RelayHost' -Value $RelayHost -LocalSettings $localSettings -Required
$SshUser = Resolve-Setting -Name 'SshUser' -Value $SshUser -LocalSettings $localSettings -Required
$IdentityFile = Resolve-Setting -Name 'IdentityFile' -Value $IdentityFile -LocalSettings $localSettings -Required
$RemoteStagingPath = Resolve-Setting -Name 'RemoteStagingPath' -Value $RemoteStagingPath -LocalSettings $localSettings
$RemoteInstallPath = Resolve-Setting -Name 'RemoteInstallPath' -Value $RemoteInstallPath -LocalSettings $localSettings
$ServiceName = Resolve-Setting -Name 'ServiceName' -Value $ServiceName -LocalSettings $localSettings
$ServiceUser = Resolve-Setting -Name 'ServiceUser' -Value $ServiceUser -LocalSettings $localSettings
$RuntimeIdentifier = Resolve-Setting -Name 'RuntimeIdentifier' -Value $RuntimeIdentifier -LocalSettings $localSettings

if (-not (Test-Path -LiteralPath $IdentityFile))
{
    throw "SSH identity file not found: $IdentityFile"
}

$publishDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "foodus-relay-publish-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
$bundleName = 'foodus-relay.tar.gz'
$bundlePath = Join-Path $publishDirectory $bundleName
$remoteBundlePath = "$RemoteStagingPath/$bundleName"
$sshTarget = "${SshUser}@${RelayHost}"

Invoke-Native -Description 'dotnet publish (self-contained)' -FilePath 'dotnet' -Arguments @(
    'publish'
    $projectPath
    '--configuration', 'Release'
    '--runtime', $RuntimeIdentifier
    '--self-contained', 'true'
    '--output', (Join-Path $publishDirectory 'app')
)

# tar is shipped with Windows 10+ and every Linux host, so one archive format works on both
# ends and keeps the transfer to a single scp of a single file.
Invoke-Native -Description 'Pack the published bundle' -FilePath 'tar' -Arguments @(
    '-czf', $bundlePath
    '-C', (Join-Path $publishDirectory 'app')
    '.'
)

Invoke-Native -Description "Create staging directory on the host" -FilePath 'ssh' -Arguments @(
    '-i', $IdentityFile
    $sshTarget
    "mkdir -p '$RemoteStagingPath'"
)

Invoke-Native -Description "Copy the bundle to the host" -FilePath 'scp' -Arguments @(
    '-i', $IdentityFile
    $bundlePath
    "${sshTarget}:${remoteBundlePath}"
)

# Stop before unpacking: the executable is running and would otherwise be overwritten in
# place. Restart is unconditional so a failed unpack still surfaces as a failed service.
$remoteDeployCommand = @(
    "set -e"
    "sudo systemctl stop $ServiceName"
    "sudo mkdir -p '$RemoteInstallPath'"
    "sudo tar -xzf '$remoteBundlePath' -C '$RemoteInstallPath'"
    "sudo chown -R ${ServiceUser}:${ServiceUser} '$RemoteInstallPath'"
    "sudo chmod +x '$RemoteInstallPath/FoodUsRelay'"
    "rm -f '$remoteBundlePath'"
    "sudo systemctl start $ServiceName"
    "sudo systemctl is-active $ServiceName"
) -join ' && '

Invoke-Native -Description "Install and restart $ServiceName" -FilePath 'ssh' -Arguments @(
    '-i', $IdentityFile
    $sshTarget
    $remoteDeployCommand
)

Remove-Item -LiteralPath $publishDirectory -Recurse -Force

Write-Host "==> Deployed. Smoke-check the capability endpoint: GET https://<your-host>/v1/capabilities"
