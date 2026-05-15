using Dalamud.Configuration;
using Dalamud.Plugin;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace SmartFaceTarget;

public enum MoveMode
{
  [UsedImplicitly] Standard,
  [UsedImplicitly] Legacy,
  Default
}

[Serializable]
public class PluginConfig : IPluginConfiguration
{
  public int Version { get; set; } = 1;

  public bool Active { get; set; } = true;
  public bool CombatOnly { get; set; } = true;
  public int RotationAngle { get; set; } = 180;
  public int RotationSpeed { get; set; } = 250;
  public int StationaryTime { get; set; } = 250;
  public int StickyDistance { get; set; } = 100;
  public int TargetingAngle { get; set; } = 90;
  public int TargetingDelay { get; set; } = 250;

  public MoveMode MoveModeInCombat { get; set; } = MoveMode.Default;
  public MoveMode MoveModeOutCombat { get; set; } = MoveMode.Default;
  public MoveMode MoveModeWithWeapon { get; set; } = MoveMode.Default;
  public MoveMode MoveModeWithoutWeapon { get; set; } = MoveMode.Default;

  [JsonIgnore] private IDalamudPluginInterface? _pluginInterface;

  public void Initialize(IDalamudPluginInterface pluginInterface)
  {
    _pluginInterface = pluginInterface;
  }

  public void Save()
  {
    _pluginInterface?.SavePluginConfig(this);
  }
}