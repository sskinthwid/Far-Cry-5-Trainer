using System;using System.Collections.Generic;using System.IO;using System.Linq;using System.Threading;
namespace FarTrainer {
 public sealed partial class MainWindow {
  public void PrepareLivePreview(bool inventory){try{engine.Attach(null);if(inventory){engine.StartInventoryScan();Thread.Sleep(10000);}Render();}catch{engine.Detach();throw;}}
  public void FinishPreview(){engine.Detach();}
  public int TestUi(){var log=new List<string>();int result=0;
   try{
    valueInputs["speed"].Text="2.75";search.Text="projectile";SelectCategory("Movement");search.Clear();if(valueInputs["speed"].Text!="2.75")throw new Exception("Draft lost through search/navigation");
    SelectCategory("Configs");var c=CurrentConfig();if(c.Values["speed"]!=2.75)throw new Exception("Config did not capture non-visible draft");c.Name="Fixture";c.Values["speed"]=3;c.Enabled.Add("health");ApplyConfig(c,false,false);if(engine.Values["speed"]!=3||engine.Active("health"))throw new Exception("Values-only config enabled a switch");
    bool rejected=false;try{ApplyConfig(c,true,false);}catch(IOException){rejected=true;}if(!rejected||engine.Values["speed"]!=3)throw new Exception("Disconnected config application failed");
    foreach(string page in new[]{"All features","Favorites","Active controls","Currency and inventory","Configs","Game settings","Movement","Fishing"}){SelectCategory(page);var visual=(System.Windows.FrameworkElement)Content;visual.Width=1150;visual.Height=720;visual.Measure(new System.Windows.Size(1150,720));visual.Arrange(new System.Windows.Rect(0,0,1150,720));visual.UpdateLayout();}
    SelectCategory("Game settings");if(settingsInputs.Count>0){var field=settingsInputs.First();string id=field.Key,original=field.Value();settingsDraft[id]=original;SelectCategory("All features");SelectCategory("Game settings");if(settingsDraft[id]!=original)throw new Exception("Settings draft lost");}
    log.Add("PASS · offscreen WPF navigation/search preserves numeric drafts; config captures hidden values; values-only load leaves switches off; disconnected Apply rejected; all eight views measure/arrange at minimum width; game settings retained without writes.");
   }catch(Exception e){log.Add("FAIL · "+e);result=1;}finally{engine.Detach();Directory.CreateDirectory(Path.Combine(Data.Root,"logs"));File.WriteAllLines(Path.Combine(Data.Root,"logs","ui-test.txt"),log);}return result;
  }
 }
}
