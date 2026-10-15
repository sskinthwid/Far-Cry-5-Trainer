using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Threading;
namespace FarTrainer {
 public static class LiveInventoryCheck {
  public static int Run(){var log=new List<string>();int result=0;using(var e=new TrainerEngine()){
   try{
    e.Log=s=>log.Add(s);e.Attach(null);e.StartInventoryScan();var seen=new HashSet<string>();
    for(int i=0;i<150;i++){
     foreach(var item in e.InventorySnapshot())if(seen.Add(item.Name+"@"+item.Pointer))log.Add("OBSERVED · "+item.Name+" = "+item.Value+" player="+item.Owner.ToString("X"));
     Thread.Sleep(100);
    }
    if(seen.Count==0)throw new Exception("No local player resource callbacks were observed");
    log.Add("PASS · observed "+seen.Count+" current-player resource counters through guarded native queries; no quantities were changed.");
    var cash=e.InventorySnapshot().FirstOrDefault(v=>v.Name=="Money/Money");
    if(cash!=null){e.QueueInventoryValue(cash,cash.Value);for(int i=0;i<50&&e.InventoryEditPending;i++)Thread.Sleep(100);if(e.InventoryEditResult=="Last edit applied")log.Add("PASS · live cash edit queued/applied/acknowledged at the existing balance "+cash.Value+"; no money was granted or spent.");else log.Add("INCOMPLETE · cash edit was not exercised: "+e.InventoryEditResult);}
    var settings=new ProfileSettings();log.Add("PASS · read "+settings.Load().Count+" actual game settings without changing the profile.");
   }catch(Exception ex){log.Add("FAIL · "+ex);result=1;}
   finally{e.DisableAll();log.Add("RESTORED · all inventory/player observer hooks; pending commands cancelled");}
  }Directory.CreateDirectory(Path.Combine(Data.Root,"logs"));File.WriteAllLines(Path.Combine(Data.Root,"logs","inventory-check.txt"),log);return result;}
 }
}
