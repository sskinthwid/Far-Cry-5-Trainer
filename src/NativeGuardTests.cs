using System;using System.Runtime.InteropServices;
namespace FarTrainer {
 public static class NativeGuardTests {
  [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr VirtualAlloc(IntPtr at,UIntPtr size,uint allocation,uint protection);
  [DllImport("kernel32.dll",SetLastError=true)]static extern bool VirtualFree(IntPtr at,UIntPtr size,uint free);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate long Invoke();
  public static void Run(Action<string> log){
   IntPtr block=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x4000),0x3000,0x40);if(block==IntPtr.Zero)throw new InvalidOperationException("Cannot allocate isolated native-test memory.");
   long b=block.ToInt64();Action<int,long> q=(o,v)=>Marshal.WriteInt64(new IntPtr(b+o),v);
   Action<int,float> f=(o,v)=>Marshal.Copy(BitConverter.GetBytes(v),0,new IntPtr(b+o),4);
   Func<int,float> read=o=>{var a=new byte[4];Marshal.Copy(new IntPtr(b+o),a,0,4);return BitConverter.ToSingle(a,0);};
   try{
    var code=new Emitter(b);TrainerEngine.EmitVehicleGuard(code,b,b+0x900);code.Hex("F3 0F 10 88 48 01 00 00");code.Jump(b+0x600);byte[] body=code.Finish();Marshal.Copy(body,0,block,body.Length);
    var thunk=new Emitter(b+0x400);thunk.Hex("48 B8");thunk.Raw(BitConverter.GetBytes(b+0x1400));thunk.Hex("48 B9");thunk.Raw(BitConverter.GetBytes(0x12345678L));thunk.Jump(b);byte[] start=thunk.Finish();Marshal.Copy(start,0,new IntPtr(b+0x400),start.Length);Marshal.Copy(TrainerEngine.Hex("48 89 C8 C3"),0,new IntPtr(b+0x600),4);
    q(0x900,b+0x1000);q(0x1008,b+0x1100);q(0x1110,b+0x2000);q(0x830,b+0x1100);q(0x838,b+0x1200);q(0x1208,b+0x1300);q(0x1408,b+0x1300);f(0x1544,1000);f(0x1548,20);Marshal.WriteByte(new IntPtr(b+0x800),1);
    var run=(Invoke)Marshal.GetDelegateForFunctionPointer(new IntPtr(b+0x400),typeof(Invoke));
    if(run()!=0x12345678L||read(0x1548)!=1000)throw new InvalidOperationException("Current vehicle guard did not protect exactly the matched vehicle or preserve RCX.");
    f(0x1548,20);q(0x1408,b+0x1500);run();if(read(0x1548)!=20)throw new InvalidOperationException("Other vehicle was protected.");
    q(0x1408,b+0x1300);q(0x1208,0);run();if(read(0x1548)!=20)throw new InvalidOperationException("Former vehicle remained protected after exit.");
    q(0x1208,b+0x1600);run();if(read(0x1548)!=20)throw new InvalidOperationException("Former vehicle remained protected after switching rides.");
    q(0x1408,b+0x1600);run();if(read(0x1548)!=1000)throw new InvalidOperationException("Newly occupied vehicle was not protected.");
    f(0x1548,20);Marshal.WriteByte(new IntPtr(b+0x800),0);run();if(read(0x1548)!=20)throw new InvalidOperationException("Disabled protection wrote health.");
    Marshal.WriteByte(new IntPtr(b+0x800),1);q(0x830,b+0x1700);run();if(read(0x1548)!=20)throw new InvalidOperationException("Old player context was accepted.");
    q(0x830,b+0x1100);q(0x1110,0);run();if(read(0x1548)!=20)throw new InvalidOperationException("Destroyed player actor was accepted.");
    log("PASS · actual x64 vehicle guard: occupied car, other car, exit, switch, disabled state, changed player, destroyed actor and register preservation.");
   }finally{VirtualFree(block,UIntPtr.Zero,0x8000);}
  }
 }
}
