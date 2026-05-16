using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace SmartFaceTarget;

public class PluginConfigUi(PluginConfig pluginConfig)
{
  public bool Visible
  {
    get => _visible;
    set => _visible = value;
  }

  private bool _visible;

  public void Draw()
  {
    if (Visible) DrawWindow();
  }

  private void DrawWindow()
  {
    try
    {
      ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.FirstUseEver, new Vector2(0.5f, 0.5f));
      ImGui.SetNextWindowSize(new Vector2(575, 415), ImGuiCond.Always);
      if (!ImGui.Begin("SmartFaceTarget Settings", ref _visible, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) return;

      ImGui.Spacing();

      DrawCheckbox("Active", pluginConfig.Active, value => pluginConfig.Active = value);
      DrawCheckbox("Camera Sync", pluginConfig.CameraSync, value => pluginConfig.CameraSync = value);
      DrawCheckbox("Combat Only", pluginConfig.CombatOnly, value => pluginConfig.CombatOnly = value);

      ImGui.Spacing();
      ImGui.Separator();
      ImGui.Spacing();

      DrawSliderInt("Rotation Angle (deg)", pluginConfig.RotationAngle, 0, 360, value => pluginConfig.RotationAngle = value);
      DrawSliderInt("Rotation Speed (x/1000)", pluginConfig.RotationSpeed, 0, 1000, value => pluginConfig.RotationSpeed = value);
      DrawSliderInt("Stationary Time (ms)", pluginConfig.StationaryTime, 0, 1000, value => pluginConfig.StationaryTime = value);

      ImGui.Spacing();
      ImGui.Separator();
      ImGui.Spacing();

      DrawSliderInt("Sticky Distance (cm)", pluginConfig.StickyDistance, 0, 1000, value => pluginConfig.StickyDistance = value);
      DrawSliderInt("Targeting Angle (deg)", pluginConfig.TargetingAngle, 0, 360, value => pluginConfig.TargetingAngle = value);
      DrawSliderInt("Targeting Delay (ms)", pluginConfig.TargetingDelay, 0, 1000, value => pluginConfig.TargetingDelay = value);

      ImGui.Spacing();
      ImGui.Separator();
      ImGui.Spacing();

      DrawComboEnum("Move Mode In Combat", pluginConfig.MoveModeInCombat, value => pluginConfig.MoveModeInCombat = value);
      DrawComboEnum("Move Mode Out Combat", pluginConfig.MoveModeOutCombat, value => pluginConfig.MoveModeOutCombat = value);
      DrawComboEnum("Move Mode With Weapon", pluginConfig.MoveModeWithWeapon, value => pluginConfig.MoveModeWithWeapon = value);
      DrawComboEnum("Move Mode Without Weapon", pluginConfig.MoveModeWithoutWeapon, value => pluginConfig.MoveModeWithoutWeapon = value);

      ImGui.Spacing();
    }
    finally
    {
      ImGui.End();
    }
  }

  private void DrawCheckbox(string label, bool refValue, Action<bool> setValue)
  {
    var value = refValue;
    if (!ImGui.Checkbox(label, ref value)) return;

    setValue(value);
    pluginConfig.Save();
  }

  private void DrawComboEnum<T>(string label, T refValue, Action<T> setValue) where T : Enum
  {
    var value = Convert.ToInt32(refValue);
    var values = Enum.GetNames(typeof(T));
    if (!ImGui.Combo(label, ref value, values, values.Length)) return;

    setValue((T)Enum.ToObject(typeof(T), value));
    pluginConfig.Save();
  }

  private void DrawSliderInt(string label, int refValue, int vMin, int vMax, Action<int> setValue)
  {
    var value = refValue;
    if (!ImGui.SliderInt(label, ref value, vMin, vMax)) return;

    setValue((int)MathF.Round(value / 5.0f, MidpointRounding.AwayFromZero) * 5);
    pluginConfig.Save();
  }
}