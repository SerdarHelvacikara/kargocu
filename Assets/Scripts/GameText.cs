using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>Small local phrasebook; no network or translation service is used.</summary>
    public static class GameText
    {
        public const string PreferenceKey = "language";
        public static bool Turkish { get; private set; }
        public static string VersionLabel { get; private set; }
        static bool isolated;

        public static void Initialize(bool isolatedTest = false)
        {
            VersionLabel = "v" + Application.version;
            isolated = isolatedTest;
            if (isolated)
            {
                Turkish = true;
                return;
            }
            string automatic = Application.systemLanguage == SystemLanguage.Turkish ? "tr" : "en";
            string saved = LocalProgress.GetString(PreferenceKey, automatic);
            Turkish = saved == "tr";
        }

        public static string Pick(string tr, string en) => Turkish ? tr : en;

        public static void SetLanguage(bool turkish)
        {
            Turkish = turkish;
            if (isolated) return;
            LocalProgress.SetString(PreferenceKey, turkish ? "tr" : "en");
            LocalProgress.Save();
        }

        public static void Toggle() => SetLanguage(!Turkish);

        public static string ParcelName(ParcelKind kind)
        {
            switch (kind)
            {
                case ParcelKind.Cake: return Pick("Doğum günü pastası", "Birthday cake");
                case ParcelKind.Plant: return Pick("Saksı çiçeği", "Potted plant");
                default: return Pick("Çay takımı", "Tea set");
            }
        }

        public static string ParcelHint(ParcelKind kind)
        {
            switch (kind)
            {
                case ParcelKind.Cake: return Pick("Virajlarda sallanır; dengeli sür.", "Wobbly on corners. Keep it steady.");
                case ParcelKind.Plant: return Pick("Ağır yük: daha yavaş hızlanır.", "Heavy load: slower acceleration.");
                default: return Pick("Çok kırılgan; çarpmamaya dikkat.", "Extra fragile. Mind the bumps.");
            }
        }

        public static string ConditionName(float integrity)
        {
            if (integrity >= 90) return Pick("Sapasağlam", "Pristine");
            if (integrity >= 60) return Pick("Biraz hırpalanmış", "A little dented");
            return Pick("Hasarlı", "Damaged");
        }

        public static string Reaction(ParcelKind kind, float integrity)
            => Reaction(kind, integrity, 0);

        // A delivery chooses its selector once. Rendering or changing language
        // never rolls another line, and legacy callers retain the original nine.
        public static string Reaction(ParcelKind kind, float integrity, int selector)
        {
            bool good = integrity >= 90;
            bool dented = integrity >= 60;
            selector = Mathf.Clamp(selector, 0, 4);
            if (selector > 0)
            {
                int parcel = Mathf.Clamp((int)kind, 0, 2);
                int tier = good ? 0 : dented ? 1 : 2;
                int index = parcel * 12 + tier * 4 + selector - 1;
                return Pick(ExtraReactionTr[index], ExtraReactionEn[index]);
            }
            switch (kind)
            {
                case ParcelKind.Cake:
                    if (good) return Pick("Mum bile yerinde. Sen kurye değil, cerrahsın!", "Not a single candle moved. Courier or surgeon?");
                    if (dented) return Pick("Krema kaymış ama olsun. Yamuk pasta artık moda.", "A little lopsided? Very fashionable.");
                    return Pick("Pasta sipariş ettim, puding geldi.", "I ordered cake. This looks like pudding.");
                case ParcelKind.Plant:
                    if (good) return Pick("Bir yaprak bile kıpırdamamış. Çiçek benden daha rahat!", "Not a leaf out of place. This plant is calmer than I am.");
                    if (dented) return Pick("Biraz eğik ama ben de bugün öyleyim.", "Leaning a little? Same here today.");
                    return Pick("Çiçeği aldım. Saksıyı süpürgeyle alıyorum.", "The plant is here. I'll need a broom for the pot.");
                default:
                    if (good) return Pick("Altı bardak, altı tabak. Hepsi tam! Bir çay iç de git.", "Every cup made it. Stay for some tea!");
                    if (dented) return Pick("Bu çatlak bardak artık kalemlik olur.", "That cracked cup can hold my pencils.");
                    return Pick("Çay takımı istemiştim, mozaik seti gelmiş.", "I ordered a tea set, not a mosaic kit.");
            }
        }

        static readonly string[] ExtraReactionTr = {
            // Cake: pristine, dented, damaged; four alternatives per condition.
            "Krema bile emniyet kemeri takmış gibi. Şahane!",
            "Mumlar dimdik! Dileğimi de sen teslim eder misin?",
            "Bu pasta fotoğrafa hazır. Sen de gülümse!",
            "Pasta tam zamanında. Alkışlar kuryeye!",
            "Biraz yana yatmış. Kutlama da eğlenceli olsun!",
            "Krema virajı alamamış ama tadı burada.",
            "Pastanın yeni saç modeli olmuş. Kabul!",
            "Bir dilim erken yürümüş galiba. Çay koyuyorum.",
            "Pastayı buldum. Şeklini hâlâ arıyorum.",
            "Mumları dikecek bir tepe kaldı mı acaba?",
            "Bu artık sürpriz pasta. Sürprizi kutunun içinde!",
            "Kaşıkla servis yapalım. Buna da çözüm bulduk!",
            // Plant.
            "Yapraklar selam veriyor. Yolculuk çok rahatmış!",
            "Saksı pırıl pırıl. Mahallemize hoş geldin filiz!",
            "Çiçeğim senden sürüş dersi istiyor.",
            "Toprak yerinde, yapraklar havalı. Harika!",
            "Bir yaprak eksik, karakteri tam yerinde.",
            "Çiçek biraz eğilmiş. Güneşe doğru çeviririz!",
            "Saksı küçük bir macera yaşamış belli.",
            "Yapraklar da mahalle turuna çıkmış sanırım.",
            "Saksı yoga yapmış. Şimdi dinlenme zamanı.",
            "Bahçe eldivenlerimi getiriyorum. Çok işimiz var!",
            "Bu çiçeğin tatil dönüşü biraz zor olmuş.",
            "Toprağı da tek tek teslim etmişsin. Sayıyorum!",
            // Tea set.
            "Bardaklar sırada, çay hazır. Tam takım!",
            "Bir tıkırtı bile yok. Bu iş sende!",
            "Bardaklar birbirine değmemiş. Ne nazik sürüş!",
            "Çay molası hak ettin. İnce belli bir teşekkür!",
            "Bir fincan hafif yamuk. Benim favorim o olsun.",
            "Çay takımı biraz sohbet etmiş yolda.",
            "Tabaklar kaymış. Serviste toparlarız!",
            "Küçük bir çatlak, büyük bir çay keyfi.",
            "Fincanlar yapboza dönmüş. Kenar parçaları nerede?",
            "Çayı termosla içelim. Daha sağlam olur.",
            "Bu takımın başından romanlık bir yol geçmiş.",
            "Misafir sayısını azaltmam gerekecek galiba!"
        };

        static readonly string[] ExtraReactionEn = {
            "Even the icing wore a seat belt. Lovely!",
            "Every candle is upright! Can you deliver my wish too?",
            "This cake is ready for its photo. Smile!",
            "Cake on time. A round of applause for our courier!",
            "A little sideways. Let's make it a lively party!",
            "The icing missed a turn, but the flavor made it.",
            "The cake has a new hairstyle. I like it!",
            "One slice went for an early walk. I'll make tea.",
            "Found the cake. Still looking for its shape.",
            "Is there a little hill left for the candles?",
            "A surprise cake! The surprise is inside the box.",
            "Let's serve it with spoons. Problem solved!",
            "The leaves are waving. It was a comfortable ride!",
            "A spotless pot. Welcome to our neighborhood, sprout!",
            "My plant would like driving lessons from you.",
            "Soil in place, leaves looking good. Brilliant!",
            "One leaf short, still full of character.",
            "A little lean. We'll turn it toward the sun!",
            "This pot has clearly had a small adventure.",
            "I think the leaves went sightseeing too.",
            "The pot has tried yoga. Time for a rest.",
            "Fetching my gardening gloves. We have work to do!",
            "This plant had a rather bumpy trip home.",
            "You delivered the soil grain by grain. I'm counting!",
            "Cups in a row, tea ready. The whole team!",
            "Not even a rattle. You've got this!",
            "The cups never touched. Such a gentle ride!",
            "You've earned a tea break. A little cup of thanks!",
            "One cup leans a little. That will be my favorite.",
            "The tea set had quite a chat on the way.",
            "The saucers slipped. We'll tidy up at teatime!",
            "A little crack, still a lovely cup of tea.",
            "The cups became a puzzle. Where are the edge pieces?",
            "Let's use a flask for tea. A sturdier choice.",
            "This tea set has been on a novel's worth of journey.",
            "I may have to invite fewer guests!"
        };
    }
}
