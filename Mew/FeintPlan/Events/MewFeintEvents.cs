using Dalamud.Game.ClientState.Objects.Types;

namespace Mew.FeintPlan.Events;

public interface IMewFeintEvent { }

public sealed record EnemyCastSpellCondParams(uint SpellId, float TotalCastTimeInSec) : IMewFeintEvent;

public sealed record ReceviceAbilityEffectCondParams(uint ActionId) : IMewFeintEvent;

public sealed record SelfActionEffectCondParams(uint ActionId) : IMewFeintEvent;

public sealed record WeatherChangedCondParams(uint NewWeatherId) : IMewFeintEvent;

public sealed record UnitCreateCondParams(IGameObject? BattleChara) : IMewFeintEvent;
