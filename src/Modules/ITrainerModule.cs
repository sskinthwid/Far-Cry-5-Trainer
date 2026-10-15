using System;
using System.Collections.Generic;
using System.Linq;

namespace FarTrainer
{
    public interface ITrainerModule
    {
        string Id { get; }
        IEnumerable<PatchDefinition> Definitions { get; }
        bool Available(TrainerEngine engine);
        bool Active(TrainerEngine engine);
        string Status(TrainerEngine engine);
        void Set(TrainerEngine engine,bool enabled);
    }

    public abstract class PatchModule : ITrainerModule
    {
        public abstract string Id { get; }
        public abstract IEnumerable<PatchDefinition> Definitions { get; }
        public virtual bool Available(TrainerEngine engine) { return Definitions.All(d=>engine.PatchAvailable(d.Id)); }
        public virtual bool Active(TrainerEngine engine) { return Definitions.All(d=>engine.PatchActive(d.Id)); }
        public virtual string Status(TrainerEngine engine)
        { return string.Join("; ",Definitions.Select(d=>d.Id+": "+engine.PatchStatus(d.Id))); }
        public virtual void Set(TrainerEngine engine,bool enabled)
        {
            if(!Available(engine))throw new InvalidOperationException(Status(engine));
            var before=Definitions.ToDictionary(d=>d.Id,d=>engine.PatchActive(d.Id));
            try { foreach(var definition in Definitions)engine.SetPatch(definition.Id,enabled); }
            catch(Exception failure)
            {
                var errors=new List<Exception> {failure};
                foreach(var definition in Definitions.Reverse())
                    try {engine.SetPatch(definition.Id,before[definition.Id]);}catch(Exception restoration){errors.Add(restoration);}
                if(errors.Count>1)throw new AggregateException("Module failed and restoration requires attention.",errors);
                throw;
            }
        }
    }

    public abstract class ResourceModule : ITrainerModule
    {
        public abstract string Id { get; }
        public abstract uint[] ResourceIds { get; }
        public virtual string[] ResourceNames {get{return new string[0];}}
        public virtual bool NamePrefix {get{return false;}}
        public virtual IEnumerable<PatchDefinition> Definitions { get { return new PatchDefinition[0]; } }
        public bool Available(TrainerEngine engine) { return engine.PatchAvailable("ammo"); }
        public bool Active(TrainerEngine engine) { return engine.ResourceActive(Id); }
        public string Status(TrainerEngine engine) { return engine.PatchStatus("ammo"); }
        public void Set(TrainerEngine engine,bool enabled) { engine.SetResource(Id,enabled); }
    }

}
