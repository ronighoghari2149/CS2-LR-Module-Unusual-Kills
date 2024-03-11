using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Utils;
using LevelsRanks.API;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LevelsRanksUnusualKills;

[MinimumApiVersion(80)]
public class LevelsRanksUnusualKills : BasePlugin
{
    private const string ModuleConfigFileName = "UnusualKills.yml";
    private const string PhrasesConfigFileName = "UnusualKills.phrases.yml";
    private ModuleConfig _moduleConfig;
    private PhrasesConfig _phrasesConfig;
    private static bool _firstKillAwardedThisRound = false;
    Dictionary<int, DateTime> playerBlindEndTime = new Dictionary<int, DateTime>();
    private readonly object _lock = new object();
    public override string ModuleName => "[LR] Module Unusual Kills";
    public override string ModuleVersion => "1.0";
    public override string ModuleAuthor => "ABKAM designed by RoadSide Romeo & Wend4r";
    public override string ModuleDescription => "Awards points for no-scope AWP kills.";
    private readonly PluginCapability<IPointsManager> _pointsManagerCapability = new("levelsranks");
    private IPointsManager? _pointsManager;
    private Dictionary<int, DateTime> playerJumpStatus = new Dictionary<int, DateTime>();
    
    public override void OnAllPluginsLoaded(bool hotReload)
    {
        base.Load(hotReload);
        _pointsManager = _pointsManagerCapability.Get();

        if (_pointsManager == null)
        {
            Server.PrintToConsole("Points management system is currently unavailable.");
            return;
        }
        
        LoadModuleConfig();
        LoadPhrasesConfig();

        RegisterEventHandler<EventPlayerJump>(OnPlayerJump);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventPlayerBlind>(OnPlayerBlind);
    }
    private HookResult OnPlayerBlind(EventPlayerBlind blindEvent, GameEventInfo info)
    {
        var player = blindEvent.Userid;
        if (player != null && !player.IsBot)
        {
            playerBlindEndTime[player.UserId.Value] = DateTime.UtcNow.AddSeconds(blindEvent.BlindDuration);
        }
        return HookResult.Continue;
    }
    private HookResult OnPlayerJump(EventPlayerJump jumpEvent, GameEventInfo info)
    {
        var player = jumpEvent.Userid;
        if (player != null && !player.IsBot)
        {
            playerJumpStatus[player.UserId.Value] = DateTime.UtcNow;
        }
        return HookResult.Continue;
    }
    private HookResult OnRoundStart(EventRoundStart roundStartEvent, GameEventInfo info)
    {
        _firstKillAwardedThisRound = false;
        return HookResult.Continue;
    }
    private bool IsPlayerRunning(CPlayer_MovementServices movementServices)
    {
        float movementThreshold = 0.5f; 
        return Math.Abs(movementServices.ForwardMove) > movementThreshold ||
               Math.Abs(movementServices.LeftMove) > movementThreshold;
    }
    private HookResult OnPlayerDeath(EventPlayerDeath deathEvent, GameEventInfo info)
    {
        var killer = deathEvent.Attacker;
        if (killer != null && !killer.IsBot)
        {
            var messageColor = ReplaceColorPlaceholders(_phrasesConfig.NoScopeAWPMessageColor);
            var message = _phrasesConfig.NoScopeAWPMessage;
            var points = 0;
            var playerPawn = killer.Pawn?.Value;
            var movementServices = playerPawn.MovementServices;
            var playerUserId = killer.UserId.Value;   
            var victim = deathEvent.Userid;
            bool killThroughSmoke = deathEvent.Thrusmoke;
            
            lock (_lock)
            {
                if (!_firstKillAwardedThisRound)
                {
                    AwardPointsForSpecialKill(killer, "FirstKill");
                    _firstKillAwardedThisRound = true;
                    
                    return HookResult.Continue;
                }
            }
            
            if (killThroughSmoke)
            {
                AwardPointsForSpecialKill(killer, "SmokeKill");
            }
            
            if (killer != null && !killer.IsBot && victim != null)
            {
                if (playerBlindEndTime.TryGetValue(killer.UserId.Value, out var blindEndTime) && DateTime.UtcNow <= blindEndTime)
                {
                    AwardPointsForSpecialKill(killer, "BlindKill");
                }
            }
            if (playerPawn != null)
            {
                if (movementServices != null && IsPlayerRunning(movementServices))
                {
                    AwardPointsForSpecialKill(killer, "RunningKill");
                    return HookResult.Continue; 
                }
            }
            if (playerJumpStatus.TryGetValue(playerUserId, out var jumpTime) && (DateTime.UtcNow - jumpTime).TotalSeconds < 2) 
            {
                playerJumpStatus.Remove(playerUserId); 
                AwardPointsForSpecialKill(killer, "JumpKill");
                return HookResult.Continue; 
            }
            if (deathEvent.Weapon == "awp" && deathEvent.Noscope)
            {
                AwardPointsForSpecialKill(killer, "NoScopeKill");
            }
            else if (deathEvent.Penetrated > 0) 
            {
                AwardPointsForSpecialKill(killer, "WallbangKill");
            }

            if (points > 0)
            {
                _pointsManager?.AddOrRemovePoints(killer.SteamID.ToString(), points, killer, message, messageColor);
            }
        }
        else
        {
            Server.PrintToConsole("Ошибка: Убийца не идентифицирован или является ботом.");
        }

        return HookResult.Continue;
    }
    private void AwardPointsForSpecialKill(CCSPlayerController killer, string killType)
    {
        switch (killType)
        {
            case "JumpKill":
                var points = _moduleConfig.PointsForJumpKill;
                var message = _phrasesConfig.JumpKillMessage;
                var messageColor = _phrasesConfig.JumpKillMessageColor;
                _pointsManager?.AddOrRemovePoints(killer.SteamID.ToString(), points, killer, message, messageColor);
                break;
            case "BlindKill":
                var points2 = _moduleConfig.PointsForBlindKill; 
                var message2 = _phrasesConfig.BlindKillMessage; 
                var messageColor2 = _phrasesConfig.BlindKillMessageColor; 
                _pointsManager?.AddOrRemovePoints(killer.SteamID.ToString(), points2, killer, message2, messageColor2);
                break;
            case "SmokeKill":
                points = _moduleConfig.PointsForSmokeKill; 
                message = _phrasesConfig.SmokeKillMessage; 
                messageColor = _phrasesConfig.SmokeKillMessageColor; 
                _pointsManager?.AddOrRemovePoints(killer.SteamID.ToString(), points, killer, message, messageColor);
                break;
            case "RunningKill":
                points = _moduleConfig.PointsForKillWhileRunning;
                message = _phrasesConfig.KillWhileRunningMessage;
                messageColor = _phrasesConfig.KillWhileRunningMessageColor;
                _pointsManager?.AddOrRemovePoints(killer.SteamID.ToString(), points, killer, message, messageColor);
                break;
            case "FirstKill":
                points = _moduleConfig.PointsForFirstKill;
                message = _phrasesConfig.FirstKillMessage;
                messageColor = _phrasesConfig.FirstKillMessageColor;
                _pointsManager?.AddOrRemovePoints(killer.SteamID.ToString(), points, killer, message, messageColor);
                break;
            case "WallbangKill":
                points = _moduleConfig.PointsForWallbang; 
                message = _phrasesConfig.WallbangKillMessage;
                messageColor = _phrasesConfig.WallbangKillMessageColor;
                _pointsManager?.AddOrRemovePoints(killer.SteamID.ToString(), points, killer, message, messageColor);
                break;
            case "NoScopeKill":
                points = _moduleConfig.PointsForNoScopeAWP;
                message = _phrasesConfig.NoScopeAWPMessage;
                messageColor = _phrasesConfig.NoScopeAWPMessageColor;
                _pointsManager?.AddOrRemovePoints(killer.SteamID.ToString(), points, killer, message, messageColor);
                break;
            default:
                return;
        }
    }
    private string ReplaceColorPlaceholders(string message)
    {
        if (message.Contains('{'))
        {
            string modifiedValue = message;
            foreach (FieldInfo field in typeof(ChatColors).GetFields())
            {
                string pattern = $"{{{field.Name}}}";
                if (message.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    modifiedValue = modifiedValue.Replace(pattern, field.GetValue(null).ToString(),
                        StringComparison.OrdinalIgnoreCase);
                }
            }

            return modifiedValue;
        }

        return message;
    }
    private void LoadModuleConfig()
    {
        var configPath = Path.Combine(ModuleDirectory, ModuleConfigFileName);
        try
        {
            if (!File.Exists(configPath))
            {
                _moduleConfig = new ModuleConfig();
                SaveModuleConfig(_moduleConfig, configPath);
            }
            else
            {
                var deserializer = new DeserializerBuilder()
                    .IgnoreUnmatchedProperties() 
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var yaml = File.ReadAllText(configPath);
                _moduleConfig = deserializer.Deserialize<ModuleConfig>(yaml);
            }
        }
        catch (Exception ex)
        {
            Server.PrintToConsole($"Error loading module configuration: {ex.Message}");
            _moduleConfig = new ModuleConfig();
        }
    }

    private void LoadPhrasesConfig()
    {
        var configPath = Path.Combine(ModuleDirectory, PhrasesConfigFileName);
        try
        {
            if (!File.Exists(configPath))
            {
                _phrasesConfig = new PhrasesConfig();
                SavePhrasesConfig(_phrasesConfig, configPath);
            }
            else
            {
                var deserializer = new DeserializerBuilder()
                    .IgnoreUnmatchedProperties() 
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var yaml = File.ReadAllText(configPath);
                _phrasesConfig = deserializer.Deserialize<PhrasesConfig>(yaml);
            }
        }
        catch (Exception ex)
        {
            Server.PrintToConsole($"Error loading phrase configuration: {ex.Message}");
            _phrasesConfig = new PhrasesConfig();
        }
    }
    public void SaveModuleConfig(ModuleConfig config, string filePath)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine("# Configuration for '[LR] Module Unusual Kills'");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("# Points for first kill");
        stringBuilder.AppendLine($"PointsForFirstKill: {config.PointsForFirstKill}");     
        stringBuilder.AppendLine("# Points for wallbang kill");
        stringBuilder.AppendLine($"PointsForWallbang: {config.PointsForWallbang}"); 
        stringBuilder.AppendLine("# Points for no-scope AWP kill");
        stringBuilder.AppendLine($"PointsForNoScopeAWP: {config.PointsForNoScopeAWP}"); 
        stringBuilder.AppendLine("# Points for running kill");
        stringBuilder.AppendLine($"PointsForKillWhileRunning: {config.PointsForKillWhileRunning}"); 
        stringBuilder.AppendLine("# Points for jump kill");
        stringBuilder.AppendLine($"PointsForJumpKill: {config.PointsForJumpKill}");  
        stringBuilder.AppendLine("# Points for blind kill");
        stringBuilder.AppendLine($"PointsForBlindKill: {config.PointsForBlindKill}");          
        stringBuilder.AppendLine("# Points for smoke kill");
        stringBuilder.AppendLine($"PointsForSmokeKill: {config.PointsForSmokeKill}");      
    
        File.WriteAllText(filePath, stringBuilder.ToString());
    }
    public void SavePhrasesConfig(PhrasesConfig config, string filePath)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine("# Module messages '[LR] Module Unusual Kills'");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("# Message displayed for the first kill");
        stringBuilder.AppendLine($"FirstKillMessage: \"{EscapeMessage(config.FirstKillMessage)}\""); 
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("# Color of the message displayed for the first kill");
        stringBuilder.AppendLine($"FirstKillMessageColor : \"{config.FirstKillMessageColor }\"");        
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("# Message displayed for a wallbang kill");
        stringBuilder.AppendLine($"WallbangKillMessage: \"{EscapeMessage(config.WallbangKillMessage)}\""); 
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("# Color of the message displayed for a wallbang kill");
        stringBuilder.AppendLine($"WallbangKillMessageColor : \"{config.WallbangKillMessageColor }\""); 
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("# Message displayed for a no-scope AWP kill");
        stringBuilder.AppendLine($"NoScopeAWPMessage: \"{EscapeMessage(config.NoScopeAWPMessage)}\""); 
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("# Color of the message displayed for a no-scope AWP kill");
        stringBuilder.AppendLine($"NoScopeAWPMessageColor: \"{config.NoScopeAWPMessageColor}\""); 
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("# Message displayed for a kill while running");
        stringBuilder.AppendLine($"KillWhileRunningMessage: \"{EscapeMessage(config.KillWhileRunningMessage)}\""); 
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("# Color of the message displayed for a kill while running");
        stringBuilder.AppendLine($"KillWhileRunningMessageColor: \"{config.KillWhileRunningMessageColor}\""); 
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("# Message displayed for a jump kill");
        stringBuilder.AppendLine($"JumpKillMessage: \"{EscapeMessage(config.JumpKillMessage)}\""); 
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("# Color of the message displayed for a jump kill");
        stringBuilder.AppendLine($"JumpKillMessageColor : \"{config.JumpKillMessageColor }\""); 
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("# Message displayed for a kill while blinded");
        stringBuilder.AppendLine($"BlindKillMessage: \"{EscapeMessage(config.BlindKillMessage)}\""); 
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("# Color of the message displayed for a kill while blinded");
        stringBuilder.AppendLine($"BlindKillMessageColor : \"{config.BlindKillMessageColor }\""); 
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("# Message displayed for a smoke kill");
        stringBuilder.AppendLine($"SmokeKillMessage: \"{EscapeMessage(config.SmokeKillMessage)}\""); 
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("# Color of the message displayed for a smoke kill");
        stringBuilder.AppendLine($"SmokeKillMessageColor : \"{config.SmokeKillMessageColor }\""); 

        File.WriteAllText(filePath, stringBuilder.ToString());
    }
    private string EscapeMessage(string message)
    {
        return message.Replace("\"", "\\\""); 
    }
    public class ModuleConfig
    {
        public int PointsForFirstKill { get; set; } = 1;
        public int PointsForWallbang { get; set; } = 1;
        public int PointsForNoScopeAWP { get; set; } = 2;
        public int PointsForKillWhileRunning { get; set; } = 2;
        public int PointsForJumpKill { get; set; } = 2;
        public int PointsForBlindKill { get; set; } = 2;
        public int PointsForSmokeKill { get; set; } = 5;
    }
    public class PhrasesConfig
    {
        public string FirstKillMessage { get; set; } = "first kill of the round";
        public string FirstKillMessageColor { get; set; } = "{Green}";     
        public string WallbangKillMessage { get; set; } = "wallbang";
        public string WallbangKillMessageColor { get; set; } = "{Green}";    
        public string NoScopeAWPMessage { get; set; } = "no-scope kill";
        public string NoScopeAWPMessageColor { get; set; } = "{Green}";
        public string KillWhileRunningMessage { get; set; } = "kill while running";
        public string KillWhileRunningMessageColor { get; set; } = "{Green}";
        public string JumpKillMessage { get; set; } = "jump kill";
        public string JumpKillMessageColor { get; set; } = "{Olive}";        
        public string BlindKillMessage { get; set; } = "kill while blinded";
        public string BlindKillMessageColor { get; set; } = "{Green}";     
        public string SmokeKillMessage { get; set; } = "kill through smoke";
        public string SmokeKillMessageColor { get; set; } = "{Grey}";     
    }

}
