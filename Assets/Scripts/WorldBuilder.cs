using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KurmaliKargo
{
    public sealed class WorldData
    {
        public Vector3 Depot;
        public Vector3[] Stops;
        public string[] StopNames;
        public string[] StopNamesEn;
        public Color[] StopColors;
        public Bounds PlayBounds;
        public CityLandmarks City;
        public Transform Root;
        public Vector3[] Pickups;
    }

    // All geometry is authored here: no asset downloads, packages or runtime services.
    public static partial class WorldBuilder
    {
        public const float PlayableHalfExtent = 20f;
        public const int DestinationCount = 6;
        internal const float AlleyWallOffset = 1.55f;
        public static Bounds PlayBounds => new Bounds(Vector3.zero, new Vector3(PlayableHalfExtent * 2, 8, PlayableHalfExtent * 2));
        private static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();
        private static readonly Dictionary<Vector4, Mesh> BoxMeshes = new Dictionary<Vector4, Mesh>();
        private static Transform world;
        private static CityLandmarks city;
        private static readonly Color Cream = Hex("FFF0CC");
        private static readonly Color White = Hex("FFFDF0");
        private static readonly Color Grass = Hex("E5C59A");
        private static readonly Color Leaf = Hex("55A880");
        private static readonly Color DarkLeaf = Hex("368C72");
        private static readonly Color Wood = Hex("C58C64");
        private static readonly Color Window = Hex("568EAB");

        public static WorldData Build()
        {
            world = new GameObject("Little delivery neighborhood").transform;
            Box("Island soil", new Vector3(0, -0.65f, 0), new Vector3(42, 1.15f, 42), Hex("E3A779"), 0.55f);
            Box("Meadow", new Vector3(0, -0.09f, 0), new Vector3(42, 0.20f, 42), Grass, 0.35f);
            BuildRoads();
            // Animated city details have their own root, outside the neighborhood batch.
            city = CityLandmarks.Create(world);

            Color yellow = Hex("F7D179");
            Color lavender = Hex("B7A7DC");
            Color mint = Hex("8DD2BA");
            House("Limon Evi", new Vector3(-11.5f, 0, 7), -90, yellow, Hex("C27663"), false, 0);
            House("Lavanta Evi", new Vector3(11.5f, 0, 7), 90, lavender, Hex("7671AA"), false, 1);
            House("Nane Evi", new Vector3(11.5f, 0, -8), 90, mint, Hex("55928F"), false, 2);

            // Interior blocks leave wide, readable roads and alternative routes around each garden.
            Garden(new Vector3(-4, 0, 4), Hex("F2C69E"));
            Garden(new Vector3(4, 0, 4), Hex("C7D8A6"));
            Garden(new Vector3(-4, 0, -4), Hex("F0DAA9"));
            Garden(new Vector3(4, 0, -4), Hex("F1BD98"));
            House("Pembe Fırın", new Vector3(-4, 0, 3.6f), 0, Hex("ECA7A4"), Hex("B67875"), true, 0, 1);
            House("Yeşil Manav", new Vector3(4, 0, 4.4f), 180, Hex("A8CBB2"), Hex("668F7E"), true, 2, 2);
            House("Mahalle Berberi", new Vector3(-4, 0, -4.3f), 0, Hex("E9BC8E"), Hex("B67866"), false, 1, 3);
            House("Bahce Kulubesi", new Vector3(4, 0, -4.3f), 0, Hex("A7C9B3"), Hex("648C80"), false);
            Depot();

            // The former northwest tree cluster is now the open little football pitch.
            Tree(new Vector3(-4.5f, 0, 11.7f), 1.08f);
            Tree(new Vector3(-1.9f, 0, 12.3f), .7f);
            Tree(new Vector3(3.6f, 0, 11.9f), 1.05f);
            Tree(new Vector3(6.0f, 0, 12.1f), .72f);
            Tree(new Vector3(12.2f, 0, 11.4f), .94f);
            Tree(new Vector3(12.3f, 0, 1.6f), 1.1f);
            Tree(new Vector3(12.1f, 0, -3.8f), .85f);
            Tree(new Vector3(12.4f, 0, -12), 1.05f);
            Tree(new Vector3(6.0f, 0, -12.0f), .84f);
            Tree(new Vector3(2.8f, 0, -12.1f), 1.02f);
            Tree(new Vector3(-3.7f, 0, -12.0f), .83f);
            Tree(new Vector3(-12.0f, 0, -2.5f), .85f);
            Tree(new Vector3(-12.3f, 0, 1.3f), 1.0f);

            Bush(new Vector3(-5.8f, 0, 5.7f), .7f);
            Bush(new Vector3(-2.1f, 0, 5.7f), .66f);
            Bush(new Vector3(2.1f, 0, 2.2f), .72f);
            Bush(new Vector3(5.8f, 0, 6.2f), .8f);
            Bush(new Vector3(-5.7f, 0, -2.0f), .63f);
            Bush(new Vector3(5.8f, 0, -5.8f), .7f);
            FlowerBed(new Vector3(-2.1f, 0, 2.0f), Hex("F8CF70"));
            FlowerBed(new Vector3(5.8f, 0, 2.0f), Hex("EBA3B5"));
            FlowerBed(new Vector3(-2.1f, 0, -5.8f), Hex("EBA3B5"));
            FlowerBed(new Vector3(2.0f, 0, -2.0f), Hex("BDA4DF"));

            Fence(new Vector3(-4, 0, 6.05f), 3.5f, 0);
            Fence(new Vector3(5.95f, 0, 4.2f), 3.2f, 90);
            Fence(new Vector3(-6.0f, 0, -4.0f), 3.0f, 90);
            Fence(new Vector3(4.0f, 0, -6.05f), 3.5f, 0);
            Bench(new Vector3(3.65f, 0, 2.05f), 0);
            Bench(new Vector3(-4.1f, 0, 11.0f), 180);
            Lamp(new Vector3(-1.85f, 0, -1.85f));
            Lamp(new Vector3(1.85f, 0, 1.85f));
            Lamp(new Vector3(-9.8f, 0, 2.0f));
            Lamp(new Vector3(9.8f, 0, -2.0f));

            Crate("Hazard_Crate_West", new Vector3(-8.45f, 0, -1.9f), .7f, true);
            Crate("Hazard_Crate_Centre", new Vector3(.42f, 0, 4.1f), .67f, true);
            Crate("Hazard_Crate_South", new Vector3(4.0f, 0, -8.45f), .65f, true);
            Crate("Hazard_Crate_East", new Vector3(8.45f, 0, 2.1f), .65f, true);
            Puddle(new Vector3(-3.7f, 0, -.55f), 1.1f);
            Puddle(new Vector3(.45f, 0, -4.5f), .82f);

            // Tiny grass tufts keep open spaces soft without obscuring routes.
            Vector3[] tufts = {
                new Vector3(-12.5f,0,4.3f), new Vector3(-10.3f,0,9.3f), new Vector3(-6,0,10.9f),
                new Vector3(1.8f,0,11.6f), new Vector3(10.4f,0,10.6f), new Vector3(12.4f,0,-.8f),
                new Vector3(10.6f,0,-11), new Vector3(-1.8f,0,-12.4f), new Vector3(-12.5f,0,-4.6f)
            };
            for (int i = 0; i < tufts.Length; i++) Tuft(tufts[i]);
            ExpandedNeighborhood();

            // Only the never-moving neighborhood is combined. Vehicles, customers,
            // effects and the garage live under their own roots and keep animating.
            var batchable = new List<GameObject>();
            foreach (MeshFilter filter in world.GetComponentsInChildren<MeshFilter>())
                if (filter.sharedMesh && filter.sharedMesh.isReadable && filter.GetComponent<MeshRenderer>()) batchable.Add(filter.gameObject);
            if (batchable.Count > 1) StaticBatchingUtility.Combine(batchable.ToArray(), world.gameObject);

            return new WorldData
            {
                Depot = new Vector3(-8, 0, -8),
                Stops = new[] { new Vector3(-8, 0, 7), new Vector3(8, 0, 7), new Vector3(8, 0, -8),
                    new Vector3(-8, 0, 16), new Vector3(16, 0, 8), new Vector3(8, 0, -16) },
                StopNames = new[] { "Limon Evi", "Lavanta Evi", "Nane Evi", "Kiremit Apartmanı", "Saatçi Atölyesi", "Paket Fabrikası" },
                StopNamesEn = new[] { "Lemon House", "Lavender House", "Mint House", "Brick House", "Clockmaker's Workshop", "Parcel Factory" },
                StopColors = new[] { yellow, lavender, mint, Hex("DBAD91"), Hex("88B7CD"), Hex("B8CCD0") },
                PlayBounds = PlayBounds,
                Root = world,
                City = city
            };
        }

        private static void BuildRoads()
        {
            Color road = Hex("BDB0A7");
            Color edge = Hex("FFF0D0");
            for (int i = -2; i <= 2; i++)
            {
                float axis = i * 8;
                RoadPiece(new Vector3(axis, 0, 0), 38, true, road, edge);
                if (i == -1 || i == 1)
                {
                    // The old outer homes and depot remain plots, not new roads
                    // painted through their walls. Cross-town links use z=0/+-16.
                    RoadPiece(new Vector3(0, 0, axis), 19, false, road, edge);
                    RoadPiece(new Vector3(-16.5f, 0, axis), 5, false, road, edge);
                    RoadPiece(new Vector3(16.2f, 0, axis), 5.6f, false, road, edge);
                }
                else RoadPiece(new Vector3(0, 0, axis), 38, false, road, edge);
                for (int j = -19; j <= 19; j += 2)
                {
                    if (Mathf.Abs(j) < 2 || Mathf.Abs(j - 8) < 2 || Mathf.Abs(j + 8) < 2 || Mathf.Abs(j - 16) < 2 || Mathf.Abs(j + 16) < 2) continue;
                    Box("Painted dash", new Vector3(axis, .052f, j), new Vector3(.09f, .012f, .55f), Hex("F4EBD5"), .025f);
                    if (Mathf.Abs(i) != 1 || Mathf.Abs(j) <= 9 || Mathf.Abs(j) >= 14)
                        Box("Painted dash", new Vector3(j, .054f, axis), new Vector3(.55f, .012f, .09f), Hex("F4EBD5"), .025f);
                }
            }
            // Pedestrian stripes signal the friendly scale of the neighborhood.
            for (int i = 0; i < 4; i++)
                Box("Crosswalk", new Vector3(-1.5f + i * .25f, .057f, -8), new Vector3(.12f, .013f, 1.75f), Cream, .025f);
        }

        private static void RoadPiece(Vector3 p, float length, bool vertical, Color road, Color edge)
        {
            Vector3 edging = vertical ? new Vector3(3.56f, .06f, length) : new Vector3(length, .06f, 3.56f);
            Vector3 paving = vertical ? new Vector3(3.16f, .035f, length) : new Vector3(length, .035f, 3.16f);
            Box("Road edging", p + Vector3.down * .005f, edging, edge, .10f);
            Box("Smooth street", p + Vector3.up * .029f, paving, road, .06f);
        }

        private static void ExpandedNeighborhood()
        {
            House("Kiremit Apartmanı", new Vector3(-8, 0, 19.25f), 0, Hex("DBAD91"), Hex("B97764"), false);
            House("Saatçi Atölyesi", new Vector3(19.25f, 0, 8), 90, Hex("88B7CD"), Hex("6D91A6"), true);
            // Stop five's unchanged approach now serves the parcel factory built by CityLandmarks.
            House("Kuzey köşe evi", new Vector3(-16, 0, 19.2f), 0, Hex("DDB5BA"), Hex("A47D91"), false);
            House("Kuzey terzi evi", new Vector3(0, 0, 19.2f), 0, Hex("A9C6B9"), Hex("6F968A"), true);
            House("Kuzey mavi evi", new Vector3(16, 0, 19.2f), 0, Hex("B1BED5"), Hex("7E8DAD"), false);
            House("Doğu sakin ev", new Vector3(19.2f, 0, 0), 90, Hex("E3C496"), Hex("AF8B72"), false);
            House("Doğu bahçeli ev", new Vector3(19.2f, 0, -8), 90, Hex("BACCA7"), Hex("7E9E87"), false);
            House("Güney kitapçı", new Vector3(-8, 0, -19.2f), 180, Hex("D5ABA0"), Hex("A47C72"), true);
            House("Güney küçük ev", new Vector3(0, 0, -19.2f), 180, Hex("C8B4CF"), Hex("947DA7"), false);
            House("Batı mahalle evi", new Vector3(-19.2f, 0, 8), -90, Hex("E1B99E"), Hex("AF8976"), false);
            House("Batı limon evi", new Vector3(-19.2f, 0, -8), -90, Hex("D9CD9A"), Hex("9B9970"), false);

            BrickAlley(new Vector3(0, 0, 13.75f), false);
            BrickAlley(new Vector3(13.75f, 0, 0), true);
            DogShortcutSign(new Vector3(-6.6f, 0, 12.4f), 0);
            DogShortcutSign(new Vector3(12.4f, 0, 1.7f), -90);
            // The southwest plot is the town hall and its fountain plaza.
            FlowerBed(new Vector3(11.5f, 0, 15.0f), Hex("DFA6B9"));
            Lamp(new Vector3(-14.0f, 0, -14.0f));
            Lamp(new Vector3(14.0f, 0, 14.0f));
            Lamp(new Vector3(1.8f, 0, 17.8f));
            Vector3[] trees = {
                new Vector3(-19.3f,0,-3.6f), new Vector3(-19.4f,0,2.8f), new Vector3(-12.0f,0,19.4f),
                new Vector3(-3.6f,0,19.3f), new Vector3(4.0f,0,19.4f), new Vector3(12.0f,0,19.4f),
                new Vector3(19.62f,0,3.7f), new Vector3(19.62f,0,-3.5f), new Vector3(12.0f,0,-19.4f),
                new Vector3(4.1f,0,-19.4f), new Vector3(-4.0f,0,-19.3f), new Vector3(-12.0f,0,-19.4f)
            };
            for (int i = 0; i < trees.Length; i++) Tree(trees[i], i % 3 == 0 ? 1.05f : .85f);
        }

        private static void BrickAlley(Vector3 p, bool vertical)
        {
            Transform alley = Group(vertical ? "East brick shortcut" : "North brick shortcut", p, vertical ? 90 : 0);
            Box("Warm alley paving", new Vector3(0, .06f, 0), new Vector3(16.0f, .05f, 2.65f), Hex("C7977C"), .04f, alley);
            for (int i = -15; i <= 15; i++)
                Box("Alley paving seam", new Vector3(i * .5f, .091f, 0), new Vector3(.025f, .012f, 2.58f), Hex("E4C2A0"), .008f, alley);
            Box("Alley paving seam", new Vector3(0, .092f, 0), new Vector3(15.8f, .012f, .025f), Hex("E4C2A0"), .008f, alley);
            foreach (float centre in new[] { -4.25f, 4.25f })
                foreach (float side in new[] { -1f, 1f })
                    BrickWall(new Vector3(centre, 0, side * AlleyWallOffset), 3.3f, alley);
            // Wide gaps at the main street and both ends preserve an easy alternate route.
            foreach (float x in new[] { -6.5f, -1.6f, 1.6f, 6.5f })
                Box("Alley entrance tile", new Vector3(x, .099f, 0), new Vector3(.3f, .02f, .48f), Cream, .04f, alley);
        }

        private static void BrickWall(Vector3 p, float length, Transform parent)
        {
            Transform wall = Group("Soft terracotta alley wall", p, 0, parent);
            Box("Brick wall collision", new Vector3(0, .93f, 0), new Vector3(length, 1.86f, .22f), Hex("C88B73"), .07f, wall, true);
            Box("Brick wall stone cap", new Vector3(0, 1.91f, 0), new Vector3(length + .1f, .15f, .32f), Hex("E3C9A6"), .05f, wall);
            for (int row = 0; row < 4; row++)
                foreach (float side in new[] { -1f, 1f })
                {
                    float y = .27f + row * .40f;
                    Box("Brick mortar line", new Vector3(0, y, side * .115f), new Vector3(length - .1f, .025f, .016f), Hex("E3BE9D"), .005f, wall);
                    for (int column = 0; column < 5; column++)
                    {
                        float x = -1.9f + column * .82f + (row % 2) * .38f;
                        if (x > length / 2 - .12f) continue;
                        Box("Brick mortar join", new Vector3(x, y + .2f, side * .115f), new Vector3(.023f, .37f, .016f), Hex("E3BE9D"), .005f, wall);
                    }
                }
        }

        private static void DogShortcutSign(Vector3 p, float yaw)
        {
            Transform sign = Group("Playful dog shortcut sign", p, yaw);
            Box("Shortcut sign post", new Vector3(0, .62f, 0), new Vector3(.1f, 1.24f, .1f), Wood, .025f, sign);
            Box("Friendly warning sign", new Vector3(0, 1.35f, 0), new Vector3(.72f, .55f, .09f), Cream, .06f, sign);
            Sphere("Paw pad", new Vector3(0, 1.29f, -.065f), new Vector3(.25f, .18f, .025f), Hex("B68668"), sign);
            foreach (float x in new[] { -.18f, -.06f, .06f, .18f })
                Sphere("Paw toe", new Vector3(x, 1.46f - Mathf.Abs(x) * .25f, -.065f), new Vector3(.095f, .10f, .025f), Hex("B68668"), sign);
        }

        private static void Garden(Vector3 p, Color grass)
        {
            Box("Garden grass", p + new Vector3(0, .018f, 0), new Vector3(4.65f, .06f, 4.65f), grass, .25f);
        }

        private static void House(string name, Vector3 p, float yaw, Color walls, Color roofColor, bool awning, int variant = -1, int shop = 0)
        {
            Transform house = Group(name, p, yaw);
            if (variant < 0) variant = Mathf.RoundToInt(Mathf.Abs(p.x) * 2 + Mathf.Abs(p.z) * 3) % 3;
            Box("Foundation", new Vector3(0, .13f, 0), new Vector3(3.5f, .25f, 2.9f), Hex("DCCBAE"), .10f, house);
            Box("Soft plaster walls", new Vector3(0, 1.18f, 0), new Vector3(3.25f, 2.05f, 2.65f), walls, .11f, house, true);
            Box("Lower wall trim", new Vector3(0, .4f, 0), new Vector3(3.32f, .13f, 2.72f), Cream, .035f, house);
            Transform roof = Group("House roof assembly", new Vector3(0, variant == 1 ? 1.02f : 0, 0), 0, house);
            if (variant == 2)
            {
                Box("Flat terrace roof", new Vector3(0, 2.26f, 0), new Vector3(3.65f, .22f, 3.03f), roofColor, .10f, roof);
                Box("Terrace pale inset", new Vector3(0, 2.40f, 0), new Vector3(3.20f, .07f, 2.55f), Cream, .06f, roof);
                for (int side = -1; side <= 1; side += 2)
                {
                    Box("Terrace parapet", new Vector3(side * 1.69f, 2.54f, 0), new Vector3(.15f, .38f, 2.85f), Lighten(walls, .12f), .045f, roof);
                    Box("Terrace parapet", new Vector3(0, 2.54f, side * 1.37f), new Vector3(3.46f, .38f, .15f), Lighten(walls, .12f), .045f, roof);
                }
                Pot(new Vector3(-1.15f, 2.41f, .80f), roof);
                Pot(new Vector3(.97f, 2.41f, .86f), roof);
            }
            else
            {
                Roof(roof, roofColor);
                Box("Roof ridge", new Vector3(0, 3.13f, 0), new Vector3(.18f, .15f, 3.1f), Lighten(roofColor, .1f), .06f, roof);
                for (int i = 0; i < 7; i++)
                {
                    float z = -1.38f + i * .46f;
                    GameObject left = Box("Roof tile rib", new Vector3(-.91f, 2.65f, z), new Vector3(1.99f, .045f, .055f), Lighten(roofColor, .09f), .015f, roof);
                    left.transform.localRotation = Quaternion.Euler(0, 0, 29);
                    GameObject right = Box("Roof tile rib", new Vector3(.91f, 2.65f, z), new Vector3(1.99f, .045f, .055f), Lighten(roofColor, .09f), .015f, roof);
                    right.transform.localRotation = Quaternion.Euler(0, 0, -29);
                }
                Box("Chimney", new Vector3(.88f, 2.97f, .55f), new Vector3(.42f, .88f, .47f), Cream, .055f, roof);
                Box("Chimney cap", new Vector3(.88f, 3.43f, .55f), new Vector3(.56f, .15f, .59f), roofColor, .04f, roof);
            }
            if (variant == 1)
            {
                // Only the old ground-floor body is solid; decorative storeys retain its footprint.
                Box("Second floor plaster", new Vector3(0, 2.70f, 0), new Vector3(3.25f, 1.02f, 2.65f), Lighten(walls, .08f), .10f, house);
                Box("Second floor waist band", new Vector3(0, 2.20f, 0), new Vector3(3.33f, .12f, 2.73f), Cream, .025f, house);
                WindowFront(new Vector3(-1.02f, 2.73f, -1.36f), house);
                WindowFront(new Vector3(1.02f, 2.73f, -1.36f), house);
                WindowSide(new Vector3(1.64f, 2.73f, .15f), house);
                Box("Little balcony floor", new Vector3(0, 2.22f, -1.62f), new Vector3(1.43f, .12f, .60f), Cream, .05f, house);
                Box("Balcony handrail", new Vector3(0, 2.66f, -1.88f), new Vector3(1.42f, .07f, .07f), Wood, .02f, house);
                for (int i = 0; i < 6; i++)
                    Box("Balcony picket", new Vector3(-.65f + i * .26f, 2.44f, -1.88f), new Vector3(.045f, .42f, .045f), Cream, .01f, house);
            }
            Box("Door frame", new Vector3(0, .91f, -1.35f), new Vector3(.84f, 1.58f, .11f), Cream, .08f, house);
            Box("Front door", new Vector3(0, .88f, -1.43f), new Vector3(.64f, 1.39f, .08f), Hex("A07159"), .06f, house);
            Sphere("Door handle", new Vector3(.20f, .88f, -1.50f), new Vector3(.075f, .075f, .06f), Hex("F2CB70"), house);
            Box("Welcome step", new Vector3(0, .12f, -1.70f), new Vector3(1.05f, .15f, .52f), Cream, .06f, house);
            Box("Path to door", new Vector3(0, .054f, -2.2f), new Vector3(1.0f, .035f, .67f), Hex("EBDCC1"), .06f, house);
            WindowFront(new Vector3(-1.02f, 1.24f, -1.36f), house);
            WindowFront(new Vector3(1.02f, 1.24f, -1.36f), house);
            WindowFlowers(new Vector3(-1.02f, .78f, -1.60f), house, variant == 2 ? Hex("F1C36D") : Hex("EAA1AE"));
            if (variant != 1) WindowFlowers(new Vector3(1.02f, .78f, -1.60f), house, Hex("C1A5D9"));
            WindowSide(new Vector3(1.64f, 1.22f, .15f), house);
            Box("Mailbox post", new Vector3(1.52f, .43f, -2.1f), new Vector3(.12f, .86f, .12f), Wood, .03f, house);
            Box("Mailbox", new Vector3(1.52f, .92f, -2.1f), new Vector3(.43f, .32f, .38f), Lighten(roofColor, .12f), .10f, house);
            Box("Mailbox opening", new Vector3(1.52f, .94f, -2.302f), new Vector3(.25f, .025f, .018f), Hex("59696F"), .008f, house);
            if (awning)
            {
                for (int i = 0; i < 7; i++)
                {
                    GameObject stripe = Box("Striped bakery awning", new Vector3(-1.2f + i * .4f, 1.96f, -1.65f), new Vector3(.4f, .14f, .8f), i % 2 == 0 ? Cream : Hex("CC7C78"), .045f, house);
                    stripe.transform.localRotation = Quaternion.Euler(-12, 0, 0);
                }
            }
            // Potted plants on either side remain decorative and never trap the player.
            Pot(new Vector3(-1.46f, 0, -1.75f), house);
            if (shop != 0) city.DecorateShop(house, shop);
        }

        private static void WindowFlowers(Vector3 p, Transform parent, Color color)
        {
            Box("Window flower box", p, new Vector3(.76f, .17f, .27f), Wood, .045f, parent);
            for (int i = 0; i < 3; i++)
            {
                Vector3 blossom = p + new Vector3(-.24f + i * .24f, .16f, -.015f);
                Sphere("Window box leaves", blossom, new Vector3(.23f, .15f, .20f), Leaf, parent);
                Sphere("Window box flower", blossom + Vector3.up * .075f, new Vector3(.13f, .10f, .13f), color, parent);
            }
        }

        private static void Roof(Transform parent, Color color)
        {
            var go = new GameObject("Pitched roof");
            go.transform.SetParent(parent, false);
            Vector3[] vertices = {
                new Vector3(-1.9f,2.18f,-1.58f), new Vector3(1.9f,2.18f,-1.58f), new Vector3(0,3.2f,-1.58f),
                new Vector3(-1.9f,2.18f,1.58f), new Vector3(1.9f,2.18f,1.58f), new Vector3(0,3.2f,1.58f)
            };
            int[] tris = {0,2,1, 3,4,5, 0,3,5,0,5,2, 2,5,4,2,4,1, 0,1,4,0,4,3};
            Mesh mesh = new Mesh { name = "Handmade roof" };
            mesh.vertices = vertices; mesh.triangles = tris; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = Mat(color);
        }

        private static void WindowFront(Vector3 p, Transform parent)
        {
            Box("Window frame", p, new Vector3(.69f, .79f, .10f), Cream, .05f, parent);
            Box("Blue glass", p + new Vector3(0, 0, -.06f), new Vector3(.54f, .64f, .035f), Window, .025f, parent);
            Box("Window centre", p + new Vector3(0, 0, -.085f), new Vector3(.045f, .65f, .025f), Cream, .01f, parent);
            Box("Window centre", p + new Vector3(0, 0, -.085f), new Vector3(.55f, .045f, .025f), Cream, .01f, parent);
            Box("Window sill", p + new Vector3(0, -.43f, -.05f), new Vector3(.83f, .12f, .27f), White, .035f, parent);
        }

        private static void WindowSide(Vector3 p, Transform parent)
        {
            Transform w = Group("Side window", p, -90, parent);
            WindowFront(Vector3.zero, w);
        }

        private static void Depot()
        {
            Transform depot = Group("POSTA delivery depot", new Vector3(-12.4f, 0, -8), -90);
            Box("Depot building", new Vector3(0, 1.23f, 0), new Vector3(3.5f, 2.45f, 2.2f), Hex("E6BB86"), .14f, depot, true);
            Box("Depot roof", new Vector3(0, 2.55f, 0), new Vector3(3.9f, .3f, 2.6f), Hex("B57B61"), .12f, depot);
            Box("Loading door frame", new Vector3(0, 1.0f, -1.15f), new Vector3(1.8f, 1.98f, .11f), Cream, .08f, depot);
            Box("Loading door", new Vector3(0, .98f, -1.23f), new Vector3(1.54f, 1.8f, .09f), Hex("799A99"), .06f, depot);
            for (int i = 0; i < 5; i++)
                Box("Door slat", new Vector3(0, .35f + i * .3f, -1.29f), new Vector3(1.44f, .035f, .025f), Hex("587C80"), .008f, depot);
            Box("Orange canopy", new Vector3(0, 2.15f, -1.5f), new Vector3(2.5f, .16f, .85f), Hex("F4A861"), .07f, depot);
            Box("Depot sign", new Vector3(0, 2.85f, -.7f), new Vector3(1.65f, .7f, .13f), Cream, .09f, depot);
            Box("Parcel sign", new Vector3(0, 2.84f, -.79f), new Vector3(.46f, .39f, .06f), Hex("CE9567"), .035f, depot);
            Box("Parcel sign tape", new Vector3(0, 2.84f, -.835f), new Vector3(.08f, .39f, .025f), Hex("F8D5A2"), .008f, depot);
            Crate("Depot spare parcel", new Vector3(-12.0f, 0, -10.4f), .72f, false);
            Crate("Depot spare parcel", new Vector3(-12.7f, 0, -11.1f), .6f, false);
        }

        private static void Tree(Vector3 p, float scale)
        {
            Transform t = Group("Round-canopy tree", p);
            Box("Tree trunk", new Vector3(0, .63f * scale, 0), new Vector3(.30f, 1.26f, .30f) * scale, Wood, .07f * scale, t, true);
            Sphere("Canopy", new Vector3(0, 1.8f, 0) * scale, new Vector3(1.65f, 1.63f, 1.63f) * scale, Leaf, t);
            Sphere("Canopy", new Vector3(-.45f, 1.5f, -.22f) * scale, new Vector3(1.22f, 1.25f, 1.21f) * scale, DarkLeaf, t);
            Sphere("Canopy highlight", new Vector3(.42f, 2.0f, -.12f) * scale, new Vector3(1.15f, 1.25f, 1.15f) * scale, Hex("75BD82"), t);
        }

        private static void Bush(Vector3 p, float scale)
        {
            Sphere("Garden bush", p + new Vector3(0, .37f * scale, 0), new Vector3(1.3f, .95f, 1.03f) * scale, Leaf);
            Sphere("Garden bush", p + new Vector3(.4f * scale, .28f * scale, 0), new Vector3(.8f, .70f, .8f) * scale, Hex("79B888"));
        }

        private static void Pot(Vector3 p, Transform parent)
        {
            Box("Terracotta pot", p + new Vector3(0, .19f, 0), new Vector3(.36f, .37f, .36f), Hex("D28F73"), .08f, parent);
            Sphere("Pot plant", p + new Vector3(0, .49f, 0), new Vector3(.48f, .5f, .46f), Leaf, parent);
            Sphere("Flower", p + new Vector3(0, .72f, -.02f), new Vector3(.18f, .14f, .18f), Hex("F4C76B"), parent);
        }

        private static void FlowerBed(Vector3 p, Color flowers)
        {
            Box("Raised flowerbed", p + new Vector3(0, .13f, 0), new Vector3(.68f, .24f, 1.25f), Wood, .07f);
            Box("Flowerbed soil", p + new Vector3(0, .27f, 0), new Vector3(.52f, .04f, 1.1f), Hex("8C735C"), .04f);
            for (int i = 0; i < 4; i++)
            {
                float z = -.4f + i * .27f;
                Box("Flower stem", p + new Vector3(0, .40f, z), new Vector3(.025f, .3f, .025f), DarkLeaf, .008f);
                Sphere("Flower blossom", p + new Vector3(0, .58f, z), new Vector3(.23f, .14f, .23f), flowers);
                Sphere("Flower centre", p + new Vector3(0, .65f, z), new Vector3(.075f, .04f, .075f), Cream);
            }
        }

        private static void Fence(Vector3 p, float length, float yaw)
        {
            Transform f = Group("Little garden fence", p, yaw);
            Box("Fence rail", new Vector3(0, .47f, 0), new Vector3(length, .09f, .07f), Cream, .025f, f);
            Box("Fence rail", new Vector3(0, .22f, 0), new Vector3(length, .09f, .07f), Cream, .025f, f);
            int count = Mathf.RoundToInt(length / .42f);
            for (int i = 0; i <= count; i++)
            {
                float x = -length / 2 + length * i / count;
                Box("Fence picket", new Vector3(x, .34f, 0), new Vector3(.13f, .68f, .13f), White, .04f, f);
            }
            var barrier = new GameObject("Garden fence collider");
            barrier.transform.SetParent(f, false);
            barrier.transform.localPosition = new Vector3(0, .3f, 0);
            barrier.AddComponent<BoxCollider>().size = new Vector3(length, .6f, .15f);
        }

        private static void Bench(Vector3 p, float yaw)
        {
            Transform b = Group("Park bench", p, yaw);
            Box("Bench seat", new Vector3(0, .41f, 0), new Vector3(1.4f, .13f, .46f), Hex("CD9E73"), .04f, b);
            Box("Bench back", new Vector3(0, .74f, .22f), new Vector3(1.4f, .46f, .10f), Hex("CD9E73"), .04f, b);
            Box("Bench leg", new Vector3(-.5f, .2f, 0), new Vector3(.09f, .4f, .36f), Hex("67887C"), .025f, b);
            Box("Bench leg", new Vector3(.5f, .2f, 0), new Vector3(.09f, .4f, .36f), Hex("67887C"), .025f, b);
        }

        private static void Lamp(Vector3 p)
        {
            Box("Street lamp pole", p + new Vector3(0, 1.2f, 0), new Vector3(.10f, 2.4f, .10f), Hex("668B83"), .035f);
            Box("Street lamp base", p + new Vector3(0, .1f, 0), new Vector3(.26f, .2f, .26f), Hex("668B83"), .06f);
            Box("Street lantern", p + new Vector3(0, 2.47f, 0), new Vector3(.34f, .46f, .34f), Hex("FFF0AD"), .07f);
            Box("Street lamp cap", p + new Vector3(0, 2.74f, 0), new Vector3(.45f, .12f, .45f), Hex("668B83"), .05f);
        }

        private static void Crate(string name, Vector3 p, float size, bool hazard)
        {
            Transform c = Group(name, p);
            Box("Parcel", new Vector3(0, size / 2, 0), new Vector3(size, size, size), Hex("CC9F72"), .07f, c);
            Box("Packing tape", new Vector3(0, size / 2, -.008f), new Vector3(size * .18f, size + .012f, size + .02f), Hex("F4D1A0"), .015f, c);
            Box("Address label", new Vector3(size * .18f, size + .015f, -.08f), new Vector3(size * .26f, .012f, size * .3f), White, .012f, c);
            if (hazard)
            {
                var collider = c.gameObject.AddComponent<BoxCollider>();
                collider.center = new Vector3(0, size / 2, 0);
                collider.size = new Vector3(size, size, size);
            }
        }

        private static void Puddle(Vector3 p, float size)
        {
            Sphere("Puddle", p + new Vector3(0, .05f, 0), new Vector3(size, .012f, size * .65f), Hex("88BAC5"));
            Sphere("Puddle gleam", p + new Vector3(-size * .16f, .06f, -.1f), new Vector3(size * .43f, .01f, size * .07f), Hex("D3E5D9"));
        }

        private static void Tuft(Vector3 p)
        {
            Sphere("Grass tuft", p + new Vector3(0, .09f, 0), new Vector3(.26f, .18f, .2f), Hex("75B77F"));
            Sphere("Grass tuft", p + new Vector3(.16f, .06f, .03f), new Vector3(.2f, .12f, .17f), Hex("75B77F"));
        }

        private static Transform Group(string name, Vector3 position, float yaw = 0, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent == null ? world : parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            return go.transform;
        }

        private static GameObject Box(string name, Vector3 p, Vector3 size, Color color, float bevel = .04f, Transform parent = null, bool solid = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent == null ? world : parent, false);
            go.transform.localPosition = p;
            go.AddComponent<MeshFilter>().sharedMesh = RoundedBoxMesh(size, bevel);
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Mat(color);
            renderer.shadowCastingMode = ShadowCastingMode.On;
            if (solid) go.AddComponent<BoxCollider>().size = size;
            return go;
        }

        private static GameObject Sphere(string name, Vector3 p, Vector3 size, Color color, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent == null ? world : parent, false);
            go.transform.localPosition = p;
            go.transform.localScale = size;
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            return go;
        }

        private static Material Mat(Color color)
        {
            Material material;
            if (Materials.TryGetValue(color, out material)) return material;
            material = new Material(Shader.Find("Standard"));
            material.name = "Neighborhood " + ColorUtility.ToHtmlStringRGB(color);
            material.color = color;
            material.SetFloat("_Glossiness", .12f);
            material.SetFloat("_Metallic", 0);
            material.SetColor("_SpecColor", new Color(.06f, .06f, .06f));
            Materials[color] = material;
            return material;
        }

        private static Mesh RoundedBoxMesh(Vector3 size, float radius)
        {
            radius = Mathf.Min(radius, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * .45f);
            Vector4 key = new Vector4(size.x, size.y, size.z, radius);
            Mesh cached;
            if (BoxMeshes.TryGetValue(key, out cached)) return cached;
            Vector3 half = size * .5f;
            Vector3 inner = half - Vector3.one * radius;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var indices = new List<int>();
            Vector3[] faceNormals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            Vector3[] faceRights = { Vector3.back, Vector3.forward, Vector3.right, Vector3.right, Vector3.right, Vector3.left };
            Vector3[] faceUps = { Vector3.up, Vector3.up, Vector3.back, Vector3.forward, Vector3.up, Vector3.up };
            for (int face = 0; face < 6; face++)
            {
                Vector3 n = faceNormals[face], right = faceRights[face], up = faceUps[face];
                float hn = Extent(half, n), hr = Extent(half, right), hu = Extent(half, up);
                float[] xs = { -hr, -hr + radius, 0, hr - radius, hr };
                float[] ys = { -hu, -hu + radius, 0, hu - radius, hu };
                int start = vertices.Count;
                for (int v = 0; v < 5; v++)
                    for (int u = 0; u < 5; u++)
                    {
                        Vector3 raw = n * hn + right * xs[u] + up * ys[v];
                        Vector3 core = new Vector3(Mathf.Clamp(raw.x, -inner.x, inner.x), Mathf.Clamp(raw.y, -inner.y, inner.y), Mathf.Clamp(raw.z, -inner.z, inner.z));
                        Vector3 normal = (raw - core).normalized;
                        vertices.Add(core + normal * radius);
                        normals.Add(normal);
                    }
                for (int v = 0; v < 4; v++)
                    for (int u = 0; u < 4; u++)
                    {
                        int a = start + v * 5 + u, b = a + 1, c = a + 6, d = a + 5;
                        indices.Add(a); indices.Add(b); indices.Add(c);
                        indices.Add(a); indices.Add(c); indices.Add(d);
                    }
            }
            Mesh mesh = new Mesh { name = "Soft bevel box" };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetTriangles(indices, 0); mesh.RecalculateBounds();
            BoxMeshes[key] = mesh;
            return mesh;
        }

        private static float Extent(Vector3 half, Vector3 axis)
        {
            return Mathf.Abs(axis.x) * half.x + Mathf.Abs(axis.y) * half.y + Mathf.Abs(axis.z) * half.z;
        }

        private static Color Hex(string value)
        {
            Color c;
            ColorUtility.TryParseHtmlString("#" + value, out c);
            return c;
        }

        private static Color Lighten(Color color, float amount)
        {
            return Color.Lerp(color, Color.white, amount);
        }
    }
}
