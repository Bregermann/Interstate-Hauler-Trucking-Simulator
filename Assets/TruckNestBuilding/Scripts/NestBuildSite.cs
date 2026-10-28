using UnityEngine;
namespace LWS.TruckNestBuilding
{
    public sealed class NestBuildSite : MonoBehaviour
    {
        public Rigidbody body;
        public BoxCollider deck;
        public NestFamily family;
        public int index;
        public Vector3 competitiveHome;
        public readonly NestMaterial[] slots = new NestMaterial[12];
        readonly NestMaterial[] candidates = new NestMaterial[12];
        readonly float[] resting = new float[12];
        readonly int[] claims = new int[12];
        public static NestMaterialKind Kind(int slot) => slot < 4 ? NestMaterialKind.Branch : slot < 8 ? NestMaterialKind.Stick : slot == 8 ? NestMaterialKind.Leaves : slot == 9 ? NestMaterialKind.Moss : NestMaterialKind.Decoration;
        public static Vector3 Position(int slot)
        {
            switch (slot)
            {
                case 0: return new Vector3(0, .3f, -2.2f); case 1: return new Vector3(0, .3f, 2.2f);
                case 2: return new Vector3(-2.2f, .86f, 0); case 3: return new Vector3(2.2f, .86f, 0);
                case 4: return new Vector3(0, 1.34f, -1.5f); case 5: return new Vector3(0, 1.34f, 1.5f);
                case 6: return new Vector3(-1.5f, 1.74f, 0); case 7: return new Vector3(1.5f, 1.74f, 0);
                case 8: return new Vector3(0, 2, 0); case 9: return new Vector3(0, 2.16f, 0);
                default: return new Vector3(slot == 10 ? -2.2f : 2.2f, 1.27f, 0);
            }
        }
        public static Quaternion Rotation(int slot) => Quaternion.Euler(0, slot == 0 || slot == 1 || slot == 4 || slot == 5 ? 90 : 0, 0);
        public Vector3 Target(int slot) => transform.TransformPoint(Position(slot));
        public Quaternion TargetRotation(int slot) => transform.rotation * Rotation(slot);
        public bool Prerequisites(int slot)
        {
            if (slot < 2) return true;
            if (slot < 4) return Rooted(slots[0]) && Rooted(slots[1]);
            if (slot < 6 || slot >= 10) return Rooted(slots[2]) && Rooted(slots[3]);
            if (slot < 8) return Rooted(slots[4]) && Rooted(slots[5]);
            return slot == 8 ? Rooted(slots[6]) && Rooted(slots[7]) : Rooted(slots[8]);
        }
        public static bool SupportAllowed(int slot, int support)
        {
            if (slot < 2) return support == -1;
            if (slot < 4) return support == 0 || support == 1;
            if (slot < 6 || slot >= 10) return support == 2 || support == 3;
            if (slot < 8) return support == 4 || support == 5;
            return support == (slot == 8 ? 6 : 8) || slot == 8 && support == 7;
        }
        public void Prepare(bool active, bool cooperative)
        {
            Clear(); gameObject.SetActive(active); if (!active) return;
            body.position = cooperative ? new Vector3(0, competitiveHome.y, competitiveHome.z) : competitiveHome; transform.position = body.position;
            family.ResetFamily();
        }
        public void Clear()
        {
            for (int i = 0; i < 12; i++) { if (slots[i] != null) slots[i].Unlock(); slots[i] = candidates[i] = null; resting[i] = 0; claims[i] = -1; }
        }
        public bool Rooted(NestMaterial item)
        {
            if (item == null || item.Site != this) return false;
            for (int i = 0; i < 13; i++)
            {
                if (item.Interlock == null || item.Interlock.connectedBody == null) return false;
                if (item.Interlock.connectedBody == body) return true;
                item = item.Interlock.connectedBody.GetComponent<NestMaterial>(); if (item == null || item.Site != this) return false;
            }
            return false;
        }
        public bool CoreReady { get { for (int i = 0; i < 10; i++) if (!Rooted(slots[i])) return false; return true; } }
        public int RootedCount { get { int n = 0; foreach (var p in slots) if (Rooted(p)) n++; return n; } }
        public int Varieties { get { int bits = 0; foreach (var p in slots) if (Rooted(p)) bits |= 1 << (int)p.kind; int n = 0; while (bits != 0) { n += bits & 1; bits >>= 1; } return n; } }
        public int Ornaments => (Rooted(slots[10]) ? 1 : 0) + (Rooted(slots[11]) ? 1 : 0);
        public int ClaimNext(int owner, NestMaterialKind? kind = null)
        {
            for (int i = 0; i < 12; i++) if (slots[i] == null && (!kind.HasValue || Kind(i) == kind.Value) && Prerequisites(i) && (claims[i] < 0 || claims[i] == owner)) { claims[i] = owner; return i; }
            return -1;
        }
        bool LiveCollectionClaim(int socket,NestBuilder[] builders,int count)
        {
            int owner=claims[socket];
            return owner>=0&&owner<count&&owner<builders.Length&&builders[owner]!=null&&builders[owner].gameObject.activeInHierarchy&&builders[owner].GoalSite==this&&builders[owner].GoalSocket==socket;
        }
        bool CollectionDependencyCovered(int socket,NestBuilder[] builders,int count)
        {return Rooted(slots[socket])||LiveCollectionClaim(socket,builders,count)&&CollectionAncestorsCovered(socket,builders,count);}
        public bool CollectionAncestorsCovered(int socket,NestBuilder[] builders,int count)
        {
            if(socket<2)return true;
            if(socket<4)return CollectionDependencyCovered(0,builders,count)&&CollectionDependencyCovered(1,builders,count);
            if(socket<6)return CollectionDependencyCovered(2,builders,count)&&CollectionDependencyCovered(3,builders,count);
            if(socket<8)return CollectionDependencyCovered(4,builders,count)&&CollectionDependencyCovered(5,builders,count);
            return socket==8?CollectionDependencyCovered(6,builders,count)&&CollectionDependencyCovered(7,builders,count):socket==9&&CollectionDependencyCovered(8,builders,count);
        }
        public int ClaimNextForCollection(int owner,NestBuilder[] builders,int count,NestMaterialKind? kind=null)
        {
            // Only actual builder assignments count as inflight prerequisite
            // work; stale/orphan claim integers cannot reserve a future layer.
            for(int i=0;i<10;i++)if(claims[i]>=0&&!LiveCollectionClaim(i,builders,count))claims[i]=-1;
            for(int pass=0;pass<2;pass++)for(int i=0;i<10;i++)
                if(slots[i]==null&&(!kind.HasValue||Kind(i)==kind.Value)&&(claims[i]<0||claims[i]==owner)
                    &&(pass==0?Prerequisites(i):CollectionAncestorsCovered(i,builders,count)))
                {claims[i]=owner;return i;}
            // Decorative work remains available after the actual core is ready;
            // future reservations are bounded to its ten known physical jobs.
            return CoreReady?ClaimNext(owner,kind):-1;
        }
        public void ReleaseClaim(int owner) { for (int i = 0; i < claims.Length; i++) if (claims[i] == owner) claims[i] = -1; }
        public bool Fits(NestMaterial piece, int slot, Vector3 position, Quaternion rotation)
        {
            if (piece.kind != Kind(slot) || Vector3.Distance(position, Target(slot)) > .75f) return false;
            return piece.kind == NestMaterialKind.Decoration || Mathf.Abs(Vector3.Dot(rotation * Vector3.forward, TargetRotation(slot) * Vector3.forward)) > .91f;
        }
        public void Tick(NestMaterial[] materials, float dt)
        {
            for (int slot = 0; slot < slots.Length; slot++)
            {
                var current = slots[slot];
                if (current != null && (!Rooted(current) || !Fits(current, slot, current.body.position, current.body.rotation))) { current.Unlock(); slots[slot] = null; }
                if (slots[slot] != null || !Prerequisites(slot)) continue;
                NestMaterial found = null;
                foreach (var part in materials) if (part.Site == null && part.Grip == null && part.body.linearVelocity.sqrMagnitude < .10f && part.body.angularVelocity.sqrMagnitude < .25f && Fits(part, slot, part.body.position, part.body.rotation) && part.ContactSupport(this, slot) != null) { found = part; break; }
                if (found != candidates[slot]) { candidates[slot] = found; resting[slot] = 0; }
                if (found == null) continue; resting[slot] += dt;
                if (resting[slot] < 1.2f) continue;
                found.Lock(this, slot, found.ContactSupport(this, slot)); slots[slot] = found; claims[slot] = -1;
            }
        }
        public void RestorePlacement(NestMaterial material, int slot, Rigidbody support) { material.Lock(this, slot, support); slots[slot] = material; }
    }
}
