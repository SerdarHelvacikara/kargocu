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
                    if (district == 3) PlazaBuilding(p, variant, accent);
                    else MarketBuilding(p, variant, district == 2, accent);
                }

            if (district == 2)
            {
                Transform arch = Group("Red and gold courtyard gate", new Vector3(0, 0, -16.6f));
                for (int side = -1; side <= 1; side += 2)
                    Box("Ceremonial gate pillar", new Vector3(side * 2.4f, 1.85f, 0), new Vector3(.31f, 3.7f, .38f), accent, .06f, arch);
                Box("Gate gold beam", new Vector3(0, 3.81f, 0), new Vector3(5.2f, .23f, .56f), Hex("F5C671"), .07f, arch);
                Box("Gate layered roof", new Vector3(0, 4.1f, 0), new Vector3(5.45f, .24f, 1.03f), Hex("744B56"), .10f, arch);
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
                Stops = new[] { new Vector3(-8, 0, 5 - district), new Vector3(8, 0, 4 + district),
                    new Vector3(8, 0, -6 - district), new Vector3(-8, 0, 16), new Vector3(16, 0, 8), new Vector3(8, 0, -16) },
                Pickups = new[] { depot, new Vector3(-16, 0, 4), new Vector3(0, 0, -8), new Vector3(-8, 0, -4),
                    new Vector3(0, 0, 12), new Vector3(16, 0, -8), new Vector3(-16, 0, 16), new Vector3(8, 0, 12) },
                StopNames = DistrictStopsTr[district], StopNamesEn = DistrictStopsEn[district],
                StopColors = new[] { accent, Hex("B7A7DC"), Hex("8DD2BA"), Hex("DBAD91"), Hex("88B7CD"), Hex("B8CCD0") }
            };
        }

        static void MarketBuilding(Vector3 p, int variant, bool east, Color accent)
        {
            if (variant % 5 == 0)
            {
                Transform square = Group(east ? "Lantern courtyard" : "Open market stalls", p);
                Box("Cream courtyard tiles", new Vector3(0, .04f, 0), new Vector3(4.35f, .07f, 4.25f), Hex("F5DEB1"), .13f, square);
                for (int side = -1; side <= 1; side += 2)
                {
                    Box("Terracotta counter", new Vector3(side * 1.02f, .40f, .16f), new Vector3(1.02f, .72f, 1.44f), Hex("DA8659"), .09f, square);
                    Box("Warm stall canopy", new Vector3(side * 1.02f, 1.63f, .16f), new Vector3(1.25f, .12f, 1.64f),
                        east ? Hex("C4524D") : Hex("EEA55F"), .07f, square);
                    for (int end = -1; end <= 1; end += 2)
                        Box("Stall upright", new Vector3(side * 1.02f, 1.13f, .16f + end * .63f), new Vector3(.07f, .92f, .07f), Wood, .017f, square);
                    Sphere("Golden market display", new Vector3(side * 1.02f, .88f, -.15f), new Vector3(.61f, .22f, .38f), Hex("F4C365"), square);
                }
                return;
            }
            Transform shop = Group(east ? "East market shop" : "Covered market shop", p, variant % 2 == 0 ? 90 : -90);
            Color walls = east ? (variant % 3 == 0 ? Hex("F1C39D") : Hex("F5DCAD")) :
                variant % 2 == 0 ? Hex("F1CE97") : Hex("ECB092");
            Box("Market shop walls", new Vector3(0, 1.08f, 0), new Vector3(3.5f, 2.14f, 2.7f), walls, .12f, shop, true);
            Box("Shop glass", new Vector3(0, .91f, -1.37f), new Vector3(2.65f, 1.25f, .07f), Window, .06f, shop);
            Box("Shop base", new Vector3(0, .31f, -1.55f), new Vector3(3.2f, .24f, .49f), Wood, .06f, shop);
            if (east)
            {
                Color roof = variant % 3 == 0 ? Hex("784D62") : Hex("A54E55");
                for (int tier = 0; tier < 3; tier++)
                    Box("Layered tiled eave", new Vector3(0, 2.22f + tier * .16f, 0), new Vector3(4.15f - tier * .51f, .19f, 3.26f - tier * .45f), roof, .075f, shop);
                for (int side = -1; side <= 1; side += 2)
                {
                    Sphere("Round red lantern", new Vector3(side * 1.15f, 1.75f, -1.63f), new Vector3(.34f, .38f, .34f), accent, shop);
                    Box("Lantern tassel", new Vector3(side * 1.15f, 1.50f, -1.63f), new Vector3(.07f, .16f, .07f), Hex("EFC475"), .018f, shop);
                }
            }
            else
            {
                Box("Market roof", new Vector3(0, 2.24f, 0), new Vector3(3.82f, .23f, 2.98f), accent, .11f, shop);
                for (int stripe = 0; stripe < 8; stripe++)
                {
                    Color color = stripe % 2 == 0 ? Cream : variant % 3 == 0 ? Leaf : accent;
                    Box("Striped awning", new Vector3(-1.45f + .414f * stripe, 1.64f, -1.68f), new Vector3(.42f, .11f, .83f), color, .032f, shop);
                    Sphere("Spice display", new Vector3(-1.23f + .35f * stripe, .55f, -1.63f), new Vector3(.24f, .18f, .25f), color, shop);
                }
            }
        }

        static void PlazaBuilding(Vector3 p, int variant, Color accent)
        {
            Transform tower = Group("Glass office building", p, variant % 2 == 0 ? 90 : -90);
            float height = 3.4f + variant % 4 * 1.15f;
            Box("Office tower body", new Vector3(0, height / 2, 0), new Vector3(3.5f, height, 2.85f), accent, .15f, tower, true);
            Box("Office canopy", new Vector3(0, 1.34f, -1.7f), new Vector3(3.25f, .19f, .86f), Cream, .06f, tower);
            Box("Office lobby glass", new Vector3(0, .73f, -1.45f), new Vector3(1.34f, 1.43f, .08f), Hex("4F788D"), .05f, tower);
            for (int floor = 0; floor < (int)height; floor++)
                for (int pane = -1; pane <= 1; pane++)
                    Box("Office window", new Vector3(pane * .98f, .61f + floor, -1.46f), new Vector3(.78f, .65f, .07f),
                        (floor + pane + variant) % 3 == 0 ? Hex("C1DEDD") : Hex("527C90"), .07f, tower);
            Box("Tower roof line", new Vector3(0, height + .09f, 0), new Vector3(3.68f, .18f, 3.04f), Cream, .07f, tower);
        }
    }
}
