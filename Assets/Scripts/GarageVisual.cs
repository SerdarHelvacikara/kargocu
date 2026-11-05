using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>Fixed-slot cutaway workshop. Its appearance is entirely driven by the owner's bit mask.</summary>
    public sealed class GarageVisual : MonoBehaviour
    {
        public Vector3 Focus => transform.position + new Vector3(-.35f, 1.1f, 0);
        public int VisibleDecorCount
        {
            get
            {
                int count = 0;
                if (decor != null) for (int i = 0; i < decor.Length; i++) if (decor[i] && decor[i].gameObject.activeSelf) count++;
                return count;
            }
        }

        readonly Transform[] decor = new Transform[GarageCatalog.Count];
        readonly Transform[] depotStages = new Transform[CourierGame.MaxDepotLevel - 1];
        Transform basicBench, fastTrim, parkedScooter;
        MinivanVisual parkedVan;
        MonsterTruckVisual parkedMonster;
        FantasyVehicleVisual parkedDragon, parkedDinosaur;
        Material scooterPaint;
        static readonly Color Cream = ToyGeometry.Hex("FFF2D6");
        static readonly Color White = ToyGeometry.Hex("FFFCF0");
        static readonly Color Teal = ToyGeometry.Hex("71B5AD");
        static readonly Color DarkTeal = ToyGeometry.Hex("497F7B");
        static readonly Color Coral = ToyGeometry.Hex("ECA291");
        static readonly Color Wood = ToyGeometry.Hex("D0A477");
        static readonly Color Ink = ToyGeometry.Hex("365663");

        public static GarageVisual Create()
        {
            var root = new GameObject("Courier's little garage");
            root.transform.position = new Vector3(48, 0, 0);
            var garage = root.AddComponent<GarageVisual>();
            garage.Build();
            garage.SetDecor(0);
            return garage;
        }

        void Build()
        {
            Room();
            RollUpDoor();
            Window();
            ToolBoard();
            basicBench = Workbench(false);
            Scooter();
            parkedVan = MinivanVisual.Create(transform);
            parkedVan.transform.localPosition = new Vector3(.8f, .07f, -1.2f);
            parkedVan.transform.localRotation = Quaternion.Euler(0, 136, 0);
            parkedVan.transform.localScale = Vector3.one * 1.35f;
            parkedVan.gameObject.SetActive(false);
            parkedMonster = MonsterTruckVisual.Create(transform);
            parkedMonster.transform.localPosition = new Vector3(.8f, .07f, -1.2f);
            parkedMonster.transform.localRotation = Quaternion.Euler(0, 136, 0);
            parkedMonster.transform.localScale = Vector3.one * 1.15f;
            parkedMonster.gameObject.SetActive(false);
            parkedDragon = FantasyVehicleVisual.Create(transform, true);
            parkedDragon.transform.localPosition = new Vector3(.8f, -.53f, -1.2f);
            parkedDragon.transform.localRotation = Quaternion.Euler(0, 136, 0);
            parkedDragon.transform.localScale = Vector3.one * 1.02f;
            parkedDragon.gameObject.SetActive(false);
            parkedDinosaur = FantasyVehicleVisual.Create(transform, false);
            parkedDinosaur.transform.localPosition = new Vector3(.8f, .07f, -1.2f);
            parkedDinosaur.transform.localRotation = Quaternion.Euler(0, 136, 0);
            parkedDinosaur.transform.localScale = Vector3.one * 1.10f;
            parkedDinosaur.gameObject.SetActive(false);
            decor[0] = Plant();
            decor[1] = FloorLamp();
            decor[2] = Rug();
            decor[3] = Workbench(true);
            decor[4] = ParcelShelf();
            decor[5] = ParcelSign();
            decor[6] = Armchair();
            decor[7] = ParcelPoster();
            decor[8] = DroneStand();
            decor[9] = SproutPalPoster();
            decor[10] = CloudPalPoster();
            decor[11] = SkylineArt();
            decor[12] = CarefulCourierTrophy();
            decor[13] = ParcelVendingMachine();
            for (int i = 0; i < depotStages.Length; i++)
            {
                int stack = i / 3, layer = i % 3;
                Transform crate = ToyGeometry.Group(transform, "Depot expansion crate " + (i + 2),
                    new Vector3(-3.24f + stack * .67f, .27f + layer * .42f, -2.93f));
                ToyGeometry.Box(crate, "Logistics upgrade crate", Vector3.zero, new Vector3(.57f, .40f, .59f), stack == 0 ? Wood : stack == 1 ? Teal : Coral);
                ToyGeometry.Box(crate, "Logistics label", new Vector3(0, .024f, .30f), new Vector3(.25f, .11f, .027f), White);
                depotStages[i] = crate;
                crate.gameObject.SetActive(false);
            }
            LocalLight("Warm workshop light", new Vector3(1.0f, 3.2f, -1.5f), new Color(1, .88f, .70f), .65f, 9, transform);
            LocalLight("Window fill", new Vector3(-4.8f, 2.4f, -1.2f), new Color(.69f, .87f, 1), .38f, 7, transform);
        }

        public void SetDecor(int equippedMask)
        {
            for (int i = 0; i < decor.Length; i++)
                if (decor[i]) decor[i].gameObject.SetActive((equippedMask & (1 << i)) != 0);
            if (basicBench) basicBench.gameObject.SetActive((equippedMask & (1 << 3)) == 0);
        }

        public void SetScooterColor(Color color)
        {
            if (scooterPaint) scooterPaint.color = color;
            if (parkedVan) parkedVan.SetColor(color);
            if (parkedMonster) parkedMonster.SetColor(color);
            if (parkedDragon) parkedDragon.SetColor(color);
            if (parkedDinosaur) parkedDinosaur.SetColor(color);
        }

        public void SetVehicleStyle(int index)
        {
            if (parkedScooter) parkedScooter.gameObject.SetActive(index < 2);
            if (parkedVan) parkedVan.gameObject.SetActive(index == 2);
            if (parkedMonster) parkedMonster.gameObject.SetActive(index == 3);
            if (parkedDragon) parkedDragon.gameObject.SetActive(index == 4);
            if (parkedDinosaur) parkedDinosaur.gameObject.SetActive(index == 5);
            if (fastTrim) fastTrim.gameObject.SetActive(index == 1);
        }

        internal void TurnPreview(float yaw)
        {
            Quaternion angle = Quaternion.Euler(0, yaw, 0);
            if (parkedScooter) parkedScooter.localRotation = angle;
            if (parkedVan) parkedVan.transform.localRotation = angle;
            if (parkedMonster) parkedMonster.transform.localRotation = angle;
            if (parkedDragon) parkedDragon.transform.localRotation = angle;
            if (parkedDinosaur) parkedDinosaur.transform.localRotation = angle;
        }

        internal void ShowDepotLevel(int level)
        {
            for (int i = 0; i < depotStages.Length; i++) depotStages[i].gameObject.SetActive(i < level - 1);
        }

        void OnDestroy()
        {
            if (scooterPaint) Destroy(scooterPaint);
        }

        void Room()
        {
            Box(transform, "Workshop foundation", new Vector3(0, -.22f, 0), new Vector3(12.3f, .42f, 10.3f), Wood);
            Box(transform, "Floor grout", new Vector3(0, -.015f, 0), new Vector3(12, .07f, 10), ToyGeometry.Hex("D2D5C5"));
            for (int x = 0; x < 10; x++)
                for (int z = 0; z < 8; z++)
                {
                    Color tile = (x + z) % 2 == 0 ? ToyGeometry.Hex("E8E7D5") : ToyGeometry.Hex("DDE5D4");
                    Box(transform, "Soft checker floor tile", new Vector3(-5.4f + x * 1.2f, .028f, -4.2f + z * 1.2f), new Vector3(1.17f, .045f, 1.17f), tile);
                }
            Box(transform, "Back plaster wall", new Vector3(0, 1.85f, 4.96f), new Vector3(12.2f, 3.7f, .22f), Cream);
            Box(transform, "Left plaster wall", new Vector3(-6.0f, 1.85f, 0), new Vector3(.22f, 3.7f, 10.0f), Cream);
            Box(transform, "Back teal wainscot", new Vector3(0, .56f, 4.81f), new Vector3(11.95f, 1.12f, .075f), Teal);
            Box(transform, "Left teal wainscot", new Vector3(-5.86f, .56f, 0), new Vector3(.075f, 1.12f, 9.9f), Teal);
            Box(transform, "Back dado rail", new Vector3(0, 1.14f, 4.75f), new Vector3(12.0f, .095f, .14f), White);
            Box(transform, "Left dado rail", new Vector3(-5.80f, 1.14f, 0), new Vector3(.14f, .095f, 10), White);
            Box(transform, "Back wall top trim", new Vector3(0, 3.66f, 4.87f), new Vector3(12.3f, .16f, .37f), Wood);
            Box(transform, "Left wall top trim", new Vector3(-5.96f, 3.66f, 0), new Vector3(.35f, .16f, 10.2f), Wood);
            Box(transform, "Back skirting", new Vector3(0, .12f, 4.73f), new Vector3(12, .21f, .14f), DarkTeal);
            Box(transform, "Left skirting", new Vector3(-5.78f, .12f, 0), new Vector3(.14f, .21f, 10), DarkTeal);
            Box(transform, "Front threshold", new Vector3(0, .07f, -4.93f), new Vector3(12.1f, .16f, .23f), Wood);

            // A few permanent practical details make the un-upgraded room feel lived in.
            Transform hooks = Group("Wall hooks", new Vector3(-5.73f, 1.72f, 2.65f));
            Box(hooks, "Hook rail", Vector3.zero, new Vector3(.09f, .16f, 1.5f), Wood);
            for (int i = 0; i < 3; i++)
                ToyGeometry.Sphere(hooks, "Rounded hook", new Vector3(.10f, -.03f, -.5f + i * .5f), new Vector3(.15f, .10f, .10f), Ink);
            Box(transform, "Folded delivery apron", new Vector3(-5.59f, 1.27f, 2.2f), new Vector3(.13f, .68f, .48f), Coral);
            Box(transform, "Apron pocket", new Vector3(-5.50f, 1.24f, 2.2f), new Vector3(.035f, .20f, .26f), Cream);
            Transform bin = Group("Small tidy bin", new Vector3(4.9f, 0, 4.0f));
            Cylinder(bin, "Bin body", new Vector3(0, .29f, 0), new Vector3(.48f, .56f, .48f), DarkTeal);
            Cylinder(bin, "Bin lid", new Vector3(0, .60f, 0), new Vector3(.54f, .09f, .54f), Teal);
            Box(bin, "Bin handle", new Vector3(0, .68f, 0), new Vector3(.17f, .07f, .07f), Ink);

            // Four corner markings keep the parked scooter grounded when no rug is equipped.
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 p = new Vector3(.8f + x * 1.9f, .057f, -1.2f + z * 1.5f);
                    Box(transform, "Parking corner", p + new Vector3(-x * .22f, 0, 0), new Vector3(.54f, .012f, .055f), White);
                    Box(transform, "Parking corner", p + new Vector3(0, 0, -z * .22f), new Vector3(.055f, .012f, .54f), White);
                }
        }

        void RollUpDoor()
        {
            Transform door = Group("Roll-up garage door", new Vector3(-2.50f, 0, 4.72f));
            Box(door, "Door dark recess", new Vector3(0, 1.44f, .025f), new Vector3(4.6f, 2.88f, .15f), DarkTeal);
            Box(door, "Left door jamb", new Vector3(-2.36f, 1.48f, -.04f), new Vector3(.18f, 2.96f, .25f), White);
            Box(door, "Right door jamb", new Vector3(2.36f, 1.48f, -.04f), new Vector3(.18f, 2.96f, .25f), White);
            Box(door, "Door header", new Vector3(0, 3.0f, -.04f), new Vector3(4.9f, .20f, .28f), White);
            for (int i = 0; i < 10; i++)
                Box(door, "Rounded shutter slat", new Vector3(0, .16f + i * .276f, -.10f), new Vector3(4.42f, .247f, .09f), i % 2 == 0 ? ToyGeometry.Hex("95C6BD") : ToyGeometry.Hex("8BBDB5"));
            Box(door, "Door handle", new Vector3(0, .48f, -.18f), new Vector3(.53f, .085f, .12f), Ink);
            Box(door, "Door handle centre", new Vector3(0, .48f, -.25f), new Vector3(.34f, .04f, .05f), Cream);
            // Tiny wall clock above the shutter.
            ToyGeometry.Sphere(door, "Clock frame", new Vector3(0, 3.37f, -.04f), new Vector3(.57f, .57f, .11f), DarkTeal);
            ToyGeometry.Sphere(door, "Clock face", new Vector3(0, 3.37f, -.105f), new Vector3(.46f, .46f, .035f), White);
            Transform hour = Box(door, "Clock hour hand", new Vector3(-.055f, 3.41f, -.132f), new Vector3(.15f, .025f, .015f), Ink);
            hour.localRotation = Quaternion.Euler(0, 0, -35);
            Transform minute = Box(door, "Clock minute hand", new Vector3(0, 3.45f, -.132f), new Vector3(.025f, .19f, .015f), Ink);
            minute.localRotation = Quaternion.Euler(0, 0, 8);
        }

        void Window()
        {
            Transform window = Group("Workshop daylight window", new Vector3(-5.78f, 2.27f, -1.25f));
            Box(window, "Window outer frame", Vector3.zero, new Vector3(.17f, 1.53f, 2.05f), White);
            Box(window, "Blue daylight glass", new Vector3(.105f, 0, 0), new Vector3(.035f, 1.31f, 1.83f), ToyGeometry.Hex("ACD4DC"));
            Box(window, "Window vertical mullion", new Vector3(.14f, 0, 0), new Vector3(.05f, 1.35f, .075f), White);
            Box(window, "Window horizontal mullion", new Vector3(.14f, 0, 0), new Vector3(.05f, .075f, 1.86f), White);
            Box(window, "Deep windowsill", new Vector3(.13f, -.80f, 0), new Vector3(.51f, .13f, 2.18f), Wood);
            Box(window, "Left curtain", new Vector3(.12f, .08f, -.95f), new Vector3(.14f, 1.52f, .22f), Coral);
            Box(window, "Right curtain", new Vector3(.12f, .08f, .95f), new Vector3(.14f, 1.52f, .22f), Coral);
            for (int i = 0; i < 3; i++)
                ToyGeometry.Sphere(window, "Small cloud outside", new Vector3(.13f, .27f + (i == 1 ? .09f : 0), -.40f + i * .19f), new Vector3(.025f, .18f, .32f), White);
        }

        void ToolBoard()
        {
            Transform board = Group("Handy pegboard", new Vector3(3.45f, 2.08f, 4.70f));
            Box(board, "Pegboard frame", Vector3.zero, new Vector3(2.74f, 1.36f, .13f), Wood);
            Box(board, "Pegboard face", new Vector3(0, 0, -.08f), new Vector3(2.57f, 1.19f, .035f), ToyGeometry.Hex("DEC69C"));
            for (int x = 0; x < 7; x++)
                for (int y = 0; y < 3; y++)
                    ToyGeometry.Sphere(board, "Peg hole", new Vector3(-1.05f + x * .35f, -.38f + y * .36f, -.105f), new Vector3(.035f, .035f, .02f), ToyGeometry.Hex("B79E7A"));
            Tool(board, new Vector3(-.7f, -.10f, -.16f), 0, Coral);
            Tool(board, new Vector3(0, -.08f, -.16f), 12, DarkTeal);
            Transform hammer = ToyGeometry.Group(board, "Little hammer", new Vector3(.72f, -.04f, -.16f));
            Box(hammer, "Hammer handle", Vector3.zero, new Vector3(.085f, .62f, .08f), Wood);
            Box(hammer, "Hammer head", new Vector3(0, .30f, 0), new Vector3(.39f, .15f, .12f), Ink);
        }

        static void Tool(Transform parent, Vector3 position, float angle, Color color)
        {
            Transform wrench = ToyGeometry.Group(parent, "Rounded workshop wrench", position);
            wrench.localRotation = Quaternion.Euler(0, 0, angle);
            ToyGeometry.Box(wrench, "Wrench grip", new Vector3(0, -.1f, 0), new Vector3(.115f, .39f, .07f), color);
            ToyGeometry.Box(wrench, "Wrench neck", new Vector3(0, .17f, 0), new Vector3(.065f, .19f, .06f), Ink);
            ToyGeometry.Box(wrench, "Wrench jaw", new Vector3(-.075f, .28f, 0), new Vector3(.075f, .20f, .07f), Ink);
            ToyGeometry.Box(wrench, "Wrench jaw", new Vector3(.075f, .28f, 0), new Vector3(.075f, .20f, .07f), Ink);
            ToyGeometry.Box(wrench, "Wrench jaw base", new Vector3(0, .22f, 0), new Vector3(.20f, .07f, .07f), Ink);
        }

        Transform Workbench(bool upgraded)
        {
            Transform bench = Group(upgraded ? "Upgrade 4 - fresh workbench" : "Original simple workbench", new Vector3(3.25f, 0, 3.72f));
            Box(bench, "Wooden worktop", new Vector3(0, 1.06f, 0), new Vector3(3.50f, .18f, 1.18f), upgraded ? ToyGeometry.Hex("E1B684") : Wood);
            if (upgraded)
            {
                Box(bench, "Teal cabinet", new Vector3(0, .53f, .08f), new Vector3(3.20f, .94f, .92f), DarkTeal);
                for (int side = -1; side <= 1; side += 2)
                    for (int row = 0; row < 3; row++)
                    {
                        Box(bench, "New drawer front", new Vector3(side * .80f, .24f + row * .285f, -.415f), new Vector3(1.45f, .245f, .055f), Teal);
                        Box(bench, "Cream drawer handle", new Vector3(side * .80f, .24f + row * .285f, -.46f), new Vector3(.34f, .043f, .055f), White);
                    }
                Box(bench, "Little vice base", new Vector3(-1.1f, 1.23f, -.15f), new Vector3(.50f, .19f, .42f), Ink);
                Box(bench, "Vice jaws", new Vector3(-1.1f, 1.40f, -.13f), new Vector3(.44f, .15f, .16f), Teal);
                Box(bench, "Vice screw", new Vector3(-1.1f, 1.24f, -.46f), new Vector3(.05f, .05f, .39f), Ink);
                Box(bench, "Vice handle", new Vector3(-1.1f, 1.24f, -.68f), new Vector3(.32f, .05f, .05f), Ink);
                Cylinder(bench, "Pencil cup", new Vector3(1.13f, 1.32f, .20f), new Vector3(.24f, .37f, .24f), Coral);
                for (int i = 0; i < 3; i++)
                {
                    Transform pencil = Box(bench, "Colored pencil", new Vector3(1.06f + i * .07f, 1.58f, .2f), new Vector3(.035f, .44f, .035f), i == 0 ? Cream : i == 1 ? Teal : Ink);
                    pencil.localRotation = Quaternion.Euler(0, 0, -9 + i * 8);
                }
                Box(bench, "Delivery notebook", new Vector3(.48f, 1.18f, -.1f), new Vector3(.54f, .045f, .39f), Coral);
                Box(bench, "Notebook label", new Vector3(.48f, 1.208f, -.1f), new Vector3(.27f, .015f, .18f), Cream);
            }
            else
            {
                foreach (float x in new[] { -1.38f, 1.38f })
                    foreach (float z in new[] { -.36f, .36f })
                        Box(bench, "Simple bench leg", new Vector3(x, .52f, z), new Vector3(.13f, .99f, .13f), DarkTeal);
                Box(bench, "Lower bench shelf", new Vector3(0, .27f, .02f), new Vector3(3.20f, .10f, .90f), Wood);
                Box(bench, "Folded workshop cloth", new Vector3(.8f, 1.19f, .05f), new Vector3(.50f, .08f, .41f), Cream);
            }
            return bench;
        }

        void Scooter()
        {
            Transform s = Group("Your parked scooter", new Vector3(.8f, .07f, -1.20f));
            parkedScooter = s;
            fastTrim = ToyGeometry.Group(s, "Zip rider racing trim", Vector3.zero);
            ToyGeometry.Box(fastTrim, "Racing stripe", new Vector3(0, .88f, .72f), new Vector3(.12f, .55f, .03f), Cream);
            ToyGeometry.Cylinder(fastTrim, "Pennant pole", new Vector3(.32f, 1.46f, -.56f), new Vector3(.025f, 1.05f, .025f), Cream);
            ToyGeometry.Box(fastTrim, "Racing pennant", new Vector3(.46f, 1.88f, -.56f), new Vector3(.28f, .17f, .025f), Coral);
            fastTrim.gameObject.SetActive(false);
            s.localRotation = Quaternion.Euler(0, 136, 0);
            s.localScale = Vector3.one * 1.35f;
            scooterPaint = new Material(Shader.Find("Standard"));
            scooterPaint.name = "Garage scooter repaint";
            scooterPaint.color = ToyGeometry.Hex("18ADA6");
            scooterPaint.SetFloat("_Glossiness", .18f);
            scooterPaint.SetFloat("_SpecularHighlights", 0);
            Color tire = ToyGeometry.Hex("344B59");
            Color metal = ToyGeometry.Hex("B7CED0");
            foreach (float z in new[] { -.57f, .70f })
            {
                Transform wheel = Cylinder(s, "Soft tire", new Vector3(0, .31f, z), new Vector3(.61f, .16f, .61f), tire);
                wheel.localRotation = Quaternion.Euler(0, 0, 90);
                foreach (float x in new[] { -.095f, .095f })
                {
                    Transform hub = Cylinder(s, "Cream wheel hub", new Vector3(x, .31f, z), new Vector3(.32f, .035f, .32f), Cream);
                    hub.localRotation = Quaternion.Euler(0, 0, 90);
                }
            }
            PaintPart(Box(s, "Scooter footboard", new Vector3(0, .47f, .02f), new Vector3(.60f, .15f, 1.36f), Teal));
            PaintPart(ToyGeometry.Sphere(s, "Rounded rear body", new Vector3(0, .64f, -.46f), new Vector3(.79f, .66f, .85f), Teal));
            PaintPart(ToyGeometry.Sphere(s, "Front fairing", new Vector3(0, .90f, .53f), new Vector3(.68f, 1.06f, .30f), Teal));
            ToyGeometry.Sphere(s, "Navy saddle", new Vector3(0, 1.01f, -.27f), new Vector3(.64f, .18f, .74f), Ink);
            ToyGeometry.Sphere(s, "Headlight cream surround", new Vector3(0, 1.36f, .65f), new Vector3(.35f, .29f, .16f), Cream);
            ToyGeometry.Sphere(s, "Headlight glass", new Vector3(0, 1.36f, .74f), new Vector3(.25f, .20f, .05f), White);
            Box(s, "Handlebar", new Vector3(0, 1.42f, .46f), new Vector3(.94f, .065f, .075f), metal);
            foreach (float x in new[] { -.43f, .43f })
            {
                Box(s, "Handle grip", new Vector3(x, 1.42f, .46f), new Vector3(.18f, .095f, .10f), Ink);
                Transform stalk = Cylinder(s, "Mirror stalk", new Vector3(x, 1.60f, .46f), new Vector3(.025f, .36f, .025f), metal);
                stalk.localRotation = Quaternion.Euler(0, 0, x < 0 ? -14 : 14);
                ToyGeometry.Sphere(s, "Round mirror", new Vector3(x * 1.12f, 1.81f, .46f), new Vector3(.18f, .21f, .075f), metal);
                ToyGeometry.Sphere(s, "Mirror glass", new Vector3(x * 1.12f, 1.81f, .50f), new Vector3(.14f, .17f, .02f), ToyGeometry.Hex("CEE4E1"));
            }
            Box(s, "Parcel carrier", new Vector3(0, 1.04f, -.80f), new Vector3(.72f, .065f, .43f), metal);
            ToyGeometry.Sphere(s, "Rear red light", new Vector3(0, .72f, -.91f), new Vector3(.20f, .14f, .06f), Coral);
            Transform stand = Box(s, "Parking stand", new Vector3(.27f, .22f, -.16f), new Vector3(.045f, .40f, .045f), Ink);
            stand.localRotation = Quaternion.Euler(0, 0, -24);
        }

        void PaintPart(Transform part) { part.GetComponent<Renderer>().sharedMaterial = scooterPaint; }

        Transform Plant()
        {
            Transform p = Group("Upgrade 1 - cheerful houseplant", new Vector3(-4.55f, .05f, -3.65f));
            Cylinder(p, "Terracotta pot", new Vector3(0, .40f, 0), new Vector3(.95f, .77f, .95f), Coral);
            Cylinder(p, "Pot rim", new Vector3(0, .81f, 0), new Vector3(1.06f, .17f, 1.06f), ToyGeometry.Hex("EFB69C"));
            Cylinder(p, "Soil", new Vector3(0, .90f, 0), new Vector3(.86f, .04f, .86f), ToyGeometry.Hex("8C7058"));
            Cylinder(p, "Stem", new Vector3(0, 1.37f, 0), new Vector3(.065f, .95f, .065f), DarkTeal);
            for (int i = 0; i < 8; i++)
            {
                float a = i * 137 * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Transform leaf = ToyGeometry.Sphere(p, "Large soft leaf", direction * .29f + Vector3.up * (1.1f + i * .095f), new Vector3(.33f, .15f, .78f), i % 2 == 0 ? ToyGeometry.Hex("68AC80") : ToyGeometry.Hex("8ABD83"));
                leaf.localRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(-26, 0, 0);
            }
            return p;
        }

        Transform FloorLamp()
        {
            Transform lamp = Group("Upgrade 2 - cozy standing lamp", new Vector3(4.50f, .05f, 1.1f));
            Cylinder(lamp, "Lamp heavy foot", new Vector3(0, .07f, 0), new Vector3(.84f, .12f, .84f), DarkTeal);
            Cylinder(lamp, "Lamp brass pole", new Vector3(0, 1.22f, 0), new Vector3(.075f, 2.35f, .075f), Wood);
            Cylinder(lamp, "Wide cream shade", new Vector3(0, 2.45f, 0), new Vector3(1.12f, .55f, 1.12f), Cream);
            Cylinder(lamp, "Shade lower piping", new Vector3(0, 2.18f, 0), new Vector3(1.17f, .055f, 1.17f), Coral);
            Cylinder(lamp, "Shade upper piping", new Vector3(0, 2.73f, 0), new Vector3(1.14f, .055f, 1.14f), Coral);
            ToyGeometry.Sphere(lamp, "Warm bulb", new Vector3(0, 2.18f, 0), new Vector3(.29f, .22f, .29f), ToyGeometry.Hex("FFE5A8"));
            LocalLight("Lamp glow", new Vector3(0, 2.12f, 0), new Color(1, .79f, .49f), .7f, 5, lamp);
            return lamp;
        }

        Transform Rug()
        {
            Transform rug = Group("Upgrade 3 - coral woven rug", new Vector3(.80f, .072f, -1.20f));
            Box(rug, "Rug woven base", Vector3.zero, new Vector3(4.5f, .045f, 3.8f), Coral);
            Box(rug, "Rug cream border", new Vector3(0, .027f, 0), new Vector3(4.14f, .009f, 3.44f), Cream);
            Box(rug, "Rug coral centre", new Vector3(0, .034f, 0), new Vector3(3.97f, .009f, 3.27f), ToyGeometry.Hex("D99080"));
            for (int i = 0; i < 15; i++)
                foreach (float z in new[] { -1.99f, 1.99f })
                    Box(rug, "Rug tassel", new Vector3(-2.08f + i * .297f, 0, z), new Vector3(.055f, .03f, .20f), Cream);
            for (int side = -1; side <= 1; side += 2)
            {
                Transform diamond = Box(rug, "Woven corner diamond", new Vector3(side * 1.65f, .044f, 0), new Vector3(.32f, .008f, .32f), Cream);
                diamond.localRotation = Quaternion.Euler(0, 45, 0);
            }
            return rug;
        }

        Transform ParcelShelf()
        {
            Transform shelf = Group("Upgrade 5 - stacked parcel shelf", new Vector3(-5.0f, .05f, .85f));
            shelf.localRotation = Quaternion.Euler(0, 90, 0);
            foreach (float x in new[] { -1.28f, 1.28f })
                foreach (float z in new[] { -.40f, .40f })
                    Box(shelf, "Shelf upright", new Vector3(x, 1.37f, z), new Vector3(.12f, 2.74f, .12f), DarkTeal);
            for (int i = 0; i < 4; i++)
                Box(shelf, "Wooden shelf", new Vector3(0, .20f + i * .81f, 0), new Vector3(2.75f, .11f, .98f), Wood);
            Parcel(shelf, new Vector3(-.65f, .55f, 0), new Vector3(.82f, .57f, .65f), Coral);
            Parcel(shelf, new Vector3(.62f, .49f, -.04f), new Vector3(.83f, .43f, .62f), ToyGeometry.Hex("CDA477"));
            Parcel(shelf, new Vector3(-.60f, 1.34f, 0), new Vector3(.86f, .53f, .63f), ToyGeometry.Hex("CDA477"));
            Parcel(shelf, new Vector3(.62f, 1.36f, 0), new Vector3(.82f, .57f, .64f), Teal);
            Parcel(shelf, new Vector3(-.58f, 2.14f, 0), new Vector3(.75f, .51f, .58f), Teal);
            Parcel(shelf, new Vector3(.58f, 2.15f, 0), new Vector3(.82f, .53f, .62f), ToyGeometry.Hex("D6AE7A"));
            return shelf;
        }

        static void Parcel(Transform parent, Vector3 p, Vector3 size, Color color)
        {
            ToyGeometry.Box(parent, "Stored parcel", p, size, color);
            ToyGeometry.Box(parent, "Packing tape", p + new Vector3(0, .005f, -.005f), new Vector3(.13f, size.y + .015f, size.z + .02f), Cream);
            ToyGeometry.Box(parent, "Shipping label", p + new Vector3(.20f, 0, -size.z * .5f - .015f), new Vector3(.20f, .17f, .02f), White);
        }

        Transform ParcelSign()
        {
            Transform sign = Group("Upgrade 6 - glowing parcel sign", new Vector3(.85f, 2.38f, 4.60f));
            Box(sign, "Sign backing", Vector3.zero, new Vector3(1.50f, 1.30f, .12f), DarkTeal);
            Color glow = ToyGeometry.Hex("FDE4A0");
            Box(sign, "Parcel sign top", new Vector3(0, .33f, -.09f), new Vector3(.95f, .075f, .06f), glow);
            Box(sign, "Parcel sign bottom", new Vector3(0, -.33f, -.09f), new Vector3(.95f, .075f, .06f), glow);
            Box(sign, "Parcel sign left", new Vector3(-.44f, 0, -.09f), new Vector3(.075f, .66f, .06f), glow);
            Box(sign, "Parcel sign right", new Vector3(.44f, 0, -.09f), new Vector3(.075f, .66f, .06f), glow);
            Box(sign, "Parcel tape neon", new Vector3(0, 0, -.10f), new Vector3(.075f, .66f, .06f), Coral);
            for (int i = 0; i < 3; i++)
                Box(sign, "Little speed ray", new Vector3(-.59f, .22f - i * .21f, -.09f), new Vector3(.15f, .035f, .05f), Cream);
            LocalLight("Parcel sign glow", new Vector3(0, 0, -.25f), new Color(.99f, .81f, .47f), .4f, 2.2f, sign);
            return sign;
        }

        Transform Armchair()
        {
            Transform chair = Group("Upgrade 7 - cozy courier armchair", new Vector3(-3.50f, .06f, -1.65f));
            chair.localRotation = Quaternion.Euler(0, -20, 0);
            Color lavender = ToyGeometry.Hex("BCAFD0");
            Color lightLavender = ToyGeometry.Hex("D0C1E0");
            foreach (float x in new[] { -.49f, .49f })
                foreach (float z in new[] { -.40f, .40f })
                    Cylinder(chair, "Chair wooden foot", new Vector3(x, .15f, z), new Vector3(.13f, .28f, .13f), Wood);
            Box(chair, "Chair soft base", new Vector3(0, .43f, 0), new Vector3(1.37f, .36f, 1.23f), lavender);
            ToyGeometry.Sphere(chair, "Chair seat cushion", new Vector3(0, .64f, -.08f), new Vector3(1.26f, .29f, 1.12f), lightLavender);
            Box(chair, "Chair back", new Vector3(0, 1.04f, .47f), new Vector3(1.48f, .96f, .28f), lavender);
            ToyGeometry.Sphere(chair, "Rounded chair back", new Vector3(0, 1.42f, .47f), new Vector3(1.49f, .31f, .30f), lavender);
            foreach (float x in new[] { -.64f, .64f })
                ToyGeometry.Capsule(chair, "Squishy armrest", new Vector3(x, .80f, -.03f), new Vector3(.30f, .46f, 1.23f), lightLavender);
            Transform pillow = Box(chair, "Little mint pillow", new Vector3(.13f, .96f, .19f), new Vector3(.60f, .49f, .20f), Teal);
            pillow.localRotation = Quaternion.Euler(-12, 0, 10);
            Box(chair, "Pillow cream stripe", new Vector3(.13f, .96f, .075f), new Vector3(.08f, .42f, .025f), Cream);
            Box(chair, "Folded soft blanket", new Vector3(-.38f, .79f, -.38f), new Vector3(.33f, .08f, .42f), Cream);
            Box(chair, "Blanket hanging edge", new Vector3(-.38f, .64f, -.59f), new Vector3(.33f, .30f, .055f), Cream);
            return chair;
        }

        Transform ParcelPoster()
        {
            Transform poster = Group("Upgrade 8 - parcel wall art", new Vector3(-5.74f, 2.72f, 3.60f));
            Box(poster, "Poster wood frame", Vector3.zero, new Vector3(.12f, 1.43f, 1.15f), Wood);
            Box(poster, "Poster cream paper", new Vector3(.075f, 0, 0), new Vector3(.035f, 1.27f, .99f), Cream);
            Box(poster, "Poster mint artwork", new Vector3(.10f, .10f, 0), new Vector3(.02f, .89f, .82f), ToyGeometry.Hex("B9DCC9"));
            Box(poster, "Poster parcel illustration", new Vector3(.122f, .13f, 0), new Vector3(.025f, .47f, .54f), Coral);
            Box(poster, "Poster parcel tape", new Vector3(.142f, .13f, 0), new Vector3(.016f, .47f, .08f), Cream);
            for (int i = 0; i < 3; i++)
                Box(poster, "Poster small caption stroke", new Vector3(.12f, -.44f + i * .055f, 0), new Vector3(.02f, .017f, .63f - i * .12f), DarkTeal);
            return poster;
        }

        Transform DroneStand()
        {
            Transform stand = Group("Upgrade 9 - drone charging stand", new Vector3(4.65f, .06f, -3.55f));
            Box(stand, "Charging cabinet foot", new Vector3(0, .12f, 0), new Vector3(1.02f, .23f, .85f), DarkTeal);
            Box(stand, "Cream charging cabinet", new Vector3(0, .58f, 0), new Vector3(.92f, .79f, .75f), Cream);
            Box(stand, "Cabinet front panel", new Vector3(0, .57f, -.39f), new Vector3(.72f, .54f, .035f), Teal);
            Box(stand, "Cabinet handle", new Vector3(0, .76f, -.42f), new Vector3(.30f, .035f, .04f), White);
            Cylinder(stand, "Charging landing pad", new Vector3(0, 1.04f, 0), new Vector3(1.24f, .12f, 1.24f), Teal);
            Cylinder(stand, "Cream charging pad inset", new Vector3(0, 1.11f, 0), new Vector3(1.04f, .025f, 1.04f), Cream);
            Box(stand, "Charging pad marking", new Vector3(0, 1.13f, 0), new Vector3(.61f, .014f, .09f), Coral);
            Box(stand, "Charging pad marking", new Vector3(0, 1.13f, 0), new Vector3(.09f, .014f, .61f), Coral);
            for (int i = 0; i < 3; i++)
                ToyGeometry.Sphere(stand, "Charge indicator", new Vector3(-.15f + i * .15f, .42f, -.425f), new Vector3(.07f, .07f, .025f), ToyGeometry.Hex("B8DB9C"));
            // The purchased object is the stand itself; helper ownership is controlled by game logic.
            Box(stand, "Charging cable", new Vector3(.57f, .14f, .05f), new Vector3(.055f, .10f, .49f), Ink);
            Box(stand, "Charging cable plug", new Vector3(.57f, .14f, -.25f), new Vector3(.13f, .10f, .14f), Ink);
            return stand;
        }

        // All artwork below is drawn from primitives. These are original workshop mascots,
        // with no downloaded pictures, character references, textures, or external assets.
        Transform SproutPalPoster()
        {
            Transform art = FramedArt("Upgrade 10 - original sprout pal", new Vector3(-5.70f, 2.68f, -4.12f), 1.08f, 1.42f, true, ToyGeometry.Hex("E6CBDF"));
            Color peach = ToyGeometry.Hex("F3B394");
            Color green = ToyGeometry.Hex("79B89A");
            ToyGeometry.Sphere(art, "Mascot ground shadow", new Vector3(0, -.37f, -.135f), new Vector3(.65f, .08f, .025f), ToyGeometry.Hex("C3A6C2"));
            ToyGeometry.Sphere(art, "Round original sprout body", new Vector3(0, -.03f, -.15f), new Vector3(.64f, .69f, .045f), peach);
            foreach (float side in new[] { -1f, 1f })
            {
                ToyGeometry.Sphere(art, "Little round foot", new Vector3(side * .17f, -.34f, -.17f), new Vector3(.19f, .12f, .035f), Coral);
                ToyGeometry.Sphere(art, "Friendly dot eye", new Vector3(side * .11f, .045f, -.19f), new Vector3(.045f, .065f, .022f), Ink);
                ToyGeometry.Sphere(art, "Peach cheek", new Vector3(side * .205f, -.06f, -.185f), new Vector3(.085f, .048f, .018f), Coral);
                Transform leaf = ToyGeometry.Sphere(art, "Leafy sprout hat", new Vector3(side * .12f, .36f, -.17f), new Vector3(.19f, .35f, .035f), green);
                leaf.localRotation = Quaternion.Euler(0, 0, side * -43);
                ArtStroke(art, "Leaf vein", new Vector2(0, .26f), new Vector2(side * .20f, .45f), .015f, DarkTeal, -.20f);
            }
            ToyGeometry.Sphere(art, "Tiny happy mouth", new Vector3(0, -.065f, -.194f), new Vector3(.065f, .035f, .022f), Ink);
            Box(art, "Mascot scarf", new Vector3(0, -.18f, -.185f), new Vector3(.48f, .065f, .025f), Teal);
            Transform scarf = Box(art, "Scarf end", new Vector3(.16f, -.27f, -.20f), new Vector3(.075f, .15f, .025f), Teal);
            scarf.localRotation = Quaternion.Euler(0, 0, -17);
            PosterCaption(art, -.52f, .62f);
            return art;
        }

        Transform CloudPalPoster()
        {
            Transform art = FramedArt("Upgrade 11 - original cloud pal", new Vector3(-5.70f, 2.75f, -2.91f), .96f, 1.27f, true, ToyGeometry.Hex("BBDCE0"));
            Color cloud = ToyGeometry.Hex("FFF9E5");
            ToyGeometry.Sphere(art, "Cloud pal lower body", new Vector3(0, .07f, -.15f), new Vector3(.64f, .35f, .04f), cloud);
            for (int i = 0; i < 3; i++)
                ToyGeometry.Sphere(art, "Fluffy cloud lobe", new Vector3(-.19f + i * .19f, .19f + (i == 1 ? .10f : 0), -.155f), new Vector3(.29f, .30f, .045f), cloud);
            foreach (float side in new[] { -1f, 1f })
            {
                ToyGeometry.Sphere(art, "Cloud friendly eye", new Vector3(side * .105f, .10f, -.19f), new Vector3(.04f, .06f, .02f), Ink);
                ToyGeometry.Sphere(art, "Cloud blush", new Vector3(side * .185f, .025f, -.18f), new Vector3(.075f, .045f, .02f), Coral);
                ArtStroke(art, "Little cloud arm", new Vector2(side * .24f, -.02f), new Vector2(side * .12f, -.19f), .035f, cloud, -.18f);
            }
            ToyGeometry.Sphere(art, "Cloud smile", new Vector3(0, .035f, -.19f), new Vector3(.05f, .025f, .02f), Ink);
            Box(art, "Cloud's little parcel", new Vector3(0, -.235f, -.19f), new Vector3(.29f, .24f, .04f), Coral);
            Box(art, "Cloud parcel tape", new Vector3(0, -.235f, -.22f), new Vector3(.045f, .24f, .018f), Cream);
            for (int i = 0; i < 3; i++)
                ToyGeometry.Sphere(art, "Playful cloud raindrop", new Vector3(-.22f + i * .22f, -.40f + (i == 1 ? -.025f : .025f), -.15f), new Vector3(.035f, .065f, .02f), Teal);
            PosterCaption(art, -.50f, .54f);
            return art;
        }

        Transform SkylineArt()
        {
            Transform art = FramedArt("Upgrade 12 - original Bosphorus panorama", new Vector3(3.45f, 3.19f, 4.66f), 2.82f, .66f, false, ToyGeometry.Hex("F1C4A7"));
            Color skyline = ToyGeometry.Hex("687E9A");
            ToyGeometry.Sphere(art, "Setting sun", new Vector3(-.37f, .105f, -.14f), new Vector3(.18f, .18f, .025f), ToyGeometry.Hex("FFE6AB"));
            Box(art, "Calm Bosphorus", new Vector3(0, -.145f, -.155f), new Vector3(2.60f, .13f, .022f), Teal);
            for (int i = 0; i < 9; i++)
            {
                float height = .045f + (i * 7 % 5) * .022f;
                Box(art, "Distant neighborhood silhouette", new Vector3(.43f + i * .095f, -.074f + height * .5f, -.17f), new Vector3(.085f, height, .02f), skyline);
            }
            Box(art, "Little coastal tower", new Vector3(1.02f, .055f, -.18f), new Vector3(.065f, .21f, .025f), skyline);
            ToyGeometry.Sphere(art, "Tower rounded roof", new Vector3(1.02f, .17f, -.18f), new Vector3(.10f, .07f, .025f), skyline);
            foreach (float x in new[] { -.98f, .20f })
                Box(art, "Suspension bridge tower", new Vector3(x, .025f, -.19f), new Vector3(.035f, .28f, .025f), Ink);
            Box(art, "Bridge deck", new Vector3(-.39f, -.055f, -.19f), new Vector3(1.72f, .025f, .025f), Ink);
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f, b = (i + 1) / 8f;
                Vector2 p = new Vector2(Mathf.Lerp(-.98f, .20f, a), .15f - Mathf.Sin(a * Mathf.PI) * .15f);
                Vector2 q = new Vector2(Mathf.Lerp(-.98f, .20f, b), .15f - Mathf.Sin(b * Mathf.PI) * .15f);
                ArtStroke(art, "Suspension cable", p, q, .014f, Ink, -.20f);
                ArtStroke(art, "Bridge vertical hanger", new Vector2(p.x, -.055f), p, .009f, Cream, -.21f);
            }
            Box(art, "Tiny ferry hull", new Vector3(.57f, -.16f, -.205f), new Vector3(.29f, .045f, .025f), Cream);
            Box(art, "Tiny ferry cabin", new Vector3(.57f, -.12f, -.21f), new Vector3(.17f, .035f, .025f), White);
            Box(art, "Tiny ferry chimney", new Vector3(.54f, -.087f, -.21f), new Vector3(.025f, .035f, .025f), Ink);
            ArtStroke(art, "Ferry's soft wake", new Vector2(.79f, -.17f), new Vector2(.97f, -.17f), .012f, Cream, -.19f);
            return art;
        }

        Transform CarefulCourierTrophy()
        {
            Transform trophy = Group("Upgrade 13 - nervous parcel trophy", new Vector3(3.04f, 1.17f, 3.73f));
            Color gold = ToyGeometry.Hex("E4BE75");
            Box(trophy, "Trophy walnut base", new Vector3(0, .06f, 0), new Vector3(.59f, .12f, .46f), Ink);
            Box(trophy, "Cream trophy plaque", new Vector3(0, .065f, -.24f), new Vector3(.37f, .055f, .018f), Cream);
            Cylinder(trophy, "Trophy pedestal", new Vector3(0, .23f, 0), new Vector3(.13f, .26f, .13f), gold);
            ToyGeometry.Sphere(trophy, "Trophy soft gold support", new Vector3(0, .36f, 0), new Vector3(.36f, .10f, .30f), gold);
            Box(trophy, "Prize parcel body", new Vector3(0, .57f, 0), new Vector3(.42f, .38f, .34f), gold);
            Box(trophy, "Prize parcel tape", new Vector3(0, .57f, 0), new Vector3(.07f, .39f, .35f), Cream);
            foreach (float side in new[] { -1f, 1f })
            {
                ToyGeometry.Sphere(trophy, "Nervous trophy eye", new Vector3(side * .11f, .60f, -.182f), new Vector3(.035f, .055f, .018f), Ink);
                Transform handle = ToyGeometry.Sphere(trophy, "Trophy curled handle", new Vector3(side * .27f, .50f, .02f), new Vector3(.18f, .26f, .10f), gold);
                ToyGeometry.Sphere(handle, "Handle hollow", new Vector3(0, 0, -.49f), new Vector3(.53f, .61f, .30f), Cream);
            }
            ArtStroke(trophy, "Wobbly smile left", new Vector2(-.06f, .49f), new Vector2(0, .52f), .018f, Ink, -.19f);
            ArtStroke(trophy, "Wobbly smile right", new Vector2(0, .52f), new Vector2(.06f, .49f), .018f, Ink, -.19f);
            Transform bandage = Box(trophy, "Tiny honorary bandage", new Vector3(.13f, .73f, -.182f), new Vector3(.15f, .045f, .02f), Coral);
            bandage.localRotation = Quaternion.Euler(0, 0, -25);
            return trophy;
        }

        Transform ParcelVendingMachine()
        {
            Transform machine = Group("Upgrade 14 - parcels only vending machine", new Vector3(4.65f, .06f, 2.40f));
            Color blush = ToyGeometry.Hex("DE9C99");
            Box(machine, "Vending machine base", new Vector3(0, .12f, 0), new Vector3(1.14f, .23f, .90f), Ink);
            Box(machine, "Vending machine rounded cabinet", new Vector3(0, 1.0f, 0), new Vector3(1.06f, 1.62f, .82f), Teal);
            ToyGeometry.Sphere(machine, "Rounded vending machine cap", new Vector3(0, 1.80f, 0), new Vector3(1.08f, .21f, .83f), Teal);
            Box(machine, "Cream front fascia", new Vector3(0, 1.01f, -.427f), new Vector3(.91f, 1.44f, .04f), Cream);
            Box(machine, "Parcel display shadow", new Vector3(-.10f, 1.19f, -.46f), new Vector3(.59f, .72f, .035f), DarkTeal);
            for (int row = 0; row < 2; row++)
            {
                Box(machine, "Display shelf", new Vector3(-.10f, .88f + row * .35f, -.49f), new Vector3(.59f, .035f, .12f), Wood);
                for (int column = 0; column < 2; column++)
                {
                    Vector3 p = new Vector3(-.25f + column * .29f, 1.03f + row * .35f, -.51f);
                    Box(machine, "Mini vending parcel", p, new Vector3(.21f, .23f, .12f), (row + column) % 2 == 0 ? blush : Wood);
                    Box(machine, "Mini vending parcel tape", p + new Vector3(0, 0, -.065f), new Vector3(.035f, .23f, .018f), Cream);
                }
            }
            Box(machine, "Friendly parcel machine marquee", new Vector3(0, 1.62f, -.46f), new Vector3(.75f, .14f, .035f), blush);
            Box(machine, "Marquee parcel symbol", new Vector3(0, 1.62f, -.49f), new Vector3(.18f, .09f, .018f), Cream);
            Box(machine, "Marquee tape symbol", new Vector3(0, 1.62f, -.505f), new Vector3(.027f, .09f, .012f), DarkTeal);
            Box(machine, "Coin slot", new Vector3(.325f, 1.30f, -.46f), new Vector3(.035f, .18f, .025f), Ink);
            for (int i = 0; i < 2; i++)
                ToyGeometry.Sphere(machine, "Parcel choice button", new Vector3(.325f, 1.02f - i * .18f, -.49f), new Vector3(.10f, .10f, .055f), i == 0 ? blush : Teal);
            Box(machine, "Delivery hatch dark opening", new Vector3(0, .54f, -.46f), new Vector3(.68f, .25f, .035f), Ink);
            Box(machine, "Delivery hatch lip", new Vector3(0, .40f, -.51f), new Vector3(.74f, .055f, .19f), blush);
            Box(machine, "Decorative out-of-order note", new Vector3(.28f, .62f, -.49f), new Vector3(.19f, .15f, .02f), White);
            ArtStroke(machine, "Note crossed parcel mark", new Vector2(.23f, .57f), new Vector2(.33f, .67f), .02f, Coral, -.51f);
            return machine;
        }

        Transform FramedArt(string name, Vector3 position, float width, float height, bool leftWall, Color background)
        {
            Transform art = Group(name, position);
            if (leftWall) art.localRotation = Quaternion.Euler(0, -90, 0);
            Box(art, "Solid wooden art frame", Vector3.zero, new Vector3(width, height, .10f), Wood);
            Box(art, "Cream art mat", new Vector3(0, 0, -.065f), new Vector3(width - .09f, height - .09f, .035f), White);
            Box(art, "Original illustration background", new Vector3(0, 0, -.095f), new Vector3(width - .20f, height - .20f, .025f), background);
            return art;
        }

        static void PosterCaption(Transform art, float y, float width)
        {
            Box(art, "Hand-drawn caption line", new Vector3(0, y, -.135f), new Vector3(width, .022f, .015f), DarkTeal);
            Box(art, "Hand-drawn caption underline", new Vector3(0, y - .055f, -.135f), new Vector3(width * .60f, .012f, .015f), Teal);
        }

        static void ArtStroke(Transform art, string name, Vector2 a, Vector2 b, float thickness, Color color, float depth)
        {
            Vector2 delta = b - a, middle = (a + b) * .5f;
            Transform stroke = Box(art, name, new Vector3(middle.x, middle.y, depth), new Vector3(delta.magnitude, thickness, .018f), color);
            stroke.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        static void LocalLight(string name, Vector3 position, Color color, float intensity, float range, Transform parent)
        {
            Transform t = ToyGeometry.Group(parent, name, position);
            Light light = t.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        Transform Group(string name, Vector3 position) => ToyGeometry.Group(transform, name, position);
        static Transform Box(Transform parent, string name, Vector3 position, Vector3 size, Color color)
            => ToyGeometry.Box(parent, name, position, size, color);
        static Transform Cylinder(Transform parent, string name, Vector3 position, Vector3 size, Color color)
            => ToyGeometry.Cylinder(parent, name, position, size, color);
    }
}
