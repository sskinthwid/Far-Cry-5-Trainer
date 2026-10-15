using System;using System.Collections.Generic;
namespace FarTrainer {
 public static class BallisticsModules {
  public static IEnumerable<ITrainerModule> Create(){
   yield return H("projectile-speed","ProjectileSpeedHook","49 8B 4E 50 F3 44 0F 10 80 14 05 00 00 F3 44 0F 59 81 DC 07 00 00 E8",4,"F3 44 0F 10 80 14 05 00 00",true);
   yield return H("projectile-damage","ProjectileDamageHook","4C 89 F9 F3 0F 58 F7 0F 28 CE E8",3,"F3 0F 58 F7 0F 28 CE",true);
   yield return H("jump-speed","JumpSpeedHook","48 8B 4B 10 0F 28 F0 E8 ? ? ? ? 48 8B 43 10",0,"48 8B 4B 10 0F 28 F0",true);
   yield return H("swim-speed","SwimSpeedHook","F3 0F 10 90 AC 00 00 00 48 8B 43 20 45 33 C9",8,"48 8B 43 20 45 33 C9",true);
   yield return H("bow-draw","BowDrawHook","F3 0F 10 8A D4 01 00 00 E8 ? ? ? ? 0F 28",0,"F3 0F 10 8A D4 01 00 00",true);
   yield return H("fishing-reel","FishingReelHook","E8 ? ? ? ? 0F 28 F0 F3 41 0F 59 F4",5,"0F 28 F0 F3 41 0F 59 F4",false);
   yield return H("fish-weight","FishWeightHook","0F 29 B4 24 30 02 00 00 F3 0F 10 B0 D0 01 00 00 E8 ? ? ? ? 39 05",8,"F3 0F 10 B0 D0 01 00 00",false);
   yield return new HookModule("fishing-line",new[]{new PatchDefinition("FishingLineHook","0F 2F 81 7C 02 00 00 72 ? E8 ? ? ? ? F3 0F 10 80 28 01 00 00 0F 2F 83 D4 03 00 00",0,"0F 2F 81 7C 02 00 00","F9 90 90 90 90 90 90")});
  }
  static HookModule H(string id,string hook,string signature,int offset,string original,bool player){return new HookModule(id,new[]{new PatchDefinition(hook,signature,offset,original,null){Hook=hook}},player?new[]{"HealthHook"}:new string[0]);}
 }
 public sealed partial class TrainerEngine {
  internal static void EmitBallistics(Emitter c,long cave,long healthCell,string id){
   bool projectile=id=="ProjectileSpeedHook",damage=id=="ProjectileDamageHook",bow=id=="BowDrawHook",jump=id=="JumpSpeedHook",swim=id=="SwimSpeedHook";
   if(projectile)c.Hex("F3 44 0F 10 80 14 05 00 00");
   if(damage)c.Hex("F3 0F 58 F7");
   if(bow)c.Hex("F3 0F 10 8A D4 01 00 00");
   if(id=="FishWeightHook")c.Hex("F3 0F 10 B0 D0 01 00 00");
   if(id=="FishingReelHook")c.Hex("0F 28 F0 F3 41 0F 59 F4");
   c.Hex("9C 41 52 41 53");
   if(projectile||damage||bow||jump||swim){
    c.Rip("4C 8B 1D",healthCell);c.Hex("4D 85 DB");c.Branch("0F 84","done");
    c.Hex("4D 8B 5B 08 4D 85 DB");c.Branch("0F 84","done");c.Hex("49 83 7B 10 00");c.Branch("0F 84","done");
    if(damage){c.Hex("4C 8B 54 24 68 4D 85 D2");c.Branch("0F 84","done");c.Hex("4D 8B 52 50");}
    else c.Hex(bow?"49 89 D2":"49 89 CA");
    c.Hex("4D 85 D2");c.Branch("0F 84","done");
    if(jump||swim){c.Hex("4D 39 5A 08");c.Branch("0F 85","done");}
    else {c.Hex("4D 39 5A 70");c.Branch("0F 85","done");c.Hex("4D 39 9A 80 00 00 00");c.Branch("0F 85","done");}
    c.Rip("4C 89 15",cave+0x810);
   }
   c.Rip("48 FF 05",cave+0x820);
   string save=projectile?"F3 44 0F 11 05":damage||id=="FishWeightHook"||id=="FishingReelHook"?"F3 0F 11 35":bow?"F3 0F 11 0D":jump?"F3 0F 11 05":"F3 0F 11 15";
   c.Rip(save,cave+0x828);
   if(bow)c.Rip("F3 0F 10 0D",cave+0x800);
   else {string mul=projectile?"F3 44 0F 59 05":damage||id=="FishWeightHook"||id=="FishingReelHook"?"F3 0F 59 35":jump?"F3 0F 59 05":"F3 0F 59 15";c.Rip(mul,cave+0x800);c.Rip(mul.Replace("59","5D"),cave+0x808);}
   c.Rip(save,cave+0x82C);c.Label("done");c.Hex("41 5B 41 5A 9D");
   if(damage)c.Hex("0F 28 CE");if(jump)c.Hex("48 8B 4B 10 0F 28 F0");if(swim)c.Hex("48 8B 43 20 45 33 C9");
  }
 }
}
