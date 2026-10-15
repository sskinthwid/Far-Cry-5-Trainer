using System;using System.IO;using System.Linq;using System.Collections.Generic;
namespace FarTrainer {
 public static class SettingsBehaviorTests {
  public static void Run(Action<string> log){
   string folder=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"HopeControl-settings-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);string path=System.IO.Path.Combine(folder,"gamerprofile.xml");
   try{
    const string original="<GamerProfile><SoundProfile MasterVolume='37'/><RenderProfile FOV='90' ShowFPS='0'/><Unknown Preserve='yes'/></GamerProfile>";File.WriteAllText(path,original);
    var settings=new ProfileSettings(path,()=>false);var values=settings.Load();if(values.Count!=3)throw new Exception("Settings detection failed");
    values["RenderProfile@FOV"]="110";string backup=settings.Save(values);if(File.ReadAllText(backup)!=original||!File.ReadAllText(path).Contains("Preserve=\"yes\""))throw new Exception("Backup or unrelated XML preservation failed");
    if(settings.Load()["RenderProfile@FOV"]!="110")throw new Exception("Setting did not roundtrip");
    values=settings.Load();values["RenderProfile@FOV"]="NaN";bool rejected=false;try{settings.Save(values);}catch(ArgumentException){rejected=true;}if(!rejected)throw new Exception("Invalid setting accepted");
    var busy=new ProfileSettings(path,()=>true);values=busy.Load();rejected=false;try{busy.Save(values);}catch(IOException){rejected=true;}if(!rejected)throw new Exception("Running-game write was accepted");
    values=settings.Load();File.AppendAllText(path," ");rejected=false;try{settings.Save(values);}catch(IOException){rejected=true;}if(!rejected)throw new Exception("Concurrent file change was overwritten");
    settings.RestoreLatest();if(settings.Load()["RenderProfile@FOV"]!="90")throw new Exception("Backup restore failed");
    foreach(var d in ProfileSettings.Definitions)if(d.Boolean){rejected=false;try{d.Validate("2");}catch(ArgumentException){rejected=true;}if(!rejected)throw new Exception("Invalid boolean accepted");}
    log("PASS · settings editor: discovery, backup, save/readback, unrelated XML preservation, invalid values, running-game guard, concurrent changes and restore.");
   }finally{foreach(string file in Directory.GetFiles(folder))File.Delete(file);Directory.Delete(folder);}
  }
 }
}
