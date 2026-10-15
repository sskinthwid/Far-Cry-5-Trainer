using System.Collections.Generic;
namespace FarTrainer {
 public sealed class FilteredResourceModule : ResourceModule {
  readonly string id;readonly uint[] ids;
  public FilteredResourceModule(string id,params uint[] ids){this.id=id;this.ids=ids;}
  public override string Id{get{return id;}}
  public override uint[] ResourceIds{get{return ids;}}
  public static IEnumerable<ResourceModule> Extra(){
   yield return new FilteredResourceModule("grenades",0xDB4CB05F);
   yield return new FilteredResourceModule("molotovs",0x83021337);
   yield return new FilteredResourceModule("remote-explosives",0xB3F8942E);
   yield return new FilteredResourceModule("proximity-explosives",0xB3F9C0EB);
   yield return new FilteredResourceModule("throwing-knives",0x8302EC24);
   yield return new FilteredResourceModule("dynamite",0x9AE00408);
   yield return new FilteredResourceModule("pipe-bombs",0x5BD2B432);
  }
 }
 public sealed class NamedResourceModule : ResourceModule {
  readonly string id,name;readonly bool prefix;
  public NamedResourceModule(string id,string name,bool prefix=false){this.id=id;this.name=name;this.prefix=prefix;}
  public override string Id{get{return id;}}
  public override uint[] ResourceIds{get{return new uint[0];}}
  public override string[] ResourceNames{get{return new[]{name};}}
  public override bool NamePrefix{get{return prefix;}}
  public static IEnumerable<ResourceModule> Extra(){
   yield return new NamedResourceModule("cash","Money/Money");
   yield return new NamedResourceModule("mars-money","Money_DLCM/DLC_Mars_Silicgel");
   yield return new NamedResourceModule("medkits","Medkit/HealthPack");
   yield return new NamedResourceModule("crafting","LootItem/CraftingComponents/",true);
  }
 }
}
