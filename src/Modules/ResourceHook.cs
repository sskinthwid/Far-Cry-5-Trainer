using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace FarTrainer
{
    public sealed partial class TrainerEngine
    {
        private Dictionary<string, bool> resourceFlags = ResourceGroups.ToDictionary(p=>p.Id,p=>false);
        internal static readonly ResourceModule[] ResourceGroups = new ResourceModule[] {
            new OrdinaryAmmoModule(),new BaitModule(),new ArrowsModule(),new FlamethrowerModule(),new ArmorPiercingModule()
        }.Concat(FilteredResourceModule.Extra()).Concat(NamedResourceModule.Extra()).ToArray();
        internal bool ResourceActive(string id) { return resourceFlags[id]; }
        internal void SetResource(string id,bool enabled)
        {

                if(!Available(id))throw new InvalidOperationException("No compatible resource hook.");
                var resourcePatch=Patches["ammo"];
                PrepareAmmo(resourcePatch);
                int index=Array.FindIndex(ResourceGroups,g=>g.Id==id);
                bool previous=resourceFlags[id];
                try
                {
                    if(enabled&&!resourcePatch.Active)SetPatch("ammo",true);
                    Memory.Write(resourcePatch.Cave+0x800+index,new[]{enabled?(byte)1:(byte)0});
                    resourceFlags[id]=enabled;
                    if(!resourceFlags.Values.Any(v=>v))SetPatch("ammo",false);
                }
                catch
                {
                    Memory.Write(resourcePatch.Cave+0x800+index,new[]{previous?(byte)1:(byte)0});resourceFlags[id]=previous;
                    if(!resourceFlags.Values.Any(v=>v)&&resourcePatch.Active)SetPatch("ammo",false);
                    throw;
                }
                Say(id+": "+(enabled?"enabled":"restored"));return;
        }
    }
}
