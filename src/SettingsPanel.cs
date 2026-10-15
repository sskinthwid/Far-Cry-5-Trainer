using System;using System.Collections.Generic;using System.Linq;using System.Windows;using System.Windows.Controls;
namespace FarTrainer {
 public sealed partial class MainWindow {
  ProfileSettings profileSettings;Dictionary<string,string> settingsBaseline,settingsDraft;readonly Dictionary<string,Func<string>> settingsInputs=new Dictionary<string,Func<string>>();
  void CaptureSettingsDraft(){if(settingsDraft!=null)foreach(var pair in settingsInputs)settingsDraft[pair.Key]=pair.Value();}
  void RenderSettings(){
   title.Text="Game settings";subtitle.Text="Built-in profile editor · settings apply on the next launch";settingsInputs.Clear();
   Dictionary<string,string> values;
   try{if(settingsBaseline==null){profileSettings=new ProfileSettings();settingsBaseline=profileSettings.Load();settingsDraft=new Dictionary<string,string>(settingsBaseline);}values=settingsDraft;}catch(Exception ex){list.Children.Add(Text("Cannot load settings: "+ex.Message,14,muted));return;}
   var tools=new WrapPanel{Margin=new Thickness(0,0,0,12)};
   tools.Children.Add(Button("Save settings for next launch",()=>{
    try{CaptureSettingsDraft();var edits=settingsDraft.Where(p=>settingsBaseline.ContainsKey(p.Key)&&settingsBaseline[p.Key]!=p.Value).ToDictionary(p=>p.Key,p=>p.Value);if(edits.Count==0){Log("No settings changes to save.");return;}profileSettings=new ProfileSettings();profileSettings.Load();string backup=profileSettings.Save(edits);settingsBaseline=profileSettings.Load();settingsDraft=new Dictionary<string,string>(settingsBaseline);Log("Settings saved. Backup: "+System.IO.Path.GetFileName(backup)+". Changes apply next launch.");}catch(Exception ex){Log(ex.Message);}
   },true));
   tools.Children.Add(Button("Reload settings",()=>{settingsInputs.Clear();settingsBaseline=null;settingsDraft=null;Render();}));tools.Children.Add(Button("Restore settings backup",()=>{try{profileSettings.RestoreLatest();settingsInputs.Clear();settingsBaseline=null;settingsDraft=null;Render();Log("Restored the most recent settings backup.");}catch(Exception ex){Log(ex.Message);}}));list.Children.Add(tools);
   list.Children.Add(Text("Edit settings here, then close Far Cry 5 before saving so it cannot overwrite them on exit. Device-dependent settings require their corresponding hardware. Unknown profile fields are preserved.",12,muted));
   string query=search.Text.Trim();
   foreach(var group in ProfileSettings.Definitions.Where(d=>values.ContainsKey(d.Id)&&(query.Length==0||(d.Name+" "+d.Group+" "+d.Attribute).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0)).GroupBy(d=>d.Group)){
    var heading=Text(group.Key,19,accent);heading.Margin=new Thickness(0,20,0,10);list.Children.Add(heading);
    foreach(var d in group){
     var grid=new Grid{Margin=new Thickness(0,0,12,8)};grid.ColumnDefinitions.Add(new ColumnDefinition());grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(200)});var label=Text(d.Name,13);label.VerticalAlignment=VerticalAlignment.Center;label.ToolTip=d.Id;grid.Children.Add(label);
     if(d.Boolean){var toggle=new CheckBox{IsChecked=values[d.Id]=="1",Content="Enabled",Foreground=ink,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(5)};Grid.SetColumn(toggle,1);grid.Children.Add(toggle);settingsInputs[d.Id]=()=>toggle.IsChecked==true?"1":"0";}
     else if(d.Choices!=null){var choices=new ComboBox{Width=180,ItemsSource=d.Choices.Contains(values[d.Id])?d.Choices:d.Choices.Concat(new[]{values[d.Id]}).ToArray(),SelectedItem=values[d.Id],Padding=new Thickness(8)};Grid.SetColumn(choices,1);grid.Children.Add(choices);settingsInputs[d.Id]=()=>choices.SelectedItem as string??values[d.Id];}
     else {var input=Input(values[d.Id],180);input.ToolTip=d.Min+" to "+d.Max;Grid.SetColumn(input,1);grid.Children.Add(input);settingsInputs[d.Id]=()=>input.Text;}
     list.Children.Add(grid);
    }
   }
   subtitle.Text=values.Count+" editable settings · applies next launch · automatic backup on save";
  }
 }
}
