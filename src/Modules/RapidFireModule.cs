using System.Collections.Generic;
namespace FarTrainer {
 public sealed class RapidFireModule : HookModule {
  public RapidFireModule():base("rapid-fire",new[]{new PatchDefinition("WeaponFireRateHook","48 8B 81 08 01 00 00 48 85 C0 74 04 48 83 C0 18 F3 0F 10 50 74 45 31 C9 41 8D 51 0E",16,"F3 0F 10 50 74",null){Hook="WeaponFireRateHook"}},new[]{"HealthHook"}){}
 }
}
