using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
namespace FarTrainer {
 public static class Validation {
  static void Require(bool c,string m){if(!c)throw new InvalidOperationException(m);}
  public static int Run(bool roundtrip){
   var output=new List<string>();Action<string> log=s=>output.Add(s);int result=0;
   try{
    // Compare the optimized wildcard matcher to a simple specification, including overlaps.
    var rand=new Random(1687);
    for(int test=0;test<1500;test++){
     int n=rand.Next(1,200),k=rand.Next(1,22);var bytes=new byte[n];rand.NextBytes(bytes);for(int i=0;i<n;i++)bytes[i]=(byte)(bytes[i]%8);
     var tokens=Enumerable.Range(0,k).Select(i=>rand.Next(3)==0?"?":rand.Next(8).ToString("X2")).ToArray();tokens[rand.Next(k)]="03";var pattern=new Pattern(string.Join(" ",tokens));var expected=new List<int>();
     for(int i=0;i<=n-k;i++)if(Enumerable.Range(0,k).All(j=>!pattern.Bytes[j].HasValue||pattern.Bytes[j].Value==bytes[i+j]))expected.Add(i);
     Require(pattern.Find(bytes,n).SequenceEqual(expected),"Wildcard matcher differs from specification.");
    }
    log("PASS · 1,500 randomized wildcard scanner comparisons, bounds and overlaps.");
    NativeGuardTests.Run(log);
    ResourceBehaviorTests.Run(log);
    WeaponBehaviorTests.Run(log);BallisticsTests.Run(log);ConfigTests.Run(log);
    InventoryBehaviorTests.Run(log);SettingsBehaviorTests.Run(log);
    var em=new Emitter(0x100000);em.Branch("0F 85","end");em.Hex("90 90");em.Label("end");em.Rip("48 8B 05",0x101000);byte[] code=em.Finish();Require(BitConverter.ToInt32(code,2)==2,"Conditional relocation failed.");Require(BitConverter.ToInt32(code,11)==0xFF1,"RIP relocation failed.");log("PASS · conditional and RIP-relative relocation targets.");
    var catalog=Data.Catalog();Require(catalog.Select(m=>m.Id).Distinct().Count()==catalog.Count,"Duplicate catalogue IDs.");
    Require(catalog.All(m=>m.Kind=="Runtime"&&m.Runtime!=null),"The trainer must contain only implemented native controls.");
    using(var e=new TrainerEngine()){
     foreach(var m in catalog.Where(m=>m.Runtime!=null))Require(e.Modules.ContainsKey(m.Runtime),"Missing native module: "+m.Runtime);
     foreach(var m in catalog.Where(m=>m.Runtime!=null))e.Configure(m.Runtime,m.Default);
     foreach(string id in new[]{"health","armor","stamina","oxygen"}){Require(string.IsNullOrEmpty(catalog.Single(m=>m.Id==id).Unit),"God/infinite controls must be switches");e.Configure(id,99);Require(e.Values[id]==0,"A saved multiplier changed a boolean control");}
     foreach(double bad in new[]{double.NaN,double.PositiveInfinity,-1.0}){bool rejected=false;try{e.Configure("speed",bad);}catch(ArgumentException){rejected=true;}Require(rejected,"Invalid multiplier accepted.");}
     log("PASS · catalogue bindings and rejection of invalid numeric values.");
     if(Environment.GetCommandLineArgs().Contains("--diagnose")){
      e.Log=log;e.Attach(null);log("Engine SHA256: "+e.Memory.Fingerprint());
      if(roundtrip){
       // Exclude store records and reward/capacity hooks which can alter persistent progress.
       var excluded=new[]{"store","xp","perk-reward","pickup","capacity","perk-spend","buff-duration","fish-weight"};
       try{
        foreach(var m in catalog.Where(m=>m.Runtime!=null&&!excluded.Contains(m.Runtime))){
         if(!e.Available(m.Runtime)){log("SKIP · "+m.Runtime+": "+e.Status(m.Runtime));continue;}
         // Test multiplier hooks at neutral 1 to avoid changing player progress or physics.
         if(!string.IsNullOrEmpty(m.Unit))e.Configure(m.Runtime,1);
         e.Set(m.Runtime,true);Require(e.Active(m.Runtime),"Activation state failed: "+m.Runtime);Thread.Sleep(25);
         if(m.Runtime=="vehicle-god"){Thread.Sleep(150);e.RefreshVehicleTarget();Thread.Sleep(150);log("Vehicle tracker: module="+e.VehicleUserModule.ToString("X")+" current reference="+e.CurrentVehicleReference.ToString("X")+" protected updates="+e.Captured("VehicleHealthHook",0x30));}
         e.Set(m.Runtime,false);Require(!e.Active(m.Runtime),"Restore state failed: "+m.Runtime);
         Require(e.Memory.Alive,"Game exited during "+m.Runtime);log("PASS · "+m.Runtime+" enable/readback/restore; process alive.");
        }
        e.Set("arrows",true);e.Set("flame",true);e.Set("arrows",false);Require(e.Active("flame")&&!e.Active("arrows"),"Resource flags are not independent.");e.Set("flame",false);log("PASS · independent resource flags.");
       }finally{e.DisableAll();}
       foreach(var p in e.Patches.Values.Where(p=>p.Original!=null&&p.Address!=0))Require(e.Memory.Read(p.Address,p.Original.Length).SequenceEqual(p.Original),"Original instructions not restored: "+p.Definition.Id);
       log("PASS · every resolved instruction restored to original bytes.");
       foreach(var p in e.Patches.Values.Where(p=>p.Cave!=0)){string dir=Path.Combine(Data.Root,"logs","emitted");Directory.CreateDirectory(dir);File.WriteAllBytes(Path.Combine(dir,p.Definition.Id+".bin"),e.Memory.Read(p.Cave,0x830));}
      }
      log("Gameplay effects are not established by instruction roundtrips. Persistent store and reward changes were excluded.");
     }
    }
   }catch(Exception e){output.Add("FAIL · "+e);result=1;}
   finally{Directory.CreateDirectory(Path.Combine(Data.Root,"logs"));File.WriteAllLines(Path.Combine(Data.Root,"logs",Environment.GetCommandLineArgs().Contains("--diagnose")?"diagnostics.txt":"self-test.txt"),output);}
   return result;
  }
 }
}

