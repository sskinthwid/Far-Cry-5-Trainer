using System;using System.Collections.Generic;using System.IO;
namespace FarTrainer {
 public sealed class VehicleProtectionModule : ITrainerModule {
  public string Id{get{return "vehicle-god";}}public IEnumerable<PatchDefinition> Definitions{get{return new PatchDefinition[0];}}
  public bool Available(TrainerEngine e){return e.PatchAvailable("HealthHook")&&e.PatchAvailable("VehicleHealthHook");}
  public bool Active(TrainerEngine e){return e.VehicleEnabled;}
  public string Status(TrainerEngine e){return Available(e)?e.VehicleEnabled?(e.VehicleUserModule!=0?"Armed · current-vehicle identity checked inside hook":"Waiting for the player component"):"Compatible · gameplay unverified":e.PatchStatus("VehicleHealthHook");}
  public void Set(TrainerEngine e,bool enabled){e.SetVehicle(enabled);}
 }
 public sealed partial class TrainerEngine {
  public bool VehicleEnabled;public long VehicleUserModule;private long vehiclePlayerRef;
  private long Q(long address){return BitConverter.ToInt64(Memory.Read(address,8),0);}
  public long CurrentVehicleReference {get{try{return VehicleUserModule!=0?Q(VehicleUserModule+8):0;}catch{return 0;}}}
  internal void SetVehicle(bool enabled){
   if(enabled){if(!PatchAvailable("HealthHook")||!PatchAvailable("VehicleHealthHook"))throw new InvalidOperationException("No compatible vehicle/player hooks.");SetPatch("HealthHook",true);SetPatch("VehicleHealthHook",true);}
   var p=Patches["VehicleHealthHook"];if(p.Cave!=0&&Memory!=null&&Memory.Alive)Memory.Write(p.Cave+0x800,new[]{enabled?(byte)1:(byte)0});
   VehicleEnabled=enabled;if(enabled)RefreshVehicleTarget();Say("Current vehicle protection "+(enabled?"armed":"off"));
  }
  // The game exposes GetPawnVehicle through CRidableUserModule. Resolve that module
  // in the local player's component list; never use whichever car was last processed.
  public void RefreshVehicleTarget(){
   if(!VehicleEnabled||Memory==null||!Memory.Alive)return;
   var p=Patches["VehicleHealthHook"];long user=0,owner=0;
   try{
    long health=Captured("HealthHook");if(health!=0){owner=Q(health+8);if(owner!=0){long entity=Q(owner+0x10);if(entity!=0){
     long components=Q(entity+0xA8);int count=(int)((Q(entity+0xB0)>>32)&0x7FFFFFFF);if(count<0||count>256)throw new IOException("Invalid player component list.");
     if(owner==vehiclePlayerRef&&VehicleUserModule!=0)user=VehicleUserModule;
     else for(int i=0;i<count&&user==0;i++){
      long component=Q(components+i*8);if(component==0)continue;
      try{
       if(Q(component+8)!=owner)continue;
       long modules=Q(component+0xA8);int size=(int)((Q(component+0xB0)>>32)&0x7FFFFFFF);if(modules==0||size<=0||size>64)continue;
       for(int j=0;j<size;j++){
        // Stable engine type hash of CRidableUserModule, confirmed from its registry.
        if(BitConverter.ToUInt32(Memory.Read(modules+j*16,4),0)!=0x3940E81D)continue;
        long candidate=Q(modules+j*16+8);
        // In this build the user module is embedded in CPawnPlayer at +E8.
        // Both map membership and this layout must agree before arming the hook.
        if(candidate==component+0xE8){user=candidate;break;}
       }
      }catch{continue;}
     }
    }}}
   }catch{user=0;owner=0;}
   if(user!=VehicleUserModule||owner!=vehiclePlayerRef){
    // Clear first, then update identity, then publish. The game can only see a complete pair.
    Memory.Write(p.Cave+0x838,BitConverter.GetBytes(0L));
    Memory.Write(p.Cave+0x830,BitConverter.GetBytes(owner));
    Memory.Write(p.Cave+0x838,BitConverter.GetBytes(user));VehicleUserModule=user;vehiclePlayerRef=owner;
   }
  }
 }
}
