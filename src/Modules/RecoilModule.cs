using System.Collections.Generic;
namespace FarTrainer
{
    public sealed class RecoilModule : PatchModule
    {
        public override string Id {get{return "recoil";}}
        public override IEnumerable<PatchDefinition> Definitions {get{return new[] {
            KnownHooks.Get("RecoilHook")
        };}}
    }
}
