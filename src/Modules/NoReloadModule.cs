using System.Collections.Generic;
namespace FarTrainer
{
    public sealed class NoReloadModule : HookModule
    {
        public NoReloadModule():base("no-reload",new[] {
            new PatchDefinition("no-reload","44 0F 4C C0 44 89 83 88 01 00 00",4,"44 89 83 88 01 00 00",null){Hook="PlayerMagazineHook"}
        },new[]{"HealthHook"}){}
    }
}
