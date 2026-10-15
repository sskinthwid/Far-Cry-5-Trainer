using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Linq;
namespace FarTrainer {
 public sealed class ModItem {
  public string Id {get;set;} public string Name {get;set;} public string Category {get;set;}
  public string Kind {get;set;} public string Notes {get;set;} public string Source {get;set;}
  public string Evidence {get;set;} public string Runtime {get;set;}
  public string Unit {get;set;} public double Default {get;set;}
 }
 public sealed class Preset { public string Name {get;set;} public Dictionary<string,double> Values {get;set;} public List<string> Enabled {get;set;} }
 public static class Data {
  public static readonly string Root=File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"RESEARCH.md"))?AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar):Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,".."));
  public static readonly string UserRoot=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"HopeControl");
  public static readonly JavaScriptSerializer Json=new JavaScriptSerializer{MaxJsonLength=16*1024*1024};
  public static List<ModItem> Catalog(){using(var r=new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("catalog.json")))return Json.Deserialize<List<ModItem>>(r.ReadToEnd());}
  public static void Save(string path,object value){Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp";File.WriteAllText(temp,Json.Serialize(value));if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);}
  public static string PresetFile {get{return Path.Combine(UserRoot,"preset.json");}}
 }
 public sealed class TableRecord {public string Name,Type,Address,Script,Path;}
 public static class TableReader {
  public static List<TableRecord> Read(string path) {
   var fi=new FileInfo(path);if(fi.Length>20*1024*1024)throw new IOException("Table exceeds the 20 MB inspection limit.");
   var settings=new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=20*1024*1024};
   using(var r=XmlReader.Create(path,settings)){
    var doc=XDocument.Load(r);if(doc.Root==null||doc.Root.Name.LocalName!="CheatTable")throw new IOException("This file is not a Cheat Engine XML table.");
    return doc.Descendants("CheatEntry").Select(x=>new TableRecord{Name=((string)x.Element("Description")??"Unnamed").Trim('"'),Type=(string)x.Element("VariableType")??"Group",Address=(string)x.Element("Address")??"",Script=(string)x.Element("AssemblerScript")??"",Path=string.Join(" / ",x.Ancestors("CheatEntry").Reverse().Select(a=>((string)a.Element("Description")??"").Trim('"')))}).ToList();
   }
  }
 }
}
