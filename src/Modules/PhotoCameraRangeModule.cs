using System.Collections.Generic;
namespace FarTrainer
{
    public sealed class PhotoCameraRangeModule : PatchModule
    {
        public override string Id {get{return "photo-range";}}
        // Skalledit's public camera constraints. Hotsampling is separate and excluded.
        public override IEnumerable<PatchDefinition> Definitions {get{return new[] {
            new PatchDefinition("photo-range-entry","EB 3C 0F 28 C6",0,"EB 3C","90 90"),
            Constraint("photo-range-x1","F3 41 0F 5C A4 24 AC 04 00 00"),
            Constraint("photo-range-x2","F3 41 0F 5C 8C 24 AC 04 00 00"),
            Constraint("photo-range-y1","F3 41 0F 5C AC 24 B0 04 00 00"),
            Constraint("photo-range-y2","F3 41 0F 5C 94 24 B0 04 00 00"),
            Constraint("photo-range-z","F3 41 0F 5C 9C 24 B4 04 00 00")
        };}}
        private static PatchDefinition Constraint(string id,string code)
        {return new PatchDefinition(id,code,0,code,"90 90 90 90 90 90 90 90 90 90");}
    }
}
