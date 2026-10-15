using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Threading;
namespace FarTrainer {
 public static class LiveWeaponCheck {
  public static int Run(){var log=new List<string>();int result=0;
   using(var e=new TrainerEngine()){
    e.Log=s=>log.Add(s);
    try{
     e.Attach(null);e.Configure("rapid-fire",2);e.Set("rapid-fire",true);e.Set("no-reload",true);
     long lastRate=-1,lastMag=-1;
     for(int i=0;i<450;i++){
      if(!e.Memory.Alive)throw new Exception("Game exited during weapon check");
      long rate=e.Captured("WeaponFireRateHook",0x10),mag=e.Captured("no-reload",0x10);
      if(rate!=lastRate||mag!=lastMag){
       var p=e.Patches["WeaponFireRateHook"];float before=BitConverter.ToSingle(e.Memory.Read(p.Cave+0x828,4),0),after=BitConverter.ToSingle(e.Memory.Read(p.Cave+0x82C,4),0);
       long weapon=e.Captured("no-reload");int quantity=weapon==0?-1:BitConverter.ToInt32(e.Memory.Read(weapon+0x188,4),0);
       log.Add(DateTime.Now.ToString("O")+" rate calls="+rate+" base="+before+" scaled="+after+" magazine calls="+mag+" magazine="+quantity+" player weapon="+weapon.ToString("X"));lastRate=rate;lastMag=mag;
      }
      Thread.Sleep(100);
     }
     if(lastRate==0||lastMag==0)log.Add("INCOMPLETE · one or both player weapon hooks were not exercised; gameplay verification requires firing.");
     else log.Add("PASS · live player-owned weapon callbacks observed; 2x base rate and magazine suppression exercised. Animation-limited firing cadence and all weapon classes are not established by this check.");
    }catch(Exception ex){log.Add("FAIL · "+ex);result=1;}
    finally{try{e.DisableAll();log.Add("RESTORED · all owned hooks");}catch(Exception ex){log.Add("RESTORE ERROR · "+ex);result=1;}}
   }
   Directory.CreateDirectory(Path.Combine(Data.Root,"logs"));File.WriteAllLines(Path.Combine(Data.Root,"logs","weapon-check.txt"),log);return result;
  }
 }
}
