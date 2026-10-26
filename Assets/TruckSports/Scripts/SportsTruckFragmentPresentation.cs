using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckSports
{
    // Only rendering is attached to the existing twelve physical pieces. Slice
    // meshes are authored offline by the installed vendor; no runtime shattering.
    public sealed class SportsTruckFragmentPresentation : MonoBehaviour
    {
        public SportsTruckAppearanceHost host;
        public SportsFragmentRig rig;
        public SportsTruckFragmentRecipe[] recipes=Array.Empty<SportsTruckFragmentRecipe>();
        public int authoredVersion;
        public string[] recipeBodyIds=Array.Empty<string>();
        public int PreparedRevision{get;private set;}=-1;
        public int PoolBuilds{get;private set;}
        public int ActualBursts{get;private set;}
        public string LastBurstBodyId{get;private set;}
        public Renderer[] ExteriorRenderers{get;private set;}=Array.Empty<Renderer>();
        readonly List<Graphic> graphics=new List<Graphic>();readonly List<GameObject> roots=new List<GameObject>();bool[] originalPieceStates;
        sealed class Graphic
        {
            public Renderer source,target;public Transform piece;
            public Mesh input,owned;public Vector3[] vertices,normals;public Vector4[] tangents;public int[][] triangles;
            public Matrix4x4 bindMatrix;
            public int sourceMaterial=-1;public Material cap;
        }
        public Transform[] eventWheelPieceSources=Array.Empty<Transform>();
        bool SupportedOwner()
        {
            if(rig==null||rig.pieces==null||rig.pieces.Length!=12)return false;
            if(rig.sourceBody!=null&&rig.truck==null)return true;
            if(rig.sourceBody!=null||rig.truck==null||host==null||rig.truck.transform!=host.transform||rig.truck.body==null||host.wheels.Length!=4||rig.truck.wheels.Length!=4||eventWheelPieceSources==null||eventWheelPieceSources.Length!=4)return false;
            var binding=rig.truck.GetComponent<SportsWheelMeshBinding>();if(binding==null||binding.truck!=rig.truck||binding.meshes==null||binding.meshes.Length!=4)return false;
            for(int i=0;i<4;i++){if(host.wheels[i]!=rig.truck.wheels[i]||eventWheelPieceSources[i]==null||eventWheelPieceSources[i]!=binding.meshes[i])return false;int count=0;foreach(var piece in rig.pieces)if(piece!=null&&piece.source==eventWheelPieceSources[i])count++;if(count!=1)return false;for(int j=0;j<i;j++)if(eventWheelPieceSources[j]==eventWheelPieceSources[i])return false;}return true;
        }
        public bool Supports(SportsTruckAppearanceCatalog.Body body,out string reason)
        {
            if(!SupportedOwner()){reason="This sport has no compatible explicit wreck owner/wheel binding.";return false;}
            var recipe=Array.Find(recipes??Array.Empty<SportsTruckFragmentRecipe>(),item=>item!=null&&body!=null&&item.bodyId==body.id);
            if(recipe==null){reason="Wreck visuals for this body are unavailable in this sport.";return false;}return recipe.ValidFor(body,out reason);
        }
        int Role(string role)
        {
            int found=-1;for(int i=0;i<rig.pieces.Length;i++)if(rig.pieces[i].source.name==role){if(found>=0)throw new InvalidOperationException("Duplicate original fragment role.");found=i;}
            if(found<0)throw new InvalidOperationException("Missing original fragment role: "+role);return found;
        }
        int WheelRole(int wheel)
        {
            if(wheel<0||wheel>=host.wheels.Length)throw new InvalidOperationException("Unknown explicit wheel index.");
            if(rig.truck!=null){if(!SupportedOwner())throw new InvalidOperationException("Original EventTruck wheel piece bindings changed.");for(int i=0;i<rig.pieces.Length;i++)if(rig.pieces[i].source==eventWheelPieceSources[wheel])return i;throw new InvalidOperationException("Missing exact original EventTruck wheel piece source.");}
            var container=host.wheels[wheel].wheel.rotatingContainer;int found=-1;
            for(int i=0;i<rig.pieces.Length;i++)if(rig.pieces[i].source.IsChildOf(container)){if(found>=0)throw new InvalidOperationException("Ambiguous existing wheel fragment binding.");found=i;}
            if(found<0)throw new InvalidOperationException("Existing wheel has no original fragment role.");return found;
        }
        public bool PrepareRevision()
        {
            if(host==null||rig==null||host.AppearanceRoot==null||!host.Applied)return false;
            if(PreparedRevision==host.AppearanceRevision){RefreshColors();return true;}
            var body=host.catalog.FindBody(host.EffectiveBodyId);if(!Supports(body,out _))return false;
            var recipe=Array.Find(recipes,item=>item!=null&&item.bodyId==body.id);
            ClearPool(false);
            try
            {
                if(!SupportedOwner())throw new InvalidOperationException("Only an explicit original twelve-piece sourceBody or EventTruck rig is supported.");
                if(originalPieceStates==null){originalPieceStates=new bool[12];for(int i=0;i<12;i++)originalPieceStates[i]=rig.pieces[i].renderer.enabled;}
                var parents=new Transform[12];for(int i=0;i<12;i++){var root=new GameObject("Owned selected-body fragment graphics");root.transform.SetParent(rig.pieces[i].body.transform,false);parents[i]=root.transform;roots.Add(root);}
                var canonical=host.ComposedBodyRenderers;var used=new HashSet<Renderer>();
                foreach(var part in recipe.parts)
                {
                    var renderer=canonical[part.rendererIndex];if(renderer==null||!renderer.enabled)continue;used.Add(renderer);
                    Add(renderer,part.mesh,parents[Role(SportsTruckFragmentRecipe.BodyRoles[part.region])],part.sourceMaterialIndex,recipe.capMaterial);
                }
                foreach(var renderer in host.AppearanceRoot.GetComponentsInChildren<Renderer>(true))
                {
                    if(!renderer.enabled||used.Contains(renderer))continue;
                    var tag=renderer.GetComponentInParent<SportsTruckAppearanceMountTag>();
                    if(tag==null)throw new InvalidOperationException("An actual visible appearance surface has no explicit body or cosmetic mapping.");
                    int piece=tag.wheelIndex>=0?WheelRole(tag.wheelIndex):Role(tag.slotId==SportsTruckAppearanceSlots.Mirrors?"mirror lft.001":tag.slotId==SportsTruckAppearanceSlots.RearOrnament?"Chassis":"Cab");
                    var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)throw new InvalidOperationException("A cosmetic has no render-only mesh.");
                    Add(renderer,filter.sharedMesh,parents[piece],-1,null);
                }
                foreach(var graphic in graphics)Bake(graphic);
                for(int i=0;i<12;i++)rig.pieces[i].renderer.enabled=false;
                ExteriorRenderers=new Renderer[graphics.Count];for(int i=0;i<graphics.Count;i++)ExteriorRenderers[i]=graphics[i].target;PreparedRevision=host.AppearanceRevision;PoolBuilds++;RefreshColors();return true;
            }
            catch(Exception exception){Debug.LogWarning("Selected-body fragment rendering refused: "+exception.Message,this);ClearPool(true);return false;}
        }
        void Add(Renderer source,Mesh input,Transform parent,int sourceMaterial,Material cap)
        {
            if(!(source is MeshRenderer)||input==null||!input.isReadable||input.vertexCount==0)throw new InvalidOperationException("Readable actual appearance geometry required.");
            var owned=Instantiate(input);owned.name="Owned fragment · "+input.name;owned.MarkDynamic();
            var item=new GameObject("Actual appearance surface · "+source.name);item.layer=source.gameObject.layer;item.transform.SetParent(parent,false);item.AddComponent<MeshFilter>().sharedMesh=owned;var target=item.AddComponent<MeshRenderer>();target.shadowCastingMode=source.shadowCastingMode;target.receiveShadows=source.receiveShadows;
            var triangles=new int[input.subMeshCount][];for(int i=0;i<triangles.Length;i++)triangles[i]=input.GetTriangles(i);
            graphics.Add(new Graphic{source=source,target=target,piece=parent.parent,input=input,owned=owned,vertices=input.vertices,normals=input.normals,tangents=input.tangents,triangles=triangles,sourceMaterial=sourceMaterial,cap=cap});
        }
        Matrix4x4 PieceMatrix(Graphic graphic)
        {
            // Use the exact world TRS assigned by the unchanged original Burst
            // loop, not the dormant zero-scale piece's inverse matrix.
            SportsFragmentRig.Piece piece=null;foreach(var candidate in rig.pieces)if(candidate.body.transform==graphic.piece){piece=candidate;break;}
            if(piece==null)throw new InvalidOperationException("Original physical piece binding lost.");
            var expected=Matrix4x4.TRS(piece.source.position,piece.source.rotation,piece.source.lossyScale);return expected.inverse*graphic.source.localToWorldMatrix;
        }
        void Bake(Graphic graphic)
        {
            var matrix=PieceMatrix(graphic);graphic.bindMatrix=matrix;
            float determinant=matrix.determinant;if(!float.IsFinite(determinant)||Mathf.Abs(determinant)<1e-10f)throw new InvalidOperationException("Degenerate fragment rendering transform.");
            var vertices=new Vector3[graphic.vertices.Length];for(int i=0;i<vertices.Length;i++)vertices[i]=matrix.MultiplyPoint3x4(graphic.vertices[i]);graphic.owned.vertices=vertices;
            var normalMatrix=matrix.inverse.transpose;
            if(graphic.normals.Length==vertices.Length){var normals=new Vector3[vertices.Length];for(int i=0;i<normals.Length;i++)normals[i]=normalMatrix.MultiplyVector(graphic.normals[i]).normalized;graphic.owned.normals=normals;}else graphic.owned.RecalculateNormals();
            if(graphic.tangents.Length==vertices.Length){var tangents=new Vector4[vertices.Length];for(int i=0;i<tangents.Length;i++){var original=graphic.tangents[i];var direction=matrix.MultiplyVector(new Vector3(original.x,original.y,original.z)).normalized;tangents[i]=new Vector4(direction.x,direction.y,direction.z,original.w*(determinant<0?-1:1));}graphic.owned.tangents=tangents;}
            for(int sub=0;sub<graphic.triangles.Length;sub++){var indices=(int[])graphic.triangles[sub].Clone();if(determinant<0)for(int i=0;i<indices.Length;i+=3){int swap=indices[i];indices[i]=indices[i+1];indices[i+1]=swap;}graphic.owned.SetTriangles(indices,sub,false);}graphic.owned.RecalculateBounds();
        }
        void PoseForBurst(Graphic graphic)
        {
            // Keep the shared owned mesh immutable after staging: earlier heat
            // replays reference it too. Per-burst wheel/ornament pose changes
            // belong to child transforms, which the existing recorder records.
            var delta=PieceMatrix(graphic)*graphic.bindMatrix.inverse;Vector3 position=delta.GetColumn(3),right=delta.GetColumn(0),up=delta.GetColumn(1),forward=delta.GetColumn(2);
            var scale=new Vector3(right.magnitude,up.magnitude,forward.magnitude);if(scale.x<1e-7f||scale.y<1e-7f||scale.z<1e-7f)throw new InvalidOperationException("Degenerate per-burst render pose.");if(delta.determinant<0)scale.x=-scale.x;
            var rotation=Quaternion.LookRotation(forward/scale.z,up/scale.y);var reconstructed=Matrix4x4.TRS(position,rotation,scale);
            for(int row=0;row<4;row++)for(int col=0;col<4;col++)if(!float.IsFinite(delta[row,col])||Mathf.Abs(delta[row,col]-reconstructed[row,col])>2e-4f)throw new InvalidOperationException("Unsupported sheared cosmetic motion; retain original compatible truck rendering.");
            graphic.target.transform.localPosition=position;graphic.target.transform.localRotation=rotation;graphic.target.transform.localScale=scale;
        }
        public void RefreshColors()
        {
            foreach(var graphic in graphics)
            {
                if(graphic.source==null||graphic.target==null)continue;var materials=graphic.source.sharedMaterials;
                if(graphic.sourceMaterial<0)graphic.target.sharedMaterials=materials;
                else{var selected=new Material[graphic.owned.subMeshCount];selected[0]=materials[graphic.sourceMaterial];for(int i=1;i<selected.Length;i++)selected[i]=graphic.cap;graphic.target.sharedMaterials=selected;}
                var block=new MaterialPropertyBlock();graphic.source.GetPropertyBlock(block);graphic.target.SetPropertyBlock(block);
                for(int slot=0;slot<graphic.target.sharedMaterials.Length;slot++){block.Clear();if(graphic.sourceMaterial<0)graphic.source.GetPropertyBlock(block,slot);else if(slot==0)graphic.source.GetPropertyBlock(block,graphic.sourceMaterial);graphic.target.SetPropertyBlock(block.isEmpty?null:block,slot);}
            }
        }
        public bool BeforeBurst()
        {
            try{if(!PrepareRevision())return false;foreach(var graphic in graphics)PoseForBurst(graphic);RefreshColors();return true;}
            catch(Exception exception){Debug.LogWarning("Selected-body burst visuals unavailable; original physics continues: "+exception.Message,this);return false;}
        }
        public void AfterBurst()
        {
            if(PreparedRevision<0)return;foreach(var piece in rig.pieces)piece.renderer.enabled=false;host.SetFragmentedPresentation(true);LastBurstBodyId=host.EffectiveBodyId;ActualBursts++;
        }
        public void ResetPresentation(){if(host!=null)host.SetFragmentedPresentation(false);if(host!=null&&host.Applied){PrepareRevision();foreach(var piece in rig.pieces)piece.renderer.enabled=false;}}
        public void ClearPool(bool restorePieces=true)
        {
            foreach(var graphic in graphics)if(graphic.owned!=null)Dispose(graphic.owned);graphics.Clear();foreach(var root in roots)if(root!=null){root.SetActive(false);root.transform.SetParent(null,true);Dispose(root);}roots.Clear();ExteriorRenderers=Array.Empty<Renderer>();PreparedRevision=-1;
            if(restorePieces&&originalPieceStates!=null&&rig!=null)for(int i=0;i<rig.pieces.Length;i++)if(rig.pieces[i].renderer!=null)rig.pieces[i].renderer.enabled=originalPieceStates[i];
        }
        static void Dispose(UnityEngine.Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        void OnDestroy(){ClearPool();}
    }
}
