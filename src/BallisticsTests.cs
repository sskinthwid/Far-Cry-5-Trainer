using System;using System.Runtime.InteropServices;
namespace FarTrainer {
 public static class BallisticsTests {
  [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr VirtualAlloc(IntPtr at,UIntPtr size,uint allocation,uint protection);
  [DllImport("kernel32.dll")]static extern bool VirtualFree(IntPtr at,UIntPtr size,uint free);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate float Call(IntPtr context);
  public static void Run(Action<string> log){
   IntPtr block=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x5000),0x3000,0x40);if(block==IntPtr.Zero)throw new Exception("Ballistics test allocation failed");long b=block.ToInt64();
   Action<int,long> q=(o,v)=>Marshal.WriteInt64(new IntPtr(b+o),v);Action<int,float> f=(o,v)=>Marshal.Copy(BitConverter.GetBytes(v),0,new IntPtr(b+o),4);Action<int,byte[]> put=(o,a)=>Marshal.Copy(a,0,new IntPtr(b+o),a.Length);
   try{int cases=0;foreach(string id in new[]{"ProjectileSpeedHook","ProjectileDamageHook","JumpSpeedHook","SwimSpeedHook","BowDrawHook","FishingReelHook","FishWeightHook"}){
    bool jump=id=="JumpSpeedHook",swim=id=="SwimSpeedHook",bow=id=="BowDrawHook",damage=id=="ProjectileDamageHook",speed=id=="ProjectileSpeedHook",fishing=id.StartsWith("Fish");
    var code=new Emitter(b);TrainerEngine.EmitBallistics(code,b,b+0x900,id);code.Jump(b+0x700);put(0,code.Finish());
    var thunk=new Emitter(b+0x500);thunk.Hex("53 48 81 EC A0 00 00 00 0F 29 74 24 60 0F 29 7C 24 70 44 0F 29 84 24 80 00 00 00 44 0F 29 A4 24 90 00 00 00");
    thunk.Hex("48 89 CB 48 8B 41 08 48 8B 11 4C 8B 51 18 4C 89 54 24 50 F3 0F 10 41 28 0F 28 F0 0F 28 D0 0F 57 FF");
    thunk.Rip("F3 44 0F 10 25",b+0x908);thunk.Hex(jump||swim?"48 8B 49 10":"48 89 D1");thunk.Jump(b);put(0x500,thunk.Finish());
    var ret=new Emitter(b+0x700);ret.Hex(speed?"41 0F 28 C0":bow||damage?"0F 28 C1":swim?"0F 28 C2":"0F 28 C6");ret.Hex("0F 28 74 24 60 0F 28 7C 24 70 44 0F 28 84 24 80 00 00 00 44 0F 28 A4 24 90 00 00 00 48 81 C4 A0 00 00 00 5B C3");put(0x700,ret.Finish());
    var run=(Call)Marshal.GetDelegateForFunctionPointer(new IntPtr(b+0x500),typeof(Call));
    q(0x1008,b+0x1100);q(0x1800,b+0x1400);q(0x1808,b+0x2000);q(0x1810,b+0x1300);q(0x1818,b+0x1900);q(0x1820,b+0x1800);q(0x1950,b+0x1400);
    f(0x2514,100);f(0x21D0,100);f(0x15D4,100);f(0x1828,100);f(0x908,1);f(0x808,2147483520f);
    for(int scenario=0;scenario<7;scenario++)foreach(float factor in new[]{0.5f,1f,3f}){
     q(0x900,scenario==3?0:b+0x1000);q(0x1110,scenario==4?0:b+0x3000);q(0x1470,scenario==1?b+0x1200:b+0x1100);q(0x1480,scenario==2?b+0x1200:b+0x1100);q(0x1308,scenario==1||scenario==2?b+0x1200:b+0x1100);q(0x1950,scenario==5?0:b+0x1400);q(0x1818,scenario==6?0:b+0x1900);f(0x800,bow?1:factor);
     bool match=fishing||!(scenario==1||scenario==2||scenario==3||scenario==4||damage&&(scenario==5||scenario==6));
     float expected=match?(bow?1:100*factor):100;float actual=run(new IntPtr(b+0x1800));
     if(actual!=expected)throw new Exception(id+" scenario "+scenario+" got "+actual+" expected "+expected);cases++;
    }
   }
   log("PASS · actual x64 ballistic/movement/fishing emitters: "+cases+" player/NPC ownership, absent/dead player, null damage sources, custom factors and original instruction replay.");
   }finally{VirtualFree(block,UIntPtr.Zero,0x8000);}
  }
 }
}
