using System.Collections.Generic;
namespace FarTrainer
{
    public sealed class SpreadModule : PatchModule
    {
        public override string Id {get{return "spread";}}
        public override IEnumerable<PatchDefinition> Definitions {get{return new[] {
            new PatchDefinition(Id,"F3 0F 11 90 ? ? ? ? 0F 28 B4 24",-5,"B0","80")
        };}}
    }
}
