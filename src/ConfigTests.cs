using System;using System.Collections.Generic;using System.IO;using System.Linq;
namespace FarTrainer {
 public static class ConfigTests {
  public static void Run(Action<string> log){
   string dir=Path.Combine(Path.GetTempPath(),"HopeControl-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);string path=Path.Combine(dir,"config.json");
   try{foreach(string name in new[]{"Exploration","Precision","Ballistics"})ConfigStore.Validate(ConfigStore.Starter(name));var c=new TrainerConfig{Version=1,Name="Exploration",Notes="Test",Values=Data.Catalog().ToDictionary(m=>m.Id,m=>m.Default),Enabled=new List<string>{"health","projectile-speed"},Favorites=new List<string>{"speed"}};
    ConfigStore.Write(path,c);var loaded=ConfigStore.Read(path);if(loaded.Name!=c.Name||loaded.Values["projectile-speed"]!=2)throw new Exception("Config roundtrip failed");
    c.Name="Changed";ConfigStore.Write(path,c);if(!File.Exists(path+".bak")||ConfigStore.Read(path+".bak").Name!="Exploration")throw new Exception("Config backup failed");
    foreach(string bad in new[]{"unknown","nan","zero","toggle","version"}){var x=ConfigStore.Read(path);if(bad=="unknown")x.Enabled.Add("unsupported");if(bad=="nan")x.Values["speed"]=double.NaN;if(bad=="zero")x.Values["projectile-speed"]=0;if(bad=="toggle")x.Values["health"]=1;if(bad=="version")x.Version=2;bool rejected=false;try{ConfigStore.Validate(x);}catch{rejected=true;}if(!rejected)throw new Exception("Config accepted "+bad);}
    if(InventoryActions.Target(120,80,true)!=200||InventoryActions.Target(120,80,false)!=80||InventoryActions.Target(120,-20,true)!=100)throw new Exception("Inventory arithmetic failed");
    foreach(int delta in new[]{int.MaxValue,-121}){bool rejected=false;try{InventoryActions.Target(120,delta,true);}catch(ArgumentException){rejected=true;}if(!rejected)throw new Exception("Invalid inventory sum accepted");}
    log("PASS · config JSON roundtrip, backups, unknown IDs/NaN/zero-speed/toggle/schema rejection; Add/Set/Subtract inventory arithmetic and overflow bounds.");
   }finally{foreach(string file in Directory.GetFiles(dir))File.Delete(file);Directory.Delete(dir);}
  }
 }
}
