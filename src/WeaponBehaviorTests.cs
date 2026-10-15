using System;using System.Runtime.InteropServices;
namespace FarTrainer {
 public static class WeaponBehaviorTests {
  [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr VirtualAlloc(IntPtr at,UIntPtr size,uint allocation,uint protection);
  [DllImport("kernel32.dll")]static extern bool VirtualFree(IntPtr at,UIntPtr size,uint free);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate float Call(IntPtr weapon,IntPtr properties,int magazine);
  public static void Run(Action<string> log){
   IntPtr block=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x4000),0x3000,0x40);if(block==IntPtr.Zero)throw new Exception("Native weapon test allocation failed");long b=block.ToInt64();
   Action<int,long> q=(o,v)=>Marshal.WriteInt64(new IntPtr(b+o),v);
   Action<int,float> f=(o,v)=>Marshal.Copy(BitConverter.GetBytes(v),0,new IntPtr(b+o),4);
   Action<int,byte[]> put=(o,a)=>Marshal.Copy(a,0,new IntPtr(b+o),a.Length);
   try{int cases=0;
    foreach(bool magazine in new[]{false,true}){
     var code=new Emitter(b);TrainerEngine.EmitPlayerWeapon(code,b,b+0x900,magazine);code.Jump(b+0x600);put(0,code.Finish());
     var thunk=new Emitter(b+0x400);thunk.Hex("53 48 89 CB 48 89 D0");thunk.Jump(b);put(0x400,thunk.Finish());
     put(0x600,TrainerEngine.Hex(magazine?"F3 0F 2A 83 88 01 00 00 5B C3":"0F 28 C2 5B C3"));
     var run=(Call)Marshal.GetDelegateForFunctionPointer(new IntPtr(b+0x400),typeof(Call));
     q(0x900,b+0x1000);q(0x1008,b+0x1100);q(0x1110,b+0x2000);f(0x1774,600);f(0x808,2147483520f);
     for(int scenario=0;scenario<6;scenario++)foreach(float multiplier in new[]{0.5f,1f,2.5f}){
      q(0x900,scenario==3?0:b+0x1000);q(0x1110,scenario==4?0:b+0x2000);
      q(0x1470,scenario==1||scenario==5?b+0x1300:b+0x1100);q(0x1480,scenario==2||scenario==5?b+0x1300:b+0x1100);
      Marshal.WriteInt32(new IntPtr(b+0x1588),10);f(0x800,multiplier);
      float actual=run(new IntPtr(b+0x1400),new IntPtr(b+0x1700),3);
      float expected=magazine?(scenario==0?10:3):(scenario==0?600*multiplier:600);
      if(actual!=expected)throw new Exception((magazine?"Magazine":"Fire rate")+" player filter failed scenario "+scenario+": "+actual+" expected "+expected);cases++;
     }
    }
    log("PASS · actual x64 weapon hooks: "+cases+" player/NPC/mismatched owner/wielder/missing player/destroyed actor cases, custom fire-rate values and player-only magazines.");
   }finally{VirtualFree(block,UIntPtr.Zero,0x8000);}
  }
 }
}
