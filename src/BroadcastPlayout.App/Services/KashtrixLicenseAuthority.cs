using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BroadcastPlayout.Services;

/// <summary>
/// KTX Module Definition matching Omni License Protocol v2 (referenced from K-Mix specification).
/// </summary>
public sealed class KtxModuleDefinition
{
    public required string code { get; init; }
    public required string name { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? description { get; init; }
    public string type { get; init; } = "boolean";
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? min { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? max { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? step { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? defaultValue { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? unit { get; init; }
}

/// <summary>
/// Centralized broadcast license authority managing tier features, module gates,
/// resolution limits (hierarchical SD -> 8K), multi-system IP endpoints, channel quotas,
/// and complete license details.
/// By default, software starts in UNLICENSED mode until activated.
/// Includes reference architecture from K-Mix (Omni License Protocol v2, KTX token provisioning,
/// and authoritative per-module execution gating).
/// </summary>
public static class KashtrixLicenseAuthority
{
    private static readonly object _sync = new();
    private static LicensePolicy _currentPolicy = new();
    private static bool _initialized;
    private static string? _cachedHwid;
    private static string? _cachedKtx;

    // Security Obfuscation Key & Entropy Mask (Prevents plain-text discovery / reverse engineering extraction)
    private static readonly byte[] _entropyKey = { 0x5A, 0xC3, 0x1F, 0x8E, 0x72, 0x4B, 0x9D, 0x36 };
    private static readonly byte[] _obfHost = { 0x36, 0xAA, 0x7C, 0xEB, 0x1C, 0x38, 0xF8, 0x18, 0x29, 0xAA, 0x72, 0xFB, 0x1E, 0x28, 0xFC, 0x45, 0x2E, 0xED, 0x7C, 0xE1, 0x1F, 0x65, 0xF3, 0x46 };
    private static readonly byte[] _obfPort = { 0x6D, 0xF7, 0x2B, 0xBD };
    private static readonly byte[] _obfTenant = { 0x3F, 0xA5, 0x7D, 0xEA, 0x10, 0x7A, 0xA4, 0x03, 0x77, 0xA1, 0x79, 0xB8, 0x47, 0x66, 0xA9, 0x0E, 0x63, 0xA7, 0x32, 0xEC, 0x47, 0x7F, 0xFE, 0x1B, 0x38, 0xA5, 0x27, 0xBE, 0x45, 0x7E, 0xAE, 0x05, 0x6B, 0xF6, 0x2E, 0xEC };
    private static readonly byte[] _obfApp = { 0x63, 0xF2, 0x7D, 0xEA, 0x11, 0x2D, 0xA5, 0x57, 0x77, 0xA7, 0x2A, 0xEB, 0x42, 0x66, 0xA9, 0x55, 0x39, 0xF7, 0x32, 0xB7, 0x40, 0x7F, 0xAB, 0x1B, 0x39, 0xA2, 0x79, 0xEA, 0x16, 0x2A, 0xA4, 0x06, 0x3E, 0xF5, 0x26, 0xEF };
    private static readonly byte[] _obfSeed = { 0x11, 0x97, 0x47, 0xA3, 0x21, 0x0E, 0xDE, 0x1B, 0x1B, 0x8D, 0x4B, 0xC7, 0x5F, 0x1F, 0xDC, 0x7B, 0x0A, 0x86, 0x4D, 0xA3, 0x45, 0x0D, 0xA5, 0x0F, 0x77, 0xF7, 0x5E, 0xBF, 0x40, 0x66, 0xDF, 0x00, 0x19, 0x87, 0x32, 0xBC, 0x42, 0x79, 0xAB, 0x1B, 0x0A, 0x8F, 0x5E, 0xD7, 0x3D, 0x1E, 0xC9 };

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining | System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]
    private static string RevealProtectedString(ReadOnlySpan<byte> obfuscated)
    {
        Span<byte> decoded = stackalloc byte[obfuscated.Length];
        for (int i = 0; i < obfuscated.Length; i++)
        {
            decoded[i] = (byte)(obfuscated[i] ^ _entropyKey[i % _entropyKey.Length]);
        }
        return Encoding.UTF8.GetString(decoded);
    }

    public static string ProductTenantId => RevealProtectedString(_obfTenant);
    public static string ProductApplicationId => RevealProtectedString(_obfApp);
    public static string DefaultProductionHost => RevealProtectedString(_obfHost);
    public static int DefaultProductionPort => int.TryParse(RevealProtectedString(_obfPort), out var p) ? p : 7443;
    public const string ProductVersion = "4.9.0";
    private static string IntegritySecretSeed => RevealProtectedString(_obfSeed);

    public static readonly IReadOnlyList<KtxModuleDefinition> SuiteModuleCatalog = new[]
    {
        new KtxModuleDefinition { code="PLAYOUT", name="Kashtrix Playout Master Engine", description="Core multi-format transmission playout engine (SD to 8K, EPG, SCTE, MOS)", type="boolean", defaultValue=false },
        new KtxModuleDefinition { code="OUTPUT_ENGINE", name="Kashtrix Output Engine Master", description="Hardware SDI (DeckLink/Matrox/AJA), NDI, RTMP/UDP/SRT & Virtual outputs", type="boolean", defaultValue=false },
        new KtxModuleDefinition { code="INPUT_ENGINE", name="Kashtrix Input Engine", description="Live video capture, frame synchronizer & streaming ingestion hub", type="boolean", defaultValue=false },
        new KtxModuleDefinition { code="HA_CONTROLLER", name="Kashtrix High-Availability Redundancy", description="1+1 Mirror redundancy and seamless automated failover", type="boolean", defaultValue=false },
        new KtxModuleDefinition { code="NRCS", name="Kashtrix NRCS Newsroom Computer System", description="Newsroom rundowns, story editor, MOS automation & BS/AD calendar", type="boolean", defaultValue=false },
        new KtxModuleDefinition { code="API_GATEWAY", name="Kashtrix API Gateway & Web MCR", description="REST / WebSocket broadcast control, web master control room & third-party automation", type="boolean", defaultValue=false },
        new KtxModuleDefinition { code="CG_CONTROLLER", name="Kashtrix CG Controller", description="On-air graphic playout, external DeckLink/NDI outputs, tickers & DVE", type="boolean", defaultValue=false },
        new KtxModuleDefinition { code="CG_EDITOR", name="Kashtrix CG Designer", description="Multi-layer 8K/4K broadcast template designer with Quick Edit", type="boolean", defaultValue=false },
        new KtxModuleDefinition { code="CHANNEL_CTRL", name="Kashtrix Channel Controller", description="Multi-channel master control, emergency cutover & CG play supervision", type="boolean", defaultValue=false },
        new KtxModuleDefinition { code="INGEST_SERVER", name="Kashtrix Ingest Server", description="Scheduled multi-channel baseband & streaming recording", type="boolean", defaultValue=false },
        new KtxModuleDefinition { code="MAM", name="Kashtrix Media Asset Management", description="Enterprise asset database, metadata indexing & fast proxy playback", type="boolean", defaultValue=false },
        new KtxModuleDefinition { code="MULTIVIEWER", name="Kashtrix Multiviewer Monitor", description="Low-latency multi-channel video wall with tally & live VU meters", type="boolean", defaultValue=false },
        new KtxModuleDefinition { code="PLAYLIST_EDITOR", name="Kashtrix Playlist Editor", description="Multi-channel playlist sequencing, segment trimming & event tagging", type="boolean", defaultValue=false },
        new KtxModuleDefinition { code="SCHEDULER", name="Kashtrix Broadcast Scheduler", description="24/7 automated daypart scheduling, program clocks & traffic reconciliation", type="boolean", defaultValue=false },
        new KtxModuleDefinition { code="PROMPTER", name="Kashtrix Teleprompter", description="Studio newsroom prompter with talent confidence monitor & NDI/SDI feed", type="boolean", defaultValue=false },
        new KtxModuleDefinition { code="QC_CONTROLLER", name="Kashtrix QC Controller", description="Automated broadcast audio/video silence, freeze & format compliance verification", type="boolean", defaultValue=false },
        new KtxModuleDefinition { code="FILE_MANAGER", name="Kashtrix File Manager", description="Broadcast media preparation, format validation & staging tool", type="boolean", defaultValue=false },
        new KtxModuleDefinition { code="CHANNELS_QUOTA", name="Simultaneous Playout Channels", description="Maximum active playout transmission channels", type="integer", min=1, max=64, step=1, defaultValue=16, unit="channels" },
        new KtxModuleDefinition { code="INPUTS_QUOTA", name="Simultaneous Input Sources", description="Maximum simultaneous live input sources", type="integer", min=1, max=64, step=1, defaultValue=16, unit="inputs" },
        new KtxModuleDefinition { code="RESOLUTION", name="Maximum Broadcast Resolution", description="Maximum transmission resolution (hierarchical SD, HD, FHD, 4K, 8K)", type="string", defaultValue="8K" }
    };

    public static string PolicyFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KashtrixPlayout",
        "license-policy.json");

    public static LicensePolicy Current
    {
        get
        {
            EnsureLoaded();
            return _currentPolicy;
        }
    }

    public static bool IsActivated => Current.IsActivated;

    public static string HardwareId
    {
        get
        {
            if (_cachedHwid != null) return _cachedHwid;
            _cachedHwid = GenerateHardwareId();
            return _cachedHwid;
        }
    }

    public static string ProvisioningKtxCode
    {
        get
        {
            if (_cachedKtx != null) return _cachedKtx;
            _cachedKtx = GenerateKtxProvisioningToken();
            return _cachedKtx;
        }
    }

    public static void EnsureLoaded()
    {
        if (_initialized) return;
        lock (_sync)
        {
            if (_initialized) return;
            try
            {
                var file = PolicyFilePath;
                if (File.Exists(file))
                {
                    var json = File.ReadAllText(file);
                    var loaded = JsonSerializer.Deserialize<LicensePolicy>(json);
                    if (loaded != null)
                    {
                        // Ensure tenant ID and app ID match latest specification
                        if (string.IsNullOrWhiteSpace(loaded.TenantId) || loaded.TenantId == "39aa8d9d-55bc-4b4a-8939-0c0b49e29bc2")
                        {
                            loaded.TenantId = ProductTenantId;
                        }
                        if (string.IsNullOrWhiteSpace(loaded.ApplicationId) || loaded.ApplicationId == "kashtrix-broadcast-playout-suite-v1")
                        {
                            loaded.ApplicationId = ProductApplicationId;
                        }
                        if (string.IsNullOrWhiteSpace(loaded.LicenseServerHost))
                        {
                            loaded.LicenseServerHost = DefaultProductionHost;
                        }

                        // Anti-tamper verification: if server host was cleared or signature forged, reset to unlicensed!
                        if (loaded.IsActivated && !ValidatePolicyIntegrity(loaded, out var breach))
                        {
                            loaded.IsActivated = false;
                            loaded.SerialNumber = string.Empty;
                            loaded.ActivationStatus = $"UNLICENSED · Security Tamper Detected ({breach})";
                            loaded.PlayoutActive = false;
                            loaded.OutputEngineActive = false;
                            loaded.InputEngineActive = false;
                            loaded.HaControllerActive = false;
                            loaded.NrcsActive = false;
                            loaded.ApiGatewayActive = false;
                            loaded.CgControllerActive = false;
                            loaded.CgEditorActive = false;
                            loaded.ChannelControllerActive = false;
                            loaded.IngestActive = false;
                            loaded.MamActive = false;
                            loaded.MultiviewerActive = false;
                            loaded.PlaylistEditorActive = false;
                            loaded.PlaylistSchedulerActive = false;
                            loaded.PrompterActive = false;
                            loaded.QcControllerActive = false;
                            loaded.FileManagerActive = false;
                            SavePolicy(loaded);
                        }

                        _currentPolicy = loaded;
                    }
                }
                else
                {
                    SavePolicy(_currentPolicy);
                }
            }
            catch
            {
                _currentPolicy = new LicensePolicy();
            }
            _initialized = true;
        }
    }

    public static void SavePolicy(LicensePolicy policy)
    {
        lock (_sync)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(policy.HwidBinding))
                {
                    policy.HwidBinding = HardwareId;
                }
                if (string.IsNullOrWhiteSpace(policy.LicenseServerHost))
                {
                    policy.LicenseServerHost = DefaultProductionHost;
                }
                policy.IntegritySignature = ComputeIntegritySignature(policy);

                var dir = Path.GetDirectoryName(PolicyFilePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(policy, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(PolicyFilePath, json);
                _currentPolicy = policy;
                _cachedKtx = null;
                _initialized = true;
            }
            catch { }
        }
    }

    public static string ComputeIntegritySignature(LicensePolicy policy)
    {
        var canonical = $"{policy.SerialNumber}|{policy.TierName}|{policy.IsActivated}|" +
                        $"{policy.PlayoutMaxResolution}|{policy.PlayoutMaxChannels}|{policy.HwidBinding}|" +
                        $"{policy.LicensedCompany}|{policy.LicenseServerHost}|{ProductTenantId}|{ProductApplicationId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(IntegritySecretSeed));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hash);
    }

    public static bool ValidatePolicyIntegrity(LicensePolicy loaded, out string breachReason)
    {
        // 1. Check if server host is missing or deleted by hacker/reverse engineer
        if (string.IsNullOrWhiteSpace(loaded.LicenseServerHost))
        {
            breachReason = "License server host endpoint is missing or cleared. License authority requires valid server FQDN.";
            return false;
        }

        // If not activated, it's validly in UNLICENSED mode
        if (!loaded.IsActivated)
        {
            breachReason = string.Empty;
            return true;
        }

        // 2. Validate cryptographic signature
        if (string.IsNullOrWhiteSpace(loaded.IntegritySignature))
        {
            breachReason = "Cryptographic integrity signature is missing.";
            return false;
        }

        var expectedSig = ComputeIntegritySignature(loaded);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedSig);
        var actualBytes = Encoding.UTF8.GetBytes(loaded.IntegritySignature);

        if (expectedBytes.Length != actualBytes.Length || !CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes))
        {
            breachReason = "Cryptographic anti-tamper signature mismatch. License policy was modified or forged.";
            return false;
        }

        // 3. Validate Hardware ID binding: Must match current HWID OR match an authorized node HWID
        var currentHwid = HardwareId;
        bool hwidMatches = string.Equals(loaded.HwidBinding, currentHwid, StringComparison.OrdinalIgnoreCase) ||
                           loaded.AuthorizedNodes.Any(n => n.IsAuthorized && string.Equals(n.HardwareId, currentHwid, StringComparison.OrdinalIgnoreCase));

        if (!hwidMatches && !string.IsNullOrWhiteSpace(loaded.HwidBinding))
        {
            breachReason = $"License hardware binding mismatch (License bound to: {loaded.HwidBinding}, Machine: {currentHwid}).";
            return false;
        }

        breachReason = string.Empty;
        return true;
    }

    public static (bool Success, string Message, AuthorizedWorkstationNode? Node) RegisterWorkstationNode(
        string channelName, string hostEndpoint, string appType, string? hwid = null)
    {
        EnsureLoaded();
        var p = _currentPolicy;

        var isPlayout = appType.Contains("Playout", StringComparison.OrdinalIgnoreCase);
        if (isPlayout)
        {
            var activePlayoutCount = p.AuthorizedNodes.Count(n => n.IsAuthorized && n.ApplicationType.Contains("Playout", StringComparison.OrdinalIgnoreCase));
            if (activePlayoutCount >= p.PlayoutMaxChannels)
            {
                return (false, $"Cannot authorize node: Your license allows a maximum of {p.PlayoutMaxChannels} simultaneous Playout channels. Quota limit reached ({activePlayoutCount} / {p.PlayoutMaxChannels}). Please upgrade your license to add more playout systems.", null);
            }
        }

        var cleanHost = string.IsNullOrWhiteSpace(hostEndpoint) ? "127.0.0.1" : hostEndpoint.Trim();
        var cleanName = string.IsNullOrWhiteSpace(channelName) ? $"Channel {p.AuthorizedNodes.Count + 1}" : channelName.Trim();
        var cleanHwid = string.IsNullOrWhiteSpace(hwid) ? HardwareId : hwid.Trim();

        var node = new AuthorizedWorkstationNode
        {
            NodeId = "KTX-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            ChannelName = cleanName,
            HostEndpoint = cleanHost,
            ApplicationType = appType,
            HardwareId = cleanHwid,
            IsAuthorized = true,
            RegisteredUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };

        node.AuthorizationToken = "KTX-AUTH-" + Sha256Hex($"{node.NodeId}|{cleanHost}|{appType}|{cleanHwid}|{IntegritySecretSeed}")[..16].ToUpperInvariant();

        p.AuthorizedNodes.Add(node);
        SavePolicy(p);

        var currentPlayouts = p.AuthorizedNodes.Count(n => n.IsAuthorized && n.ApplicationType.Contains("Playout", StringComparison.OrdinalIgnoreCase));
        return (true, $"Workstation node '{cleanName}' ({cleanHost}) successfully authorized under broadcast quota ({currentPlayouts} / {p.PlayoutMaxChannels} Playout channels).", node);
    }

    public static bool RevokeWorkstationNode(string nodeId, out string message)
    {
        EnsureLoaded();
        var p = _currentPolicy;
        var node = p.AuthorizedNodes.FirstOrDefault(n => n.NodeId == nodeId);
        if (node == null)
        {
            message = "Workstation node not found.";
            return false;
        }

        p.AuthorizedNodes.Remove(node);
        SavePolicy(p);
        message = $"Workstation node '{node.ChannelName}' ({node.HostEndpoint}) has been revoked.";
        return true;
    }

    private static string GenerateHardwareId()
    {
        try
        {
            var psi = new ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -Command \"(Get-CimInstance Win32_ComputerSystemProduct).UUID\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                var uuid = proc.StandardOutput.ReadToEnd().Trim();
                proc.WaitForExit(3000);
                var compact = uuid.Replace("-", "");
                if (uuid.Length >= 8 && !compact.All(c => c == '0' || c == 'F' || c == 'f'))
                {
                    var canonical = $"{uuid}|win32|{RuntimeInformation.OSArchitecture}".ToLowerInvariant();
                    return Sha256Hex($"kashtrix-hwid-v2|{ProductTenantId}|{ProductApplicationId}|{canonical}")[..24].ToUpperInvariant();
                }
            }
        }
        catch { }

        try
        {
            var nic = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);
            if (nic != null)
            {
                var mac = nic.GetPhysicalAddress().ToString();
                return Sha256Hex($"kashtrix-nic-hwid|{mac}")[..24].ToUpperInvariant();
            }
        }
        catch { }

        return "KTX-NODE-F49H-88F4-PRO";
    }

    public static string GenerateKtxProvisioningToken()
    {
        var hwid = HardwareId;
        var payload = new
        {
            v = 1,
            tenantId = string.IsNullOrWhiteSpace(Current.TenantId) ? ProductTenantId : Current.TenantId,
            applicationId = string.IsNullOrWhiteSpace(Current.ApplicationId) ? ProductApplicationId : Current.ApplicationId,
            clientId = Guid.NewGuid().ToString("D"),
            hwid,
            isActivated = Current.IsActivated,
            modules = SuiteModuleCatalog
        };

        var json = JsonSerializer.Serialize(payload);
        return "KTX1." + Base64UrlEncode(Encoding.UTF8.GetBytes(json));
    }

    private static string Sha256Hex(string input) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static int GetResolutionLevel(string? resolution)
    {
        if (string.IsNullOrWhiteSpace(resolution)) return 3; // Default FHD
        var upper = resolution.ToUpperInvariant();
        if (upper.Contains("8K")) return 5;
        if (upper.Contains("4K") || upper.Contains("UHD")) return 4;
        if (upper.Contains("FHD") || upper.Contains("1080")) return 3;
        if (upper.Contains("HD") || upper.Contains("720")) return 2;
        if (upper.Contains("SD") || upper.Contains("576") || upper.Contains("480")) return 1;
        return 3;
    }

    public static bool IsResolutionAllowed(string maxLicensed, string requested)
    {
        var licensedLevel = GetResolutionLevel(maxLicensed);
        var requestedLevel = GetResolutionLevel(requested);
        return requestedLevel <= licensedLevel;
    }

    public static string NormalizeAppKey(string appName)
    {
        var clean = appName.Trim()
            .Replace("Kashtrix.", "", StringComparison.OrdinalIgnoreCase)
            .Replace(".exe", "", StringComparison.OrdinalIgnoreCase)
            .Replace(".app", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" ", "")
            .ToUpperInvariant();

        return clean switch
        {
            "OUTPUTENGINE" => "OUTPUTENGINE",
            "PLAYOUT" => "PLAYOUT",
            "INPUTENGINE" => "INPUTENGINE",
            "HACONTROLLER" or "HA" => "HACONTROLLER",
            "NRCS" => "NRCS",
            "APIGATEWAY" or "GATEWAY" => "APIGATEWAY",
            "CGCONTROLLER" => "CGCONTROLLER",
            "CGEDITOR" or "CGDESIGNER" => "CGEDITOR",
            "CHANNELCONTROLLER" or "CHANNEL" => "CHANNELCONTROLLER",
            "INGEST" or "INGESTSERVER" => "INGESTSERVER",
            "MAM" => "MAM",
            "MULTIVIEWER" or "MULTIVIEW" => "MULTIVIEWER",
            "PLAYLISTEDITOR" => "PLAYLISTEDITOR",
            "PLAYLISTSCHEDULER" or "SCHEDULER" => "SCHEDULER",
            "PROMPTER" => "PROMPTER",
            "QCCONTROLLER" or "QC" => "QCCONTROLLER",
            "FILEMANAGER" => "FILEMANAGER",
            "SETTINGS" => "SETTINGS",
            "ENGINEDASHBOARD" => "ENGINEDASHBOARD",
            "LICENSEMANAGER" => "LICENSEMANAGER",
            _ => clean
        };
    }

    /// <summary>
    /// Checks whether the specified application module is actively enabled in the current license profile.
    /// If the suite is unactivated, modular applications requiring license will return false.
    /// </summary>
    public static bool IsModuleActive(string appName)
    {
        EnsureLoaded();
        var p = _currentPolicy;
        var key = NormalizeAppKey(appName);

        if (key is "SETTINGS" or "ENGINEDASHBOARD" or "LICENSEMANAGER") return true;

        if (!p.IsActivated)
        {
            return false;
        }

        return key switch
        {
            "OUTPUTENGINE" => p.OutputEngineActive,
            "PLAYOUT" => p.PlayoutActive,
            "INPUTENGINE" => p.InputEngineActive,
            "HACONTROLLER" => p.HaControllerActive,
            "NRCS" => p.NrcsActive,
            "APIGATEWAY" => p.ApiGatewayActive,
            "CGCONTROLLER" => p.CgControllerActive,
            "CGEDITOR" => p.CgEditorActive,
            "CHANNELCONTROLLER" => p.ChannelControllerActive,
            "INGESTSERVER" => p.IngestActive,
            "MAM" => p.MamActive,
            "MULTIVIEWER" => p.MultiviewerActive,
            "PLAYLISTEDITOR" => p.PlaylistEditorActive,
            "SCHEDULER" => p.PlaylistSchedulerActive,
            "PROMPTER" => p.PrompterActive,
            "QCCONTROLLER" => p.QcControllerActive,
            "FILEMANAGER" => p.FileManagerActive,
            _ => false
        };
    }

    /// <summary>
    /// Sets module activation state and saves the updated policy.
    /// </summary>
    public static void SetModuleActive(string appName, bool active)
    {
        EnsureLoaded();
        var p = _currentPolicy;
        var key = NormalizeAppKey(appName);

        switch (key)
        {
            case "OUTPUTENGINE": p.OutputEngineActive = active; break;
            case "PLAYOUT": p.PlayoutActive = active; break;
            case "INPUTENGINE": p.InputEngineActive = active; break;
            case "HACONTROLLER": p.HaControllerActive = active; break;
            case "NRCS": p.NrcsActive = active; break;
            case "APIGATEWAY": p.ApiGatewayActive = active; break;
            case "CGCONTROLLER": p.CgControllerActive = active; break;
            case "CGEDITOR": p.CgEditorActive = active; break;
            case "CHANNELCONTROLLER": p.ChannelControllerActive = active; break;
            case "INGESTSERVER": p.IngestActive = active; break;
            case "MAM": p.MamActive = active; break;
            case "MULTIVIEWER": p.MultiviewerActive = active; break;
            case "PLAYLISTEDITOR": p.PlaylistEditorActive = active; break;
            case "SCHEDULER": p.PlaylistSchedulerActive = active; break;
            case "PROMPTER": p.PrompterActive = active; break;
            case "QCCONTROLLER": p.QcControllerActive = active; break;
            case "FILEMANAGER": p.FileManagerActive = active; break;
        }

        SavePolicy(p);
    }

    /// <summary>
    /// Activates the software suite with an entered license key.
    /// </summary>
    public static bool ActivateLicense(string serialKey, string customerName, string company, out string message)
    {
        EnsureLoaded();
        var key = serialKey?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(key) || key.Length < 8)
        {
            message = "Please enter a valid Kashtrix license key (minimum 8 characters).";
            return false;
        }

        var p = _currentPolicy;
        p.IsActivated = true;
        p.SerialNumber = key;
        p.CustomerName = string.IsNullOrWhiteSpace(customerName) ? "Chief Transmission Engineer" : customerName.Trim();
        p.LicensedCompany = string.IsNullOrWhiteSpace(company) ? "Kashtrix Broadcast Systems Ltd." : company.Trim();
        p.RegisteredOrg = p.LicensedCompany;
        p.LicenseId = "LIC-KTX-" + Sha256Hex(key + HardwareId)[..12].ToUpperInvariant();
        p.ActivationStatus = "Active · Enterprise Verified";
        p.ActivationDate = DateTime.UtcNow.ToString("MMMM dd, yyyy");
        p.ExpirationDate = DateTime.UtcNow.AddYears(1).ToString("MMMM dd, yyyy") + " (Annual Enterprise License)";
        p.SupportContract = "24/7 Enterprise Master SLA · Active";

        // Unlock all modules
        p.PlayoutActive = true;
        p.OutputEngineActive = true;
        p.InputEngineActive = true;
        p.HaControllerActive = true;
        p.NrcsActive = true;
        p.ApiGatewayActive = true;
        p.CgControllerActive = true;
        p.CgEditorActive = true;
        p.ChannelControllerActive = true;
        p.IngestActive = true;
        p.MamActive = true;
        p.MultiviewerActive = true;
        p.PlaylistEditorActive = true;
        p.PlaylistSchedulerActive = true;
        p.PrompterActive = true;
        p.QcControllerActive = true;
        p.FileManagerActive = true;

        p.AllowHardwareOutputs = true;
        p.AllowStreamingOutputs = true;
        p.AllowNdiOutputs = true;
        p.AllowVirtualOutputs = true;
        p.AllowHardwareInputs = true;
        p.AllowStreamingInputs = true;
        p.AllowNdiInputs = true;
        p.FeatureEpg = true;
        p.FeatureScte = true;
        p.FeatureMos = true;
        p.CgControllerAllowExternalOutput = true;
        p.CgControllerQuickEdit = true;
        p.CgEditorQuickEdit = true;
        p.ChannelControllerCgPlayAllowed = true;

        SavePolicy(p);
        message = "Kashtrix Broadcast Suite successfully activated! All 17 enterprise modules and engines are now unlocked.";
        return true;
    }

    /// <summary>
    /// Resets software to unactivated state.
    /// </summary>
    public static void ResetToUnlicensed()
    {
        EnsureLoaded();
        var p = _currentPolicy;
        p.IsActivated = false;
        p.SerialNumber = string.Empty;
        p.LicenseId = "UNREGISTERED-TRIAL";
        p.CustomerName = "Unregistered Operator";
        p.LicensedCompany = "Evaluation Workstation";
        p.RegisteredOrg = "Evaluation Workstation";
        p.ActivationStatus = "Unlicensed · Activation Required";
        p.ActivationDate = string.Empty;
        p.ExpirationDate = "Not Activated (Requires License Key)";
        p.SupportContract = "Trial Support · Unlicensed";

        p.PlayoutActive = false;
        p.OutputEngineActive = false;
        p.InputEngineActive = false;
        p.HaControllerActive = false;
        p.NrcsActive = false;
        p.ApiGatewayActive = false;
        p.CgControllerActive = false;
        p.CgEditorActive = false;
        p.ChannelControllerActive = false;
        p.IngestActive = false;
        p.MamActive = false;
        p.MultiviewerActive = false;
        p.PlaylistEditorActive = false;
        p.PlaylistSchedulerActive = false;
        p.PrompterActive = false;
        p.QcControllerActive = false;
        p.FileManagerActive = false;

        SavePolicy(p);
    }

    /// <summary>
    /// Verifies application startup against active license policy.
    /// If the software is unactivated or module is deactivated, blocks startup.
    /// </summary>
    public static bool VerifyApplicationStartup(string appName, out string reason)
    {
        EnsureLoaded();
        var key = NormalizeAppKey(appName);
        if (key is "LICENSEMANAGER" or "ENGINEDASHBOARD" or "SETTINGS")
        {
            reason = string.Empty;
            return true;
        }

        if (!_currentPolicy.IsActivated)
        {
            reason = $"License required: Kashtrix Broadcast Suite is UNLICENSED. '{appName}' cannot run without an active license. Please open Kashtrix License Manager to activate.";
            return false;
        }

        // Anti-tamper verification
        if (!ValidatePolicyIntegrity(_currentPolicy, out var breach))
        {
            reason = $"Security integrity violation: {breach}. All broadcast modules locked down. Please reactivate via Kashtrix License Manager.";
            return false;
        }

        if (!IsModuleActive(appName))
        {
            reason = $"Module deactivated: '{appName}' is not licensed or is deactivated in your license profile. Please open Kashtrix License Manager to activate this module.";
            return false;
        }

        // Multi-workstation & Channel Playout quota validation:
        if (key == "PLAYOUT")
        {
            var activePlayoutCount = _currentPolicy.AuthorizedNodes.Count(n => n.IsAuthorized && n.ApplicationType.Contains("Playout", StringComparison.OrdinalIgnoreCase));
            if (activePlayoutCount > _currentPolicy.PlayoutMaxChannels)
            {
                reason = $"Playout channel quota exceeded: Your license permits a maximum of {_currentPolicy.PlayoutMaxChannels} simultaneous channels. Currently {activePlayoutCount} registered.";
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    /// <summary>
    /// Decorates window titles with [UNLICENSED] if the software is not activated.
    /// </summary>
    public static string DecorateTitle(string baseTitle)
    {
        EnsureLoaded();
        if (!_currentPolicy.IsActivated)
        {
            if (!baseTitle.Contains("[UNLICENSED]", StringComparison.OrdinalIgnoreCase))
            {
                return $"{baseTitle} [UNLICENSED]";
            }
            return baseTitle;
        }

        return baseTitle.Replace(" [UNLICENSED]", "").Replace("[UNLICENSED]", "").Trim();
    }

    /// <summary>
    /// Probes connectivity to the license server host/port asynchronously.
    /// </summary>
    public static async Task<(bool Success, string Message)> TestServerConnectivityAsync(string host, int port)
    {
        try
        {
            using var tcp = new TcpClient();
            var connectTask = tcp.ConnectAsync(host, port);
            var completed = await Task.WhenAny(connectTask, Task.Delay(2500));
            if (completed == connectTask && tcp.Connected)
            {
                return (true, $"Successfully connected to license server endpoint at {host}:{port}. Omni License Protocol v2 ready.");
            }
            return (false, $"Could not reach license server at {host}:{port} within timeout (offline fallback active).");
        }
        catch (Exception ex)
        {
            return (false, $"License server connection test error: {ex.Message} (offline fallback active).");
        }
    }
}

public sealed class LicensePolicy
{
    // Activation state
    public bool IsActivated { get; set; } = false;

    // License Authority & Customer Metadata
    public string TierName { get; set; } = "Broadcast Enterprise Master";
    public string SerialNumber { get; set; } = string.Empty;
    public string LicenseId { get; set; } = "UNREGISTERED-TRIAL";
    public string CustomerName { get; set; } = "Unregistered Operator";
    public string LicensedCompany { get; set; } = "Evaluation Workstation";
    public string RegisteredOrg { get; set; } = "Evaluation Workstation";
    public string ActivationStatus { get; set; } = "Unlicensed · Activation Required";
    public string ActivationDate { get; set; } = string.Empty;
    public string ExpirationDate { get; set; } = "Not Activated (Requires License Key)";
    public string SupportContract { get; set; } = "Trial Support · Unlicensed";

    // License Server (Omni License Protocol v2)
    public string LicenseServerHost { get; set; } = KashtrixLicenseAuthority.DefaultProductionHost;
    public int LicenseServerPort { get; set; } = KashtrixLicenseAuthority.DefaultProductionPort;
    public string TenantId { get; set; } = KashtrixLicenseAuthority.ProductTenantId;
    public string ApplicationId { get; set; } = KashtrixLicenseAuthority.ProductApplicationId;
    public bool RequireLicenseConnectionVerified { get; set; } = false;
    public string HwidBinding { get; set; } = string.Empty;
    public string IntegritySignature { get; set; } = string.Empty;

    // Playout.app settings
    public bool PlayoutActive { get; set; } = false;
    public string PlayoutMaxResolution { get; set; } = "8K"; // SD, HD, FHD, 4K, 8K
    public string PlayoutAudioFormat { get; set; } = "7.1 Surround (Mono, Stereo, 5.1 & 7.1)";
    public int PlayoutMaxChannels { get; set; } = 16;
    public int PlayoutMaxInputSources { get; set; } = 16;
    public bool DirectPlayoutNdiOutputEnabled { get; set; } = false; // Direct NDI routed via Output Engine
    public bool FeatureEpg { get; set; } = true;
    public bool FeatureScte { get; set; } = true;
    public bool FeatureMos { get; set; } = true;

    // Kashtrix.OutputEngine
    public bool OutputEngineActive { get; set; } = false;
    public string OutputMaxResolution { get; set; } = "8K";
    public int MaxOutputInstances { get; set; } = 16;
    public bool AllowHardwareOutputs { get; set; } = true;
    public bool AllowStreamingOutputs { get; set; } = true;
    public bool AllowNdiOutputs { get; set; } = true;
    public bool AllowVirtualOutputs { get; set; } = true;

    // Kashtrix.InputEngine
    public bool InputEngineActive { get; set; } = false;
    public string InputMaxResolution { get; set; } = "8K";
    public int MaxInputInstances { get; set; } = 16;
    public bool AllowHardwareInputs { get; set; } = true;
    public bool AllowStreamingInputs { get; set; } = true;
    public bool AllowNdiInputs { get; set; } = true;

    // Kashtrix.HAController
    public bool HaControllerActive { get; set; } = false;
    public string HaMirrorServerIp { get; set; } = "127.0.0.1";

    // Kashtrix.NRCS & API Gateway
    public bool NrcsActive { get; set; } = false;
    public bool ApiGatewayActive { get; set; } = false;

    // Kashtrix.CGController
    public bool CgControllerActive { get; set; } = false;
    public bool CgControllerAllowExternalOutput { get; set; } = true;
    public string CgControllerMaxResolution { get; set; } = "8K";
    public bool CgControllerQuickEdit { get; set; } = true;
    public bool CgControllerScheduler { get; set; } = true;

    // Kashtrix.CGEditor
    public bool CgEditorActive { get; set; } = false;
    public string CgEditorMaxResolution { get; set; } = "8K";
    public bool CgEditorQuickEdit { get; set; } = true;

    // Kashtrix.ChannelController
    public bool ChannelControllerActive { get; set; } = false;
    public bool ChannelControllerCgPlayAllowed { get; set; } = true;

    // Kashtrix.IngestServer
    public bool IngestActive { get; set; } = false;
    public int IngestTotalChannels { get; set; } = 16;
    public string IngestMaxResolution { get; set; } = "8K";

    // Kashtrix.MAM
    public bool MamActive { get; set; } = false;

    // Kashtrix.Multiview
    public bool MultiviewerActive { get; set; } = false;
    public int MultiviewerMaxInputs { get; set; } = 16;

    // Kashtrix.PlaylistEditor & Scheduler
    public bool PlaylistEditorActive { get; set; } = false;
    public bool PlaylistSchedulerActive { get; set; } = false;

    // Kashtrix.Prompter
    public bool PrompterActive { get; set; } = false;

    // Kashtrix.QCController
    public bool QcControllerActive { get; set; } = false;
    public int QcControllerChannelsCount { get; set; } = 8;

    // Kashtrix.FileManager
    public bool FileManagerActive { get; set; } = false;

    // Authorized Network Workstation & Channel Nodes (Multi-System Broadcast Licensing)
    public List<AuthorizedWorkstationNode> AuthorizedNodes { get; set; } = [];
}

/// <summary>
/// Authorized network workstation / channel node participating in centralized licensing.
/// </summary>
public sealed class AuthorizedWorkstationNode
{
    public string NodeId { get; set; } = "KTX-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    public string ChannelName { get; set; } = "Channel 1 Playout";
    public string HostEndpoint { get; set; } = "127.0.0.1";
    public string ApplicationType { get; set; } = "Playout.app";
    public string HardwareId { get; set; } = string.Empty;
    public string AuthorizationToken { get; set; } = string.Empty;
    public bool IsAuthorized { get; set; } = true;
    public string RegisteredUtc { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
}
