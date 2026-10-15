using System;using System.Collections.Generic;using System.Linq;using System.Windows;using System.Windows.Controls;using System.Windows.Input;
namespace FarTrainer {
 public sealed class InventoryJob {public InventoryEntry Entry;public int Before,Target;public string State="Staged";public DateTime Started;}
 public static class InventoryActions {
  public static int Target(int current,int input,bool add){long target=add?(long)current+input:input;if(target<0||target>1000000000)throw new ArgumentException("Result must be between 0 and 1,000,000,000.");return (int)target;}
  public static string Group(string name){return name.StartsWith("Money")?"Currency":name.StartsWith("Ammo/")?"Ammunition":name.StartsWith("Medkit/")?"Medical":"Crafting";}
  public static string Label(string name){if(name=="Money/Money")return "Cash";if(name=="Money_DLCM/DLC_Mars_Silicgel")return "Mars energy";if(name=="Medkit/HealthPack")return "Medkits";return System.Text.RegularExpressions.Regex.Replace(name.Split('/').Last(),"([a-z])([A-Z])","$1 $2").Replace('_',' ');}
 }
 public sealed partial class MainWindow {
  readonly List<InventoryJob> inventoryJobs=new List<InventoryJob>();readonly Dictionary<string,string> inventoryDraft=new Dictionary<string,string>();bool inventoryRunning;string inventoryGroup="All";bool addInventory=true;
  string InventoryKey(InventoryEntry e){return e.Owner.ToString("X")+":"+e.Pointer.ToString("X");}
  void StageInventory(InventoryEntry entry,int input,bool add){
   var current=engine.InventorySnapshot().FirstOrDefault(x=>x.Pointer==entry.Pointer&&x.Owner==entry.Owner&&x.Descriptor==entry.Descriptor);if(current==null)throw new InvalidOperationException("Resource changed. Refresh and try again.");
   if(inventoryJobs.Any(j=>j.Entry.Pointer==entry.Pointer&&(j.State=="Waiting"||j.State=="Queued")))throw new InvalidOperationException("This resource already has a pending edit.");
   inventoryJobs.RemoveAll(j=>j.Entry.Pointer==entry.Pointer&&j.State=="Staged");inventoryJobs.Add(new InventoryJob{Entry=current,Before=current.Value,Target=InventoryActions.Target(current.Value,input,add)});Log("Staged "+InventoryActions.Label(entry.Name)+". Review the queue, then apply.");
  }
  void CancelInventoryJobs(){engine.CancelInventoryEdit();foreach(var j in inventoryJobs.Where(j=>j.State=="Waiting"||j.State=="Queued"))j.State="Cancelled";inventoryRunning=false;}
  void PumpInventory(){
   if(!inventoryRunning)return;
   if(engine.Memory==null||!engine.Memory.Alive){foreach(var j in inventoryJobs.Where(j=>j.State=="Waiting"||j.State=="Queued"))j.State="Disconnected";inventoryRunning=false;return;}
   var waiting=inventoryJobs.FirstOrDefault(j=>j.State=="Waiting");
   if(waiting!=null){
    if(engine.InventoryEditPending){if((DateTime.UtcNow-waiting.Started).TotalSeconds<20)return;engine.CancelInventoryEdit();waiting.State="Timed out";Log("Edit timed out. Open the resource in-game and retry.");}
    else waiting.State=engine.InventoryEditResult=="Last edit applied"?"Applied":"Changed; retry";
   }
   var next=inventoryJobs.FirstOrDefault(j=>j.State=="Queued");if(next==null){inventoryRunning=false;return;}
   var current=engine.InventorySnapshot().FirstOrDefault(x=>x.Pointer==next.Entry.Pointer&&x.Owner==next.Entry.Owner&&x.Descriptor==next.Entry.Descriptor);
   if(current==null||current.Value!=next.Before){next.State="Changed; retry";return;}
   engine.QueueInventoryValue(current,next.Target);next.State="Waiting";next.Started=DateTime.UtcNow;
  }
  void TickInventory(){if(busy)return;try{PumpInventory();if(category=="Currency and inventory"&&!(Keyboard.FocusedElement is TextBox))Render();}catch(Exception e){try{CancelInventoryJobs();}catch{}Log("Inventory: "+e.Message);}}
  void RenderInventory(){
   title.Text="Inventory studio";subtitle.Text="Add or set observed player resources · review before applying";
   var tools=new WrapPanel{Margin=new Thickness(0,0,0,14)};list.Children.Add(tools);
   bool connected=engine.Memory!=null&&engine.Memory.Alive;
   var scan=Button(engine.Active("observer-resource")?"Refresh resources":"Scan player inventory",()=>Run(()=>engine.StartInventoryScan()),true);scan.IsEnabled=connected;tools.Children.Add(scan);
   tools.Children.Add(Button(addInventory?"Mode: Add amount":"Mode: Set total",()=>{addInventory=!addInventory;Render();}));
   tools.Children.Add(Button("Cancel queue",()=>Run(CancelInventoryJobs)));
   var multBox=Input("10",60);multBox.Margin=new Thickness(12,0,4,0);tools.Children.Add(multBox);
   var multBtn=Button("⚡ Multiply All Resources (×)",()=>Run(()=>{
    int m;if(!int.TryParse(multBox.Text,out m)||m<=0||m>100000)throw new ArgumentException("Enter a multiplier from 1 to 100,000.");
    int updated=engine.MultiplyAllDiscoveredResources(m);
    Log("Instantly multiplied "+updated+" discovered resources by "+m+"× in memory!");
    Render();
   }),true);multBtn.IsEnabled=connected;tools.Children.Add(multBtn);
   list.Children.Add(Text(connected?"Open the weapon wheel, inventory or a shop to discover resources. Quantities update automatically. Edits run when the game queries each item and may persist in your save.":"Connect to your campaign, then scan inventory. Resources appear as the game reads them; unobserved items cannot be spawned by this editor.",12,muted));
   var groups=new WrapPanel{Margin=new Thickness(0,14,0,14)};list.Children.Add(groups);foreach(string group in new[]{"All","Currency","Ammunition","Medical","Crafting"}){string g=group;groups.Children.Add(Button(g,()=>{inventoryGroup=g;Render();},g==inventoryGroup));}
   var entries=engine.InventorySnapshot();string query=search.Text.Trim();var shown=entries.Where(e=>(inventoryGroup=="All"||InventoryActions.Group(e.Name)==inventoryGroup)&&(query==""||(e.Name+" "+InventoryActions.Label(e.Name)).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0)).ToList();
   subtitle.Text=entries.Count+" discovered · "+shown.Count+" shown · "+inventoryJobs.Count(j=>j.State=="Waiting"||j.State=="Queued")+" queued";
   RenderInventoryQueue();
   foreach(var entry in shown){
    var card=new Border{Background=panel,CornerRadius=new CornerRadius(10),Padding=new Thickness(16),Margin=new Thickness(0,0,0,10),BorderBrush=line,BorderThickness=new Thickness(1)};list.Children.Add(card);
    var row=new Grid();card.Child=row;row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(380)});
    var detail=new StackPanel();row.Children.Add(detail);detail.Children.Add(Text(InventoryActions.Label(entry.Name)+"   "+entry.Value.ToString("N0"),16));detail.Children.Add(Text(InventoryActions.Group(entry.Name)+" · "+entry.Name,11,muted));
    var controls=new WrapPanel();Grid.SetColumn(controls,1);row.Children.Add(controls);string key=InventoryKey(entry);string draft;var input=Input(inventoryDraft.TryGetValue(key,out draft)?draft:"100",80);input.TextChanged+=(s,e)=>inventoryDraft[key]=input.Text;controls.Children.Add(input);
    var directBtn=Button("⚡ Set",()=>Run(()=>{int n;if(!int.TryParse(input.Text,out n))throw new ArgumentException("Enter a whole number.");engine.SetInventoryValueDirect(entry,n);Render();}));directBtn.Margin=new Thickness(4,0,0,0);controls.Children.Add(directBtn);
    var multiBtn=Button("× Multi",()=>Run(()=>{int n;if(!int.TryParse(input.Text,out n)||n<=0)throw new ArgumentException("Enter a multiplier >= 1.");long v=Math.Min(1000000000L,(long)entry.Value*n);engine.SetInventoryValueDirect(entry,(int)v);Render();}));multiBtn.Margin=new Thickness(4,0,0,0);controls.Children.Add(multiBtn);
    var stage=Button(addInventory?"+ Stage":"Stage set",()=>Run(()=>{int n;if(!int.TryParse(input.Text,out n))throw new ArgumentException("Enter a whole number.");StageInventory(entry,n,addInventory);}));stage.Margin=new Thickness(4,0,0,0);controls.Children.Add(stage);
   }
   if(shown.Count==0)list.Children.Add(Text(entries.Count==0?"No resources discovered yet. Start scanning and open an inventory screen in the game.":"No resources match this filter.",14,accent));
  }
  void RenderInventoryQueue(){
   if(inventoryJobs.Count==0)return;
   var body=new StackPanel{Margin=new Thickness(0,0,0,18)};list.Children.Add(body);body.Children.Add(Text("Edit queue",18,accent));
   var actions=new WrapPanel{Margin=new Thickness(0,10,0,10)};body.Children.Add(actions);
   actions.Children.Add(Button("Apply staged edits",()=>Run(()=>{if(engine.Memory==null||!engine.Memory.Alive)throw new InvalidOperationException("Connect first.");foreach(var j in inventoryJobs.Where(j=>j.State=="Staged"))j.State="Queued";inventoryRunning=true;PumpInventory();}),true));
   actions.Children.Add(Button("Clear finished",()=>{inventoryJobs.RemoveAll(j=>j.State!="Waiting"&&j.State!="Queued"&&j.State!="Staged");Render();}));
   foreach(var job in inventoryJobs.ToArray()){
    var row=new DockPanel{Margin=new Thickness(0,0,0,6)};body.Children.Add(row);var buttons=new WrapPanel();DockPanel.SetDock(buttons,Dock.Right);row.Children.Add(buttons);
    if(job.State=="Applied")buttons.Children.Add(Button("Undo",()=>Run(()=>{var current=engine.InventorySnapshot().FirstOrDefault(x=>x.Pointer==job.Entry.Pointer&&x.Owner==job.Entry.Owner&&x.Descriptor==job.Entry.Descriptor);if(current==null||current.Value!=job.Target)throw new InvalidOperationException("Quantity changed after this edit; undo would overwrite gameplay changes.");StageInventory(current,job.Before,false);} )));
    if(job.State=="Staged")buttons.Children.Add(Button("Remove",()=>{inventoryJobs.Remove(job);Render();}));
    if(job.State=="Timed out"||job.State=="Changed; retry")buttons.Children.Add(Button("Restage total",()=>Run(()=>StageInventory(job.Entry,job.Target,false))));
    row.Children.Add(Text(InventoryActions.Label(job.Entry.Name)+"   "+job.Before.ToString("N0")+" → "+job.Target.ToString("N0")+"   · "+job.State,12,job.State=="Applied"?accent:muted));
   }
  }
 }
}
