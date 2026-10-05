using System;
using System.Collections.Generic;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiNavigationLayer { RideFinal, PassengerDiversion, PlayerService, Emergency }

    // One replaceable lease per responsibility. Tokens prevent late completions from popping replacements.
    public sealed class TruckTaxiNavigationIntents<T> where T : class
    {
        public sealed class Lease
        {
            public readonly TruckTaxiNavigationLayer Layer;
            public readonly string OwnerId, TargetId;
            public readonly long Token;
            public readonly T Value;
            internal Lease(TruckTaxiNavigationLayer layer, string owner, string target, long token, T value)
            { Layer=layer; OwnerId=owner; TargetId=target; Token=token; Value=value; }
            public override string ToString() => Layer+" / "+OwnerId+" / "+TargetId;
        }
        private readonly Lease[] layers=new Lease[4];
        private long sequence;
        public event Action<string> Trace;
        public Lease Active
        {
            get { for(int i=layers.Length-1;i>=0;i--) if(layers[i]!=null) return layers[i]; return null; }
        }
        public Lease Get(TruckTaxiNavigationLayer layer) => layers[(int)layer];
        public IEnumerable<Lease> TopFirst
        { get { for(int i=layers.Length-1;i>=0;i--) if(layers[i]!=null) yield return layers[i]; } }
        public Lease Set(TruckTaxiNavigationLayer layer,string owner,string target,T value)
        {
            if(string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(target) || value==null)
                throw new ArgumentException("Navigation requires an owner, stable target ID, and value.");
            var prior=Active;
            var replaced=Get(layer);
            if(replaced!=null) Trace?.Invoke("NAV POP "+replaced+" (replaced)");
            var lease=new Lease(layer,owner,target,++sequence,value);
            layers[(int)layer]=lease;
            Trace?.Invoke("NAV PUSH "+lease);
            if(prior!=null && prior!=Active && prior!=replaced) Trace?.Invoke("NAV SUSPEND "+prior);
            if(Active!=lease) Trace?.Invoke("NAV SUSPEND "+lease);
            return lease;
        }
        public bool Release(TruckTaxiNavigationLayer layer,string owner,string target=null,long token=0)
        {
            var lease=Get(layer);
            if(lease==null || lease.OwnerId!=owner || (target!=null && lease.TargetId!=target) || (token!=0 && lease.Token!=token)) return false;
            var prior=Active;
            layers[(int)layer]=null;
            Trace?.Invoke("NAV POP "+lease);
            if(prior==lease && Active!=null) Trace?.Invoke("NAV RESTORE "+Active);
            return true;
        }
        public void Clear()
        {
            for(int i=layers.Length-1;i>=0;i--)
                if(layers[i]!=null) Release((TruckTaxiNavigationLayer)i,layers[i].OwnerId);
        }
    }
}
