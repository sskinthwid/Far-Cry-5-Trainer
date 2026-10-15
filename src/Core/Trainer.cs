using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FarTrainer
{
    public sealed class PatchDefinition
    {
        public string Id, Signature, Expected, Replacement, Hook;
        public int Offset;
        public bool StoreHook, AmmoHook;
        public PatchDefinition(string id, string signature, int offset, string expected, string replacement)
        { Id=id; Signature=signature; Offset=offset; Expected=expected; Replacement=replacement; }
    }
    public sealed class ResolvedPatch
    {
        public PatchDefinition Definition;
        public long Address;
        public byte[] Original, EnabledBytes;
        public bool Active;
        public string Status;
        public long Cave;
    }
    public sealed class Feature
    {
        public int Number;
        public string Name, Category, Detail, PatchId, State;
        public Feature(int number, string name, string category, string detail, string patchId)
        { Number=number; Name=name; Category=category; Detail=detail; PatchId=patchId; State=patchId == null ? "Needs hook" : "Not scanned"; }
    }
    public sealed partial class TrainerEngine : IDisposable
    {
        public GameMemory Memory;
        public Dictionary<string, ResolvedPatch> Patches = new Dictionary<string, ResolvedPatch>();
        public Action<string> Log;
        public static byte[] Hex(string text) { return text.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries).Select(s => Convert.ToByte(s,16)).ToArray(); }
        public readonly Dictionary<string,ITrainerModule> Modules = ModuleCatalog.Create().ToDictionary(m=>m.Id);
        public static List<PatchDefinition> Definitions()
        { return ModuleCatalog.Create().SelectMany(m=>m.Definitions).GroupBy(d=>d.Id).Select(g=>g.First()).ToList(); }
        internal bool PatchAvailable(string id)
        { return id != null && Memory != null && Memory.Alive && Patches.ContainsKey(id) && Patches[id].Status.StartsWith("Compatible"); }
        internal bool PatchActive(string id) { return id != null && Patches.ContainsKey(id) && Patches[id].Active; }
        internal string PatchStatus(string id) { return Patches.ContainsKey(id)?Patches[id].Status:"Not scanned"; }
        private void Say(string message) { if (Log != null) Log(message); }
        public void Attach(Action<int> progress)
        {
            if (Memory != null) Detach();
            var memory = new GameMemory();
            try
            {
                memory.Attach();
                var definitions=Definitions();
                var results=memory.Scan(definitions.ToDictionary(d=>d.Id,d=>new Pattern(d.Signature)),progress);
                Patches.Clear();
                foreach (var definition in definitions)
                {
                    var resolved=new ResolvedPatch { Definition=definition, Status="Signature absent" };
                    var addresses=results[definition.Id];
                    if (addresses.Count > 1) resolved.Status="Ambiguous signature (" + addresses.Count + ")";
                    if (addresses.Count == 1)
                    {
                        resolved.Address=addresses[0]+definition.Offset;
                        resolved.Original=Hex(definition.Expected);
                        if (!memory.Read(resolved.Address,resolved.Original.Length).SequenceEqual(resolved.Original)) resolved.Status="Unexpected original bytes";
                        else { resolved.Status="Compatible · gameplay unverified"; if (!definition.StoreHook && !definition.AmmoHook && definition.Hook==null) resolved.EnabledBytes=Hex(definition.Replacement); }
                    }
                    Patches.Add(definition.Id,resolved);
                    Say(definition.Id+": "+resolved.Status+(resolved.Address==0?"":" @ FC_m64.dll+0x"+(resolved.Address-memory.Base).ToString("X")));
                }
                Memory=memory;
                Say("Attached to PID "+Memory.Process.Id+". No features enabled automatically.");
            }
            catch { memory.Dispose(); throw; }
        }
        public bool Available(string id)
        { ITrainerModule module; return Modules.TryGetValue(id ?? "",out module)?module.Available(this):PatchAvailable(id); }
        public string Status(string id)
        { ITrainerModule module; return Modules.TryGetValue(id ?? "",out module)?module.Status(this):id==null?"Not implemented":PatchStatus(id); }
        public bool Active(string id)
        { ITrainerModule module; return Modules.TryGetValue(id ?? "",out module)?module.Active(this):PatchActive(id); }
        public void Set(string id,bool enabled)
        {
            ITrainerModule module;
            if (Modules.TryGetValue(id ?? "",out module)) { module.Set(this,enabled); return; }
            SetPatch(id,enabled);
        }
        internal void SetPatch(string id,bool enabled)
        {
            if (!PatchAvailable(id)) throw new InvalidOperationException("This feature has no compatible resolved hook.");
            var patch=Patches[id]; if (patch.Active==enabled) return;
            if (enabled && patch.Definition.StoreHook) PrepareStore(patch);
            if (enabled && patch.Definition.Hook!=null) PrepareHook(patch);
            byte[] expected=enabled?patch.Original:patch.EnabledBytes;
            if (!Memory.Read(patch.Address,expected.Length).SequenceEqual(expected))
                throw new IOException("Patch bytes changed externally. Refusing to overwrite another modification.");
            // Record ownership before writing so a partially failed write is recoverable.
            if (enabled) patch.Active=true;
            try { Memory.Write(patch.Address,enabled?patch.EnabledBytes:patch.Original); patch.Active=enabled; }
            catch
            {
                if (enabled)
                {
                    try { Memory.Write(patch.Address,patch.Original); patch.Active=false; }
                    catch { Say("RESTORE REQUIRED: "+id); }
                }
                throw;
            }
            Say(id+": "+(enabled?"enabled":"restored"));
        }
        public bool AutoTeleportWaypoint { get; set; }
        public bool OneHitKill { get; set; }

        public Tuple<float, float, float> GetPlayerCoordinates()
        {
            if (Memory == null || !Memory.Alive || !PatchActive("CoordHook")) return null;
            long coord = Captured("CoordHook");
            if (coord == 0) return null;
            try
            {
                byte[] data = Memory.Read(coord + 0x120, 12);
                float x = BitConverter.ToSingle(data, 0);
                float z = BitConverter.ToSingle(data, 4);
                float y = BitConverter.ToSingle(data, 8);
                return Tuple.Create(x, y, z);
            }
            catch { return null; }
        }

        public Tuple<float, float, float> GetWaypointCoordinates()
        {
            if (Memory == null || !Memory.Alive || !PatchActive("WaypointHook")) return null;
            long wp = Captured("WaypointHook");
            if (wp == 0) return null;
            try
            {
                byte[] data = Memory.Read(wp - 0xC, 12);
                float x = BitConverter.ToSingle(data, 0);
                float z = BitConverter.ToSingle(data, 4);
                float y = BitConverter.ToSingle(data, 8);
                return Tuple.Create(x, y, z);
            }
            catch { return null; }
        }

        public void TeleportToWaypoint()
        {
            if (Memory == null || !Memory.Alive) throw new InvalidOperationException("Connect to Far Cry 5 first.");
            if (!PatchActive("CoordHook")) SetPatch("CoordHook", true);
            if (!PatchActive("WaypointHook")) SetPatch("WaypointHook", true);

            long coord = Captured("CoordHook");
            long wp = Captured("WaypointHook");
            if (coord == 0) throw new IOException("Player coordinate reference not yet observed. Move your character in-game.");
            if (wp == 0) throw new IOException("Waypoint reference not yet observed. Place a waypoint on the game map.");

            byte[] wpData = Memory.Read(wp - 0xC, 12);
            float wx = BitConverter.ToSingle(wpData, 0);
            float wz = BitConverter.ToSingle(wpData, 4);
            float wy = BitConverter.ToSingle(wpData, 8);

            if (Math.Abs(wx) < 0.01f && Math.Abs(wz) < 0.01f)
                throw new IOException("No waypoint set. Open your map and set a waypoint first.");
            if (float.IsNaN(wx) || float.IsInfinity(wx) || float.IsNaN(wz) || float.IsInfinity(wz))
                throw new IOException("Invalid waypoint coordinates.");

            float targetY = wy + 2.0f;
            byte[] xBytes = BitConverter.GetBytes(wx);
            byte[] zBytes = BitConverter.GetBytes(wz);
            byte[] yBytes = BitConverter.GetBytes(targetY);

            for (int i = 0; i < 5; i++)
            {
                Memory.Write(coord + 0x120, xBytes);
                Memory.Write(coord + 0x124, zBytes);
                Memory.Write(coord + 0x128, yBytes);
                System.Threading.Thread.Sleep(25);
            }
            Say(string.Format("Teleported to waypoint: ({0:F1}, {1:F1}, {2:F1})", wx, targetY, wz));
        }

        public Tuple<float, float, float> SavedCoordinates { get; set; }

        public void SavePlayerCoordinates()
        {
            if (Memory == null || !Memory.Alive) throw new InvalidOperationException("Connect to Far Cry 5 first.");
            if (!PatchActive("CoordHook")) SetPatch("CoordHook", true);
            var pos = GetPlayerCoordinates();
            if (pos == null) throw new IOException("Player coordinates not yet observed. Move your character in-game.");
            SavedCoordinates = pos;
            Say(string.Format("Saved location: ({0:F1}, {1:F1}, {2:F1})", pos.Item1, pos.Item2, pos.Item3));
        }

        public void TeleportToSavedCoordinates()
        {
            if (Memory == null || !Memory.Alive) throw new InvalidOperationException("Connect to Far Cry 5 first.");
            if (SavedCoordinates == null) throw new InvalidOperationException("No location saved yet. Click Save Position first.");
            if (!PatchActive("CoordHook")) SetPatch("CoordHook", true);
            long coord = Captured("CoordHook");
            if (coord == 0) throw new IOException("Player coordinate reference not yet observed. Move your character in-game.");

            float sx = SavedCoordinates.Item1;
            float sy = SavedCoordinates.Item2 + 0.5f;
            float sz = SavedCoordinates.Item3;
            byte[] xBytes = BitConverter.GetBytes(sx);
            byte[] yBytes = BitConverter.GetBytes(sy);
            byte[] zBytes = BitConverter.GetBytes(sz);

            for (int i = 0; i < 5; i++)
            {
                Memory.Write(coord + 0x120, xBytes);
                Memory.Write(coord + 0x124, zBytes);
                Memory.Write(coord + 0x128, yBytes);
                System.Threading.Thread.Sleep(25);
            }
            Say(string.Format("Teleported to saved location: ({0:F1}, {1:F1}, {2:F1})", sx, sy, sz));
        }

        public void SetInventoryValueDirect(InventoryEntry entry, int value)
        {
            if (value < 0 || value > 1000000000) throw new ArgumentException("Quantity must be between 0 and 1,000,000,000.");
            if (Memory == null || !Memory.Alive) throw new InvalidOperationException("Connect to game first.");
            long health = Captured("HealthHook");
            long owner = health == 0 ? 0 : Q(health + 8);
            if (owner == 0 || entry.Owner != owner) throw new IOException("Player owner changed. Refresh inventory.");
            if (Q(entry.Pointer + 0x28) != entry.Descriptor) throw new IOException("Resource descriptor changed. Refresh inventory.");
            Memory.Write(entry.Pointer + 0x4C, BitConverter.GetBytes(value));
            entry.Value = value;
            Say("Updated " + entry.Name + " = " + value.ToString("N0") + " instantly.");
        }

        public int MultiplyAllDiscoveredResources(int multiplier)
        {
            if (multiplier <= 0 || multiplier > 100000) throw new ArgumentException("Multiplier must be between 1 and 100,000.");
            var snapshot = InventorySnapshot();
            int count = 0;
            foreach (var entry in snapshot)
            {
                long newVal = Math.Min(1000000000L, Math.Max(1L, (long)entry.Value * multiplier));
                try
                {
                    SetInventoryValueDirect(entry, (int)newVal);
                    count++;
                }
                catch { }
            }
            return count;
        }

        public void DisableAll()
        {
            if (Memory==null) return;
            AutoTeleportWaypoint = false;
            OneHitKill = false;
            if (!Memory.Alive) { VehicleEnabled=false;VehicleUserModule=0;vehiclePlayerRef=0;foreach(var p in Patches.Values)p.Active=false; foreach(var key in resourceFlags.Keys.ToArray())resourceFlags[key]=false; return; }
            var errors=new List<string>();
            try{CancelInventoryEdit();}catch(Exception e){errors.Add("inventory edit: "+e.Message);}
            if(VehicleEnabled)try{SetVehicle(false);}catch(Exception e){errors.Add("vehicle-god: "+e.Message);}
            foreach(var key in resourceFlags.Keys.ToArray())
                if(resourceFlags[key])try {Set(key,false);}catch(Exception e){errors.Add(key+": "+e.Message);}
            foreach(var patch in Patches.Values.Where(p=>p.Active).ToArray())
                try { SetPatch(patch.Definition.Id,false); } catch(Exception e) { errors.Add(patch.Definition.Id+": "+e.Message); }
            if (errors.Count>0) throw new IOException(string.Join(Environment.NewLine,errors));
        }
        public void Detach() { DisableAll(); if (Memory!=null) Memory.Dispose(); Memory=null; Patches.Clear();VehicleEnabled=false;VehicleUserModule=0;vehiclePlayerRef=0;AutoTeleportWaypoint=false;OneHitKill=false; }
        public void Dispose() { Detach(); }
    }
}
