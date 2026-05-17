using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud_ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using JetBrains.Annotations;

namespace SmartFaceTarget;

// ReSharper disable ArrangeObjectCreationWhenTypeEvident AutoPropertyCanBeMadeGetOnly.Local ConvertIfStatementToSwitchStatement ForCanBeConvertedToForeach InvertIf LoopCanBeConvertedToQuery RedundantDefaultMemberInitializer
public class Service
{
  [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
  [PluginService] public static IClientState ClientState { get; private set; } = null!;
  [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
  [PluginService] public static ICondition Condition { get; private set; } = null!;
  [PluginService] public static IFramework Framework { get; private set; } = null!;
  [PluginService] public static IGameConfig GameConfig { get; private set; } = null!;
  [PluginService] public static IObjectTable ObjectTable { get; private set; } = null!;
  [PluginService] public static ITargetManager TargetManager { get; private set; } = null!;
}

[UsedImplicitly]
public sealed class Plugin : IDalamudPlugin
{
  private static readonly ConditionFlag[] PlayerBusyConditions =
  [
    ConditionFlag.BeingMoved,
    ConditionFlag.BetweenAreas, ConditionFlag.BetweenAreas51,
    ConditionFlag.ChocoboRacing,
    ConditionFlag.Crafting,
    ConditionFlag.CreatingCharacter,
    ConditionFlag.DutyRecorderPlayback,
    ConditionFlag.EditingPortrait,
    ConditionFlag.EditingStrategyBoard,
    ConditionFlag.Emoting,
    ConditionFlag.ExecutingCraftingAction,
    ConditionFlag.ExecutingGatheringAction,
    ConditionFlag.Fishing,
    ConditionFlag.Gathering,
    ConditionFlag.LoggingOut,
    ConditionFlag.MeldingMateria,
    ConditionFlag.MountImmobile,
    ConditionFlag.Mounting, ConditionFlag.Mounting71,
    ConditionFlag.Occupied, ConditionFlag.Occupied30, ConditionFlag.Occupied33, ConditionFlag.Occupied38, ConditionFlag.Occupied39,
    ConditionFlag.OccupiedInCutSceneEvent,
    ConditionFlag.OccupiedInEvent,
    ConditionFlag.OccupiedInQuestEvent,
    ConditionFlag.OccupiedSummoningBell,
    ConditionFlag.OperatingSiegeMachine,
    ConditionFlag.Performing,
    ConditionFlag.PilotingMech,
    ConditionFlag.PlayingMiniGame,
    ConditionFlag.PreparingToCraft,
    ConditionFlag.ReadyingVisitOtherWorld,
    ConditionFlag.RidingPillion,
    ConditionFlag.RolePlaying,
    ConditionFlag.TradeOpen,
    ConditionFlag.Transformed,
    ConditionFlag.Unconscious,
    ConditionFlag.UsingChocoboTaxi,
    ConditionFlag.UsingFashionAccessory,
    ConditionFlag.WaitingToVisitOtherWorld,
    ConditionFlag.WatchingCutscene, ConditionFlag.WatchingCutscene78
  ];

  private const string PluginCommand = "/sft";

  private const Dalamud_ObjectKind BattleNpc = Dalamud_ObjectKind.BattleNpc;
  private const byte BattleNpcCombatant = (byte)BattleNpcSubKind.Combatant;
  private const long ConfigCheckDelay = 500;
  private const float Deg001 = MathF.PI / 180.0f; // 1 deg (radian)
  private const float Deg180 = MathF.PI; // 180 deg (radian)
  private const float Deg360 = MathF.PI * 2.0f; // 360 deg (radian)
  private const float Dev1Mm = 0.001f; // ~ 1 mm dev by meter (radian)
  private const float Dis1Cm = 0.010936f; // ~ 1 cm (yard)

  private PluginConfig PluginConfig { get; init; }
  private PluginConfigUi PluginConfigUi { get; init; }

  private uint _cfgOptAutoFaceTargetOnAction = 0u;
  private uint _cfgOptKeyboardCameraInterpolationType = 0u;
  private uint _cfgOptLegacyCameraCorrectionFix = 0u;
  private uint _cfgOptMoveMode = 0u;

  private long _lastConfigCheck = 0;
  private long _lastPlayerMove = 0;
  private Vector3 _lastPlayerPosition = Vector3.Zero;
  private float _lastPlayerRotation = 0.0f;
  private long _lastTargetCheck = 0;
  private long _lastTickCount = 0;

  public Plugin(IDalamudPluginInterface pluginInterface)
  {
    pluginInterface.Create<Service>();
    PluginConfig = Service.PluginInterface.GetPluginConfig() as PluginConfig ?? new PluginConfig();
    PluginConfig.Initialize(Service.PluginInterface);
    PluginConfigUi = new PluginConfigUi(PluginConfig);

    Service.PluginInterface.UiBuilder.Draw += OnDraw;
    Service.PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
    Service.Framework.Update += OnUpdate;
    Service.CommandManager.AddHandler(PluginCommand, new CommandInfo(OnCommand)
    {
      HelpMessage = "Opens the SmartFaceTarget settings window."
    });

    SaveConfigOptions();
  }

  public void Dispose()
  {
    RestoreConfigOptions();

    Service.CommandManager.RemoveHandler(PluginCommand);
    Service.Framework.Update -= OnUpdate;
    Service.PluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;
    Service.PluginInterface.UiBuilder.Draw -= OnDraw;
  }

  private void SaveConfigOptions()
  {
    _cfgOptAutoFaceTargetOnAction = Service.GameConfig.UiControl.GetUInt("AutoFaceTargetOnAction");
    _cfgOptKeyboardCameraInterpolationType = Service.GameConfig.UiControl.GetUInt("KeyboardCameraInterpolationType");
    _cfgOptLegacyCameraCorrectionFix = Service.GameConfig.UiConfig.GetUInt("LegacyCameraCorrectionFix");
    _cfgOptMoveMode = Service.GameConfig.UiControl.GetUInt("MoveMode");
  }

  private void RestoreConfigOptions()
  {
    if (!PluginConfig.Active) return;

    Service.GameConfig.UiControl.Set("AutoFaceTargetOnAction", _cfgOptAutoFaceTargetOnAction);
    Service.GameConfig.UiControl.Set("KeyboardCameraInterpolationType", _cfgOptKeyboardCameraInterpolationType);
    Service.GameConfig.UiConfig.Set("LegacyCameraCorrectionFix", _cfgOptLegacyCameraCorrectionFix);
    Service.GameConfig.UiControl.Set("MoveMode", _cfgOptMoveMode);
  }

  private void OnCommand(string command, string args)
  {
    PluginConfigUi.Visible = !PluginConfigUi.Visible;
  }

  private void OnDraw()
  {
    PluginConfigUi.Draw();
  }

  private void OnOpenConfigUi()
  {
    PluginConfigUi.Visible = true;
  }

  private void OnUpdate(IFramework framework)
  {
    _lastTickCount = Environment.TickCount64;

    var player = Service.ObjectTable.LocalPlayer;
    if ((player == null) || player.IsDead) return;
    var playerAddress = player.Address;
    if (playerAddress == IntPtr.Zero) return;

    UpdateGameConfig(playerAddress);

    if (!PluginConfig.Active || (PluginConfig.CombatOnly && !Service.Condition[ConditionFlag.InCombat]) || IsPlayerBusy()) return;

    var target = Service.TargetManager.Target;
    var playerPosition = player.Position;
    var playerRotation = player.Rotation;

    HandleMovement(playerPosition, playerRotation);
    if (PluginConfig.TargetingAngle > 0) HandleTargeting(ref target, playerPosition, playerRotation);
    if (PluginConfig is { RotationAngle: > 0, RotationSpeed: > 0 }) HandleRotation(target, playerAddress, playerPosition, playerRotation);
  }

  private void HandleMovement(Vector3 playerPosition, float playerRotation)
  {
    if ((Vector3.Distance(playerPosition, _lastPlayerPosition) <= Dis1Cm) && (MathF.Abs(playerRotation - _lastPlayerRotation) <= Dev1Mm)) return;

    _lastPlayerPosition = playerPosition;
    _lastPlayerRotation = playerRotation;

    _lastPlayerMove = _lastTickCount;
  }

  private void HandleTargeting(ref IGameObject? target, Vector3 playerPosition, float playerRotation)
  {
    if (((_lastTickCount - _lastTargetCheck) < PluginConfig.TargetingDelay) || ((target == null) && !Service.Condition[ConditionFlag.InCombat]) || ((target != null) && !IsAttackableEnemy(target))) return;

    var halfAngle = (PluginConfig.TargetingAngle / 2.0f) * Deg001;
    var closestEnemy = target;
    var closestDistance = target != null ? MathF.Max(Vector3.Distance(playerPosition, target.Position) - target.HitboxRadius, 0.0f) - (PluginConfig.StickyDistance * Dis1Cm) : float.MaxValue;

    for (var idx = 0; idx < Service.ObjectTable.Length; idx++)
    {
      var gameObject = Service.ObjectTable[idx];
      if ((gameObject == null) || !IsAttackableEnemy(gameObject) || ((target != null) && (gameObject.GameObjectId == target.GameObjectId))) continue;

      var enemyPosition = gameObject.Position;
      var enemyDistance = MathF.Max(Vector3.Distance(playerPosition, enemyPosition) - gameObject.HitboxRadius, 0.0f);
      if (enemyDistance >= closestDistance) continue;

      var enemyRotation = GetObjectRotation(enemyPosition, playerPosition, playerRotation);
      if (MathF.Abs(enemyRotation) > halfAngle) continue;

      closestEnemy = gameObject;
      closestDistance = enemyDistance;
    }

    if (closestEnemy?.GameObjectId != target?.GameObjectId)
    {
      Service.TargetManager.Target = closestEnemy;
      target = closestEnemy;
    }

    _lastTargetCheck = _lastTickCount;
  }

  private unsafe void HandleRotation(IGameObject? target, IntPtr playerAddress, Vector3 playerPosition, float playerRotation)
  {
    if (((_lastTickCount - _lastPlayerMove) < PluginConfig.StationaryTime) || (target == null)) return;

    var halfAngle = (PluginConfig.RotationAngle / 2.0f) * Deg001;
    var targetRotation = GetObjectRotation(target.Position, playerPosition, playerRotation);
    if ((MathF.Abs(targetRotation) > halfAngle) || (MathF.Abs(targetRotation) <= Dev1Mm)) return;

    var playerDestination = NormalizeAngle(playerRotation + targetRotation);
    var rotationSpeed = PluginConfig.RotationSpeed / 1000.0f;
    var onUpdateDelta = (float)Service.Framework.UpdateDelta.TotalSeconds;
    var rotationFactor = 1.0f - MathF.Exp(-rotationSpeed * onUpdateDelta * 60.0f);
    var rotationStep = targetRotation * rotationFactor;
    var smoothTargetRotation = MathF.Abs(targetRotation - rotationStep) <= Dev1Mm ? targetRotation : rotationStep;

    playerRotation = NormalizeAngle(playerRotation + smoothTargetRotation);
    ((GameObject*)playerAddress)->SetRotation(playerRotation);

    _lastPlayerRotation = playerRotation;

    if (!PluginConfig.CameraSync) return;

    var cameraManager = CameraManager.Instance();
    if (cameraManager == null) return;
    var activeCamera = cameraManager->GetActiveCamera();
    if (activeCamera == null) return;

    var cameraDestination = NormalizeAngle(playerDestination + Deg180);
    var cameraDirection = activeCamera->DirH;
    var cameraRotation = NormalizeAngle(cameraDestination - cameraDirection);
    if (MathF.Abs(cameraRotation) <= Dev1Mm) return;

    var cameraStep = cameraRotation * rotationFactor;
    var smoothCameraRotation = MathF.Abs(cameraRotation - cameraStep) <= Dev1Mm ? cameraRotation : cameraStep;

    cameraDirection = NormalizeAngle(cameraDirection + smoothCameraRotation);
    activeCamera->DirH = cameraDirection;
  }

  private unsafe void UpdateGameConfig(IntPtr playerAddress)
  {
    if ((_lastTickCount - _lastConfigCheck) < ConfigCheckDelay) return;

    if (PluginConfig is { Active: true, CameraSync: true })
    {
      const uint autoFaceTargetOnAction = 0u; // OFF
      if (Service.GameConfig.UiControl.GetUInt("AutoFaceTargetOnAction") != autoFaceTargetOnAction) Service.GameConfig.UiControl.Set("AutoFaceTargetOnAction", autoFaceTargetOnAction);
      const uint keyboardCameraInterpolationType = 2u; // OFF
      if (Service.GameConfig.UiControl.GetUInt("KeyboardCameraInterpolationType") != keyboardCameraInterpolationType) Service.GameConfig.UiControl.Set("KeyboardCameraInterpolationType", keyboardCameraInterpolationType);
      const uint legacyCameraCorrectionFix = 1u; // ON
      if (Service.GameConfig.UiConfig.GetUInt("LegacyCameraCorrectionFix") != legacyCameraCorrectionFix) Service.GameConfig.UiConfig.Set("LegacyCameraCorrectionFix", legacyCameraCorrectionFix);
    }

    var moveMode = Service.Condition[ConditionFlag.InCombat] ? PluginConfig.MoveModeInCombat : PluginConfig.MoveModeOutCombat;
    if (moveMode == MoveMode.Default) moveMode = ((Character*)playerAddress)->IsWeaponDrawn ? PluginConfig.MoveModeWithWeapon : PluginConfig.MoveModeWithoutWeapon;
    if ((moveMode != MoveMode.Default) && (Service.GameConfig.UiControl.GetUInt("MoveMode") != (uint)moveMode)) Service.GameConfig.UiControl.Set("MoveMode", (uint)moveMode);

    _lastConfigCheck = _lastTickCount;
  }

  private static float GetObjectRotation(Vector3 objectPosition, Vector3 playerPosition, float playerRotation)
  {
    return NormalizeAngle(MathF.Atan2(objectPosition.X - playerPosition.X, objectPosition.Z - playerPosition.Z) - playerRotation);
  }

  private static unsafe bool IsAttackableEnemy(IGameObject gameObject)
  {
    if (gameObject is not ICharacter { ObjectKind: BattleNpc, SubKind: BattleNpcCombatant, CurrentHp: > 0, IsDead: false, IsTargetable: true } character) return false;

    var characterAddress = character.Address;
    if (characterAddress == IntPtr.Zero) return false;

    return ((Character*)characterAddress)->GetTargetType() == TargetType.Enemy;
  }

  private static bool IsPlayerBusy()
  {
    for (var idx = 0; idx < PlayerBusyConditions.Length; idx++)
    {
      var playerBusyCondition = PlayerBusyConditions[idx];
      if (Service.Condition[playerBusyCondition]) return true;
    }

    return Service.ClientState.IsGPosing;
  }

  private static float NormalizeAngle(float angle)
  {
    angle %= Deg360;

    if (angle > Deg180) angle -= Deg360;
    else if (angle < -Deg180) angle += Deg360;

    return angle;
  }
}