using System;
using System.Collections.Generic;
using System.Linq;
namespace FarTrainer {
 // Small explicit x64 emitter. All emitted code is authored here, never compiled from a CT.
 public sealed class Emitter {
  public readonly long Base; public readonly List<byte> Bytes=new List<byte>();
  private readonly Dictionary<string,int> labels=new Dictionary<string,int>();
  private readonly List<Tuple<int,string>> fixups=new List<Tuple<int,string>>();
  public Emitter(long start){Base=start;}
  public void Hex(string bytes){Bytes.AddRange(TrainerEngine.Hex(bytes));}
  public void Raw(byte[] bytes){Bytes.AddRange(bytes);}
  public void Label(string name){labels.Add(name,Bytes.Count);}
  public void Rip(string opcode,long target,int trailing=0){Hex(opcode);Raw(BitConverter.GetBytes(checked((int)(target-(Base+Bytes.Count+4+trailing)))));}
  public void Branch(string opcode,string label){Hex(opcode);fixups.Add(Tuple.Create(Bytes.Count,label));Raw(new byte[4]);}
  public void Jump(long target){Rip("E9",target);}
  public byte[] Finish(){foreach(var f in fixups){byte[] d=BitConverter.GetBytes(labels[f.Item2]-f.Item1-4);for(int j=0;j<4;j++)Bytes[f.Item1+j]=d[j];}return Bytes.ToArray();}
  public void SaveXmm0(){Hex("9C 48 83 EC 20 F3 0F 7F 04 24");}
  public void RestoreXmm0(){Hex("F3 0F 6F 04 24 48 83 C4 20 9D");}
 }
 public class HookModule : PatchModule {
  private readonly string id;private readonly PatchDefinition[] definitions;private readonly string[] dependencies;
  public HookModule(string id,PatchDefinition[] definitions,params string[] dependencies){this.id=id;this.definitions=definitions;this.dependencies=dependencies;}
  public override string Id {get{return id;}}
  public override IEnumerable<PatchDefinition> Definitions {get{return definitions;}}
  public override bool Available(TrainerEngine e){return base.Available(e)&&dependencies.All(d=>e.PatchAvailable(d));}
  public override void Set(TrainerEngine e,bool enabled){
   if(enabled)foreach(var d in dependencies)e.SetPatch(d,true);
   base.Set(e,enabled);
  }
 }
}
