using System;
using System.Collections.Generic;
using UnityEngine;
using S = LWS.TruckTaxi.TruckTaxiWobbleVisual.Silhouette;
using H = LWS.TruckTaxi.TruckTaxiWobbleVisual.Headwear;
using A = LWS.TruckTaxi.TruckTaxiWobbleVisual.Accessory;

namespace LWS.TruckTaxi.Editor
{
    // Stable-ID art direction. These are original shorthand designs, not imported character likenesses.
    public static class TruckTaxiRosterVisualCatalog
    {
        public readonly struct Entry
        {
            public readonly string id, bodyHex, clothesHex, accentHex;
            public readonly S silhouette;
            public readonly H headwear;
            public readonly A accessory;
            public readonly float width, headScale;
            public Entry(string id, string body, string clothes, string accent, S silhouette, H headwear, A accessory, float width, float headScale)
            { this.id=id; bodyHex=body; clothesHex=clothes; accentHex=accent; this.silhouette=silhouette; this.headwear=headwear; this.accessory=accessory; this.width=width; this.headScale=headScale; }
        }

        private static Entry E(string id, string body, string clothes, string accent, S silhouette, H headwear, A accessory, float width=1f, float head=1f) =>
            new Entry(id,body,clothes,accent,silhouette,headwear,accessory,width,head);

        public static readonly Entry[] Entries = {
            E("passenger-d9f5d0d1a6a6","628F7B","3D6572","DFC268",S.Human,H.Hair,A.Book,.86f),
            E("passenger-488bd780fe51","A86648","354F75","E5C55B",S.Human,H.Cap,A.Clock,1.08f),
            E("passenger-e85cc6083da9","6B849A","D6B584","D46E58",S.Human,H.Visor,A.Camera,.9f),
            E("passenger-3c18da10302b","52889A","474A70","DAA75B",S.Human,H.Crest,A.Circuit,.83f,1.17f),
            E("passenger-a492d1d13dab","E1A759","C46F63","E8D575",S.Human,H.Curls,A.Trophy,.96f),
            E("passenger-009ae3a402da","A45174","314C69","E6B954",S.Wide,H.Crest,A.Microphone,1.23f),
            E("passenger-0dc6acff58d9","69827C","D9D3BA","CB7960",S.Human,H.Braid,A.Shield,.91f),
            E("passenger-ca28f1b00a01","B19A5D","4E7E8B","E3C86C",S.Human,H.Visor,A.Tool,.88f),
            E("passenger-419497acb1af","B3574D","353F51","E4B35A",S.Wide,H.Crest,A.Crate,1.25f),
            E("passenger-6571dd82a0e1","6E8B6D","D0B67D","BB637D",S.Human,H.WideHat,A.Card,.94f),
            E("p001","386C55","29455F","E7A742",S.Wide,H.Cap,A.Tool,1.17f),
            E("p002","4A9D91","344E75","F5CC63",S.Human,H.Hair,A.Backpack,.82f,1.12f),
            E("p003","303D54","F2F0DF","C44748",S.Human,H.Hair,A.Tie,.94f),
            E("p004","E67176","4E9DAA","F2D563",S.Human,H.Curls,A.Satchel,.9f,1.1f),
            E("p005","B84F45","252F42","E6C65C",S.Wide,H.Crest,A.Trophy,1.08f),
            E("p006","7D4C82","EEE2BD","C9A34E",S.Wide,H.WideHat,A.Book,1.13f),
            E("p007","607D9E","E0D7BD","659B70",S.Human,H.Hair,A.Book,.85f),
            E("p008","D68A45","386E81","F0C958",S.Wide,H.Hair,A.Food,1.65f,.95f),
            E("p009","9B474A","776B58","E7BE76",S.Human,H.Hair,A.Coin,.86f,1.14f),
            E("sports-commentator","37765E","E6D394","D7594D",S.Wide,H.Cap,A.Headset,1.34f),
            E("color-commentator","4B7791","DBD5C7","D69F53",S.Human,H.Hair,A.Microphone,.9f),
            E("nascar-superfan","B8413D","313B60","EFE6D4",S.Wide,H.Cap,A.Trophy,1.1f),
            E("stock-car-veteran","56617D","CD9953","D5D3C7",S.Human,H.Cap,A.Tool,1.06f),
            E("street-baller","B56342","6842A5","EDC44F",S.Human,H.Crest,A.Trophy,1.07f),
            E("veteran-trucker","426B68","A76940","D8BA76",S.Wide,H.Cap,A.Tool,1.22f),
            E("offroad-lunatic","B95E38","768045","EEE3B8",S.Wide,H.Helmet,A.Goggles,1.14f),
            E("purple-dragon","7D4BBA","CC913D","BBD866",S.Dragon,H.Horns,A.None,.85f,1.19f),
            E("hover-companion","B9D856","4DA7BD","F7E693",S.Bird,H.Crest,A.None,.65f,1.26f),
            E("cheetah-challenger","C5A23F","3A394A","E9D888",S.Cat,H.Ears,A.Clock,.82f),
            E("merchant-bear","91633F","3F664F","E5C256",S.Bear,H.WideHat,A.Coin,1.29f),
            E("angry-fantasy-villain","B84F3E","68549B","E8C150",S.Small,H.Horns,A.Staff,1.1f,1.2f),
            E("sensible-faun","B78B5E","477F68","D7BF80",S.Faun,H.Braid,A.Book,.88f),
            E("kangaroo-jumper","BD8451","397FA5","E9D575",S.Kangaroo,H.None,A.Backpack,.9f),
            E("military-bird","839B65","525F43","D6B34F",S.Bird,H.Helmet,A.Badge,.93f),
            E("large-yeti","D9DDD3","668FAB","A8C8D8",S.Yeti,H.None,A.Scarf,1.58f,1.13f),
            E("space-monkey","9B6D4C","344F91","6CC6CE",S.Primate,H.Helmet,A.Goggles,.84f,1.13f),
            E("fantasy-professor","63768D","A6805F","E6C782",S.Human,H.Hair,A.Book,.85f,1.15f),
            E("insult-sensitive-creature","6D9566","A55A4F","E2CB70",S.Alien,H.Ears,A.Shield,1.2f),
            E("large-roadside-creature","8A9B4F","5E6B45","D8B665",S.Yeti,H.Horns,A.Food,1.7f,1.12f),
            E("alien-tinkerer","BF8A58","388C9A","D8CF6B",S.Alien,H.Ears,A.Tool,.91f),
            E("analytical-robot","6C99A4","394D68","B8E5DD",S.Robot,H.Visor,A.Circuit,.73f,1.13f),
            E("space-hero","5F80A9","F0D47C","C84A51",S.Alien,H.Crest,A.Cape,1.31f),
            E("robot-villain","70466B","38384D","DB6074",S.Robot,H.Horns,A.Cape,1.13f,1.14f),
            E("quiet-hero","69775B","435B83","E9D1A1",S.Human,H.Crest,A.Scarf,.93f),
            E("talking-sidekick","C07E3A","3E9DA6","E6CD65",S.Primate,H.Ears,A.Backpack,.72f,1.24f),
            E("mechanic-inventor","5A9C9A","606F8C","E3B556",S.Human,H.Braid,A.Tool,.94f),
            E("critical-old-mentor","8A845F","4E7565","D4BA87",S.Faun,H.Beard,A.Staff,.89f,1.12f),
            E("chaotic-marsupial","CC783E","3E83A1","F1D364",S.Kangaroo,H.Crest,A.Crate,.91f,1.14f),
            E("scheming-scientist","8E5C86","E1DFD3","65B4AD",S.Human,H.Crest,A.Goggles,.82f,1.22f),
            E("mystical-mask","A76E46","527E6E","EBC96E",S.Mask,H.Crest,A.Staff,.87f,1.2f),
            E("laughing-lunatic","2C92A0","D68B3F","E7CC6E",S.Kangaroo,H.Crest,A.Crate,.75f,1.18f),
            E("muscle-animal","BC8D3D","334F56","EBCC6B",S.Bear,H.Ears,A.Armor,1.64f,.9f),
            E("unstable-engineer","7E8B9A","5F6B55","E4A94D",S.Robot,H.Crest,A.Circuit,.99f),
            E("time-scientist","457C9D","E9DCC2","D9A748",S.Human,H.Visor,A.Clock,.85f,1.1f),
            E("tech-hero","E2A651","3989A2","E8D36C",S.Cat,H.Curls,A.Circuit,.84f),
            E("nervous-scientist","87956A","E5E5D7","B45755",S.Human,H.Hair,A.Goggles,.76f,1.19f),
            E("giant-alien-elder","79A552","705B8A","DDD48A",S.Alien,H.None,A.Food,1.7f,1.2f),
            E("bald-chaos-warrior","BE8551","8B543C","E2C461",S.Wide,H.None,A.Armor,1.62f,.87f),
            E("bird-loving-android","739B79","3C646F","F2A642",S.Robot,H.Crest,A.Flower,1.29f),
            E("ominous-attendant","DDD5C9","5C477C","A481BD",S.Mask,H.WideHat,A.Staff,.96f,1.14f),
            E("martial-arts-celebrity","BD794A","E8CF7D","476A8D",S.Wide,H.Curls,A.Trophy,1.14f),
            E("angry-power-warrior","4274A0","E7D7B2","EAAE4A",S.Wide,H.Crest,A.Armor,1.19f),
            E("smug-bio-villain","6E9B67","3E6574","BDCE59",S.Alien,H.Crest,A.Armor,1.15f),
            E("polite-tyrant","E2D5D6","6B4E83","E9B858",S.Alien,H.Crown,A.Cape,.82f,1.22f),
            E("short-anxious-fighter","BF8E63","C66A3B","E4D0A2",S.Small,H.None,A.Scarf,.82f,1.1f),
            E("grumpy-green-warrior","64A37A","745C8D","D0B771",S.Alien,H.Ears,A.Cape,1.1f),
            E("food-obsessed-dad","B67C4F","518091","E2B859",S.Wide,H.Cap,A.Food,1.48f),
            E("ancient-billionaire","A0A6A3","293F51","D6B95E",S.Human,H.Hair,A.Coin,.72f,1.17f),
            E("comic-book-nerd","8682B9","424F61","E5C65A",S.Human,H.Hair,A.Book,.85f,1.14f),
            E("clueless-adult","D39858","5BA995","F2D16D",S.Human,H.Cap,A.Food,.94f,1.2f),
            E("lazy-cop","557CA1","293F5A","D4BE68",S.Wide,H.Cap,A.Badge,1.17f),
            E("washed-up-entertainer","B96D79","4C5568","E1BC6D",S.Human,H.WideHat,A.Microphone,.93f),
            E("rich-card-rival","47598A","DDD5C4","D4A74A",S.Human,H.Hair,A.Card,.92f),
            E("card-game-hero","8A587C","414C89","E2C85C",S.Human,H.Crest,A.Card,.85f,1.15f),
            E("crime-psychopath","9A654B","4F7766","D8B557",S.Wide,H.Crest,A.Crate,1.19f),
            E("simulation-passenger","67A9A3","D8B67D","A26193",S.Human,H.Curls,A.Flower,.99f,1.2f),
            E("smuggler","6B7663","4F4D61","D8B86C",S.Human,H.Hood,A.Satchel,.89f),
            E("giant-chicken","F0E2C3","E4B464","D95745",S.Chicken,H.Crest,A.None,1.36f,1.18f),
            E("property-cat","B89665","4F7081","E8D5A5",S.Cat,H.Ears,A.Crate,.7f,1.28f),
            E("sleepy-inventor","8799A6","D4C6A7","68A29C",S.Human,H.Hair,A.Tool,.78f,1.2f),
            E("reptile-superfan","7D995D","4C5D8E","D6B85E",S.Human,H.Cap,A.Book,.94f),
            E("manual-comedy","6F8D9D","E4D7BB","C65D4D",S.Human,H.Visor,A.Book,.89f),
            E("fantasy-dwarf","A77B53","536D8D","DCC176",S.Wide,H.Beard,A.Shield,1.42f,1.13f),
            E("fantasy-archer","668364","B78B5C","E3C97D",S.Faun,H.Hood,A.Bow,.84f),
            E("jersey-creature","3E6263","685079","D9B569",S.Bird,H.Horns,A.Cape,1.06f,1.13f),
            E("racing-blackstripe-veteran","343C42","AF7848","E3C379",S.Human,H.Cap,A.Trophy,.94f),
            E("racing-shorttrack-deadpan","88AEC1","596879","D6B26B",S.Human,H.Cap,A.Tool,.82f),
            E("racing-excitement-hothead","C64D43","EFE6D1","3E454E",S.Wide,H.Crest,A.Trophy,1.17f),
            E("racing-southern-commentator","278A8C","E7D3AF","D8875B",S.Wide,H.Hair,A.Headset,1.26f),
            E("racing-tennessee-veteran","D0A346","4E6E86","E1D0B0",S.Human,H.Hair,A.Tool,1.04f),
            E("glam-redhead-reporter","E4735B","3C5365","E9C064",S.Human,H.Hair,A.Camera,.92f),
            E("glam-space-femme","65B5C8","E5E3D9","E3BD62",S.Human,H.Braid,A.Badge,.82f),
            E("glam-genius-heiress","785686","EEE5D8","D7AE55",S.Human,H.Curls,A.Circuit,.86f),
            E("glam-dual-personality","2E4E58","289D9D","E0C47A",S.Human,H.Hair,A.Card,.95f),
            E("glam-witch-gunslinger","865174","B26B4D","DBC170",S.Human,H.WideHat,A.Staff,.88f),
            E("glam-spy-agent","454C58","697F8E","D6BC71",S.Human,H.Braid,A.Badge,.81f),
            E("glam-redhead-survivor","748060","B77250","D9C273",S.Human,H.Braid,A.Backpack,.93f),
            E("glam-tactical-agent","475B79","334358","D4BB72",S.Human,H.Crest,A.Armor,.91f),
            E("glam-tomb-adventurer","B58B5C","607D66","E0C27A",S.Human,H.Braid,A.Satchel,.89f),
            E("glam-redhead-sorceress","A94250","733F68","E6BF68",S.Human,H.Curls,A.Staff,.87f),
            E("glam-dark-sorceress","353C56","695388","B8A6DD",S.Human,H.Hood,A.Cape,.9f),
            E("glam-wild-witch","56805B","A08858","D1BD75",S.Human,H.Curls,A.Flower,.94f),
            E("glam-perfect-operative","DDE0D8","434D56","D3AC61",S.Human,H.Braid,A.Circuit,.82f),
            E("glam-rebel-biotic","893F64","4C526D","B786CE",S.Human,H.Crest,A.Cape,.91f),
            E("glam-brawler-bartender","A77256","E2CFAD","4E7281",S.Wide,H.Braid,A.Apron,1.13f),
            E("glam-flower-mystic","5A8C68","C3A1BE","E6C779",S.Human,H.Braid,A.Flower,.88f),
            E("glam-armored-huntress","8C775A","677E72","C5B883",S.Human,H.Braid,A.Armor,.99f),
            E("glam-special-forces","B39D74","465B63","E1CA85",S.Human,H.Curls,A.Badge,.89f),
            E("glam-fire-fighter","B75640","C6A55B","E3D3AE",S.Human,H.Helmet,A.Badge,.98f),
            E("glam-aristocrat-warrior","416B9E","E5D9C7","D7B75B",S.Human,H.Braid,A.Shield,.94f),
            E("glam-soul-officer","5A80AD","E2E0D6","9BB7DB",S.Human,H.Curls,A.Badge,.9f),
            E("glam-shadow-agent","515D66","333A43","B09A8E",S.Human,H.Hair,A.Satchel,.82f),
            E("glam-legendary-medic","E1E0D3","5E9D9F","D0AD5D",S.Human,H.Braid,A.Medical,.9f),
            E("glam-redhead-college-adult","B66E61","6D8EAA","E2C683",S.Human,H.Curls,A.Backpack,.87f)
        };

        private static readonly Dictionary<string, Entry> ById = BuildIndex();
        private static Dictionary<string, Entry> BuildIndex()
        {
            var result = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in Entries)
            {
                if (result.ContainsKey(entry.id)) throw new InvalidOperationException("Duplicate visual design: " + entry.id);
                result.Add(entry.id, entry);
            }
            return result;
        }
        public static bool TryGet(string id, out Entry entry) => ById.TryGetValue(id ?? string.Empty, out entry);
        public static Color Color(string hex)
        {
            if (!ColorUtility.TryParseHtmlString("#" + hex, out var color))
                throw new ArgumentException("Invalid roster visual color: " + hex, nameof(hex));
            return color;
        }
    }
}
