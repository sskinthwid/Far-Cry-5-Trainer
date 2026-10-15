using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
namespace FarTrainer
{
    public static class ModuleCatalog
    {
        public static List<ITrainerModule> Create()
        {
            var modules=new List<ITrainerModule> {
                new SyringeBuffModule(),new StoreUnlockModule(),new FlareModule(),
                new ScopedSwayModule(),new BaitModule(),
                new ArrowsModule(),new FlamethrowerModule(),new ArmorPiercingModule(),
                new OrdinaryAmmoModule(),new SpreadModule(),new NoReloadModule(),new PhotoCameraRangeModule()
            };
            modules.AddRange(ExtraModules());
            modules.AddRange(FilteredResourceModule.Extra());
            modules.AddRange(NamedResourceModule.Extra());
            modules.Add(new VehicleProtectionModule());
            modules.Add(new RapidFireModule());
            modules.AddRange(BallisticsModules.Create());
            return modules;
        }
        private static IEnumerable<ITrainerModule> ExtraModules(){
            yield return H("observer-health",new[]{"HealthHook"});
            yield return new HookModule("observer-resource",new[]{new PatchDefinition("ResourceQueryHook","48 85 C0 74 ? 48 8D 48 40 48 8B 40 40 FF ? ? EB ? 48 ? ? 48",5,"48 8D 48 40 48 8B 40 40",null){Hook="ResourceQueryHook"}},"HealthHook");
            yield return H("observer-vehicle",new[]{"VehicleHealthHook"});
            yield return H("observer-coordinate",new[]{"CoordHook"});
            yield return H("observer-axes",new[]{"PrincipalAxesHook"});
            yield return H("observer-time",new[]{"TimeHook"});
            yield return H("observer-waypoint",new[]{"WaypointHook"});
            yield return H("health",new[]{"HealthDecHook"},"HealthHook");
            yield return H("no-fall-damage",new[]{"HealthDecFallHook","HealthDecWingsuitHook"},"HealthHook");
            yield return H("no-ragdoll",new[]{"FallHeightLimitHook"});
            yield return new HookModule("stealth",new[]{KnownHooks.Get("DetectionIncHook")});
            yield return H("move-speed",new[]{"MoveSpeedHook"});
            yield return H("energy-overheat",new[]{"EnergyOverheatHook"});
            yield return new AutoTeleportModule();
            yield return new OneHitKillModule();
            yield return H("recoil",new[]{"RecoilHook"},"HealthHook");
            yield return new HookModule("mission-timer",new[]{KnownHooks.Get("MissionTimerHook")});
            yield return H("armor",new[]{"BodyArmorDecHook"},"HealthHook");
            yield return H("stamina",new[]{"StaminaWrtHook"},"HealthHook");
            yield return H("oxygen",new[]{"OxygenDecHook"});
            yield return H("speed",new[]{"DeltaMulHook"});
            yield return H("xp",new[]{"XpIncHook"});
            yield return H("perk-reward",new[]{"PerkPointsIncHook"});
            yield return H("pickup",new[]{"AmmoIncHook"});
            yield return H("capacity",new[]{"AmmoCapHook"});
            yield return H("buff-duration",new[]{"DrugTimerStartHook","DrugTimerStart2Hook"});
            yield return H("time-scale",new[]{"TimeIncHook"},"TimeHook");
            yield return H("interaction-delay",new[]{"ActionHoldHook"});
            yield return H("turret-heat",new[]{"TurretHeatIncHook"});
            yield return H("aircraft-bombs",new[]{"AircraftBombsDecHook"});
            yield return H("gravity-belt",new[]{"GravityBeltFuelDecHook"});
            yield return H("npc-damage",new[]{"CombatantHealthDecHook"});
            yield return H("npc-armor",new[]{"CombatantBodyArmorDecHook"});
            yield return H("animal-damage",new[]{"AnimalHealthDecHook"});
            yield return H("perk-spend",new[]{"PerkPointsDecHook"});
        }
        private static HookModule H(string id,string[] names,params string[] dependencies){return new HookModule(id,System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(names,n=>KnownHooks.Get(n))),dependencies);}
    }
}
