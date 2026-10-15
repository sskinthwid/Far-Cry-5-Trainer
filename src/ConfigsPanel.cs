using System;using System.Collections.Generic;using System.IO;using System.Linq;using System.Windows;using System.Windows.Controls;using Microsoft.Win32;
namespace FarTrainer {
 public sealed partial class MainWindow {
  readonly HashSet<string> favorites=new HashSet<string>();bool compact;string configNotes="";
  string PreferencesPath{get{return Path.Combine(Data.UserRoot,"ui.json");}}
  void ReadPreferences(){try{if(File.Exists(PreferencesPath)){var p=Data.Json.Deserialize<UiPreferences>(File.ReadAllText(PreferencesPath));if(p!=null){compact=p.Compact;if(p.Favorites!=null)foreach(var id in p.Favorites.Where(id=>catalog.Any(m=>m.Id==id)))favorites.Add(id);}}}catch{}}
  void SavePreferences(){try{Data.Save(PreferencesPath,new UiPreferences{Favorites=favorites.ToList(),Compact=compact});}catch(Exception e){Log(e.Message);}}
  TrainerConfig CurrentConfig(){CommitValues();return new TrainerConfig{Version=1,Name=profileName.Text.Trim(),Notes=configNotes,Values=catalog.ToDictionary(m=>m.Id,m=>engine.Values.ContainsKey(m.Id)?engine.Values[m.Id]:m.Default),Enabled=catalog.Where(m=>engine.Active(m.Id)).Select(m=>m.Id).ToList(),Favorites=favorites.ToList()};}
  void ApplyConfig(TrainerConfig c,bool switches,bool persist=true){
   ConfigStore.Validate(c);if(switches&&(engine.Memory==null||!engine.Memory.Alive||c.Enabled.Any(id=>!engine.Available(id))))throw new IOException("Connect to a compatible game before applying switches.");
   var oldValues=new Dictionary<string,double>(engine.Values);var oldEnabled=catalog.Where(m=>engine.Active(m.Id)).Select(m=>m.Id).ToArray();
   try{if(switches){CancelInventoryJobs();engine.DisableAll();}foreach(var m in catalog)engine.Configure(m.Id,c.Values.ContainsKey(m.Id)?c.Values[m.Id]:m.Default);if(switches)foreach(var id in c.Enabled)engine.Set(id,true);}
   catch{if(switches)engine.DisableAll();foreach(var pair in oldValues)engine.Configure(pair.Key,pair.Value);if(switches)foreach(var id in oldEnabled)engine.Set(id,true);throw;}
   featureDraft.Clear();profileName.Text=c.Name;configNotes=c.Notes??"";favorites.Clear();foreach(var id in c.Favorites)favorites.Add(id);if(persist)SavePreferences();BuildNavigation();Log(switches?"Config applied: "+c.Name:"Config values loaded: "+c.Name+". Switches retain their current state.");
  }
  void RenderConfigs(){
   title.Text="Configs";subtitle.Text="Named loadouts · portable JSON · explicit switch activation";
   var editor=new StackPanel{Margin=new Thickness(0,0,0,20)};list.Children.Add(editor);
   editor.Children.Add(Text("Save your current setup",18));var row=new WrapPanel{Margin=new Thickness(0,12,0,8)};editor.Children.Add(row);
   var name=Input(profileName.Text,240);name.TextChanged+=(s,e)=>profileName.Text=name.Text;row.Children.Add(name);
   row.Children.Add(Button("Save new config",()=>Run(()=>{ConfigStore.SaveNew(CurrentConfig());Log("Saved a new named config.");}),true));
   row.Children.Add(Button("Import JSON",()=>{var d=new OpenFileDialog{Filter="Hope Control config (*.json)|*.json"};if(d.ShowDialog(this)==true)Run(()=>{var c=ConfigStore.Read(d.FileName);ConfigStore.SaveNew(c);Log("Imported "+c.Name+"; switches remain unchanged.");});}));
   row.Children.Add(Button("Export current",()=>{var d=new SaveFileDialog{Filter="Hope Control config (*.json)|*.json",FileName="HopeControl-config.json"};if(d.ShowDialog(this)==true)Run(()=>{ConfigStore.Write(d.FileName,CurrentConfig());Log("Config exported.");});}));
   editor.Children.Add(Text("Notes (optional)",11,muted));var notes=Input(configNotes,double.NaN);notes.ToolTip="Optional config notes";notes.TextChanged+=(s,e)=>configNotes=notes.Text;editor.Children.Add(notes);
   editor.Children.Add(Text("Includes feature values, enabled switches and favorites. Inventory edits and game settings are saved separately.",12,muted));
   if(File.Exists(Data.PresetFile))editor.Children.Add(Button("Import old preset",()=>Run(()=>{var p=Data.Json.Deserialize<Preset>(File.ReadAllText(Data.PresetFile));var c=new TrainerConfig{Version=1,Name=p.Name,Notes="Imported legacy preset",Values=p.Values,Enabled=p.Enabled,Favorites=new List<string>()};ConfigStore.SaveNew(c);Log("Legacy preset imported.");})));
   var starters=new WrapPanel{Margin=new Thickness(0,0,0,16)};list.Children.Add(starters);starters.Children.Add(Text("STARTERS   ",11,muted));foreach(string starter in new[]{"Exploration","Precision","Ballistics"}){string nameOfStarter=starter;starters.Children.Add(Button(starter,()=>Run(()=>{var c=ConfigStore.Starter(nameOfStarter);ConfigStore.SaveNew(c);Log("Saved starter config: "+c.Name+". Apply it explicitly when ready.");})));}
   int count=0;foreach(var path in ConfigStore.Files()){
    TrainerConfig c;try{c=ConfigStore.Read(path);}catch(Exception e){list.Children.Add(Text(Path.GetFileName(path)+": "+e.Message,12,muted));continue;}
    if(search.Text.Length>0&&((c.Name+" "+c.Notes).IndexOf(search.Text,StringComparison.OrdinalIgnoreCase)<0))continue;count++;
    var card=new Border{Background=panel,CornerRadius=new CornerRadius(12),Padding=new Thickness(18),Margin=new Thickness(0,0,0,12),BorderBrush=line,BorderThickness=new Thickness(1)};var body=new StackPanel();card.Child=body;list.Children.Add(card);
    body.Children.Add(Text(c.Name,18));body.Children.Add(Text(c.Enabled.Count+" switches · "+c.Values.Count+" values · "+(c.Notes??""),12,muted));
    var buttons=new WrapPanel{Margin=new Thickness(0,14,0,0)};body.Children.Add(buttons);
    buttons.Children.Add(Button("Load values",()=>Run(()=>ApplyConfig(c,false))));buttons.Children.Add(Button("Apply switches + values",()=>Run(()=>ApplyConfig(c,true)),true));
    buttons.Children.Add(Button("Update from current",()=>Run(()=>{var update=CurrentConfig();update.Name=c.Name;ConfigStore.Write(path,update);Log("Updated "+c.Name+"; previous copy retained as .bak.");})));
    buttons.Children.Add(Button("Archive",()=>Run(()=>{File.Move(path,path+".archived-"+DateTime.Now.ToString("yyyyMMddHHmmssfff"));Log("Archived "+c.Name+". File remains in the config folder.");})));
   }
   if(count==0)list.Children.Add(Text("Your saved configs will appear here. Configure some values and save your first setup.",14,muted));
  }
 }
}
