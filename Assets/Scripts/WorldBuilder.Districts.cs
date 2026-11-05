using UnityEngine;

namespace KurmaliKargo
{
    public static partial class WorldBuilder
    {
        static readonly string[][] DistrictStopsTr = {
            null,
            new[] { "Baharatçı", "Kumaşçı", "Çiçek Pazarı", "Bakır Atölyesi", "Fener Dükkânı", "Çarşı Yükleme Alanı" },
            new[] { "Çay Evi", "Erişte Dükkânı", "Porselen Atölyesi", "Bambu Avlusu", "Fener Atölyesi", "Doğu Çarşısı Deposu" },
            new[] { "Cam Kule", "Güneş Plaza", "Tasarım Ofisi", "Teras Katı", "Otopark Ofisi", "Yükleme Merkezi" }
        };
        static readonly string[][] DistrictStopsEn = {
            null,
            new[] { "Spice Shop", "Fabric Shop", "Flower Market", "Copper Workshop", "Lantern Shop", "Market Loading Bay" },
            new[] { "Tea House", "Noodle Shop", "Porcelain Studio", "Bamboo Courtyard", "Lantern Workshop", "East Market Depot" },
            new[] { "Glass Tower", "Sun Plaza", "Design Office", "Terrace Suite", "Parking Office", "Loading Center" }
        };

        // Per-district palettes keep every block different while the district stays recognisable.
        static readonly Color[] MarketWalls = { Hex("F3C98E"), Hex("EDA98C"), Hex("BFDCC6"), Hex("F2D3D0"), Hex("B9D2E6"), Hex("F7E2B4") };
        static readonly Color[] MarketRoofs = { Hex("C9744F"), Hex("4E9C8C"), Hex("D9A43F"), Hex("9C6A8E"), Hex("C9744F") };
        static readonly Color[] MarketAwnings = { Hex("E2634F"), Hex("5CAE7E"), Hex("4F92C9"), Hex("EAB63E"), Hex("A66BB0") };
        static readonly Color[] EastWalls = { Hex("F7EBD3"), Hex("DCEBD9"), Hex("F4D6CF"), Hex("EEDDBE") };
        static readonly Color[] EastRoofs = { Hex("B8433D"), Hex("2F7C6E"), Hex("6B3F5E"), Hex("C9573F") };
        static readonly Color[] TowerGlass = { Hex("6FB3C9"), Hex("4C7FB0"), Hex("8DCBB5"), Hex("E5C9A0"), Hex("9DB7D8"), Hex("F0A98E") };

        /// <summary>Background tint for each district's sky; the starting neighborhood keeps its mint.</summary>
        public static Color DistrictSky(int district) => district == 1 ? Hex("F1D5B3") : district == 2 ? Hex("E8C2BE") : district == 3 ? Hex("B9CFE6") : Hex("A8D4C7");

        public static WorldData BuildDistrict(int district)
        {
            world = new GameObject("Courier district " + district).transform;
            Color accent = district == 1 ? Hex("D79C67") : district == 2 ? Hex("BE5755") : Hex("78AABA");
            Box("District base", new Vector3(0, -.57f, 0), new Vector3(42, 1.10f, 42), Hex("AC9A8E"), .45f);
            Box("District ground", new Vector3(0, -.065f, 0), new Vector3(42, .15f, 42),
                district == 3 ? Hex("CBD7D8") : district == 2 ? Hex("D5D6AF") : Hex("E5CAA4"), .3f);
            Color asphalt = district == 3 ? Hex("87959F") : district == 2 ? Hex("AF9995") : Hex("BAAA94");
            for (int axis = -16; axis <= 16; axis += 8)
            {
                RoadPiece(new Vector3(axis, 0, 0), 39, true, asphalt, Cream);
                RoadPiece(new Vector3(0, 0, axis), 39, false, asphalt, Cream);
                for (int mark = -18; mark <= 18; mark += 3)
                {
                    if (Mathf.Abs(mark % 8) <= 1) continue;
                    Box("District street stripe", new Vector3(axis, .056f, mark), new Vector3(.075f, .012f, .63f), Cream, .018f);
                    Box("District street stripe", new Vector3(mark, .058f, axis), new Vector3(.63f, .012f, .075f), Cream, .018f);
                }
            }

            for (int row = 0; row < 4; row++)
                for (int col = 0; col < 4; col++)
                {
                    Vector3 p = new Vector3(-12 + col * 8, 0, -12 + row * 8);
                    int variant = row * 4 + col;
                    if (district == 3)
                    {
                        if (variant == 5 || variant == 10) PlazaSquare(p, variant);
                        else PlazaBuilding(p, variant, accent);
                    }
                    else if (district == 2)
                    {
                        if (variant == 6) Pagoda(p);
                        else if (variant == 9) KoiGarden(p);
                        else MarketBuilding(p, variant, true, accent);
                    }
                    else MarketBuilding(p, variant, false, accent);
                    BlockCorners(p, district, variant);
                }
            DistrictEdges(district);
            StreetDecorations(district);

            if (district == 2)
            {
                Transform arch = Group("Red and gold courtyard gate", new Vector3(0, 0, -16.6f));
                for (int side = -1; side <= 1; side += 2)
                    Box("Ceremonial gate pillar", new Vector3(side * 2.4f, 1.85f, 0), new Vector3(.31f, 3.7f, .38f), accent, .06f, arch);
                Box("Gate gold beam", new Vector3(0, 3.81f, 0), new Vector3(5.2f, .23f, .56f), Hex("F5C671"), .07f, arch);
                Box("Gate layered roof", new Vector3(0, 4.1f, 0), new Vector3(5.45f, .24f, 1.03f), Hex("744B56"), .10f, arch);
                for (int side = -1; side <= 1; side += 2)
                    Box("Gate upturned eave", new Vector3(side * 2.85f, 4.28f, 0), new Vector3(.38f, .18f, .8f), Hex("F5C671"), .06f, arch);
                Transform garden = Group("Bamboo courtyard planting", new Vector3(-12.1f, 0, 10));
                for (int i = 0; i < 8; i++)
                {
                    float x = (i % 4 - 1.5f) * .42f, z = (i / 4 - .5f) * .40f;
                    float height = 1.15f + i % 3 * .24f;
                    ToyGeometry.Cylinder(garden, "Bamboo stem", new Vector3(x, height / 2, z), new Vector3(.085f, height, .085f), Leaf);
                    Sphere("Bamboo leaves", new Vector3(x + .11f, height, z), new Vector3(.44f, .19f, .19f), DarkLeaf, garden);
                }
            }
            if (district == 3)
            {
                Transform bay = Group("Logistics loading dock", new Vector3(11.7f, 0, -18.1f));
                Box("Dock landing", new Vector3(0, .10f, 0), new Vector3(3.5f, .20f, 1.65f), Hex("B8C1C7"), .05f, bay);
                for (int stripe = 0; stripe < 7; stripe++)
                    Box("Yellow parking line", new Vector3(-1.4f + .45f * stripe, .22f, 0), new Vector3(.17f, .016f, 1.5f), Hex("F0BC5A"), .013f, bay);
            }

            var batch = new System.Collections.Generic.List<GameObject>();
            foreach (MeshFilter filter in world.GetComponentsInChildren<MeshFilter>())
                if (filter.sharedMesh && filter.sharedMesh.isReadable && filter.GetComponent<MeshRenderer>()) batch.Add(filter.gameObject);
            if (batch.Count > 1) StaticBatchingUtility.Combine(batch.ToArray(), world.gameObject);

            Vector3 depot = district == 1 ? new Vector3(-16, 0, -8) : district == 2 ? new Vector3(0, 0, -16) : new Vector3(16, 0, -16);
            return new WorldData
            {
                Root = world, Depot = depot, PlayBounds = PlayBounds,
                // People live outside the static batch so they can walk.
                Life = DistrictLife.Create(district),
                Stops = new[] { new Vector3(-8, 0, 5 - district), new Vector3(8, 0, 4 + district),
                    new Vector3(8, 0, -6 - district), new Vector3(-8, 0, 16), new Vector3(16, 0, 8), new Vector3(8, 0, -16) },
                Pickups = new[] { depot, new Vector3(-16, 0, 4), new Vector3(0, 0, -8), new Vector3(-8, 0, -4),
                    new Vector3(0, 0, 12), new Vector3(16, 0, -8), new Vector3(-16, 0, 16), new Vector3(8, 0, 12) },
                StopNames = DistrictStopsTr[district], StopNamesEn = DistrictStopsEn[district],
                StopColors = new[] { accent, Hex("B7A7DC"), Hex("8DD2BA"), Hex("DBAD91"), Hex("88B7CD"), Hex("B8CCD0") }
            };
        }

        // Block buildings keep the original 3.5 x 2.7 collider so every road,
        // stop and pickup stays exactly as reachable as before.
        static void MarketBuilding(Vector3 p, int variant, bool east, Color accent)
        {
            if (variant % 5 == 0)
            {
                Transform square = Group(east ? "Lantern courtyard" : "Open market stalls", p);
                Box("Cream courtyard tiles", new Vector3(0, .04f, 0), new Vector3(4.35f, .07f, 4.25f), east ? Hex("EFD9C0") : Hex("F5DEB1"), .13f, square);
                for (int side = -1; side <= 1; side += 2)
                {
                    Color canopy = east ? EastRoofs[(variant + side + 2) % EastRoofs.Length] : MarketAwnings[(variant + side + 2) % MarketAwnings.Length];
                    Box("Terracotta counter", new Vector3(side * 1.02f, .40f, .16f), new Vector3(1.02f, .72f, 1.44f), Hex("DA8659"), .09f, square);
                    for (int s = 0; s < 4; s++)
                        Box("Striped stall canopy", new Vector3(side * 1.02f - .47f + s * .31f, 1.63f, .16f), new Vector3(.32f, .12f, 1.64f),
                            s % 2 == 0 ? canopy : Cream, .04f, square);
                    for (int end = -1; end <= 1; end += 2)
                        Box("Stall upright", new Vector3(side * 1.02f, 1.13f, .16f + end * .63f), new Vector3(.07f, .92f, .07f), Wood, .017f, square);
                    Produce(square, new Vector3(side * 1.02f, .78f, -.05f), variant + side);
                }
                Umbrella(square, new Vector3(0, 0, 1.75f), east ? Hex("C4524D") : MarketAwnings[variant % MarketAwnings.Length]);
                return;
            }
            Transform shop = Group(east ? "East market shop" : "Covered market shop", p, variant % 2 == 0 ? 90 : -90);
            bool tall = variant % 3 == 1;
            float wallH = tall ? 3.25f : 2.14f;
            Color walls = east ? EastWalls[variant % EastWalls.Length] : MarketWalls[variant % MarketWalls.Length];
            Box("Market shop walls", new Vector3(0, wallH / 2 + .01f, 0), new Vector3(3.5f, wallH, 2.7f), walls, .12f, shop, true);
            Box("Shop glass", new Vector3(0, .91f, -1.37f), new Vector3(2.65f, 1.25f, .07f), Window, .06f, shop);
            Box("Shop base", new Vector3(0, .31f, -1.55f), new Vector3(3.2f, .24f, .49f), Wood, .06f, shop);
            if (tall)
            {
                // Upper floor: two windows with shutters, a balcony and a flower box.
                for (int side = -1; side <= 1; side += 2)
                {
                    Box("Upper window", new Vector3(side * .85f, 2.62f, -1.37f), new Vector3(.66f, .7f, .07f), Window, .05f, shop);
                    Box("Window shutter", new Vector3(side * .85f - .43f, 2.62f, -1.39f), new Vector3(.16f, .74f, .05f), east ? EastRoofs[1] : MarketRoofs[(variant + 1) % MarketRoofs.Length], .03f, shop);
                    Box("Window shutter", new Vector3(side * .85f + .43f, 2.62f, -1.39f), new Vector3(.16f, .74f, .05f), east ? EastRoofs[1] : MarketRoofs[(variant + 1) % MarketRoofs.Length], .03f, shop);
                    Box("Flower box", new Vector3(side * .85f, 2.2f, -1.5f), new Vector3(.72f, .16f, .2f), Wood, .03f, shop);
                    for (int f = 0; f < 3; f++)
                        Sphere("Window flowers", new Vector3(side * .85f - .22f + f * .22f, 2.33f, -1.52f), new Vector3(.18f, .14f, .14f),
                            f % 2 == 0 ? Hex("EE7F8D") : Hex("F6CF5C"), shop);
                }
            }
            float roofY = wallH + .1f;
            if (east)
            {
                Color roof = EastRoofs[variant % EastRoofs.Length];
                for (int tier = 0; tier < 3; tier++)
                    Box("Layered tiled eave", new Vector3(0, roofY + tier * .16f, 0), new Vector3(4.15f - tier * .51f, .19f, 3.26f - tier * .45f), roof, .075f, shop);
                Box("Gold ridge", new Vector3(0, roofY + .5f, 0), new Vector3(2.6f, .1f, .14f), Hex("F5C671"), .03f, shop);
                for (int cx = -1; cx <= 1; cx += 2)
                    for (int cz = -1; cz <= 1; cz += 2)
                        Box("Upturned eave corner", new Vector3(cx * 1.98f, roofY + .16f, cz * 1.55f), new Vector3(.3f, .14f, .3f), Hex("F5C671"), .05f, shop);
                Box("Red shop sign", new Vector3(1.48f, 1.25f, -1.45f), new Vector3(.34f, 1.25f, .07f), Hex("C0453A"), .04f, shop);
                Box("Sign gold edge", new Vector3(1.48f, 1.25f, -1.49f), new Vector3(.22f, 1.05f, .03f), Hex("F5C671"), .02f, shop);
                for (int side = -1; side <= 1; side += 2)
                {
                    Sphere("Round red lantern", new Vector3(side * 1.15f, 1.75f, -1.63f), new Vector3(.34f, .38f, .34f), accent, shop);
                    Box("Lantern tassel", new Vector3(side * 1.15f, 1.50f, -1.63f), new Vector3(.07f, .16f, .07f), Hex("EFC475"), .018f, shop);
                }
            }
            else
            {
                Color roof = MarketRoofs[variant % MarketRoofs.Length];
                Box("Market roof", new Vector3(0, roofY + .02f, 0), new Vector3(3.82f, .23f, 2.98f), roof, .11f, shop);
                Box("Roof cornice", new Vector3(0, roofY - .12f, -1.42f), new Vector3(3.7f, .12f, .16f), Cream, .03f, shop);
                Color awning = MarketAwnings[variant % MarketAwnings.Length];
                for (int stripe = 0; stripe < 8; stripe++)
                    Box("Striped awning", new Vector3(-1.45f + .414f * stripe, 1.64f, -1.68f), new Vector3(.42f, .11f, .83f), stripe % 2 == 0 ? Cream : awning, .032f, shop);
                Produce(shop, new Vector3(-.8f, .52f, -1.75f), variant);
                Produce(shop, new Vector3(.8f, .52f, -1.75f), variant + 3);
                // A woven rug hangs on the side wall.
                Box("Hanging rug", new Vector3(1.77f, 1.05f, 0), new Vector3(.05f, 1.2f, .9f), MarketAwnings[(variant + 2) % MarketAwnings.Length], .02f, shop);
                Box("Rug pattern", new Vector3(1.8f, 1.05f, 0), new Vector3(.03f, .3f, .7f), Cream, .01f, shop);
            }
        }

        // Crate with a little pyramid of fruit or spice sacks.
        static void Produce(Transform parent, Vector3 p, int variant)
        {
            Color[] goods = { Hex("F09A3E"), Hex("E2574C"), Hex("9BC56A"), Hex("F4D35E"), Hex("B65A3A") };
            Color c = goods[Mathf.Abs(variant) % goods.Length];
            Box("Produce crate", p, new Vector3(.7f, .2f, .42f), Wood, .03f, parent);
            for (int i = 0; i < 3; i++)
                Sphere("Produce", p + new Vector3(-.2f + i * .2f, .16f, 0), new Vector3(.19f, .17f, .19f), c, parent);
            Sphere("Produce", p + new Vector3(-.1f, .3f, 0), new Vector3(.18f, .16f, .18f), c, parent);
            Sphere("Produce", p + new Vector3(.1f, .3f, 0), new Vector3(.18f, .16f, .18f), c, parent);
        }

        static void Umbrella(Transform parent, Vector3 p, Color color)
        {
            Box("Umbrella pole", p + new Vector3(0, .95f, 0), new Vector3(.06f, 1.9f, .06f), Cream, .02f, parent);
            Sphere("Umbrella canopy", p + new Vector3(0, 1.95f, 0), new Vector3(1.5f, .32f, 1.5f), color, parent);
            Sphere("Umbrella tip", p + new Vector3(0, 2.12f, 0), new Vector3(.14f, .14f, .14f), Cream, parent);
        }

        static void Pagoda(Vector3 p)
        {
            Transform tower = Group("Five-tier pagoda", p);
            Box("Pagoda stone terrace", new Vector3(0, .12f, 0), new Vector3(4.2f, .24f, 4.0f), Hex("E8D8BE"), .1f, tower);
            Box("Pagoda base", new Vector3(0, .9f, 0), new Vector3(2.7f, 1.6f, 2.7f), Hex("F4E3C9"), .1f, tower, true);
            float y = 1.75f;
            for (int tier = 0; tier < 5; tier++)
            {
                float w = 3.4f - tier * .45f;
                Box("Pagoda eave", new Vector3(0, y, 0), new Vector3(w, .2f, w), tier % 2 == 0 ? Hex("B8433D") : Hex("2F7C6E"), .08f, tower);
                for (int cx = -1; cx <= 1; cx += 2)
                    for (int cz = -1; cz <= 1; cz += 2)
                        Box("Pagoda eave tip", new Vector3(cx * w * .5f, y + .12f, cz * w * .5f), new Vector3(.24f, .12f, .24f), Hex("F5C671"), .04f, tower);
                if (tier < 4) Box("Pagoda storey", new Vector3(0, y + .4f, 0), new Vector3(w * .6f, .6f, w * .6f), Hex("F4E3C9"), .06f, tower);
                y += .82f;
            }
            Box("Pagoda spire", new Vector3(0, y + .1f, 0), new Vector3(.12f, .9f, .12f), Hex("F5C671"), .03f, tower);
            Sphere("Spire pearl", new Vector3(0, y + .6f, 0), new Vector3(.24f, .24f, .24f), Hex("F5C671"), tower);
        }

        static void KoiGarden(Vector3 p)
        {
            Transform garden = Group("Koi pond garden", p);
            Box("Garden stones", new Vector3(0, .04f, 0), new Vector3(4.3f, .07f, 4.2f), Hex("D9D2BC"), .12f, garden);
            Sphere("Koi pond", new Vector3(0, .07f, 0), new Vector3(3.1f, .04f, 2.4f), Hex("6FB9C9"), garden);
            Color[] koi = { Hex("F07A3A"), Hex("FFFFFF"), Hex("F2B13A") };
            for (int i = 0; i < 5; i++)
                Sphere("Koi", new Vector3(-1 + i * .5f, .1f, (i % 2 == 0 ? .4f : -.3f)), new Vector3(.32f, .05f, .12f), koi[i % 3], garden);
            // Little arched bridge across the pond.
            for (int s = 0; s < 5; s++)
            {
                float t = s / 4f;
                Box("Bridge plank", new Vector3(-.9f + t * 1.8f, .18f + Mathf.Sin(t * Mathf.PI) * .38f, 0), new Vector3(.42f, .08f, .7f), Hex("B8433D"), .03f, garden);
            }
            for (int i = 0; i < 4; i++)
                Sphere("Garden shrub", new Vector3(i < 2 ? -1.75f : 1.75f, .3f, i % 2 == 0 ? -1.6f : 1.6f), new Vector3(.7f, .55f, .7f), i % 2 == 0 ? Leaf : DarkLeaf, garden);
            Box("Stone lantern post", new Vector3(1.6f, .4f, 0), new Vector3(.22f, .8f, .22f), Hex("BDB6A3"), .04f, garden);
            Box("Stone lantern top", new Vector3(1.6f, .88f, 0), new Vector3(.42f, .16f, .42f), Hex("BDB6A3"), .05f, garden);
        }

        static void PlazaBuilding(Vector3 p, int variant, Color accent)
        {
            Transform tower = Group("Glass office building", p, variant % 2 == 0 ? 90 : -90);
            float height = 3.4f + variant % 4 * 1.25f + (variant % 7 == 3 ? 1.6f : 0);
            Color glass = TowerGlass[variant % TowerGlass.Length];
            Box("Office tower body", new Vector3(0, height / 2, 0), new Vector3(3.5f, height, 2.85f), glass, .15f, tower, true);
            Box("Office canopy", new Vector3(0, 1.34f, -1.7f), new Vector3(3.25f, .19f, .86f), Cream, .06f, tower);
            Box("Office lobby glass", new Vector3(0, .73f, -1.45f), new Vector3(1.34f, 1.43f, .08f), Hex("4F788D"), .05f, tower);
            Color dark = Color.Lerp(glass, Hex("2E4E63"), .55f), light = Color.Lerp(glass, Color.white, .55f);
            for (int floor = 1; floor < (int)height; floor++)
                for (int pane = -1; pane <= 1; pane++)
                    Box("Office window", new Vector3(pane * .98f, .61f + floor, -1.46f), new Vector3(.78f, .65f, .07f),
                        (floor + pane + variant) % 3 == 0 ? light : dark, .07f, tower);
            for (int floor = 1; floor < (int)height; floor++)
                Box("Side window band", new Vector3(1.77f, .61f + floor, 0), new Vector3(.06f, .5f, 2.3f), dark, .03f, tower);
            Box("Tower roof line", new Vector3(0, height + .09f, 0), new Vector3(3.68f, .18f, 3.04f), Cream, .07f, tower);
            switch (variant % 4)
            {
                case 0:
                    Sphere("Rooftop helipad", new Vector3(0, height + .2f, 0), new Vector3(2.2f, .05f, 2.2f), Hex("5B6770"), tower);
                    Box("Helipad H", new Vector3(0, height + .24f, 0), new Vector3(.12f, .02f, .9f), Cream, .01f, tower);
                    Box("Helipad H", new Vector3(-.3f, height + .24f, 0), new Vector3(.12f, .02f, .9f), Cream, .01f, tower);
                    Box("Helipad H", new Vector3(.3f, height + .24f, 0), new Vector3(.12f, .02f, .9f), Cream, .01f, tower);
                    Box("Helipad H bar", new Vector3(0, height + .24f, 0), new Vector3(.6f, .02f, .12f), Cream, .01f, tower);
                    break;
                case 1:
                    for (int g = 0; g < 4; g++)
                        Sphere("Roof garden tree", new Vector3(-1 + g * .66f, height + .5f, g % 2 == 0 ? -.5f : .5f), new Vector3(.62f, .62f, .62f), g % 2 == 0 ? Leaf : DarkLeaf, tower);
                    break;
                case 2:
                    Box("Antenna mast", new Vector3(.8f, height + 1.0f, .5f), new Vector3(.08f, 1.8f, .08f), Hex("D9DEE2"), .02f, tower);
                    Sphere("Antenna light", new Vector3(.8f, height + 1.95f, .5f), new Vector3(.16f, .16f, .16f), Hex("F0645A"), tower);
                    break;
                default:
                    Box("Stepped crown", new Vector3(0, height + .55f, 0), new Vector3(2.4f, .9f, 2.0f), light, .1f, tower);
                    Box("Crown cap", new Vector3(0, height + 1.1f, 0), new Vector3(1.4f, .3f, 1.2f), Cream, .06f, tower);
                    break;
            }
            // A bright billboard on the street side.
            if (variant % 3 == 2)
            {
                Box("Billboard frame", new Vector3(0, 2.3f, -1.5f), new Vector3(2.2f, .9f, .08f), Hex("2E4E63"), .04f, tower);
                Box("Billboard art", new Vector3(-.45f, 2.3f, -1.55f), new Vector3(1.1f, .7f, .03f), Hex("F3BF48"), .02f, tower);
                Box("Billboard art", new Vector3(.55f, 2.3f, -1.55f), new Vector3(.9f, .7f, .03f), Hex("E97760"), .02f, tower);
            }
        }

        static void PlazaSquare(Vector3 p, int variant)
        {
            Transform square = Group("Plaza square", p);
            Box("Plaza paving", new Vector3(0, .04f, 0), new Vector3(4.35f, .07f, 4.25f), Hex("E9EEF0"), .12f, square);
            Box("Paving inlay", new Vector3(0, .08f, 0), new Vector3(3.2f, .02f, 3.2f), Hex("C9D6DD"), .1f, square);
            ToyGeometry.Cylinder(square, "Fountain basin", new Vector3(0, .25f, 0), new Vector3(1.9f, .36f, 1.9f), Hex("BFD2DA"));
            ToyGeometry.Cylinder(square, "Fountain water", new Vector3(0, .43f, 0), new Vector3(1.6f, .04f, 1.6f), Hex("7CC3D8"));
            ToyGeometry.Cylinder(square, "Fountain column", new Vector3(0, .8f, 0), new Vector3(.3f, .9f, .3f), Hex("E3EAEE"));
            Sphere("Fountain spray", new Vector3(0, 1.35f, 0), new Vector3(.7f, .5f, .7f), Hex("CDEAF2"), square);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI / 2 + Mathf.PI / 4;
                Vector3 at = new Vector3(Mathf.Cos(a) * 1.6f, 0, Mathf.Sin(a) * 1.6f);
                Box("Planter", at + new Vector3(0, .25f, 0), new Vector3(.6f, .5f, .6f), Hex("D0D8DC"), .06f, square);
                Sphere("Planter tree", at + new Vector3(0, .95f, 0), new Vector3(.8f, .9f, .8f), variant % 2 == 0 ? Leaf : Hex("7DC08E"), square);
            }
        }

        // Non-solid corner dressing: lamps and flower pots.
        static void BlockCorners(Vector3 p, int district, int variant)
        {
            Color lamp = district == 2 ? Hex("C0453A") : district == 3 ? Hex("5E6E78") : Hex("8A6A4E");
            for (int c = 0; c < 4; c++)
            {
                float sx = c % 2 == 0 ? -1 : 1, sz = c < 2 ? -1 : 1;
                Vector3 corner = p + new Vector3(sx * 2.0f, 0, sz * 2.0f);
                if ((c + variant) % 2 == 0)
                {
                    Box("District lamp pole", corner + new Vector3(0, 1.1f, 0), new Vector3(.09f, 2.2f, .09f), lamp, .03f);
                    if (district == 2) Sphere("Hanging red lantern", corner + new Vector3(0, 2.3f, 0), new Vector3(.36f, .42f, .36f), Hex("E2473B"));
                    else Box("District lamp head", corner + new Vector3(0, 2.3f, 0), new Vector3(.3f, .36f, .3f), Hex("FFF0AD"), .06f);
                }
                else Pot(corner, null);
            }
        }

        // The outer rim gets its own row of small buildings instead of bare ground.
        static void DistrictEdges(int district)
        {
            int n = 0;
            for (int side = 0; side < 4; side++)
                for (int k = -12; k <= 12; k += 8)
                {
                    n++;
                    Vector3 p = side == 0 ? new Vector3(k, 0, 19.3f) : side == 1 ? new Vector3(k, 0, -19.3f) : side == 2 ? new Vector3(19.3f, 0, k) : new Vector3(-19.3f, 0, k);
                    float yaw = side == 0 ? 0 : side == 1 ? 180 : side == 2 ? 90 : -90;
                    Transform b = Group("District rim building", p, yaw);
                    // The camera looks in from the south-east: rims on that side stay low
                    // so they never hide the courier.
                    bool nearCamera = side == 1 || side == 2;
                    float h = nearCamera ? .9f + n % 2 * .25f : district == 3 ? 2.6f + n % 3 * 1.1f : 1.6f + n % 2 * .7f;
                    Color wall = district == 3 ? TowerGlass[n % TowerGlass.Length] : district == 2 ? EastWalls[n % EastWalls.Length] : MarketWalls[n % MarketWalls.Length];
                    Box("Rim walls", new Vector3(0, h / 2, 0), new Vector3(3.0f, h, 1.3f), wall, .1f, b, true);
                    Box("Rim window", new Vector3(-.6f, h * .6f, -.66f), new Vector3(.6f, .5f, .05f), Window, .04f, b);
                    Box("Rim window", new Vector3(.6f, h * .6f, -.66f), new Vector3(.6f, .5f, .05f), Window, .04f, b);
                    Color roof = district == 2 ? EastRoofs[n % EastRoofs.Length] : district == 3 ? Cream : MarketRoofs[n % MarketRoofs.Length];
                    Box("Rim roof", new Vector3(0, h + .1f, 0), new Vector3(3.3f, .2f, 1.6f), roof, .06f, b);
                }
        }

        // Bunting (market), lantern strings (east) or banners (plaza) above the streets.
        static void StreetDecorations(int district)
        {
            for (int axis = -8; axis <= 8; axis += 8)
                for (int k = -12; k <= 12; k += 8)
                {
                    if ((axis + k) % 16 == 0) continue;
                    bool along = (k / 8 + axis / 8) % 2 == 0;
                    Vector3 c = along ? new Vector3(axis, 0, k) : new Vector3(k, 0, axis);
                    Vector3 across = along ? Vector3.right : Vector3.forward;
                    if (district == 3)
                    {
                        Box("Street banner", c + across * 1.95f + Vector3.up * 1.9f, new Vector3(.12f, .9f, .12f) + (along ? new Vector3(0, 0, .5f) : new Vector3(.5f, 0, 0)),
                            TowerGlass[(axis + k + 40) / 8 % TowerGlass.Length], .03f);
                        continue;
                    }
                    for (int i = 0; i <= 8; i++)
                    {
                        float t = i / 8f;
                        Vector3 at = c + across * Mathf.Lerp(-2.05f, 2.05f, t) + Vector3.up * (3.1f - Mathf.Sin(t * Mathf.PI) * .35f);
                        if (district == 2)
                            Sphere("String lantern", at, new Vector3(.24f, .28f, .24f), i % 3 == 1 ? Hex("F5C671") : Hex("E2473B"));
                        else
                            Box("Bunting flag", at + Vector3.down * .1f, new Vector3(.2f, .24f, .2f), MarketAwnings[i % MarketAwnings.Length], .02f);
                    }
                }
        }
    }
}
