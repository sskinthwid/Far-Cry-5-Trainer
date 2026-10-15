using System;using System.Collections.Generic;using System.IO;using System.Linq;using System.Threading;
namespace FarTrainer {
 public static class LiveBallisticsCheck {
  public static int Run(){var log=new List<string>();int result=0;using(var e=new TrainerEngine()){
   try{e.Log=s=>log.Add(s);e.Attach(null);string[] ids={"projectile-speed","projectile-damage","jump-speed","swim-speed","bow-draw","fishing-reel","fish-weight"};
    foreach(string id in ids){e.Configure(id,id=="projectile-speed"?2:1);e.Set(id,true);}
    for(int i=0;i<600;i++){if(!e.Memory.Alive)throw new Exception("Game exited during test");Thread.Sleep(100);}
    foreach(string id in ids){var p=e.Modules[id].Definitions.First();var hook=e.Patches[p.Id];long calls=e.Captured(p.Id,0x10);float before=BitConverter.ToSingle(e.Memory.Read(hook.Cave+0x828,4),0),after=BitConverter.ToSingle(e.Memory.Read(hook.Cave+0x82C,4),0);log.Add((calls>0?"OBSERVED":"NOT EXERCISED")+" · "+id+" callbacks="+calls+" base="+before+" result="+after+" matched object="+e.Captured(p.Id).ToString("X"));if(calls>0&&id=="projectile-speed"&&after!=before*2)throw new Exception("Projectile multiplier telemetry mismatch");}
    log.Add("Callback telemetry validates the matching calculation, not all projectile classes, animations or final gameplay outcomes. Damage, jump, swim, reel and fish-weight checks used neutral 1x.");
   }catch(Exception ex){log.Add("FAIL · "+ex);result=1;}finally{e.DisableAll();log.Add("RESTORED · all ballistic/player hooks");}
  }Directory.CreateDirectory(Path.Combine(Data.Root,"logs"));File.WriteAllLines(Path.Combine(Data.Root,"logs","ballistics-check.txt"),log);return result;}
 }
}
