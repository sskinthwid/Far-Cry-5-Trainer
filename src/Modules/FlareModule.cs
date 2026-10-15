using System.Collections.Generic;
namespace FarTrainer
{
    public sealed class FlareModule : PatchModule
    {
        public override string Id {get{return "flares";}}
        public override IEnumerable<PatchDefinition> Definitions {get{return new[] {
            new PatchDefinition(Id,"74 04 FF 4C C6 74",0,"74","EB")
        };}}
    }
}
