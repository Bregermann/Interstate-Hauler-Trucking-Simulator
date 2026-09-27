using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiMapIconSetup
    {
        public const string AssetPath="Assets/LWS/TruckTaxi/Resources/TruckTaxi/TruckTaxiMapIcons.asset";
        public const string ArtPath="Assets/LWS/TruckTaxi/Art/MapIcons";
        private const string Heat="Assets/Heat - Complete Modern UI/Textures/Icons/";

        [MenuItem("Truck Taxi/Map/Ensure Typed Map Icons")]
        public static void EnsureAssets()
        {
            Folder("Assets/LWS/TruckTaxi/Resources/TruckTaxi"); Folder(ArtPath);
            EnsureNorthOnly();
            var registry=AssetDatabase.LoadAssetAtPath<TruckTaxiMapIconRegistry>(AssetPath);
            if(registry==null) { registry=ScriptableObject.CreateInstance<TruckTaxiMapIconRegistry>(); AssetDatabase.CreateAsset(registry,AssetPath); }
            var entries=new List<TruckTaxiMapIconRegistry.Entry>(registry.entries);
            foreach(TruckTaxiMapMarkerType type in Enum.GetValues(typeof(TruckTaxiMapMarkerType)))
            {
                var entry=entries.Find(e=>e!=null && e.type==type);
                if(entry==null) { entry=new TruckTaxiMapIconRegistry.Entry { type=type,color=DefaultColor(type) }; entries.Add(entry); }
                if(entry.icon==null) entry.icon=VendorSprite(type) ?? CreateGlyph(type);
                if(string.IsNullOrWhiteSpace(entry.label)) entry.label=LegendLabel(type);
                if(string.IsNullOrWhiteSpace(entry.explanation)) entry.explanation=LegendExplanation(type);
            }
            registry.entries=entries.ToArray(); EditorUtility.SetDirty(registry); AssetDatabase.SaveAssets();
            Debug.Log("Truck Taxi typed map icons ready: "+AssetPath+" (existing designer mappings preserved).");
        }
        private static Sprite VendorSprite(TruckTaxiMapMarkerType type)
        {
            string path=null;
            switch(type)
            {
                case TruckTaxiMapMarkerType.Player: path="HUD/Map Arrow (64x).png"; break;
                case TruckTaxiMapMarkerType.PrivateEventStop: path="HUD/Heart (64x).png"; break;
                case TruckTaxiMapMarkerType.Collectible: path="Misc/Box (256x).png"; break;
                case TruckTaxiMapMarkerType.SpecialEvent: path="Misc/Extras (64x).png"; break;
                case TruckTaxiMapMarkerType.Debug: path="Misc/Information (64x).png"; break;
                case TruckTaxiMapMarkerType.ActiveRoute: path="HUD/Map Arrow Dual (64x).png"; break;
                case TruckTaxiMapMarkerType.Store: path="Misc/Shop (64x).png"; break;
            }
            return path!=null ? AssetDatabase.LoadAssetAtPath<Sprite>(Heat+path) : null;
        }
        private static void EnsureNorthOnly()
        {
            const string path="Assets/LWS/TruckTaxi/Resources/TruckTaxi/NorthOnly.png";
            if(AssetDatabase.LoadAssetAtPath<Sprite>(path)!=null) return;
            const int size=256;
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                var p=new Vector2((x+.5f)/size,(y+.5f)/size);
                bool ink=Line(p,.465f,.88f,.465f,.97f,.008f) || Line(p,.535f,.88f,.535f,.97f,.008f) ||
                    Line(p,.465f,.97f,.535f,.88f,.008f);
                pixels[y*size+x]=ink ? new Color32(255,255,255,255) : new Color32(0,0,0,0);
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false); texture.SetPixels32(pixels); texture.Apply();
            File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single; importer.spritePixelsPerUnit=size;
            importer.mipmapEnabled=false; importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
        public static Color DefaultColor(TruckTaxiMapMarkerType type)
        {
            switch(type)
            {
                case TruckTaxiMapMarkerType.Player: return new Color(.2f,.88f,1);
                case TruckTaxiMapMarkerType.PassengerPickup: return new Color(1,.84f,.24f);
                case TruckTaxiMapMarkerType.Destination: case TruckTaxiMapMarkerType.Dropoff: return Color.white;
                case TruckTaxiMapMarkerType.ScenicStop: case TruckTaxiMapMarkerType.PhotoStop: return new Color(.38f,1,.66f);
                case TruckTaxiMapMarkerType.IllicitStop: return new Color(1,.68f,.35f);
                case TruckTaxiMapMarkerType.PrivateEventStop: return new Color(1,.55f,.73f);
                case TruckTaxiMapMarkerType.Gas: return new Color(.95f,.78f,.28f);
                case TruckTaxiMapMarkerType.Repair: return new Color(.55f,.85f,.95f);
                case TruckTaxiMapMarkerType.Racetrack: return new Color(1,.42f,.25f);
                case TruckTaxiMapMarkerType.SportsStadium: return new Color(.45f,.9f,.7f);
                case TruckTaxiMapMarkerType.ConcertVenue: return new Color(.95f,.5f,.75f);
                case TruckTaxiMapMarkerType.Surge: return new Color(1,.75f,.2f);
                case TruckTaxiMapMarkerType.Hazard: case TruckTaxiMapMarkerType.RoadClosure: return new Color(1,.3f,.2f);
                case TruckTaxiMapMarkerType.WorkArea: return new Color(.25f,.83f,.65f);
                case TruckTaxiMapMarkerType.TargetVehicle: case TruckTaxiMapMarkerType.Danger: return new Color(1,.38f,.38f);
                case TruckTaxiMapMarkerType.Shortcut: return new Color(.66f,.82f,1);
                case TruckTaxiMapMarkerType.DiscoveredShortcut: return new Color(.35f,1,.95f);
                default: return new Color(.92f,.92f,.8f);
            }
        }
        private static string LegendLabel(TruckTaxiMapMarkerType type)
        {
            switch(type)
            {
                case TruckTaxiMapMarkerType.PassengerPickup: return "Passenger pickup";
                case TruckTaxiMapMarkerType.ActiveObjective: return "Objective";
                case TruckTaxiMapMarkerType.ScenicStop: return "Scenic stop";
                case TruckTaxiMapMarkerType.IllicitStop: return "Sketchy pickup";
                case TruckTaxiMapMarkerType.PrivateEventStop: return "Private stop";
                case TruckTaxiMapMarkerType.DiscoveredShortcut: return "Discovered shortcut";
                case TruckTaxiMapMarkerType.TargetVehicle: return "Target vehicle";
                case TruckTaxiMapMarkerType.FoodStop: return "Restaurant / food";
                case TruckTaxiMapMarkerType.Bathroom: return "Restroom";
                case TruckTaxiMapMarkerType.TrainStation: return "Train station";
                case TruckTaxiMapMarkerType.BusTerminal: return "Bus terminal";
                case TruckTaxiMapMarkerType.ServiceArea: return "Service area";
                case TruckTaxiMapMarkerType.SportsStadium: return "Sports stadium";
                case TruckTaxiMapMarkerType.ConcertVenue: return "Concert venue";
                case TruckTaxiMapMarkerType.Surge: return "Event surge";
                case TruckTaxiMapMarkerType.RoadClosure: return "Road closure";
                case TruckTaxiMapMarkerType.WorkArea: return "Ride Work Area";
                default: return type.ToString();
            }
        }
        private static string LegendExplanation(TruckTaxiMapMarkerType type)
        {
            switch(type)
            {
                case TruckTaxiMapMarkerType.Player: return "Your truck";
                case TruckTaxiMapMarkerType.PassengerPickup: return "Waiting fare";
                case TruckTaxiMapMarkerType.Destination: return "Final passenger dropoff";
                case TruckTaxiMapMarkerType.ActiveObjective: return "Current ride objective";
                case TruckTaxiMapMarkerType.PrivateEventStop: return "Companion parking destination";
                case TruckTaxiMapMarkerType.Shortcut: return "Unexplored shortcut";
                case TruckTaxiMapMarkerType.DiscoveredShortcut: return "Shortcut you found";
                case TruckTaxiMapMarkerType.TargetVehicle: return "Ride target on the road";
                case TruckTaxiMapMarkerType.Gas: return "Refuel the truck";
                case TruckTaxiMapMarkerType.Repair: return "Fix vehicle damage";
                case TruckTaxiMapMarkerType.Store: return "Buy supplies";
                case TruckTaxiMapMarkerType.Bathroom: return "Use the restroom";
                case TruckTaxiMapMarkerType.FoodStop: return "Food and drinks";
                case TruckTaxiMapMarkerType.Racetrack: return "Speedway and taxi stops";
                case TruckTaxiMapMarkerType.TrainStation: return "Rail passenger stop";
                case TruckTaxiMapMarkerType.BusTerminal: return "Bus passenger stop";
                case TruckTaxiMapMarkerType.ServiceArea: return "Highway services";
                case TruckTaxiMapMarkerType.Danger: return "Hazard ahead";
                case TruckTaxiMapMarkerType.SportsStadium: return "Sports events and taxi bays";
                case TruckTaxiMapMarkerType.ConcertVenue: return "Shows and taxi pickup queues";
                case TruckTaxiMapMarkerType.Surge: return "Increased regional ride demand";
                case TruckTaxiMapMarkerType.Hazard: return "Active hazard; GPS may not reroute";
                case TruckTaxiMapMarkerType.RoadClosure: return "Explicitly closed road";
                case TruckTaxiMapMarkerType.WorkArea: return "Your pickup-only dispatch boundary";
                default: return "Point of interest";
            }
        }
        public static Sprite CreateGlyph(TruckTaxiMapMarkerType type)
        {
            string path=ArtPath+"/"+type+".png";
            var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path); if(existing!=null) return existing;
            const int size=64;
            var mask=new bool[size*size]; var pixels=new Color32[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++) mask[y*size+x]=Glyph(type,new Vector2((x+.5f)/size,(y+.5f)/size));
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                if(mask[y*size+x]) { pixels[y*size+x]=Color.white; continue; }
                bool outline=false;
                for(int oy=-2;oy<=2 && !outline;oy++) for(int ox=-2;ox<=2;ox++)
                    if(x+ox>=0 && x+ox<size && y+oy>=0 && y+oy<size && mask[(y+oy)*size+x+ox]) { outline=true; break; }
                pixels[y*size+x]=outline ? new Color32(20,24,27,255) : new Color32(0,0,0,0);
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false); texture.SetPixels32(pixels); texture.Apply();
            File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=64; importer.mipmapEnabled=false; importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed; importer.filterMode=FilterMode.Bilinear;
            importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        // Small, bold silhouettes rasterized only by editor setup, never at runtime.
        public static bool Glyph(TruckTaxiMapMarkerType type,Vector2 p)
        {
            switch(type)
            {
                case TruckTaxiMapMarkerType.PassengerPickup:
                    return Circle(p,.4f,.74f,.12f) || Box(p,.26f,.25f,.54f,.55f) ||
                        Line(p,.2f,.13f,.3f,.46f,.065f) || Line(p,.6f,.13f,.5f,.46f,.065f) ||
                        Line(p,.73f,.37f,.73f,.69f,.045f) || Line(p,.59f,.53f,.87f,.53f,.045f);
                case TruckTaxiMapMarkerType.Destination:
                    return Box(p,.17f,.12f,.24f,.86f) || (Box(p,.24f,.45f,.84f,.86f) &&
                        (((int)((p.x-.24f)/.15f)+(int)((p.y-.45f)/.137f))%2==0));
                case TruckTaxiMapMarkerType.ScenicStop:
                    return Line(p,.12f,.25f,.39f,.76f,.05f) || Line(p,.39f,.76f,.67f,.25f,.05f) ||
                        Line(p,.49f,.57f,.62f,.78f,.05f) || Line(p,.62f,.78f,.89f,.25f,.05f) ||
                        Line(p,.13f,.22f,.89f,.22f,.05f) || Circle(p,.81f,.8f,.07f);
                case TruckTaxiMapMarkerType.IllicitStop:
                    return Box(p,.2f,.2f,.8f,.65f) && !(Box(p,.29f,.32f,.71f,.4f) && (p.x<.43f || p.x>.57f)) ||
                        (Circle(p,.5f,.69f,.18f) && !Circle(p,.5f,.69f,.1f) && p.y>.64f);
                case TruckTaxiMapMarkerType.Shortcut: case TruckTaxiMapMarkerType.DiscoveredShortcut:
                    return Line(p,.2f,.18f,.2f,.55f,.065f) || Line(p,.2f,.55f,.8f,.55f,.065f) ||
                        Line(p,.62f,.36f,.81f,.55f,.065f) || Line(p,.62f,.74f,.81f,.55f,.065f) ||
                        (type==TruckTaxiMapMarkerType.DiscoveredShortcut && (Line(p,.41f,.22f,.53f,.13f,.04f) || Line(p,.53f,.13f,.76f,.31f,.04f)));
                case TruckTaxiMapMarkerType.TargetVehicle:
                    return Box(p,.25f,.3f,.75f,.57f) || Box(p,.34f,.57f,.66f,.73f) ||
                        Circle(p,.33f,.26f,.06f) || Circle(p,.67f,.26f,.06f) ||
                        Line(p,.08f,.5f,.2f,.5f,.035f) || Line(p,.8f,.5f,.92f,.5f,.035f) ||
                        Line(p,.5f,.78f,.5f,.93f,.035f) || Line(p,.5f,.07f,.5f,.21f,.035f);
                case TruckTaxiMapMarkerType.ActiveObjective:
                    return (Circle(p,.5f,.5f,.32f) && !Circle(p,.5f,.5f,.25f)) || Circle(p,.5f,.5f,.07f) ||
                        Line(p,.08f,.5f,.3f,.5f,.035f) || Line(p,.7f,.5f,.92f,.5f,.035f) ||
                        Line(p,.5f,.08f,.5f,.3f,.035f) || Line(p,.5f,.7f,.5f,.92f,.035f);
                case TruckTaxiMapMarkerType.Dropoff:
                    return Line(p,.5f,.83f,.5f,.35f,.06f) || Line(p,.32f,.53f,.5f,.35f,.06f) ||
                        Line(p,.68f,.53f,.5f,.35f,.06f) || Box(p,.2f,.15f,.8f,.22f);
                case TruckTaxiMapMarkerType.FoodStop:
                    return Box(p,.24f,.2f,.31f,.76f) || Box(p,.15f,.6f,.4f,.67f) || Box(p,.15f,.6f,.21f,.83f) ||
                        Box(p,.35f,.6f,.41f,.83f) || Box(p,.65f,.2f,.72f,.65f) || Circle(p,.685f,.73f,.13f);
                case TruckTaxiMapMarkerType.PhotoStop:
                    return (Box(p,.15f,.25f,.85f,.69f) && !Circle(p,.5f,.47f,.14f)) || Box(p,.32f,.69f,.58f,.79f);
                case TruckTaxiMapMarkerType.Danger:
                case TruckTaxiMapMarkerType.Hazard:
                    return Line(p,.13f,.2f,.5f,.85f,.05f) || Line(p,.5f,.85f,.87f,.2f,.05f) || Line(p,.13f,.2f,.87f,.2f,.05f) ||
                        Box(p,.46f,.4f,.54f,.64f) || Circle(p,.5f,.3f,.04f);
                case TruckTaxiMapMarkerType.Bathroom:
                    return Box(p,.19f,.48f,.4f,.84f) || Box(p,.3f,.18f,.48f,.42f) ||
                        (Circle(p,.5f,.48f,.28f) && p.y<=.49f) || Box(p,.19f,.49f,.82f,.56f);
                case TruckTaxiMapMarkerType.Gas:
                    return Box(p,.2f,.22f,.56f,.75f) || Box(p,.56f,.25f,.65f,.64f) ||
                        Line(p,.65f,.64f,.82f,.54f,.04f) || Box(p,.77f,.27f,.85f,.53f);
                case TruckTaxiMapMarkerType.Store:
                    return Box(p,.17f,.22f,.83f,.58f) || Box(p,.23f,.59f,.77f,.7f) ||
                        Box(p,.29f,.7f,.71f,.8f) || Box(p,.42f,.23f,.58f,.49f);
                case TruckTaxiMapMarkerType.Repair:
                    return Circle(p,.38f,.66f,.18f) || Line(p,.43f,.58f,.77f,.23f,.09f) ||
                        Circle(p,.79f,.21f,.11f);
                case TruckTaxiMapMarkerType.TrainStation:
                    return Box(p,.2f,.34f,.8f,.75f) || Box(p,.27f,.25f,.37f,.34f) ||
                        Box(p,.63f,.25f,.73f,.34f) || Line(p,.18f,.18f,.82f,.18f,.04f);
                case TruckTaxiMapMarkerType.BusTerminal:
                    return Box(p,.16f,.32f,.84f,.72f) || Circle(p,.29f,.27f,.07f) ||
                        Circle(p,.71f,.27f,.07f) || Box(p,.3f,.56f,.7f,.65f);
                case TruckTaxiMapMarkerType.Racetrack:
                    return (Circle(p,.5f,.5f,.35f) && !Circle(p,.5f,.5f,.23f)) ||
                        Line(p,.25f,.5f,.75f,.5f,.03f);
                case TruckTaxiMapMarkerType.ServiceArea:
                    return Box(p,.18f,.19f,.26f,.81f) || Box(p,.74f,.19f,.82f,.81f) ||
                        Box(p,.25f,.46f,.75f,.54f);
                case TruckTaxiMapMarkerType.SportsStadium:
                    return (Box(p,.15f,.25f,.85f,.75f) && !Box(p,.23f,.33f,.77f,.67f)) ||
                        Line(p,.5f,.3f,.5f,.7f,.025f) || Circle(p,.5f,.5f,.09f);
                case TruckTaxiMapMarkerType.ConcertVenue:
                    return Line(p,.38f,.25f,.38f,.77f,.05f) || Line(p,.74f,.35f,.74f,.86f,.05f) ||
                        Line(p,.38f,.77f,.74f,.86f,.05f) || Circle(p,.29f,.25f,.11f) || Circle(p,.65f,.35f,.11f);
                case TruckTaxiMapMarkerType.Surge:
                    return Line(p,.62f,.88f,.35f,.49f,.08f) || Line(p,.35f,.49f,.65f,.49f,.07f) ||
                        Line(p,.65f,.49f,.37f,.12f,.08f);
                case TruckTaxiMapMarkerType.RoadClosure:
                    return Box(p,.15f,.36f,.85f,.65f) || Box(p,.23f,.18f,.31f,.75f) || Box(p,.69f,.18f,.77f,.75f);
                case TruckTaxiMapMarkerType.WorkArea:
                    return (Circle(p,.5f,.5f,.35f) && !Circle(p,.5f,.5f,.28f)) || Circle(p,.5f,.5f,.07f);
                default: throw new ArgumentOutOfRangeException(nameof(type),type,"Expected an installed Heat icon for this type.");
            }
        }
        private static bool Box(Vector2 p,float x,float y,float right,float top) => p.x>=x && p.x<=right && p.y>=y && p.y<=top;
        private static bool Circle(Vector2 p,float x,float y,float radius) => (p-new Vector2(x,y)).sqrMagnitude<=radius*radius;
        private static bool Line(Vector2 p,float x,float y,float endX,float endY,float halfWidth)
        {
            var a=new Vector2(x,y); var delta=new Vector2(endX,endY)-a;
            return (p-(a+delta*Mathf.Clamp01(Vector2.Dot(p-a,delta)/delta.sqrMagnitude))).sqrMagnitude<=halfWidth*halfWidth;
        }
        private static void Folder(string path)
        {
            if(AssetDatabase.IsValidFolder(path)) return;
            string parent=path.Substring(0,path.LastIndexOf('/')); Folder(parent); AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
    }
}
