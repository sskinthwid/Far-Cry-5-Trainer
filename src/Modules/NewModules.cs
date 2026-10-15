using System;
using System.Collections.Generic;

namespace FarTrainer
{
    public sealed class AutoTeleportModule : ITrainerModule
    {
        public string Id { get { return "auto-teleport-waypoint"; } }
        public IEnumerable<PatchDefinition> Definitions
        {
            get { return new[] { KnownHooks.Get("CoordHook"), KnownHooks.Get("WaypointHook") }; }
        }
        public bool Available(TrainerEngine engine)
        {
            return engine.PatchAvailable("CoordHook") && engine.PatchAvailable("WaypointHook");
        }
        public string Status(TrainerEngine engine)
        {
            return Available(engine) ? "Compatible" : "Observer required";
        }
        public bool Active(TrainerEngine engine)
        {
            return engine.AutoTeleportWaypoint;
        }
        public void Set(TrainerEngine engine, bool enabled)
        {
            engine.AutoTeleportWaypoint = enabled;
            if (enabled)
            {
                if (!engine.PatchActive("CoordHook")) engine.SetPatch("CoordHook", true);
                if (!engine.PatchActive("WaypointHook")) engine.SetPatch("WaypointHook", true);
            }
        }
    }

    public sealed class OneHitKillModule : ITrainerModule
    {
        public string Id { get { return "one-hit-kill"; } }
        public IEnumerable<PatchDefinition> Definitions
        {
            get { return new[] { KnownHooks.Get("CombatantHealthDecHook"), KnownHooks.Get("AnimalHealthDecHook") }; }
        }
        public bool Available(TrainerEngine engine)
        {
            return engine.PatchAvailable("CombatantHealthDecHook") && engine.PatchAvailable("AnimalHealthDecHook");
        }
        public string Status(TrainerEngine engine)
        {
            return Available(engine) ? "Compatible" : "Combatant hook required";
        }
        public bool Active(TrainerEngine engine)
        {
            return engine.OneHitKill;
        }
        public void Set(TrainerEngine engine, bool enabled)
        {
            engine.OneHitKill = enabled;
            if (enabled)
            {
                engine.Configure("npc-damage", 100);
                engine.Configure("animal-damage", 100);
                if (!engine.PatchActive("CombatantHealthDecHook")) engine.SetPatch("CombatantHealthDecHook", true);
                if (!engine.PatchActive("AnimalHealthDecHook")) engine.SetPatch("AnimalHealthDecHook", true);
            }
            else
            {
                engine.Configure("npc-damage", 1);
                engine.Configure("animal-damage", 1);
                if (engine.PatchActive("CombatantHealthDecHook")) engine.SetPatch("CombatantHealthDecHook", false);
                if (engine.PatchActive("AnimalHealthDecHook")) engine.SetPatch("AnimalHealthDecHook", false);
            }
        }
    }
}
