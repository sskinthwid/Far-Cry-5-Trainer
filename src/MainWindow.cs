using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
namespace FarTrainer {
 public sealed partial class MainWindow : Window {
  readonly TrainerEngine engine=new TrainerEngine();readonly List<ModItem> catalog=Data.Catalog();
  readonly Brush ink=B("#F1F3FA"),muted=B("#939BAF"),accent=B("#A6A0FF"),panel=B("#181C28"),line=B("#2B3144");
  readonly StackPanel list=new StackPanel(),nav=new StackPanel();
  TextBox search,profileName;TextBlock status,counts,title,subtitle,message,coords;ProgressBar progress;
  Button attach,density;string category="All features";bool busy,pendingRestore;HwndSource source;Tuple<float,float,float> lastWaypoint;
  readonly Dictionary<string,string> featureDraft=new Dictionary<string,string>();
  readonly Dictionary<string,TextBox> valueInputs=new Dictionary<string,TextBox>();readonly List<int> hotkeys=new List<int>();
  readonly string[] keys={"health","speed","ammo","no-reload","recoil","spread","stamina","oxygen","flares"};
  static Brush B(string hex){return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));}
  static readonly ControlTemplate buttonTemplate=(ControlTemplate)System.Windows.Markup.XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Button'><Border x:Name='box' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' CornerRadius='7' Padding='{TemplateBinding Padding}'><ContentPresenter HorizontalAlignment='{TemplateBinding HorizontalContentAlignment}' VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.38'/></Trigger><Trigger Property='IsMouseOver' Value='True'><Setter Property='Opacity' Value='0.8'/></Trigger><Trigger Property='IsKeyboardFocused' Value='True'><Setter Property='BorderBrush' Value='#A6A0FF'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");
  TextBlock Text(string s,double size,Brush color=null){return new TextBlock{Text=s,FontSize=size,Foreground=color??ink,TextWrapping=TextWrapping.Wrap};}
  Button Button(string text,Action action,bool green=false){var b=new Button{Template=buttonTemplate,HorizontalContentAlignment=HorizontalAlignment.Center,Content=text,Padding=new Thickness(14,9,14,9),Margin=new Thickness(0,0,8,0),Background=green?accent:B("#23283A"),Foreground=green?B("#10121C"):ink,BorderBrush=line,BorderThickness=new Thickness(1),FontSize=12,Cursor=System.Windows.Input.Cursors.Hand};b.Click+=(s,e)=>action();return b;}
  TextBox Input(string value,double width=140){return new TextBox{Text=value,Width=width,Background=B("#10131D"),Foreground=ink,BorderBrush=line,Padding=new Thickness(10,8,10,8),FontSize=13,VerticalContentAlignment=VerticalAlignment.Center};}
  public MainWindow(){
   Title="Hope Control · Far Cry 5";Width=1400;Height=940;MinWidth=1150;MinHeight=650;WindowStartupLocation=WindowStartupLocation.CenterScreen;Background=B("#0E111A");FontFamily=new FontFamily("Segoe UI");Foreground=ink;
   foreach(var m in catalog.Where(m=>m.Runtime!=null))engine.Values[m.Runtime]=m.Default;
   engine.Log=Log;InstallTheme();ReadPreferences();profileName=Input("My loadout",200);
   var root=new Grid{Background=Background};root.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(216)});root.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});Content=root;
   var sidebar=new Border{Background=B("#121620"),Padding=new Thickness(18,26,14,20),BorderBrush=line,BorderThickness=new Thickness(0,0,1,0)};root.Children.Add(sidebar);
   var side=new DockPanel();sidebar.Child=side;
   var sideBottom=new StackPanel();DockPanel.SetDock(sideBottom,Dock.Bottom);side.Children.Add(sideBottom);
   sideBottom.Children.Add(Text("F12  Restore all",13,accent));sideBottom.Children.Add(Text("Ctrl+Alt+W  Teleport WP",12,accent));sideBottom.Children.Add(Text("Ctrl+Alt+S  Save Pos",12,accent));sideBottom.Children.Add(Text("Ctrl+Alt+L  Load Pos",12,accent));var local=Text("CAMPAIGN WORKSPACE\nLocal · Windows x64",11,muted);local.Margin=new Thickness(0,12,0,0);sideBottom.Children.Add(local);
   var logo=new StackPanel{Margin=new Thickness(0,0,0,28)};DockPanel.SetDock(logo,Dock.Top);side.Children.Add(logo);
   logo.Children.Add(Text("◈  HOPE",26,accent));logo.Children.Add(Text("CONTROL / 02",13));var game=Text("FAR CRY 5",10,muted);game.Margin=new Thickness(0,12,0,0);logo.Children.Add(game);
   side.Children.Add(new ScrollViewer{Content=nav,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
   var main=new DockPanel{Margin=new Thickness(24,24,24,18)};Grid.SetColumn(main,1);root.Children.Add(main);
   var footer=new Border{Background=panel,Padding=new Thickness(14,9,14,9),Margin=new Thickness(0,14,0,0)};DockPanel.SetDock(footer,Dock.Bottom);main.Children.Add(footer);message=Text("Start the game, load a campaign save, then connect. All features start off.",12,muted);footer.Child=message;
   var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);main.Children.Add(top);
   var header=new Grid();header.ColumnDefinitions.Add(new ColumnDefinition());header.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});top.Children.Add(header);
   var headings=new StackPanel();header.Children.Add(headings);headings.Children.Add(Text("Campaign workspace",24));var hint=Text("Tune your loadout. Shape your campaign.",12,muted);hint.Margin=new Thickness(0,8,0,0);headings.Children.Add(hint);
   var badge=new Border{BorderBrush=line,BorderThickness=new Thickness(1),Padding=new Thickness(12,8,12,8),VerticalAlignment=VerticalAlignment.Top};badge.Child=Text("SINGLE PLAYER",10,accent);Grid.SetColumn(badge,1);header.Children.Add(badge);
   var connection=new Border{Background=panel,BorderBrush=line,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(12),Padding=new Thickness(16),Margin=new Thickness(0,16,0,14)};top.Children.Add(connection);
   var cg=new Grid();cg.ColumnDefinitions.Add(new ColumnDefinition());cg.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});connection.Child=cg;
   var cs=new StackPanel();cg.Children.Add(cs);status=Text("Game disconnected",17);counts=Text("",12,muted);counts.Margin=new Thickness(0,6,0,0);cs.Children.Add(status);cs.Children.Add(counts);coords=Text("",11,accent);coords.Margin=new Thickness(0,4,0,0);cs.Children.Add(coords);progress=new ProgressBar{Height=3,Width=320,Maximum=100,Foreground=accent,Visibility=Visibility.Collapsed,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,10,0,0)};cs.Children.Add(progress);
   var cb=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(cb,1);cg.Children.Add(cb);attach=Button("Connect to game",Connect,true);cb.Children.Add(attach);cb.Children.Add(Button("Restore all",()=>Run(()=>{CancelInventoryJobs();engine.DisableAll();})));
   var tools=new WrapPanel{Margin=new Thickness(0,0,0,16)};top.Children.Add(tools);
   tools.Children.Add(Button("📍 Teleport to Waypoint",TeleportWaypointManual));tools.Children.Add(Button("💾 Save Pos",SavePositionManual));tools.Children.Add(Button("🚀 Load Pos",LoadPositionManual));tools.Children.Add(Button("⚡ 10× Resources",MultiplyResourcesManual));tools.Children.Add(Button("Inventory",()=>SelectCategory("Currency and inventory")));tools.Children.Add(Button("Configs",()=>SelectCategory("Configs")));density=Button(compact?"Card view":"List view",()=>{compact=!compact;density.Content=compact?"Card view":"List view";SavePreferences();Render();});tools.Children.Add(density);tools.Children.Add(Button("Export compatibility",Export));
   var filters=new Grid{Margin=new Thickness(0,0,0,20)};filters.ColumnDefinitions.Add(new ColumnDefinition());filters.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(210)});top.Children.Add(filters);
   search=Input("",double.NaN);search.ToolTip="Search controls, resources, configs or settings · Ctrl+F";search.Margin=new Thickness(0,0,12,0);search.TextChanged+=(s,e)=>Render();filters.Children.Add(search);var placeholder=Text("Search this workspace…                         Ctrl+F",12,muted);placeholder.IsHitTestVisible=false;placeholder.Margin=new Thickness(12,9,0,0);Grid.SetColumnSpan(placeholder,2);filters.Children.Add(placeholder);search.TextChanged+=(s,e)=>placeholder.Visibility=search.Text.Length==0?Visibility.Visible:Visibility.Collapsed;
   Grid.SetColumnSpan(search,2);
   var section=new StackPanel{Margin=new Thickness(0,0,0,14)};title=Text("All features",21);subtitle=Text("",12,muted);subtitle.Margin=new Thickness(0,5,0,0);section.Children.Add(title);section.Children.Add(subtitle);top.Children.Add(section);
   main.Children.Add(new ScrollViewer{Content=list,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
   BuildNavigation();Render();
   var inventoryTimer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};inventoryTimer.Tick+=(s,e)=>TickInventory();inventoryTimer.Start();
   PreviewKeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.F&&(System.Windows.Input.Keyboard.Modifiers&System.Windows.Input.ModifierKeys.Control)!=0){search.Focus();search.SelectAll();e.Handled=true;}if(e.Key==System.Windows.Input.Key.Escape){search.Clear();e.Handled=true;}};
   SourceInitialized+=(s,e)=>RegisterKeys();Closing+=OnClosing;
   var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(2)};timer.Tick+=(s,e)=>{if(!busy&&engine.Memory!=null&&!engine.Memory.Alive){try{engine.Detach();Log("Game exited; process patches expired.");}catch(Exception ex){Log(ex.Message);}Render();}};timer.Start();
   var tracking=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(200)};
   tracking.Tick+=(s,e)=>{
    if(!busy&&engine.VehicleEnabled)try{engine.RefreshVehicleTarget();}catch(Exception ex){Log("Vehicle tracking: "+ex.Message);}
    if(engine.Memory!=null&&engine.Memory.Alive){
     var p=engine.GetPlayerCoordinates();var saved=engine.SavedCoordinates;var w=engine.GetWaypointCoordinates();
     string pText=p!=null?string.Format(CultureInfo.InvariantCulture,"Player: {0:F1}, {1:F1}, {2:F1}",p.Item1,p.Item2,p.Item3):"Player: (waiting)";
     string sText=saved!=null?string.Format(CultureInfo.InvariantCulture,"Saved: {0:F1}, {1:F1}, {2:F1}",saved.Item1,saved.Item2,saved.Item3):"Saved: none";
     string wText=w!=null&&(Math.Abs(w.Item1)>0.01f||Math.Abs(w.Item3)>0.01f)?string.Format(CultureInfo.InvariantCulture,"Waypoint: {0:F1}, {1:F1}, {2:F1}",w.Item1,w.Item2,w.Item3):"Waypoint: none";
     if(coords!=null)coords.Text=pText+"   │   "+sText+"   │   "+wText;
     if(!busy&&engine.AutoTeleportWaypoint){
      if(w!=null&&(Math.Abs(w.Item1)>0.01f||Math.Abs(w.Item3)>0.01f)){
       if(lastWaypoint==null||Math.Abs(w.Item1-lastWaypoint.Item1)>1.0f||Math.Abs(w.Item3-lastWaypoint.Item3)>1.0f){
        lastWaypoint=w;
        try{engine.TeleportToWaypoint();}catch(Exception ex){Log("Auto-teleport: "+ex.Message);}
       }
      }
     }else if(!engine.AutoTeleportWaypoint)lastWaypoint=null;
    }else if(coords!=null)coords.Text="";
   };
   tracking.Start();
  }
  public void SelectCategory(string value){category=value;BuildNavigation();Render();}
  void BuildNavigation(){
   nav.Children.Clear();foreach(var name in new[]{"All features","Favorites","Active controls"}.Concat(catalog.Select(m=>m.Category).Distinct()).Concat(new[]{"Currency and inventory","Configs","Game settings"})){
    string n=name;string label=n=="Currency and inventory"?"Inventory studio":n=="Configs"?"Configs":n+"   "+(n=="Game settings"?ProfileSettings.Definitions.Count:n=="All features"?catalog.Count:n=="Favorites"?favorites.Count:n=="Active controls"?catalog.Count(m=>engine.Active(m.Id)):catalog.Count(m=>m.Category==n));
    var b=Button(label,()=>SelectCategory(n));b.Margin=new Thickness(0,0,0,6);b.HorizontalContentAlignment=HorizontalAlignment.Left;b.Background=n==category?B("#2D2B4A"):B("#121620");b.BorderThickness=new Thickness(0);nav.Children.Add(b);
   }
  }
  void Render(){
   if(search==null||subtitle==null)return;
   CaptureSettingsDraft();list.Children.Clear();valueInputs.Clear();string query=search.Text.Trim();
   var shown=catalog.Where(m=>(category=="All features"||m.Category==category||category=="Favorites"&&favorites.Contains(m.Id)||category=="Active controls"&&engine.Active(m.Id))&&(query==""||(m.Name+" "+m.Notes+" "+m.Evidence).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0)).ToList();
   title.Text=category;subtitle.Text=shown.Count+" implemented controls";
   bool connected=engine.Memory!=null&&engine.Memory.Alive;
   status.Text=busy?"Working…":connected?"Connected · PID "+engine.Memory.Process.Id:"Game disconnected";
   int ready=catalog.Count(m=>m.Runtime!=null&&engine.Available(m.Runtime)),active=catalog.Count(m=>m.Runtime!=null&&engine.Active(m.Runtime));
   counts.Text=connected?ready+" compatible native controls · "+active+" enabled":catalog.Count+" native controls · "+ProfileSettings.Definitions.Count+" game settings";
   attach.Content=connected?"Disconnect":"Connect to game";attach.IsEnabled=!busy;
   if(category=="Configs"){RenderConfigs();return;}
   if(category=="Currency and inventory"){RenderInventory();return;}
   if(category=="Game settings"){RenderSettings();return;}
   if(compact){foreach(var m in shown)list.Children.Add(Card(m));}
   else for(int i=0;i<shown.Count;i+=2){var row=new Grid();row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition());for(int j=0;j<2&&i+j<shown.Count;j++){var card=Card(shown[i+j]);card.Margin=new Thickness(j==0?0:6,0,j==0?6:0,12);Grid.SetColumn(card,j);row.Children.Add(card);}list.Children.Add(row);}
   if(shown.Count==0)list.Children.Add(Text("No matching entries. Try another search or category.",14,muted));
  }
  Border Card(ModItem m){
   var border=new Border{Background=panel,BorderBrush=line,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(12),Padding=new Thickness(16,14,16,14),Margin=new Thickness(0,0,0,10)};
   var grid=new Grid();grid.ColumnDefinitions.Add(new ColumnDefinition());grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(compact?230:122)});border.Child=grid;
   var detail=new StackPanel{Margin=new Thickness(0,0,20,0)};grid.Children.Add(detail);
   var heading=new DockPanel();var star=Button(favorites.Contains(m.Id)?"★":"☆",()=>{if(!favorites.Add(m.Id))favorites.Remove(m.Id);SavePreferences();BuildNavigation();Render();});star.Padding=new Thickness(4,0,4,0);star.Background=Brushes.Transparent;star.BorderThickness=new Thickness(0);star.Foreground=accent;star.ToolTip="Add or remove favorite";DockPanel.SetDock(star,Dock.Right);heading.Children.Add(star);heading.Children.Add(Text(m.Name,15));detail.Children.Add(heading);var scope=Text(m.Notes,11,muted);scope.MaxHeight=compact?32:48;scope.TextTrimming=TextTrimming.CharacterEllipsis;scope.ToolTip=m.Notes;scope.Margin=new Thickness(0,7,0,0);detail.Children.Add(scope);
   var foot=new WrapPanel{Margin=new Thickness(0,9,0,0)};detail.Children.Add(foot);
   bool enabled=m.Runtime!=null&&engine.Active(m.Runtime),ready=m.Runtime!=null&&engine.Available(m.Runtime);
   string state=m.Runtime==null?(m.Kind=="Package"?"EXTERNAL PACKAGE":"RESEARCH LEAD"):enabled?"ENABLED":ready?"READY · SIGNATURE MATCHED":engine.Memory==null?"NATIVE · CONNECT TO CHECK":"UNAVAILABLE ON THIS BUILD";
   foot.Children.Add(Text(state,10,enabled?accent:muted));foot.ToolTip=m.Evidence;
   var controls=new StackPanel{VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(controls,1);grid.Children.Add(controls);
   if(!string.IsNullOrEmpty(m.Unit)){
    double value=m.Runtime!=null&&engine.Values.ContainsKey(m.Runtime)?engine.Values[m.Runtime]:m.Default;
    string draft;var input=Input(featureDraft.TryGetValue(m.Id,out draft)?draft:value.ToString("G7",CultureInfo.InvariantCulture),compact?100:66);input.TextChanged+=(s,e)=>featureDraft[m.Id]=input.Text;input.ToolTip="Custom value · "+m.Unit;valueInputs[m.Id]=input;
    var row=new WrapPanel{Margin=new Thickness(0,0,0,8)};row.Children.Add(input);var unit=Text(m.Unit,10,muted);unit.Margin=new Thickness(0,0,0,5);controls.Children.Add(unit);controls.Children.Add(row);
    if(m.Runtime!=null){var set=Button("Set",()=>{double v;if(TryValue(input,out v))Run(()=>{engine.Configure(m.Runtime,v);featureDraft.Remove(m.Id);});});set.Padding=new Thickness(8,5,8,5);set.Margin=new Thickness(8,0,0,0);row.Children.Add(set);input.KeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.Enter){double v;if(TryValue(input,out v))Run(()=>{engine.Configure(m.Runtime,v);featureDraft.Remove(m.Id);});}};}
   }
   if(m.Runtime!=null){
    var button=Button(enabled?"●  ON":"○  OFF",()=>Toggle(m),enabled);button.IsEnabled=ready&&!busy;button.HorizontalAlignment=HorizontalAlignment.Left;button.MinWidth=108;controls.Children.Add(button);
    if(!ready&&engine.Memory!=null){var why=Text(engine.Status(m.Runtime),10,muted);why.Margin=new Thickness(0,5,0,0);controls.Children.Add(why);}
   }
   return border;
  }
  bool TryValue(TextBox input,out double v){if(!double.TryParse(input.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out v)||double.IsNaN(v)||double.IsInfinity(v)||v<0||v>float.MaxValue){Log("Enter a finite non-negative number, such as 2.5.");input.BorderBrush=B("#DB8C79");return false;}return true;}
  void CommitValues(){foreach(var m in catalog.Where(m=>!string.IsNullOrEmpty(m.Unit))){string text;if(featureDraft.TryGetValue(m.Id,out text)){double value;if(!double.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out value))throw new ArgumentException("Invalid value for "+m.Name);engine.Configure(m.Id,value);}}featureDraft.Clear();}
  void Toggle(ModItem m){Run(()=>{TextBox box;double v;if(!string.IsNullOrEmpty(m.Unit)&&valueInputs.TryGetValue(m.Id,out box)){if(!TryValue(box,out v))throw new ArgumentException("Invalid number.");engine.Configure(m.Runtime,v);}engine.Set(m.Runtime,!engine.Active(m.Runtime));featureDraft.Remove(m.Id);});}
  async void Connect(){if(busy)return;busy=true;progress.Visibility=Visibility.Visible;Render();try{await Task.Run(()=>{if(engine.Memory!=null){CancelInventoryJobs();engine.Detach();}else engine.Attach(p=>Dispatcher.BeginInvoke(new Action(()=>progress.Value=p)));});}catch(Exception e){Log(e.Message);}finally{busy=false;progress.Visibility=Visibility.Collapsed;Render();}}
  void Run(Action action){if(busy){Log("Wait for the current operation to finish.");return;}busy=true;try{action();}catch(Exception e){Log(e.Message);}finally{busy=false;BuildNavigation();Render();if(pendingRestore){pendingRestore=false;Run(()=>{CancelInventoryJobs();engine.DisableAll();});}}}
  void Log(string text){if(!Dispatcher.CheckAccess()){Dispatcher.BeginInvoke(new Action(()=>Log(text)));return;}if(message!=null)message.Text=text;try{Directory.CreateDirectory(Data.UserRoot);File.AppendAllText(Path.Combine(Data.UserRoot,"session.log"),DateTimeOffset.Now.ToString("o")+" "+text+Environment.NewLine);}catch{}}
  void Export(){Run(()=>{Directory.CreateDirectory(Path.Combine(Data.Root,"logs"));var path=Path.Combine(Data.Root,"logs","compatibility.txt");var rows=new List<string>{"Hope Control · "+DateTimeOffset.Now.ToString("o"),"Gameplay verification is separate from compatible bytes."};foreach(var m in catalog)rows.Add(m.Name+" | "+(m.Runtime==null?m.Kind:engine.Status(m.Runtime))+" | "+m.Source);File.WriteAllLines(path,rows);Log("Exported "+path);});}
  void TeleportWaypointManual(){Run(()=>{engine.TeleportToWaypoint();});}
  void SavePositionManual(){Run(()=>{engine.SavePlayerCoordinates();});}
  void LoadPositionManual(){Run(()=>{engine.TeleportToSavedCoordinates();});}
  void MultiplyResourcesManual(){Run(()=>{int n=engine.MultiplyAllDiscoveredResources(10);Log("Multiplied all discovered resources 10× ("+n+" updated).");});}
  [DllImport("user32.dll",SetLastError=true)]static extern bool RegisterHotKey(IntPtr hwnd,int id,uint modifiers,uint key);
  [DllImport("user32.dll")]static extern bool UnregisterHotKey(IntPtr hwnd,int id);
  void RegisterKeys(){IntPtr hwnd=new WindowInteropHelper(this).Handle;source=HwndSource.FromHwnd(hwnd);source.AddHook(Message);for(int i=0;i<keys.Length;i++){int id=i+1;if(RegisterHotKey(hwnd,id,0x4003,(uint)(0x31+i)))hotkeys.Add(id);else Log("Hotkey Ctrl+Alt+"+(i+1)+" is in use by another app.");}if(RegisterHotKey(hwnd,98,0x4003,0x57))hotkeys.Add(98);else Log("Hotkey Ctrl+Alt+W is in use by another app.");if(RegisterHotKey(hwnd,97,0x4003,0x53))hotkeys.Add(97);else Log("Hotkey Ctrl+Alt+S is in use by another app.");if(RegisterHotKey(hwnd,96,0x4003,0x4C))hotkeys.Add(96);else Log("Hotkey Ctrl+Alt+L is in use by another app.");if(RegisterHotKey(hwnd,99,0x4000,0x7B))hotkeys.Add(99);else Log("F12 is in use by another app; use Restore all.");}
  IntPtr Message(IntPtr hwnd,int msg,IntPtr wparam,IntPtr lparam,ref bool handled){if(msg==0x0312){int id=wparam.ToInt32();if(id==99){if(busy)pendingRestore=true;else Run(()=>{CancelInventoryJobs();engine.DisableAll();});}else if(id==98){TeleportWaypointManual();}else if(id==97){SavePositionManual();}else if(id==96){LoadPositionManual();}else if(id>0&&id<=keys.Length){var m=catalog.FirstOrDefault(x=>x.Runtime==keys[id-1]);if(m!=null&&engine.Available(m.Runtime))Toggle(m);}handled=true;}return IntPtr.Zero;}
  void OnClosing(object sender,System.ComponentModel.CancelEventArgs e){if(busy){e.Cancel=true;Log("Finish connecting before closing.");return;}try{CancelInventoryJobs();engine.Detach();IntPtr hwnd=new WindowInteropHelper(this).Handle;foreach(int key in hotkeys)UnregisterHotKey(hwnd,key);if(source!=null)source.RemoveHook(Message);}catch(Exception ex){e.Cancel=true;Log("Could not restore a patch: "+ex.Message+". Keep the app open or exit the game before closing.");}}
 }
}
