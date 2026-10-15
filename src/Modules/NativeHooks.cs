using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace FarTrainer
{
    public sealed partial class TrainerEngine
    {
        private static byte[] Jump(long from,long to,int length)
        {
            long delta=to-from-5;
            if (delta<int.MinValue||delta>int.MaxValue||length<5) throw new IOException("Jump out of range.");
            var bytes=Enumerable.Repeat((byte)0x90,length).ToArray(); bytes[0]=0xE9;
            Array.Copy(BitConverter.GetBytes((int)delta),0,bytes,1,4); return bytes;
        }
        private void PrepareStore(ResolvedPatch patch)
        {
            if (patch.Cave != 0) return;
            long cave=Memory.AllocateNear(patch.Address);
            // Replay the original load/compare, conditionally mark the same item unlocked,
            // then replay the compare so the original conditional branch receives its flags.
            var code=Hex("48 8B 47 50 48 39 43 50 75 0A 80 7F 3C 01 7D 04 C6 47 3C 02 48 39 43 50").ToList();
            code.AddRange(Jump(cave+code.Count,patch.Address+8,5));
            Memory.Write(cave,code.ToArray());
            patch.Cave=cave; patch.EnabledBytes=Jump(patch.Address,cave,8);
        }
        internal static void EmitNameCompare(Emitter c,string name,bool prefix,string mismatch){
            byte[] bytes=System.Text.Encoding.ASCII.GetBytes(name+(prefix?"":"\0"));
            for(int i=0;i<bytes.Length;i++){c.Hex("80 79");c.Raw(new[]{checked((byte)(12+i)),bytes[i]});c.Branch("0F 85",mismatch);}
        }
        private void PrepareAmmo(ResolvedPatch patch)
        {
            if(patch.Cave!=0)return;
            long cave=Memory.AllocateNear(patch.Address);
            byte[] bytes=BuildResourceHook(cave,patch.Address);
            Memory.Write(cave,bytes);Memory.Write(cave+0x800,new byte[resourceFlags.Count]);
            patch.Cave=cave;patch.EnabledBytes=Jump(patch.Address,cave,15);
        }
        internal static byte[] BuildResourceHook(long cave,long address)
        {
            var c=new Emitter(cave);
            c.Branch("0F 85","exit");c.Hex("50 51 8B 57 4C 39 D3 0F 42 D3");
            for(int index=0;index<ResourceGroups.Length;index++){
                var group=ResourceGroups[index];string next="next"+index;
                c.Rip("80 3D",cave+0x800+index,1);c.Hex("00");c.Branch("0F 84",next);
                foreach(uint id in group.ResourceIds){c.Hex("41 81 FC");c.Raw(BitConverter.GetBytes(id));c.Branch("0F 84","preserve");}
                foreach(string name in group.ResourceNames){
                    c.Hex("48 8B 4F 28 48 85 C9");c.Branch("0F 84",next);c.Hex("48 8B 49 10 48 85 C9");c.Branch("0F 84",next);
                    EmitNameCompare(c,name,group.NamePrefix,next);c.Branch("E9","preserve");
                }
                c.Label(next);
            }
            c.Hex("29 57 4C");c.Label("preserve");c.Hex("89 D3 59 58");c.Label("exit");c.Jump(address+15);
            byte[] bytes=c.Finish();if(bytes.Length>=0x800)throw new IOException("Resource hook overlaps its flags.");return bytes;
        }
    }
}
