using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckSports.Editor
{
    public static partial class SportsTruckAppearanceContentAuthoring
    {
        // Missing truck-scale accessories are authored original meshes. Existing
        // NWH wheels/mirrors and owned Sports animal meshes remain asset refs.
        static SportsTruckAppearanceCatalog.Cosmetic[] CreateCosmetics()
        {
            Directory.CreateDirectory(Folder + "/Accessories");
            var items = new List<SportsTruckAppearanceCatalog.Cosmetic>();
            var felt = Material("Midnight hat felt", new Color(.07f, .04f, .14f), 0, .22f);
            var leather = Material("Saddle leather", new Color(.32f, .12f, .045f), 0, .3f);
            var gold = Material("Showtime gold", new Color(1, .64f, .08f), .72f, .72f);
            var chrome = Material("Party chrome", new Color(.82f, .9f, .95f), .92f, .82f);
            var white = Material("Cartoon ivory", new Color(1, .98f, .88f), 0, .36f);
            var black = Material("Cartoon ink", new Color(.015f, .02f, .035f), 0, .46f);
            var ruby = Material("Cherry lacquer", new Color(.65f, .015f, .035f), 0, .72f);
            Add("hat.cowboy", "Highway Cowboy", "hat", root => {
                Lathe(root, "Saddle crown", new[] { new Vector2(0,0), new Vector2(.48f,0), new Vector2(.5f,.12f), new Vector2(.4f,.65f), new Vector2(.25f,.75f), new Vector2(0,.67f) }, leather);
                Lathe(root, "Upturned cowboy brim", new[] { new Vector2(.45f,.04f), new Vector2(.65f,.02f), new Vector2(.92f,.11f), new Vector2(1,.18f), new Vector2(.98f,.24f), new Vector2(.65f,.08f), new Vector2(.45f,.1f) }, leather, 40, .18f);
                Band(root, "Cowboy gold band", .45f,.14f,.1f,gold);
            });
            Add("hat.top", "Big Rig Top Hat", "hat", root => { Brim(root,.83f,felt); Lathe(root,"Tall tapered crown",new[]{new Vector2(.5f,.08f),new Vector2(.43f,.12f),new Vector2(.5f,1.06f),new Vector2(.47f,1.12f),new Vector2(0,1.12f)},felt);Band(root,"Ruby ribbon",.45f,.13f,.2f,ruby); });
            Add("hat.crown", "King of the Highway", "hat", root => {
                Band(root,"Crown gold rim",.58f,0,.22f,gold);
                for(int i=0;i<7;i++){float angle=i*Mathf.PI*2/7;var point=new Vector3(Mathf.Cos(angle)*.52f,.4f,Mathf.Sin(angle)*.52f);Surface(root,"Crown ruby tip "+i,point,new Vector3(.085f,.085f,.085f),ruby,10,12);var bar=Lathe(root,"Crown point "+i,new[]{new Vector2(.13f,0),new Vector2(.03f,.5f),new Vector2(0,.52f)},gold,12);bar.transform.localPosition=new Vector3(point.x,.15f,point.z);}
            });
            Add("hat.wizard", "Midnight Wizard", "hat", root => {Brim(root,.88f,felt);Lathe(root,"Crooked wizard cone",new[]{new Vector2(.52f,.08f),new Vector2(.44f,.3f),new Vector2(.29f,.72f),new Vector2(.12f,1.1f),new Vector2(0,1.45f)},Material("Wizard violet",new Color(.23f,.08f,.52f),0,.22f),32,0,.2f);Band(root,"Wizard gold ribbon",.48f,.14f,.12f,gold);});
            Add("hat.viking", "Road Viking", "hat", root => {
                Lathe(root,"Hammered viking dome",new[]{new Vector2(.58f,0),new Vector2(.6f,.15f),new Vector2(.48f,.43f),new Vector2(.25f,.64f),new Vector2(0,.71f)},chrome);Band(root,"Viking leather rim",.59f,0,.12f,leather);
                for(int side=-1;side<=1;side+=2){var horn=Lathe(root,"Swept ivory horn "+side,new[]{new Vector2(.2f,0),new Vector2(.16f,.23f),new Vector2(.1f,.47f),new Vector2(0,.76f)},white,24,0,.34f*side);horn.transform.localPosition=new Vector3(.48f*side,.35f,0);horn.transform.localRotation=Quaternion.Euler(0,0,-38*side);}
            });
            Add("hat.party", "Never-Ending Party", "hat", root => {Lathe(root,"Party cone",new[]{new Vector2(.48f,0),new Vector2(.43f,.1f),new Vector2(.02f,1.03f),new Vector2(0,1.07f)},Material("Party cyan",new Color(.02f,.72f,.84f),.08f,.38f));Surface(root,"Party pompom",new Vector3(0,1.13f,0),Vector3.one*.18f,ruby,14,18);Band(root,"Party gold hem",.48f,0,.08f,gold);});
            Add("hat.baseball", "Truck Stop Ball Cap", "hat", root => {Lathe(root,"Ball cap crown",new[]{new Vector2(.53f,0),new Vector2(.56f,.15f),new Vector2(.43f,.41f),new Vector2(.18f,.54f),new Vector2(0,.56f)},ruby);Surface(root,"Long curved visor",new Vector3(0,.025f,.47f),new Vector3(.63f,.055f,.52f),ruby,18,24);Surface(root,"Cap button",new Vector3(0,.58f,0),Vector3.one*.055f,gold,8,12);});
            Add("hat.hardhat", "Certified Chaos", "hat", root => {var yellow=Material("Safety yellow",new Color(1,.68f,.015f),.08f,.56f);Brim(root,.7f,yellow);Lathe(root,"Hardhat shell",new[]{new Vector2(.55f,.06f),new Vector2(.59f,.2f),new Vector2(.45f,.52f),new Vector2(.15f,.68f),new Vector2(0,.7f)},yellow);Surface(root,"Hardhat raised ridge",new Vector3(0,.6f,0),new Vector3(.07f,.12f,.5f),yellow,20,12);});
            Add("hat.pirate", "Captain Cargo", "hat", root => {Lathe(root,"Tricorn crown",new[]{new Vector2(.52f,0),new Vector2(.45f,.48f),new Vector2(.2f,.65f),new Vector2(0,.65f)},felt);Lathe(root,"Three-corner pirate brim",new[]{new Vector2(.47f,.04f),new Vector2(.92f,.16f),new Vector2(.98f,.42f),new Vector2(.92f,.47f),new Vector2(.47f,.14f)},felt,48,.22f,0,3);Band(root,"Captain gold band",.48f,.12f,.13f,gold);});
            Add("hat.propeller", "Air Freight Beanie", "hat", root => {Lathe(root,"Beanie cap",new[]{new Vector2(.53f,0),new Vector2(.54f,.13f),new Vector2(.38f,.38f),new Vector2(0,.51f)},Material("Beanie blue",new Color(.04f,.28f,.75f),0,.25f));var stem=Lathe(root,"Propeller spindle",new[]{new Vector2(.035f,0),new Vector2(.035f,.3f),new Vector2(0,.32f)},chrome,12);stem.transform.localPosition=Vector3.up*.5f;Surface(root,"Ruby propeller",new Vector3(0,.82f,0),new Vector3(.75f,.035f,.09f),ruby,20,12);});
            Add("eyes.friendly", "Bright Eyes", "face.eyes", root => {Eyes(root,white,black,false);});
            Add("eyes.sleepy", "Sleepy Hauler", "face.eyes", root => {Eyes(root,white,black,true);});
            Add("brows.angry", "Determined Brows", "face.brows", root => {for(int side=-1;side<=1;side+=2){string name="Expressive brow "+side;Surface(root,name,new Vector3(.34f*side,.37f,.06f),new Vector3(.3f,.06f,.065f),black,12,12);root.transform.Find(name).localRotation=Quaternion.Euler(0,0,-18*side);}});
            Add("mouth.smile", "Highway Grin", "face.mouth", root => {Surface(root,"Broad grin",new Vector3(0,-.47f,.025f),new Vector3(.56f,.22f,.06f),black,20,24);Surface(root,"Ivory smile",new Vector3(0,-.43f,.08f),new Vector3(.46f,.105f,.035f),white,18,20);});
            Add("mouth.goofy", "Goofy Grille", "face.mouth", root => {Surface(root,"Goofy open mouth",new Vector3(0,-.45f,.035f),new Vector3(.34f,.26f,.065f),black,20,24);Surface(root,"Goofy tongue",new Vector3(.1f,-.58f,.09f),new Vector3(.18f,.12f,.025f),ruby,12,16);});
            Add("mustache.handlebar", "Chrome Handlebar", "face.mustache", root => {for(int side=-1;side<=1;side+=2){string name="Waxed handlebar "+side;Surface(root,name,new Vector3(.28f*side,-.2f,.1f),new Vector3(.39f,.105f,.085f),leather,20,16);root.transform.Find(name).localRotation=Quaternion.Euler(0,0,20*side);}});
            Add("lashes.long", "Movie Star Lashes", "face.lashes", root => {for(int side=-1;side<=1;side+=2)for(int i=0;i<3;i++){var lash=Lathe(root,"Long lash "+side+"-"+i,new[]{new Vector2(.025f,0),new Vector2(.018f,.2f),new Vector2(0,.25f)},black,10);lash.transform.localPosition=new Vector3((.2f+i*.13f)*side,.28f,.07f);lash.transform.localRotation=Quaternion.Euler(0,0,-(15+i*16)*side);}});
            foreach(var pair in new[]{new[]{"mirrors.chrome","Chrome Highway Mirrors"},new[]{"mirrors.gold","Golden Oversize Mirrors"},new[]{"mirrors.ruby","Cherry Mirrors"}})
                Add(pair[0],pair[1],"mirrors",root=>{var mat=pair[0].EndsWith("gold")?gold:pair[0].EndsWith("ruby")?ruby:chrome;for(int side=-1;side<=1;side+=2){Surface(root,"Mirror stalk "+side,new Vector3(1.3f*side,-.12f,-.2f),new Vector3(.22f,.035f,.035f),mat,12,12);Surface(root,"Mirror casing "+side,new Vector3(1.48f*side,0,-.2f),new Vector3(.14f,.3f,.08f),mat,16,16);Surface(root,"Mirror glass "+side,new Vector3(1.48f*side,0,-.28f),new Vector3(.115f,.265f,.018f),chrome,16,16);}});
            Add("rear.chrome","Chrome Hitch Ornaments","rear.ornament",root=>{for(int side=-1;side<=1;side+=2){var link=Lathe(root,"Ornament short chain "+side,new[]{new Vector2(.025f,0),new Vector2(.025f,.24f),new Vector2(0,.25f)},chrome,12);link.transform.localPosition=new Vector3(.12f*side,-.25f,0);Surface(root,"Chrome ornament "+side,new Vector3(.14f*side,-.38f,0),new Vector3(.13f,.17f,.13f),chrome,16,20);}});
            Add("rear.party","Party Hitch Pompoms","rear.ornament",root=>{Surface(root,"Ruby hitch pompom",new Vector3(-.15f,-.3f,0),Vector3.one*.17f,ruby,16,20);Surface(root,"Gold hitch pompom",new Vector3(.15f,-.3f,0),Vector3.one*.17f,gold,16,20);});
            AddAnimal("dashboard.cat","Dashboard Cat","Assets/TruckCheeseRolling/Art/Animals/Cat pose 0.asset",white);
            AddAnimal("dashboard.wolf","Dashboard Wolf","Assets/TruckCheeseRolling/Art/Animals/Wolf pose 0.asset",chrome);
            foreach(var pair in new[]{new[]{"wheels.chrome","Chrome Road Rims"},new[]{"wheels.gold","Gold Show Rims"},new[]{"wheels.ruby","Cherry Painted Rims"}})
            {
                var mesh=AssetDatabase.LoadAssetAtPath<GameObject>(Semi).GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(x=>x.name.IndexOf("Whl",StringComparison.OrdinalIgnoreCase)>=0);
                if(mesh==null)throw new InvalidOperationException("Owned GR3D wheel geometry required; no generic wheel substitute.");
                var wheel=Add(pair[0],pair[1],"wheels",root=>{var part=new GameObject("Original NWH road wheel");part.transform.SetParent(root.transform,false);part.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;var materials=mesh.GetComponent<MeshRenderer>().sharedMaterials.ToArray();var finish=pair[0].EndsWith("gold")?gold:pair[0].EndsWith("ruby")?ruby:chrome;bool matched=false;for(int k=0;k<materials.Length;k++)if(materials[k]!=null&&(materials[k].name.IndexOf("rim",StringComparison.OrdinalIgnoreCase)>=0||materials[k].name.IndexOf("metal",StringComparison.OrdinalIgnoreCase)>=0||materials[k].name.IndexOf("chrome",StringComparison.OrdinalIgnoreCase)>=0)){materials[k]=finish;matched=true;}part.AddComponent<MeshRenderer>().sharedMaterials=materials;Fit(root,Vector3.one);foreach(Transform child in root.transform)child.localPosition-=Vector3.up*.5f;if(!matched)VisibleWheelFinish(root,finish);});wheel.rotatesWithWheel=true;
            }
            return items.ToArray();

            SportsTruckAppearanceCatalog.Cosmetic Add(string id,string label,string slot,Action<GameObject> build)
            {
                string path=Folder+"/Accessories/"+id+".prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(prefab==null||InvalidGeneratedGeometry(prefab)){rebuiltPreviews.Add(id);var root=new GameObject(label);try{build(root);prefab=PrefabUtility.SaveAsPrefabAsset(root,path);}finally{UnityEngine.Object.DestroyImmediate(root);}}
                var item=new SportsTruckAppearanceCatalog.Cosmetic{id=id,label=label,slotId=slot,visualPrefab=prefab};items.Add(item);return item;
            }
            void AddAnimal(string id,string label,string path,Material material)
            {
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null)throw new InvalidOperationException("Owned dashboard figure mesh missing: "+path);
                Add(id,label,"dashboard",root=>{var figure=new GameObject(label+" figure");figure.transform.SetParent(root.transform,false);figure.transform.localRotation=Quaternion.Euler(-90,0,0);figure.AddComponent<MeshFilter>().sharedMesh=mesh;figure.AddComponent<MeshRenderer>().sharedMaterial=material;Fit(root,Vector3.one*.2f);});
            }
        }
        static void VisibleWheelFinish(GameObject root,Material finish)
        {
            var bounds=BoundsOf(root);
            // The tyre itself remains the purchased geometry. The visual hub
            // sits outside its sidewall rather than disappearing inside it.
            Surface(root,"Visible show rim finish",new Vector3(bounds.max.x+.008f,bounds.center.y,bounds.center.z),new Vector3(.02f,bounds.size.y*.29f,bounds.size.z*.29f),finish,20,28);
        }
        [MenuItem("Truck Sports/Customizer/Refine Initial Owned Previews And Wheel Finishes")]
        public static void RefineInitialArt()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            var catalog=AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>(CatalogPath);
            if(catalog==null||catalog.bodies.Length!=7||catalog.cosmetics.Length!=27)throw new InvalidOperationException("This explicit initial-art migration requires the reviewed seven-body/27-accessory catalog; customized rosters are preserved.");
            foreach(var item in catalog.cosmetics.Where(item=>item.slotId=="dashboard"||item.slotId=="wheels"))
            {
                string path=AssetDatabase.GetAssetPath(item.visualPrefab);var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if(item.slotId=="dashboard")
                    {foreach(Transform child in root.transform){child.localRotation=Quaternion.Euler(-90,0,0);child.localPosition=Vector3.zero;}Fit(root,Vector3.one*.2f);}
                    else
                    {
                        var old=root.transform.Find("Show rim hub finish");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
                        if(root.transform.Find("Visible show rim finish")==null)
                        {var finish=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/"+(item.id.EndsWith("gold")?"Showtime gold":item.id.EndsWith("ruby")?"Cherry lacquer":"Party chrome")+".mat");VisibleWheelFinish(root,finish);}
                    }
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            foreach(var body in catalog.bodies){rebuiltPreviews.Add(body.id);body.thumbnail=Thumbnail(body.id,body.visualPrefab);}
            foreach(var item in catalog.cosmetics){rebuiltPreviews.Add(item.id);item.thumbnail=Thumbnail(item.id,item.visualPrefab);}
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();Debug.Log("TRUCK_CUSTOMIZER_INITIAL_ART_REFINED 34 projected-bounds previews, upright dashboard figures, visible distinct wheel finishes; no physics or vendor source modified.");
        }
        static void Eyes(GameObject root,Material white,Material ink,bool sleepy)
        {
            for(int side=-1;side<=1;side+=2){Surface(root,"Ivory eye "+side,new Vector3(side*.34f,0,.025f),new Vector3(.27f,sleepy?.14f:.32f,.055f),white,18,24);Surface(root,"Ink pupil "+side,new Vector3(side*.34f,-.025f,.085f),new Vector3(.1f,sleepy?.07f:.16f,.035f),ink,14,18);Surface(root,"Eye highlight "+side,new Vector3(side*.34f-.025f,.025f,.12f),new Vector3(.027f,.04f,.01f),white,8,10);}
        }
        static void Brim(GameObject root,float radius,Material material)=>Lathe(root,"Rounded brim",new[]{new Vector2(0,0),new Vector2(radius-.06f,0),new Vector2(radius,.04f),new Vector2(radius,.08f),new Vector2(radius-.06f,.12f),new Vector2(0,.12f)},material);
        static void Band(GameObject root,string name,float radius,float y,float height,Material material)=>Lathe(root,name,new[]{new Vector2(radius-.02f,y),new Vector2(radius,y+.015f),new Vector2(radius,y+height-.015f),new Vector2(radius-.02f,y+height)},material);
        static GameObject Lathe(GameObject root,string name,Vector2[] profile,Material material,int sides=32,float wave=0,float bend=0,int lobes=2)
        {
            string path=Folder+"/Meshes/"+root.name+" "+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null)
            {
                var points=new Vector3[profile.Length*(sides+1)];var uv=new Vector2[points.Length];var indices=new List<int>();
                for(int ring=0;ring<profile.Length;ring++)for(int s=0;s<=sides;s++)
                {
                    float angle=s*Mathf.PI*2/sides;int i=ring*(sides+1)+s;float y=profile[ring].y+Mathf.Cos(angle*lobes)*wave*profile[ring].x;
                    points[i]=new Vector3(Mathf.Cos(angle)*profile[ring].x+bend*y*y,y,Mathf.Sin(angle)*profile[ring].x);uv[i]=new Vector2(s/(float)sides,ring/(float)(profile.Length-1));
                    if(ring+1<profile.Length&&s<sides){int b=i+sides+1;indices.AddRange(new[]{i,b,i+1,i+1,b,b+1});}
                }
                mesh=new Mesh{name=name,vertices=points,uv=uv,triangles=indices.ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
            }
            var part=new GameObject(name);part.transform.SetParent(root.transform,false);part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=material;return part;
        }
    }
}
