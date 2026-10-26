using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using LWS.TruckBowling;
using LWS.TruckCornhole;
using LWS.TruckCheeseRolling;
using LWS.TruckHockey;
using LWS.TruckAirHockey;
using LWS.TruckWallball;
using LWS.TruckBilliards;

namespace LWS.TruckSports
{
    public sealed class SportsTruckCompatibilityView
    {
        public string profileId,name,requested,effective,notice;public bool hasHost,canApply,bodyFallback;public int unavailable;
        public string[] lines=Array.Empty<string>(),paragraphs=Array.Empty<string>();
        public void FitDetails(Func<string,float> measure,float width){lines=SportsTruckCompatibilitySource.WrapDetails(paragraphs.Length>0?paragraphs:lines,measure,width);}
        public string ShortId=>string.IsNullOrEmpty(profileId)?"UNASSIGNED":profileId.Substring(0,Mathf.Min(8,profileId.Length));
        public int DetailPages=>Mathf.Max(1,Mathf.CeilToInt(lines.Length/9f));
        public string Details(int page)=>string.Join("\n",lines.Skip(Mathf.Clamp(page,0,DetailPages-1)*9).Take(9));
    }
    // One detached-draft context. Evaluation creates display data only; the host
    // remains the sole compatibility authority and the registry owns persistence.
    public sealed class SportsTruckCompatibilityContext
    {
        public SportsTruckCompatibilitySource source;public int slot;public string profileId;
        public Action<SportsTruckAppearanceData,SportsTruckCustomizerMenu> showDetails;
        public SportsTruckCompatibilityView Read(SportsTruckAppearanceData draft)=>source!=null?source.Evaluate(slot,profileId,draft):SportsTruckCompatibilitySource.Original(profileId,"Driver",draft,null,"This sport is no longer available.");
    }
    public sealed class SportsTruckCompatibilitySource:MonoBehaviour
    {
        public SportsEventSession session;public BowlingMatch bowling;public CornholeMatch cornhole;public CheeseMatch cheese;
        public HockeyMatch hockey;public AirHockeyMatch airHockey;public WallballMatch wallball;public BilliardsSession billiards;
        public MonoBehaviour menu;public GameObject setupPage;public CanvasGroup setupInteraction;public string sportName;
        public SportsTruckAppearanceCatalog catalog;public SportsTruckAppearanceHost[] participantHosts=Array.Empty<SportsTruckAppearanceHost>();
        // Explicit authoring only: a turn-based tractor can serve every setup
        // participant without preparing or assigning that tractor in this menu.
        public SportsTruckAppearanceHost sharedTurnHost;
        public ISportsParticipantSlot[] Participants=>session!=null?session.setup.players:bowling!=null?bowling.setup.players:cornhole!=null?cornhole.setup.players:cheese!=null?cheese.setup.players:hockey!=null?hockey.players:airHockey!=null?airHockey.players:wallball!=null?wallball.players:billiards!=null?billiards.players:Array.Empty<ISportsParticipantSlot>();
        public int Count=>Mathf.Clamp(session!=null?session.setup.count:bowling!=null?bowling.setup.count:cornhole!=null?cornhole.setup.count:cheese!=null?cheese.setup.count:hockey!=null?6:airHockey!=null?2:wallball!=null?wallball.Count:billiards!=null?billiards.IsPractice?1:2:0,0,Participants.Length);
        public bool IsSetup=>session!=null?!session.Running&&session.menus.Screen==SportsEventScreen.Setup:bowling!=null?!bowling.InMatch&&bowling.menus.Screen==BowlingScreen.Setup:cornhole!=null?!cornhole.InMatch&&cornhole.menus.Screen==CornholeScreen.Setup:cheese!=null?!cheese.InMatch&&cheese.menus.Screen==CheeseScreen.Setup:hockey!=null?!hockey.InMatch&&hockey.menus.Screen==HockeyScreen.Setup:airHockey!=null?!airHockey.InMatch&&airHockey.menus.Screen==AirHockeyScreen.Setup:wallball!=null?!wallball.InMatch&&wallball.menus.Screen==WallballScreen.Setup:billiards!=null?!billiards.InMatch&&billiards.Screen==BilliardsScreen.Setup:false;
        public SportsTruckAppearanceHost Host(int slot)
        {
            if(slot<0||slot>=Count)return null;
            if(sharedTurnHost!=null)return sharedTurnHost;
            if(bowling!=null&&bowling.setup.mode==BowlingMode.HotSeat)return bowling.lanes!=null&&bowling.lanes.Length>0&&bowling.lanes[0]!=null&&bowling.lanes[0].truck!=null?bowling.lanes[0].truck.appearanceHost:null;
            return slot<participantHosts.Length?participantHosts[slot]:null;
        }
        public void GateFresh()
        {
            if(session!=null)session.Gate();else if(bowling!=null)bowling.Gate();else if(cornhole!=null)cornhole.Gate();else if(cheese!=null)cheese.Gate();else if(hockey!=null)hockey.Gate();else if(airHockey!=null)airHockey.Gate();else if(wallball!=null)wallball.Gate();else if(billiards!=null&&billiards.input!=null)billiards.input.GateAll();
        }
        public SportsTruckCompatibilityView Read(int slot)
        {
            if(slot<0||slot>=Count)return Original("","Unassigned driver",null,catalog,"No participant is assigned to this position.");
            string id=Participants[slot].ParticipantId;return Evaluate(slot,id,SportsParticipantRegistry.GetAppearance(id));
        }
        public SportsTruckCompatibilityView Evaluate(int slot,string id,SportsTruckAppearanceData draft)
        {
            var participant=slot>=0&&slot<Count?Participants[slot]:null;string name=participant!=null?participant.DisplayName:"Driver";var host=Host(slot);var content=host!=null&&host.catalog!=null?host.catalog:catalog!=null?catalog:Resources.Load<SportsTruckAppearanceCatalog>("SportsTruckAppearanceCatalog");
            if(host==null)return Original(id,name,draft,content,"This sport uses its original truck appearance. Your saved body and accessories are kept for other sports.");
            var report=host.EvaluateDraft(draft);var view=new SportsTruckCompatibilityView{profileId=id,name=name,requested=BodyName(content,draft?.bodyId),effective=report.canApply?BodyName(content,report.effectiveBodyId):"ORIGINAL SPORT TRUCK",notice=report.notice??"",hasHost=true,canApply=report.canApply,bodyFallback=report.canApply&&!string.IsNullOrEmpty(draft?.bodyId)&&draft.bodyId!=report.effectiveBodyId};
            var lines=new List<string>{"DRIVER: "+name,"PROFILE ID: "+id,"REQUESTED: "+view.requested,"USED HERE: "+view.effective,"Saved choices are retained; this report does not apply or save anything."};
            if(!string.IsNullOrEmpty(report.notice))lines.Add(report.notice);
            foreach(var selected in draft?.selections??new List<SportsTruckCosmeticSelection>())
            {
                if(selected==null||string.IsNullOrEmpty(selected.itemId))continue;bool unavailable=!report.canApply||Array.IndexOf(report.unavailableSlots??Array.Empty<string>(),selected.slotId)>=0;
                if(unavailable)view.unavailable++;lines.Add(SlotName(selected.slotId)+": "+selected.itemId+(string.IsNullOrEmpty(selected.variantId)?"":" · VARIANT "+selected.variantId)+" · "+(unavailable?Reason(host,content,report,selected):"SUPPORTED HERE"));
            }
            view.paragraphs=lines.ToArray();view.lines=Wrap(view.paragraphs);return view;
        }
        public static SportsTruckCompatibilityView Original(string id,string name,SportsTruckAppearanceData draft,SportsTruckAppearanceCatalog catalog,string reason)
        {
            var view=new SportsTruckCompatibilityView{profileId=id,name=name,requested=BodyName(catalog,draft?.bodyId),effective="ORIGINAL SPORT TRUCK",notice=reason};var lines=new List<string>{"DRIVER: "+name,"PROFILE ID: "+id,"REQUESTED: "+view.requested,"USED HERE: "+view.effective,reason,"Your saved design is unchanged."};
            foreach(var selected in draft?.selections??new List<SportsTruckCosmeticSelection>())if(selected!=null&&!string.IsNullOrEmpty(selected.itemId)){view.unavailable++;lines.Add(SlotName(selected.slotId)+": "+selected.itemId+(string.IsNullOrEmpty(selected.variantId)?"":" · VARIANT "+selected.variantId)+" · NOT APPLIED IN THIS SPORT");}view.paragraphs=lines.ToArray();view.lines=Wrap(view.paragraphs);return view;
        }
        static string BodyName(SportsTruckAppearanceCatalog content,string id)
        {
            if(string.IsNullOrEmpty(id)){var body=content!=null?content.FindBody(content.defaultBodyId):null;return body!=null?body.label+" (DEFAULT)":"DEFAULT SAVED BODY";}
            var found=content!=null?content.FindBody(id):null;return found!=null?found.label+" ["+id+"]":"Unavailable body ["+id+"]";
        }
        static string Reason(SportsTruckAppearanceHost host,SportsTruckAppearanceCatalog content,SportsTruckAppearanceCompatibilityReport report,SportsTruckCosmeticSelection selected)
        {
            if(!report.canApply)return "ORIGINAL SPORT TRUCK";
            if(Array.IndexOf(host.modeOwnedSlots??Array.Empty<string>(),selected.slotId)>=0)return "RESERVED FOR THIS SPORT'S EQUIPMENT";
            if(!string.IsNullOrEmpty(selected.variantId))return "SAVED VARIANT IS NOT AUTHORED HERE";
            var item=Array.Find(content?.cosmetics??Array.Empty<SportsTruckAppearanceCatalog.Cosmetic>(),candidate=>candidate!=null&&candidate.id==selected.itemId&&candidate.slotId==selected.slotId);
            if(item==null||item.visualPrefab==null)return "SAVED ITEM CONTENT IS UNAVAILABLE";if(!item.CompatibleWith(report.effectiveBodyId))return "ITEM DOES NOT FIT THE EFFECTIVE BODY";
            if(selected.slotId==SportsTruckAppearanceSlots.Wheels)return "REQUIRED WHEEL ATTACHMENT IS UNAVAILABLE";return "REQUIRED BODY MOUNT IS UNAVAILABLE";
        }
        public static string SlotName(string slot)
        {
            switch(slot){case "hat":return "HAT";case "face.eyes":return "EYES";case "face.brows":return "EYEBROWS";case "face.mouth":return "MOUTH";case "face.mustache":return "MUSTACHE";case "face.lashes":return "EYELASHES";case "mirrors":return "MIRRORS";case "wheels":return "WHEELS";case "rear.ornament":return "REAR ORNAMENT";case "dashboard":return "DASHBOARD";default:return slot??"UNKNOWN SLOT";}
        }
        static string[] Wrap(IEnumerable<string> paragraphs)=>WrapDetails(paragraphs.ToArray(),value=>new StringInfo(value).LengthInTextElements,44);
        // Detached report formatting. Prefer complete words, retain whitespace
        // and split only tokens that cannot fit (including long stable IDs).
        // The existing TMP presenter supplies actual font/rectangle measurement.
        public static string[] WrapDetails(string[] paragraphs,Func<string,float> measure,float width)
        {
            if(measure==null||!float.IsFinite(width)||width<=0)throw new ArgumentException("A finite positive report width and measurement are required.");var lines=new List<string>();
            foreach(string paragraph in paragraphs??Array.Empty<string>())foreach(string part in (paragraph??"").Replace("\r","").Split('\n'))
            {
                var starts=StringInfo.ParseCombiningCharacters(part);if(starts.Length==0){lines.Add("");continue;}int first=0;
                string Slice(int end)=>part.Substring(starts[first],(end<starts.Length?starts[end]:part.Length)-starts[first]);
                while(first<starts.Length)
                {
                    int low=first+1,high=starts.Length,end=first;
                    while(low<=high){int middle=low+(high-low)/2;float measured=measure(Slice(middle));if(!float.IsFinite(measured)||measured<0)throw new ArgumentException("Report measurement must be finite and nonnegative.");if(measured<=width){end=middle;low=middle+1;}else high=middle-1;}
                    if(end==first)end=first+1;
                    if(end<starts.Length){for(int at=end-1;at>=first;at--)if(char.IsWhiteSpace(part,starts[at])){end=at+1;break;}}
                    lines.Add(Slice(end));first=end;
                }
            }
            return lines.ToArray();
        }
    }
}
