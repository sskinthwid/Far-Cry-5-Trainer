using System.Collections.Generic;
namespace FarTrainer
{
    public sealed class StoreUnlockModule : PatchModule
    {
        public override string Id {get{return "store";}}
        public override IEnumerable<PatchDefinition> Definitions {get{return new[] {
            new PatchDefinition(Id,"48 8B 47 50 48 39 43 50 0F",0,"48 8B 47 50 48 39 43 50",null) {StoreHook=true}
        };}}
    }
}
