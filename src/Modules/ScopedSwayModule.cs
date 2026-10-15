using System.Collections.Generic;
namespace FarTrainer
{
    public sealed class ScopedSwayModule : PatchModule
    {
        public override string Id {get{return "sway";}}
        public override IEnumerable<PatchDefinition> Definitions {get{return new[] {
            new PatchDefinition(Id,"0F 85 D7 00 00 00 BA",1,"85","86")
        };}}
    }
}
