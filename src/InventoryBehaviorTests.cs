using System;using System.Runtime.InteropServices;
namespace FarTrainer {
 public static class InventoryBehaviorTests {
  [DllImport("kernel32.dll")]static extern IntPtr VirtualAlloc(IntPtr at,UIntPtr size,uint allocation,uint protection);
  [DllImport("kernel32.dll")]static extern bool VirtualFree(IntPtr at,UIntPtr size,uint free);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate long Call(IntPtr counter,IntPtr context);
  public static void Run(Action<string> log){
   IntPtr block=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x4000),0x3000,0x40);if(block==IntPtr.Zero)throw new Exception("Inventory native test allocation failed");long b=block.ToInt64();
   Action<int,long> q=(o,v)=>Marshal.WriteInt64(new IntPtr(b+o),v);Action<int,byte[]> put=(o,a)=>Marshal.Copy(a,0,new IntPtr(b+o),a.Length);
   try{
    var c=new Emitter(b);TrainerEngine.EmitInventoryQuery(c,b,b+0x900);c.Jump(b+0x600);put(0,c.Finish());
    var t=new Emitter(b+0x400);t.Hex("57 48 89 C8 48 89 D7");t.Jump(b);put(0x400,t.Finish());put(0x600,TrainerEngine.Hex("5F C3"));
    var run=(Call)Marshal.GetDelegateForFunctionPointer(new IntPtr(b+0x400),typeof(Call));
    q(0x900,b+0x1000);q(0x1008,b+0x1100);q(0x1110,b+0x2000);q(0x1308,b+0x1100);q(0x1528,b+0x2200);q(0x1540,b+0x2300);
    for(int scenario=0;scenario<7;scenario++){
     q(0x900,scenario==3?0:b+0x1000);q(0x1110,scenario==4?0:b+0x2000);q(0x1308,scenario==1?b+0x1200:b+0x1100);
     q(0xE00,b+0x1500);q(0xE08,scenario==2?b+0x2400:b+0x2200);q(0xE10,scenario==5?b+0x1200:b+0x1100);Marshal.WriteInt32(new IntPtr(b+0xE18),123);Marshal.WriteInt32(new IntPtr(b+0xE1C),scenario==6?4:5);Marshal.WriteInt32(new IntPtr(b+0x154C),5);
     if(run(new IntPtr(b+0x1500),new IntPtr(b+0x1300))!=b+0x2300)throw new Exception("Inventory query original load was corrupted");
     if(Marshal.ReadInt32(new IntPtr(b+0x154C))!=(scenario==0?123:5))throw new Exception("Queued inventory edit accepted invalid ownership/descriptor scenario "+scenario);
     if(scenario==0&&Marshal.ReadInt64(new IntPtr(b+0xE00))!=0)throw new Exception("Inventory command was not acknowledged");
     if(scenario==6&&(Marshal.ReadInt64(new IntPtr(b+0xE00))!=0||Marshal.ReadInt32(new IntPtr(b+0xE30))!=2))throw new Exception("Stale quantity was not rejected and acknowledged");
    }
    if(TrainerEngine.EditableResource("Ammo/NPC_Large")||TrainerEngine.EditableResource("Intel/CultMap")||!TrainerEngine.EditableResource("Money/Money"))throw new Exception("Inventory editor resource allowlist failed");
    log("PASS · actual x64 inventory editor: player query, NPC context, changed descriptor, missing player, destroyed actor, changed owner, stale quantity rejection, acknowledgement and original getter replay.");
   }finally{VirtualFree(block,UIntPtr.Zero,0x8000);}
  }
 }
}
