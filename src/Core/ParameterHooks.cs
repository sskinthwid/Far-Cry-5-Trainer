using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace FarTrainer {
 public sealed partial class TrainerEngine {
  public readonly Dictionary<string,double> Values=new Dictionary<string,double>();
  public static readonly Dictionary<string,string> ParameterOwners=new Dictionary<string,string>{
   {"ProjectileSpeedHook","projectile-speed"},{"ProjectileDamageHook","projectile-damage"},{"JumpSpeedHook","jump-speed"},{"SwimSpeedHook","swim-speed"},{"FishingReelHook","fishing-reel"},{"FishWeightHook","fish-weight"},
   {"HealthDecHook","health"},{"HealthDecFallHook","no-fall-damage"},{"HealthDecWingsuitHook","no-fall-damage"},
   {"FallHeightLimitHook","no-ragdoll"},{"MoveSpeedHook","move-speed"},{"EnergyOverheatHook","energy-overheat"},
   {"BodyArmorDecHook","armor"},{"StaminaWrtHook","stamina"},{"OxygenDecHook","oxygen"},
   {"DeltaMulHook","speed"},{"XpIncHook","xp"},{"AmmoIncHook","pickup"},{"AmmoCapHook","capacity"},
   {"PerkPointsIncHook","perk-reward"},{"DrugTimerStartHook","buff-duration"},
   {"TimeIncHook","time-scale"},{"ActionHoldHook","interaction-delay"},
   {"CombatantHealthDecHook","npc-damage"},{"CombatantBodyArmorDecHook","npc-armor"},{"AnimalHealthDecHook","animal-damage"},{"WeaponFireRateHook","rapid-fire"},{"RecoilHook","recoil"}
  };
  public void Configure(string id,double value){
   if(double.IsNaN(value)||double.IsInfinity(value)||value<0||value>float.MaxValue)throw new ArgumentException("Enter a finite, non-negative number.");
   if((id=="speed"||id=="projectile-speed"||id=="jump-speed"||id=="swim-speed"||id=="move-speed") && (float)value==0)throw new ArgumentException("Speed must be greater than zero.");
   if(id=="rapid-fire"&&value==0)throw new ArgumentException("Fire rate must be greater than zero.");
   if((id=="speed"||id=="rapid-fire")&&(float)value==0)throw new ArgumentException("This positive value is too small for the game’s floating-point format.");
   if(id=="health"||id=="armor"||id=="stamina"||id=="oxygen"||id=="no-fall-damage"||id=="energy-overheat"||id=="stealth"||id=="auto-teleport-waypoint"||id=="one-hit-kill"||id=="recoil"||id=="mission-timer")value=0;
   if(id=="no-ragdoll")value=1000;
   foreach(var p in Patches.Values.Where(p=>p.Cave!=0&&ParameterOwners.ContainsKey(p.Definition.Id)&&ParameterOwners[p.Definition.Id]==id))Memory.Write(p.Cave+0x800,BitConverter.GetBytes((float)value));
   Values[id]=value;
  }
  private double ValueFor(string hook){string owner;if(ParameterOwners.TryGetValue(hook,out owner)){if(owner=="no-ragdoll")return 1000;if(Values.ContainsKey(owner))return Values[owner];if(owner=="health"||owner=="armor"||owner=="stamina"||owner=="oxygen"||owner=="no-fall-damage"||owner=="energy-overheat"||owner=="recoil")return 0;}return hook=="ActionHoldHook"?0.075:1;}
  public long Captured(string id,int offset=0){ResolvedPatch p;return Memory!=null&&Memory.Alive&&Patches.TryGetValue(id,out p)&&p.Cave!=0?BitConverter.ToInt64(Memory.Read(p.Cave+0x810+offset,8),0):0;}
  private long Cell(string id){var p=Patches[id];if(p.Cave==0)PrepareHook(p);return p.Cave+0x810;}
  private void PrepareHook(ResolvedPatch p){
   if(p.Cave!=0)return;
   long cave=Memory.AllocateNear(p.Address);long param=cave+0x800;long pointer=cave+0x810;
   var c=new Emitter(cave);string id=p.Definition.Hook;
   Memory.Write(param,BitConverter.GetBytes((float)ValueFor(id)));
   Memory.Write(cave+0x808,BitConverter.GetBytes(2147483520f));
   Memory.Write(pointer,new byte[64]);
   switch(id){
    case "ProjectileSpeedHook":case "ProjectileDamageHook":case "JumpSpeedHook":case "SwimSpeedHook":case "BowDrawHook":EmitBallistics(c,cave,Cell("HealthHook"),id);break;
    case "FishingReelHook":case "FishWeightHook":EmitBallistics(c,cave,0,id);break;
    case "ResourceQueryHook":Memory.Write(cave+0xA00,new byte[0x440]);EmitInventoryQuery(c,cave,Cell("HealthHook"));break;
    case "WeaponFireRateHook":EmitPlayerWeapon(c,cave,Cell("HealthHook"),false);break;
    case "PlayerMagazineHook":EmitPlayerWeapon(c,cave,Cell("HealthHook"),true);break;
    case "HealthHook":c.Rip("48 89 0D",pointer);c.Raw(p.Original);break;
    case "VehicleHealthHook":
     EmitVehicleGuard(c,cave,Cell("HealthHook"));c.Raw(p.Original);break;
    case "VehicleCoordHook":case "CoordHook":case "PrincipalAxesHook":case "TimeHook":c.Rip("48 89 05",pointer);c.Raw(p.Original);break;
    case "WaypointHook":c.Rip("48 89 0D",pointer);c.Raw(p.Original);break;
    case "DeltaMulHook":c.Rip("48 89 05",pointer);c.Raw(p.Original);c.Rip("F3 0F 59 35",param);break;
    case "FallHeightLimitHook":
     c.Raw(p.Original);
     c.Rip("F3 44 0F 59 05",param);
     c.Rip("F3 44 0F 59 0D",param);
     c.Rip("F3 44 0F 59 15",param);
     c.Rip("F3 44 0F 59 1D",param);
     c.Rip("F3 44 0F 59 25",param);break;
    case "MoveSpeedHook":
     c.Raw(p.Original);
     c.Rip("F3 44 0F 59 25",param);
     c.Hex("F3 0F 10 65 20");c.Rip("F3 0F 59 25",param);c.Hex("F3 0F 11 65 20");
     c.Hex("F3 0F 10 65 30");c.Rip("F3 0F 59 25",param);c.Hex("F3 0F 11 65 30");break;
    case "EnergyOverheatHook":
     c.Hex("0F 57 C9");c.Raw(p.Original);break;
    case "RecoilHook":
     c.Hex("9C 50");
     c.Rip("48 8B 05",Cell("HealthHook"));
     c.Hex("48 85 C0");c.Branch("0F 84","ordinary");
     c.Hex("48 8B 40 08 48 85 C0");c.Branch("0F 84","ordinary");
     c.Hex("49 39 45 70");c.Branch("0F 85","ordinary");
     c.Hex("49 39 85 80 00 00 00");c.Branch("0F 85","ordinary");
     c.Rip("F3 0F 59 15",param);
     c.Label("ordinary");c.Hex("58 9D");
     c.Hex("41 B1 01 BA 05 00 00 00");break;
    case "HealthDecHook":
     c.SaveXmm0();c.Rip("4C 3B 25",Cell("HealthHook"));c.Branch("0F 85","original");
     c.Hex("F3 41 0F 10 06");c.Rip("F3 0F 59 05",param);c.Hex("F3 0F 5C F0");c.Branch("E9","done");
     c.Label("original");c.Raw(p.Original);c.Label("done");c.RestoreXmm0();break;
    case "HealthDecFallHook":case "HealthDecWingsuitHook":
     // Player-specific environmental routines. Preserve the source damage SIMD register.
     c.Hex("9C 48 83 EC 20 F3 0F 7F 0C 24");
     c.Hex(id=="HealthDecFallHook"?"0F 28 CE":"0F 28 CF");c.Rip("F3 0F 59 0D",param);c.Hex("F3 0F 5C C1 F3 0F 6F 0C 24 48 83 C4 20 9D");
     c.Hex(id=="HealthDecFallHook"?"4D 8B C7":"4C 8B C5");break;
    case "BodyArmorDecHook":
     c.Hex("9C 48 83 EC 20 F3 0F 7F 0C 24");c.Rip("48 3B 1D",Cell("HealthHook"));c.Branch("0F 85","ordinary");
     c.Hex("0F 28 CF");c.Rip("F3 0F 59 0D",param);c.Hex("F3 0F 5C C1");c.Branch("E9","done");
     c.Label("ordinary");c.Hex("F3 0F 5C C7");c.Label("done");c.Hex("F3 0F 6F 0C 24 48 83 C4 20 9D 45 33 C9");break;
    case "StaminaWrtHook":
     c.SaveXmm0();c.Raw(p.Original);c.Rip("48 3B 1D",Cell("HealthHook"));c.Branch("0F 85","done");
     c.Hex("0F 57 C0 0F 2F 83 58 04 00 00");c.Branch("0F 82","done");c.Rip("F3 0F 59 3D",param);
     c.Label("done");c.RestoreXmm0();break;
    case "OxygenDecHook":c.Rip("F3 0F 59 05",param);c.Raw(p.Original);break;
    case "XpIncHook":case "PerkPointsIncHook":case "AmmoCapHook":
     c.SaveXmm0();c.Hex(id=="XpIncHook"?"F3 0F 2A C3":id=="PerkPointsIncHook"?"F3 0F 2A C6":"F3 0F 2A C2");
     c.Rip("F3 0F 59 05",param);c.Rip("F3 0F 5D 05",cave+0x808);
     c.Hex(id=="XpIncHook"?"F3 0F 2C D8":id=="PerkPointsIncHook"?"F3 0F 2C F0":"F3 0F 2C D0");c.RestoreXmm0();c.Raw(p.Original);break;
    case "AmmoIncHook":
     c.SaveXmm0();
     c.Hex("41 83 FD 00");c.Branch("0F 8E","done");
     c.Hex("F3 41 0F 2A C5");c.Rip("F3 0F 59 05",param);c.Rip("F3 0F 5D 05",cave+0x808);c.Hex("F3 44 0F 2C E8");
     c.Label("done");c.RestoreXmm0();c.Raw(p.Original);break;
    case "DrugTimerStartHook":c.Rip("F3 0F 59 05",param);c.Rip("F3 0F 11 05",pointer+8);c.Raw(p.Original);break;
    case "DrugTimerStart2Hook":c.Rip("8B 0D",Cell("DrugTimerStartHook")+8);c.Raw(p.Original);break;
    case "TimeIncHook":
     c.Hex("9C");c.Raw(p.Original);c.Rip("48 3B 0D",Cell("TimeHook"));c.Branch("0F 85","done");c.Rip("F3 0F 59 0D",param);c.Label("done");c.Hex("9D");break;
    case "ActionHoldHook":
     c.Hex("9C 49 81 FA 00 04 00 00");c.Branch("0F 85","ordinary");c.Rip("0F 2F 05",param);c.Branch("0F 82","ordinary");
     c.Hex("F3 0F 11 4C 9F 08");c.Branch("E9","done");c.Label("ordinary");c.Raw(p.Original);c.Label("done");c.Hex("9D");break;
    case "TurretHeatIncHook":c.Hex("31 FF");c.Raw(p.Original);break;
    case "GravityBeltFuelDecHook":c.Hex("F3 0F 10 4B 48");c.Raw(p.Original);break;
    case "GravityBeltFuelIncHook":c.Raw(p.Original);break;
    case "CombatantHealthDecHook":
     c.SaveXmm0();c.Hex("0F 28 C2");c.Rip("F3 0F 59 05",param);c.Hex("F3 44 0F 5C C8");c.RestoreXmm0();break;
    case "CombatantBodyArmorDecHook":
     c.SaveXmm0();c.Hex("F3 0F 10 44 24 58");c.Rip("F3 0F 59 05",param);c.Hex("F3 0F 5C F8");c.RestoreXmm0();break;
    case "AnimalHealthDecHook":
     c.SaveXmm0();c.Hex("41 0F 28 C0");c.Rip("F3 0F 59 05",param);c.Hex("F3 0F 5C F0");c.RestoreXmm0();break;
    default:throw new InvalidOperationException("No native emitter for "+id);
   }
   c.Jump(p.Address+p.Original.Length);byte[] code=c.Finish();if(code.Length>=0x800)throw new IOException("Hook is too large.");
   Memory.Write(cave,code);p.Cave=cave;p.EnabledBytes=Jump(p.Address,cave,p.Original.Length);
  }
  internal static void EmitVehicleGuard(Emitter c,long cave,long healthCell){
   c.Rip("48 89 05",cave+0x810);c.Rip("48 89 1D",cave+0x818);
   c.Hex("9C 51");c.Rip("80 3D",cave+0x800,1);c.Hex("01");c.Branch("0F 85","original");
   c.Rip("48 8B 0D",healthCell);c.Hex("48 85 C9");c.Branch("0F 84","original");
   c.Hex("48 8B 49 08");c.Rip("48 3B 0D",cave+0x830);c.Branch("0F 85","original");
   c.Hex("48 85 C9");c.Branch("0F 84","original");c.Hex("48 83 79 10 00");c.Branch("0F 84","original");
   c.Rip("48 8B 0D",cave+0x838);c.Hex("48 85 C9");c.Branch("0F 84","original");
   c.Hex("48 8B 49 08 48 85 C9");c.Branch("0F 84","original");
   c.Hex("48 3B 48 08");c.Branch("0F 85","original");
   c.Rip("48 FF 05",cave+0x840);
   c.Hex("F3 0F 10 88 44 01 00 00 F3 0F 11 88 48 01 00 00");
   c.Label("original");c.Hex("59 9D");
  }
  internal static void EmitPlayerWeapon(Emitter c,long cave,long healthCell,bool magazine){
   if(!magazine)c.Hex("F3 0F 10 50 74");
   c.Hex("9C 50");
   c.Rip("48 8B 05",healthCell);c.Hex("48 85 C0");c.Branch("0F 84","ordinary");
   c.Hex("48 8B 40 08 48 85 C0");c.Branch("0F 84","ordinary");
   // CWeapon's wielder and owner must BOTH be this captured local player.
   c.Hex(magazine?"48 39 43 70":"48 39 41 70");c.Branch("0F 85","ordinary");
   c.Hex(magazine?"48 39 83 80 00 00 00":"48 39 81 80 00 00 00");
   c.Branch("0F 85","ordinary");
   c.Hex("48 83 78 10 00");c.Branch("0F 84","ordinary");
   c.Rip(magazine?"48 89 1D":"48 89 0D",cave+0x810);
   c.Rip("48 FF 05",cave+0x820);
   if(!magazine){c.Rip("F3 0F 11 15",cave+0x828);c.Rip("F3 0F 59 15",cave+0x800);c.Rip("F3 0F 5D 15",cave+0x808);c.Rip("F3 0F 11 15",cave+0x82C);}
   c.Branch("E9","done");
   c.Label("ordinary");if(magazine)c.Hex("44 89 83 88 01 00 00");
   c.Label("done");c.Hex("58 9D");
  }
 }
}
