using System.Collections.Generic;
namespace FarTrainer
{
    public sealed class OrdinaryAmmoModule : ResourceModule
    {
        public override string Id {get{return "ammo";}}
        public override uint[] ResourceIds {get{return new uint[]{0xB3F8DE3E,0xDB72B88A,0xB3F944C4,0x1B93266E,0xDB707C3F,0xDB75CA49,0xB3FA4191,0xB3F97273,0xB3F959AC,0x7A75CFE2,0xDB78B52A,0xB3F9A6CB};}}
        public override IEnumerable<PatchDefinition> Definitions {get{return new[] {
            new PatchDefinition(Id,"75 0D 8B 57 4C 39 D3 0F 42 D3 29 57 4C 89 D3",0,"75 0D 8B 57 4C 39 D3 0F 42 D3 29 57 4C 89 D3",null) {AmmoHook=true}
        };}}
    }
}
