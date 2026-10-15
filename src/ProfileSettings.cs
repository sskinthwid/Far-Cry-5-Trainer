using System;using System.Collections.Generic;using System.Diagnostics;using System.Globalization;using System.IO;using System.Linq;using System.Xml;using System.Xml.Linq;
namespace FarTrainer {
 public sealed class SettingDefinition {
  public string Path,Attribute,Name,Group;public double Min,Max;public string[] Choices;public bool Boolean;
  public string Id{get{return Path+"@"+Attribute;}}
  public string Validate(string value){
   if(Boolean){if(value!="0"&&value!="1")throw new ArgumentException(Name+" must be on or off.");return value;}
   if(Choices!=null){if(!Choices.Contains(value))throw new ArgumentException("Unsupported value for "+Name);return value;}
   double n;if(!double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out n)||double.IsNaN(n)||double.IsInfinity(n)||n<Min||n>Max)throw new ArgumentException(Name+" must be between "+Min+" and "+Max+".");
   if((Attribute=="width"||Attribute=="height"||Attribute=="FrameRateLock"||Attribute=="MasterVolume"||Attribute=="VoiceChatPlaybackVolume")&&n!=Math.Truncate(n))throw new ArgumentException(Name+" requires a whole number.");
   return n.ToString("G9",CultureInfo.InvariantCulture);
  }
 }
 public sealed class ProfileSettings {
  public static readonly List<SettingDefinition> Definitions=Build();
  public static string DefaultPath{get{return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"My Games","Far Cry 5","gamerprofile.xml");}}
  public readonly string Path;readonly Func<bool> running;byte[] snapshot;XDocument document;
  public ProfileSettings(string path=null,Func<bool> running=null){Path=path??DefaultPath;this.running=running??(()=>Process.GetProcessesByName("FarCry5").Length!=0);}
  static List<SettingDefinition> Build(){
   var d=new List<SettingDefinition>();
   Action<string,string,string,bool,double,double> add=(path,key,group,boolean,min,max)=>d.Add(new SettingDefinition{Path=path,Attribute=key,Name=System.Text.RegularExpressions.Regex.Replace(key,"([a-z])([A-Z])","$1 $2").Replace('_',' '),Group=group,Boolean=boolean,Min=min,Max=max});
   foreach(string key in new[]{"HelpCrosshair","UseRoadSignHilight","TaggingEnabled","TutorialUpdatedEnabled","DetectionIndicatorEnabled","ExplosiveIndicatorEnabled","CompassEnabled","HealthIndicatorEnabled","CrouchIndicatorEnabled","AmmoIndicatorEnabled","LootReadoutEnabled","CoopPartnerIndicatorEnabled","ObjectiveStayOnScreen","InteractionPromptsEnabled","TooltipsEnabled","ShowDamageFeedback","ShowResistanceUpdates","ShowPerkUpdates","ShowBuffTimers","WorldIconsEnabled","EnemyTagIconsEnabled","GFHIconsEnabled","ChallengeMessagesEnabled","DiscoveryMessagesEnabled","OtherMessagesEnabled","ShowVehicleRepairGauge","ShowVehicleIndicators","ShowGFHControlsWidget","ShowOnScreenControls","RegionDiscoveryMessageEnabled","GPSFeedback","NetworkMessages","IntelAcquiredMessage","WarningMessage","CultUpdateMessagesEnabled"})add("ProfileSpecificGameProfile",key,"HUD",true,0,1);
   foreach(string key in new[]{"UseMouseSmooth","MouseAccelerationOn","MouseInvert_y","Mouse_Invert_y_Plane","Mouse_Invert_y_Helicopter","Mouse_Invert_y_Wingsuit","RunToggle","CrouchToggle","UseAlternativePlaneControl","DriveAndShootAssistKbAndMouseEnabled"})add("ProfileSpecificGameProfile",key,"Mouse and keyboard",true,0,1);
   foreach(string key in new[]{"MouseLookSensitivity","MouseZoomLookSensitivity","MortarZoomLookSensitivity","BomberZoomLookSensitivity","BinocularZoomLookSensitivity","PlaneMouseControlSensitivity"})add("ProfileSpecificGameProfile",key,"Mouse and keyboard",false,0.01,100);
   add("ProfileSpecificGameProfile","MouseAcceleration","Mouse and keyboard",false,0,10);
   foreach(string key in new[]{"Invert_x","Invert_y","Invert_y_Plane","Invert_y_Helicopter","Invert_y_Wingsuit","Swap_Throttle","Swap_Triggers","InvertSticks","Gamepad_vibration","UseAimingHelpers","DrivingAssistEnabled","DrivingAutoAimEnabled","TouchPadEnabled","SpeakerPadEnabled"})add("ProfileSpecificGameProfile",key,"Controller",true,0,1);
   foreach(string key in new[]{"Sensitivity","ZoomSensitivity","Smoothness","Smoothness_Ironsight"})add("ProfileSpecificGameProfile",key,"Controller",false,0.01,10);
   foreach(string key in new[]{"EyeTrackingEnabled","EyeTrackingExtendedViewEnabled","EyeTrackingExtendedViewInIronsightEnabled","EyeTrackingAimAtGazeEnabled","EyeTrackingBinocularsAtGazeEnabled","EyeTrackingEnemyTaggingEnabled","EyeTrackingAlliedCommandingEnabled","EyeTrackingDynamicLightAdaptationEnabled","CorsairRGBLighting"})add("ProfileSpecificGameProfile",key,"Devices",true,0,1);
   foreach(string key in new[]{"EyeTrackingExtendedViewResponsiveness","EyeTrackingExtendedViewGazeContribution","EyeTrackingExtendedViewHeadContribution"})add("ProfileSpecificGameProfile",key,"Devices",false,0,1);
   add("SoundProfile","MusicEnabled","Audio",true,0,1);add("SoundProfile","MasterVolume","Audio",false,0,100);add("SoundProfile","VoiceChatPlaybackVolume","Audio",false,0,100);add("ProfileSpecificGameProfile","MuteAllVoiceChats","Audio",true,0,1);add("NetworkProfile","VoiceChatEnabled","Audio",true,0,1);
   foreach(string key in new[]{"MotionBlur","FrameRateLockEnabled","DynamicResolutionEnabled","HDTexturesEnabled","ShowFPS"})add("RenderProfile",key,"Video",true,0,1);
   add("RenderProfile","FOV","Video",false,60,120);add("RenderProfile","FrameRateLock","Video",false,30,1000);add("RenderProfile","ResolutionScale","Video",false,0.5,2);
   foreach(string key in new[]{"Brightness","Contrast","GammaRamp"})add("RenderProfile",key,"Video",false,0.5,2);
   add("RenderProfile","HDRReferenceWhiteNits","Video",false,80,1000);
   foreach(string key in new[]{"EnvironmentQuality","PostFxQuality","TextureQuality","VolumetricFogQuality","WaterQuality","VegetationQuality","TerrainQuality","GeometryQuality","AmbientQuality","ShadowQuality"})d.Add(new SettingDefinition{Path="RenderProfile/CustomQuality/quality",Attribute=key,Name=key.Replace("Quality"," quality"),Group="Graphics quality",Choices=new[]{"low","medium","high","ultrahigh"}});
   d.Add(new SettingDefinition{Path="RenderProfile",Attribute="Quality",Name="Graphics preset",Group="Video",Choices=new[]{"low","medium","high","ultrahigh","custom"}});
   d.Add(new SettingDefinition{Path="RenderProfile",Attribute="VSyncPreset",Name="VSync",Group="Video",Choices=new[]{"off","on"}});
   d.Add(new SettingDefinition{Path="RenderProfile",Attribute="AntiAliasingMode",Name="Anti-aliasing mode (0 / 1 / 2)",Group="Video",Choices=new[]{"0","1","2"}});
   foreach(string mode in new[]{"VideoModeProfileFullscreen","VideoModeProfileWindow"}){add("RenderProfile/"+mode,"width",mode=="VideoModeProfileWindow"?"Window resolution":"Fullscreen resolution",false,640,16384);add("RenderProfile/"+mode,"height",mode=="VideoModeProfileWindow"?"Window resolution":"Fullscreen resolution",false,480,16384);}
   return d;
  }
  static XDocument Parse(byte[] bytes){using(var stream=new MemoryStream(bytes))using(var reader=XmlReader.Create(stream,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=1024*1024}))return XDocument.Load(reader,LoadOptions.PreserveWhitespace);}
  XAttribute Attribute(SettingDefinition d){XElement n=document.Root;foreach(string part in d.Path.Split('/')){n=n==null?null:n.Element(part);}return n==null?null:n.Attribute(d.Attribute);}
  public Dictionary<string,string> Load(){snapshot=File.ReadAllBytes(Path);document=Parse(snapshot);if(document.Root==null||document.Root.Name!="GamerProfile")throw new IOException("Not a Far Cry GamerProfile document.");return Definitions.Where(d=>Attribute(d)!=null).ToDictionary(d=>d.Id,d=>Attribute(d).Value);}
  public string Save(Dictionary<string,string> values){
   if(running())throw new IOException("Close Far Cry 5 before saving settings. The game can overwrite its profile on exit. Your edits remain in this panel.");
   if(document==null||snapshot==null)throw new IOException("Load the game settings first.");
   if(!File.ReadAllBytes(Path).SequenceEqual(snapshot))throw new IOException("The game profile changed after loading. Reload settings before saving.");
   var checkedValues=new Dictionary<string,string>();foreach(var pair in values){var d=Definitions.SingleOrDefault(x=>x.Id==pair.Key);if(d==null||Attribute(d)==null)throw new IOException("Unsupported setting: "+pair.Key);checkedValues[pair.Key]=d.Validate(pair.Value);}
   foreach(var pair in checkedValues)Attribute(Definitions.Single(d=>d.Id==pair.Key)).Value=pair.Value;
   if(checkedValues.Keys.Any(k=>k.StartsWith("RenderProfile/CustomQuality/",StringComparison.Ordinal))&&document.Root.Element("RenderProfile")!=null)document.Root.Element("RenderProfile").SetAttributeValue("Quality","custom");
   string backup=Path+".hopecontrol-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+".bak",temp=Path+".hopecontrol.tmp";
   using(var writer=XmlWriter.Create(temp,new XmlWriterSettings{OmitXmlDeclaration=true,Encoding=new System.Text.UTF8Encoding(false),Indent=false}))document.Save(writer);
   Parse(File.ReadAllBytes(temp));File.Replace(temp,Path,backup);snapshot=File.ReadAllBytes(Path);return backup;
  }
  public string RestoreLatest(){
   if(running())throw new IOException("Close Far Cry 5 before restoring settings.");
   string folder=System.IO.Path.GetDirectoryName(Path),name=System.IO.Path.GetFileName(Path)+".hopecontrol-*.bak";
   string backup=Directory.GetFiles(folder,name).OrderByDescending(p=>p,StringComparer.Ordinal).FirstOrDefault();if(backup==null)throw new IOException("No settings backup exists.");
   byte[] bytes=File.ReadAllBytes(backup);if(Parse(bytes).Root.Name!="GamerProfile")throw new IOException("Invalid backup.");
   string temp=Path+".hopecontrol.tmp";File.WriteAllBytes(temp,bytes);File.Replace(temp,Path,Path+".before-restore.bak");Load();return backup;
  }
 }
}
