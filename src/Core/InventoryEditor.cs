using System;using System.Collections.Generic;using System.IO;using System.Linq;using System.Text;
namespace FarTrainer {
 public sealed class InventoryEntry {public string Name;public int Value;public long Pointer,Context,Descriptor,Owner;}
 public sealed partial class TrainerEngine {
  readonly Dictionary<string,InventoryEntry> inventoryEntries=new Dictionary<string,InventoryEntry>();long inventoryOwner;
  public void StartInventoryScan(){Set("observer-resource",true);}
  public List<InventoryEntry> InventorySnapshot(){
   if(Memory==null||!Memory.Alive||!PatchActive("ResourceQueryHook"))return new List<InventoryEntry>();
   long owner;try{long health=Captured("HealthHook");owner=health==0?0:Q(health+8);if(owner==0||Q(owner+0x10)==0)return new List<InventoryEntry>();}catch{inventoryEntries.Clear();inventoryOwner=0;return new List<InventoryEntry>();}
   if(owner!=inventoryOwner){inventoryEntries.Clear();inventoryOwner=owner;}
   var hook=Patches["ResourceQueryHook"];
   for(int i=0;i<64;i++)try{
    long counter=Q(hook.Cave+0xA00+i*16),context=Q(hook.Cave+0xA08+i*16);
    if(counter==0||context==0||Q(context+8)!=owner)continue;
    long descriptor=Q(counter+0x28),namePointer=Q(descriptor+0x10)+0xC;
    string name=Encoding.ASCII.GetString(Memory.Read(namePointer,160)).Split('\0')[0];
    if(!EditableResource(name))continue;
    int value=BitConverter.ToInt32(Memory.Read(counter+0x4C,4),0);if(value<0)continue;
    inventoryEntries[name+"@"+counter.ToString("X")]=new InventoryEntry{Name=name,Value=value,Pointer=counter,Context=context,Descriptor=descriptor,Owner=owner};
   }catch{}
   foreach(string name in inventoryEntries.Keys.ToArray())try{var entry=inventoryEntries[name];if(Q(entry.Context+8)!=owner||Q(entry.Pointer+0x28)!=entry.Descriptor)inventoryEntries.Remove(name);else entry.Value=BitConverter.ToInt32(Memory.Read(entry.Pointer+0x4C,4),0);}catch{inventoryEntries.Remove(name);}
   return inventoryEntries.Values.Where(v=>v.Value>=0).OrderBy(v=>v.Name).Select(v=>new InventoryEntry{Name=v.Name,Value=v.Value,Pointer=v.Pointer,Context=v.Context,Descriptor=v.Descriptor,Owner=v.Owner}).ToList();
  }
  public void CancelInventoryEdit(){ResolvedPatch hook;if(Memory!=null&&Memory.Alive&&Patches.TryGetValue("ResourceQueryHook",out hook)&&hook.Cave!=0)Memory.Write(hook.Cave+0xE00,BitConverter.GetBytes(0L));}
  public bool InventoryEditPending{get{ResolvedPatch hook;return Memory!=null&&Memory.Alive&&Patches.TryGetValue("ResourceQueryHook",out hook)&&hook.Cave!=0&&Q(hook.Cave+0xE00)!=0;}}
  public static bool EditableResource(string name){return name=="Money/Money"||name=="Money_DLCM/DLC_Mars_Silicgel"||name=="Medkit/HealthPack"||(name.StartsWith("Ammo/",StringComparison.Ordinal)&&!name.StartsWith("Ammo/NPC",StringComparison.Ordinal)&&name!="Ammo/GenericInfinitePouch")||name.StartsWith("LootItem/CraftingComponents/",StringComparison.Ordinal);}
  public void QueueInventoryValue(InventoryEntry entry,int value){
   if(value<0||value>1000000000)throw new ArgumentException("Enter a whole quantity between 0 and 1,000,000,000.");
   var current=InventorySnapshot().FirstOrDefault(x=>x.Name==entry.Name&&x.Pointer==entry.Pointer&&x.Descriptor==entry.Descriptor&&x.Owner==entry.Owner);
   if(current==null)throw new IOException("This resource is no longer in the current player context. Refresh inventory.");
   if(current.Value!=entry.Value)throw new IOException("Quantity changed while staging this edit. Refresh and retry.");
   long cell=Patches["ResourceQueryHook"].Cave+0xE00;
   if(Q(cell)!=0)throw new IOException("The previous edit is waiting for the game to read that resource. Open its inventory/store screen or use the item.");
   Memory.Write(cell+8,BitConverter.GetBytes(entry.Descriptor));Memory.Write(cell+16,BitConverter.GetBytes(entry.Owner));Memory.Write(cell+24,BitConverter.GetBytes(value));Memory.Write(cell+28,BitConverter.GetBytes(current.Value));Memory.Write(cell+48,BitConverter.GetBytes(0));Memory.Write(cell,BitConverter.GetBytes(entry.Pointer));
   Say("Queued "+entry.Name+" = "+value+". Applied when the game next queries this resource.");
  }
  internal static void EmitInventoryQuery(Emitter c,long cave,long healthCell){
   c.Hex("9C 52 41 50");
   c.Rip("4C 8B 05",healthCell);c.Hex("4D 85 C0");c.Branch("0F 84","query-original");
   c.Hex("4D 8B 40 08 4D 85 C0");c.Branch("0F 84","query-original");
   c.Hex("4C 39 47 08");c.Branch("0F 85","query-original");c.Hex("49 83 78 10 00");c.Branch("0F 84","query-original");
   c.Hex("48 89 C2 48 C1 EA 04 83 E2 3F 48 C1 E2 04");c.Rip("48 8D 0D",cave+0xA00);c.Hex("48 89 04 11 48 89 7C 11 08");
   c.Rip("48 3B 05",cave+0xE00);c.Branch("0F 85","query-original");
   c.Rip("4C 3B 05",cave+0xE10);c.Branch("0F 85","query-original");
   c.Hex("48 8B 50 28");c.Rip("48 3B 15",cave+0xE08);c.Branch("0F 85","query-original");
   c.Hex("8B 50 4C");c.Rip("3B 15",cave+0xE1C);c.Branch("0F 85","edit-changed");
   c.Rip("8B 15",cave+0xE18);c.Hex("89 50 4C");c.Rip("48 FF 05",cave+0xE20);c.Rip("C7 05",cave+0xE30,4);c.Hex("01 00 00 00");c.Branch("E9","edit-ack");
   c.Label("edit-changed");c.Rip("C7 05",cave+0xE30,4);c.Hex("02 00 00 00");c.Label("edit-ack");c.Rip("48 C7 05",cave+0xE00,4);c.Hex("00 00 00 00");
   c.Label("query-original");c.Hex("41 58 5A 9D 48 8D 48 40 48 8B 40 40");
  }
  public string InventoryEditResult {get{ResolvedPatch hook;if(Memory==null||!Memory.Alive||!Patches.TryGetValue("ResourceQueryHook",out hook)||hook.Cave==0)return "";int result=BitConverter.ToInt32(Memory.Read(hook.Cave+0xE30,4),0);return InventoryEditPending?"Edit pending game query":result==1?"Last edit applied":result==2?"Quantity changed before the edit; refresh and retry":"";}}
 }
}
