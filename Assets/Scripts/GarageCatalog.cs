namespace KurmaliKargo
{
    /// <summary>Stable decoration IDs shared by saves, the shop, and fixed room slots.</summary>
    public static class GarageCatalog
    {
        public const int Count = 14;
        // IDs 0-8 deliberately retain the original names, prices and saved mask bits.
        public static readonly int[] Prices = { 40, 65, 85, 110, 135, 160, 180, 210, 240, 275, 300, 340, 380, 440 };

        static readonly string[] NamesTr = {
            "Saksı", "Lamba", "Halı", "Tezgâh", "Raf", "Kargo tabelası", "Rahat koltuk", "Paket posteri", "Drone platformu",
            "Filiz dost posteri", "Bulut dost posteri", "Boğaz manzarası", "Sağlam paket kupası", "Paket otomatı"
        };
        static readonly string[] NamesEn = {
            "Plant", "Lamp", "Rug", "Workbench", "Shelf", "Parcel sign", "Cozy chair", "Parcel poster", "Drone pad",
            "Sprout pal poster", "Cloud pal poster", "Bosphorus skyline", "Handle-with-care trophy", "Parcel vending machine"
        };
        static readonly string[] HintsTr = {
            "Teslimat aralarında biraz yeşillik.",
            "Atölyene sıcak bir ışık ekler.",
            "Aracının altına yumuşak bir renk.",
            "Çekmeceli, düzenli bir çalışma köşesi.",
            "Paketlerin için dört katlı bir raf.",
            "Garajına ışıklı bir kargo simgesi.",
            "Mesai bitince dinlenmek için.",
            "Kargo sevgisini duvara as.",
            "Drone'un için şık bir park yeri.",
            "Yaprak şapkalı özgün, minik bir dost.",
            "Bulut dostun mesaisi hep açık hava.",
            "Köprü, vapur ve gün batımı manzarası.",
            "Paket sağlam. Kupa biraz heyecanlı.",
            "Atıştırmalık yok; yalnızca minik paketler."
        };
        static readonly string[] HintsEn = {
            "A little greenery between deliveries.",
            "Add a warm glow to your workshop.",
            "A soft splash of color under your ride.",
            "A tidy workspace with fresh drawers.",
            "Four shelves for your precious parcels.",
            "A glowing parcel for your garage wall.",
            "Put your feet up after a busy shift.",
            "Hang your love of parcels on the wall.",
            "A smart parking spot for your drone.",
            "An original little pal in a leafy hat.",
            "This cloud pal always works outdoors.",
            "A bridge, a ferry, and a sunset view.",
            "The parcel is steady. The trophy is nervous.",
            "No snacks. Just adorable little parcels."
        };

        public static string Name(int index) => index >= 0 && index < Count
            ? GameText.Pick(NamesTr[index], NamesEn[index]) : "";
        public static string Hint(int index) => index >= 0 && index < Count
            ? GameText.Pick(HintsTr[index], HintsEn[index]) : "";
    }
}
