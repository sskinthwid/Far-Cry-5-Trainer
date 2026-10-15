using System;using System.Linq;using System.Runtime.InteropServices;
namespace FarTrainer {
 public static class ResourceBehaviorTests {
  [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr VirtualAlloc(IntPtr at,UIntPtr size,uint allocation,uint protection);
  [DllImport("kernel32.dll")]static extern bool VirtualFree(IntPtr at,UIntPtr size,uint free);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int Call(IntPtr item,uint resource,int amount,int branch);
  public static void Run(Action<string> log){
   IntPtr block=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x4000),0x3000,0x40);if(block==IntPtr.Zero)throw new Exception("Native resource test allocation failed");long b=block.ToInt64();
   Action<long,byte[]> put=(p,a)=>Marshal.Copy(a,0,new IntPtr(p),a.Length);
   try{
    put(b,TrainerEngine.BuildResourceHook(b,b+0x1800-15));
    var t=new Emitter(b+0x1000);t.Hex("53 57 41 54 48 89 CF 41 89 D4 44 89 C3 45 85 C9");t.Jump(b);put(b+0x1000,t.Finish());
    put(b+0x1800,TrainerEngine.Hex("8B 47 4C 41 5C 5F 5B C3"));
    var run=(Call)Marshal.GetDelegateForFunctionPointer(new IntPtr(b+0x1000),typeof(Call));
    var groups=ModuleCatalog.Create().OfType<ResourceModule>().ToList();
    // Registry order is used by the same emitted hook and its runtime flags.
    var ordered=TrainerEngine.ResourceGroups.ToList();
    int cases=0;
    for(int enabled=-1;enabled<ordered.Count;enabled++){
     for(int i=0;i<ordered.Count;i++)Marshal.WriteByte(new IntPtr(b+0x800+i),(byte)(i==enabled?1:0));
     for(int group=0;group<ordered.Count;group++)foreach(uint id in ordered[group].ResourceIds){
      Marshal.WriteInt32(new IntPtr(b+0x204C),10);int result=run(new IntPtr(b+0x2000),id,3,0);
      if(result!=(enabled==group?10:7))throw new Exception("Inventory filter changed an unrelated resource: "+ordered[group].Id);cases++;
     }
     Marshal.WriteInt64(new IntPtr(b+0x2028),b+0x2300);Marshal.WriteInt64(new IntPtr(b+0x2310),b+0x2400);
     foreach(string name in new[]{"Money/Money","Money/MoneyFake","Money_DLCM/DLC_Mars_Silicgel","Medkit/HealthPack","LootItem/CraftingComponents/Nitro","LootItem/CraftingComponents/Blasting Cap","Ammo/NPC_Large"}){
      put(b+0x240C,new byte[160]);put(b+0x240C,System.Text.Encoding.ASCII.GetBytes(name+"\0"));Marshal.WriteInt32(new IntPtr(b+0x204C),10);
      bool match=enabled>=0&&ordered[enabled].ResourceNames.Any(n=>ordered[enabled].NamePrefix?name.StartsWith(n,StringComparison.Ordinal):name==n);
      if(run(new IntPtr(b+0x2000),0x12345678,3,0)!=(match?10:7))throw new Exception("Named resource filter failed: "+name);cases++;
     }
     Marshal.WriteInt64(new IntPtr(b+0x2028),0);
     Marshal.WriteInt32(new IntPtr(b+0x204C),10);if(run(new IntPtr(b+0x2000),0x12345678,3,0)!=7)throw new Exception("Unknown resource was preserved");
     Marshal.WriteInt32(new IntPtr(b+0x204C),2);if(run(new IntPtr(b+0x2000),0x12345678,8,0)!=0)throw new Exception("Consumption was not clamped to inventory");
     Marshal.WriteInt32(new IntPtr(b+0x204C),10);if(run(new IntPtr(b+0x2000),0x12345678,3,1)!=10)throw new Exception("Original conditional branch was not relocated");
    }
    log("PASS · actual x64 inventory hook: "+cases+" filtered cases, all independent toggles, disabled state, unknown IDs, clamping and original branch.");
   }finally{VirtualFree(block,UIntPtr.Zero,0x8000);}
  }
 }
}
