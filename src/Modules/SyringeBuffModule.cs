using System.Collections.Generic;
namespace FarTrainer
{
    public sealed class SyringeBuffModule : PatchModule
    {
        public override string Id {get{return "buffs";}}
        public override IEnumerable<PatchDefinition> Definitions {get{return new[] {
            new PatchDefinition("buff-real","76 18 F3 0F 5C C1 0F 2F C2 F3 0F 11 41 64",0,"76","EB"),
            new PatchDefinition("buff-ui","74 5E F3 0F 10 43 08 8B 43 10 F3 0F 5C C6 F3 0F 11 43 08",0,"74","EB")
        };}}
    }
}
