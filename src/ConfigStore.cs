using System;using System.Collections.Generic;using System.IO;using System.Linq;
namespace FarTrainer {
 public sealed class TrainerConfig {
  public int Version{get;set;} public string Name{get;set;} public string Notes{get;set;}
  public Dictionary<string,double> Values{get;set;} public List<string> Enabled{get;set;} public List<string> Favorites{get;set;}
 }
 public static class ConfigStore {
  public static string Folder{get{return Path.Combine(Data.UserRoot,"configs");}}
  public static void Validate(TrainerConfig c){
   if(c==null||c.Version!=1||string.IsNullOrWhiteSpace(c.Name)||c.Name.Length>64||c.Values==null||c.Enabled==null||c.Favorites==null)throw new IOException("Invalid config format or unsupported version.");
   var catalog=Data.Catalog().ToDictionary(m=>m.Id);var allowed=new HashSet<string>(catalog.Keys);
   if(c.Enabled.Concat(c.Favorites).Any(id=>!allowed.Contains(id))||c.Values.Keys.Any(id=>!allowed.Contains(id)))throw new IOException("Config contains an unknown control.");
   if(c.Enabled.Distinct().Count()!=c.Enabled.Count)throw new IOException("Duplicate config switches.");
   using(var engine=new TrainerEngine())foreach(var pair in c.Values){engine.Configure(pair.Key,pair.Value);if(string.IsNullOrEmpty(catalog[pair.Key].Unit)&&pair.Value!=catalog[pair.Key].Default)throw new IOException("A toggle cannot contain a custom value.");}
  }
  public static TrainerConfig Starter(string name){
   var c=new TrainerConfig{Version=1,Name=name,Notes="Starter config. Customize and save your own version.",Values=Data.Catalog().ToDictionary(m=>m.Id,m=>m.Default),Enabled=new List<string>(),Favorites=new List<string>()};
   if(name=="Exploration"){c.Enabled.AddRange(new[]{"health","stamina","oxygen","speed"});c.Values["speed"]=1.5;}
   else if(name=="Precision"){c.Enabled.AddRange(new[]{"recoil","spread","sway","ammo"});}
   else if(name=="Ballistics"){c.Enabled.AddRange(new[]{"projectile-speed","rapid-fire","no-reload"});c.Values["projectile-speed"]=2;c.Values["rapid-fire"]=2;}
   else throw new ArgumentException("Unknown starter config");c.Favorites.AddRange(c.Enabled);Validate(c);return c;
  }
  public static TrainerConfig Read(string path){if(new FileInfo(path).Length>256*1024)throw new IOException("Config exceeds 256 KB.");var c=Data.Json.Deserialize<TrainerConfig>(File.ReadAllText(path));Validate(c);return c;}
  public static void Write(string path,TrainerConfig c){Validate(c);Data.Save(path,c);}
  public static string SaveNew(TrainerConfig c){Directory.CreateDirectory(Folder);string path=Path.Combine(Folder,Guid.NewGuid().ToString("N")+".json");Write(path,c);return path;}
  public static IEnumerable<string> Files(){return Directory.Exists(Folder)?Directory.GetFiles(Folder,"*.json").OrderByDescending(File.GetLastWriteTimeUtc):Enumerable.Empty<string>();}
 }
 public sealed class UiPreferences {public List<string> Favorites{get;set;}public bool Compact{get;set;}}
}
