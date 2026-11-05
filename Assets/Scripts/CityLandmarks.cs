using System;
using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>Original little city scenery. CourierGame supplies the only simulation clock.</summary>
    public sealed partial class CityLandmarks : MonoBehaviour
    {
        public static readonly Vector3 TownHallPosition = new Vector3(-12.5f, 0, -12);
        public static readonly Vector3 FactoryPosition = new Vector3(8, 0, -19.25f);
        public static readonly Vector3 FootballPosition = new Vector3(-12, 0, 12.2f);
        public static readonly Vector3 FountainPosition = new Vector3(-10.2f, 0, -12);
        public float Clock { get; private set; }
        public int GoalCount { get; private set; }
        public int PlayerCount => players.Length;
        public event Action<Vector3, string, string> Speech;

        static readonly Color Cream = ToyGeometry.Hex("FFF1D5");
        static readonly Color White = ToyGeometry.Hex("FFFCF0");
        static readonly Color Ink = ToyGeometry.Hex("405C68");
        static readonly Color Teal = ToyGeometry.Hex("78B6AD");
        static readonly Color Coral = ToyGeometry.Hex("DA9787");
        static readonly Color Glass = ToyGeometry.Hex("87B9C7");
        static readonly Color Wood = ToyGeometry.Hex("BD8E6C");
        readonly Neighbour[] players = new Neighbour[6];
        readonly Vector3[] playerHomes = {
            new Vector3(-1.55f, 0, -1.75f), new Vector3(.95f, 0, -1.05f),
            new Vector3(-1.15f, 0, .55f), new Vector3(1.5f, 0, 1.45f),
            new Vector3(.15f, 0, 2.0f), new Vector3(.15f, 0, -2.05f)
        };
        readonly Transform[] fountainDrops = new Transform[12];
        readonly Transform[] chimneyPuffs = new Transform[8];
        readonly Transform[] confetti = new Transform[18];
        readonly Label[] labels = new Label[16];
        readonly Neighbour[] neighbours = new Neighbour[2];
        Transform architecture, minuteHand, hourHand, flag, ball, ballShadow, goalBadge;
        int labelCount;
        bool labelsTurkish;
        float nextGoal = 13, nextSpeech = 7, celebrationLeft;
        int speechIndex;

        sealed class Label
        {
            public TextMesh text;
            public string tr, en;
        }

        sealed class Neighbour
        {
            public Transform root, body, leftArm, rightArm, leftLeg, rightLeg;
            public Vector3 home;
        }

        public static CityLandmarks Create(Transform staticParent)
        {
            var city = new GameObject("Living city - explicitly ticked").AddComponent<CityLandmarks>();
            city.architecture = ToyGeometry.Group(staticParent, "City landmark architecture", Vector3.zero);
            city.BuildTownHall();
            city.BuildFactory();
            city.BuildFootball();
            city.BuildNeighbours();
            city.BuildStreetLife();
            city.labelsTurkish = GameText.Turkish;
            city.Pose(true);
            return city;
        }

        // Sign geometry joins the static neighborhood; mutable lettering stays outside its batch.
        public void DecorateShop(Transform shop, int kind)
        {
            Color accent = kind == 1 ? Coral : kind == 2 ? ToyGeometry.Hex("6DAF83") : ToyGeometry.Hex("83A9C5");
            Box(shop, "Shop sign surround", new Vector3(0, 2.10f, -1.46f), new Vector3(2.25f, .42f, .12f), accent);
            Box(shop, "Shop sign face", new Vector3(0, 2.10f, -1.535f), new Vector3(2.10f, .32f, .04f), Cream);
            string tr = kind == 1 ? "FIRIN" : kind == 2 ? "MANAV" : "BERBER";
            string en = kind == 1 ? "BAKERY" : kind == 2 ? "GREENGROCER" : "BARBER";
            AddLabel(shop.TransformPoint(new Vector3(0, 2.10f, -1.565f)), shop.rotation, tr, en, kind == 2 ? .035f : .048f);
            if (kind == 1)
            {
                Transform tray = Box(shop, "Bakery window tray", new Vector3(-1.02f, .94f, -1.72f), new Vector3(.80f, .10f, .37f), Wood);
                for (int i = 0; i < 3; i++)
                {
                    Transform loaf = ToyGeometry.Capsule(tray, "Fresh bread display", new Vector3(-.24f + i * .24f, .12f, 0), new Vector3(.16f, .31f, .16f), ToyGeometry.Hex("E6B86D"));
                    loaf.localRotation = Quaternion.Euler(90, 0, 16);
                }
            }
            else if (kind == 2)
            {
                for (int row = 0; row < 2; row++)
                {
                    Box(shop, "Greengrocer wooden produce tray", new Vector3(.98f, .52f + row * .28f, -1.72f), new Vector3(.75f, .11f, .38f), Wood);
                    for (int i = 0; i < 3; i++)
                        ToyGeometry.Sphere(shop, "Round market fruit", new Vector3(.75f + i * .23f, .64f + row * .28f, -1.73f), new Vector3(.19f, .19f, .19f), row == 0 ? ToyGeometry.Hex("E8AC68") : ToyGeometry.Hex("9AC272"));
                }
            }
            else
            {
                ToyGeometry.Cylinder(shop, "Barber pole", new Vector3(1.48f, 1.30f, -1.60f), new Vector3(.21f, .95f, .21f), Cream);
                for (int i = 0; i < 5; i++)
                    ToyGeometry.Cylinder(shop, "Barber pole colour band", new Vector3(1.48f, .94f + i * .17f, -1.60f), new Vector3(.216f, .07f, .216f), i % 2 == 0 ? Coral : Glass);
                ToyGeometry.Sphere(shop, "Barber pole cap", new Vector3(1.48f, 1.82f, -1.60f), Vector3.one * .24f, Cream);
            }
        }

        public void Tick(float dt, bool playing, bool reducedMotion, Vector3 courierPosition, float courierMargin)
        {
            if (!playing || !Finite(dt) || dt <= 0) return;
            Clock += Mathf.Min(dt, 60);
            if (labelsTurkish != GameText.Turkish)
            {
                labelsTurkish = GameText.Turkish;
                for (int i = 0; i < labelCount; i++) labels[i].text.text = GameText.Pick(labels[i].tr, labels[i].en);
            }
            celebrationLeft = Mathf.Max(0, celebrationLeft - dt);
            if (Clock >= nextGoal)
            {
                nextGoal = Clock + 22;
                GoalCount++;
                celebrationLeft = 2.0f;
                Speech?.Invoke(FootballPosition + new Vector3(.4f, 1.8f, .6f), "Gooool! Mahalle kupası bizim!", "Gooal! The neighborhood cup is ours!");
            }
            if (Clock >= nextSpeech)
            {
                nextSpeech = Clock + 19;
                if ((speechIndex++ & 1) == 0)
                    Speech?.Invoke(neighbours[0].root.position + Vector3.up * 1.8f, "Saat tam vaktinde. Kargo da öyle!", "The clock is on time. So is our courier!");
                else
                    Speech?.Invoke(neighbours[1].root.position + Vector3.up * 1.8f, "Bu kutuların hepsi bir yere gidiyor!", "Every little box is going somewhere!");
            }
            Pose(reducedMotion);
            TickStreetLife(Mathf.Min(dt, .1f), courierPosition, courierMargin, reducedMotion);
        }

        void Pose(bool reducedMotion)
        {
            minuteHand.localRotation = Quaternion.Euler(0, 0, -Clock * 1.8f - 36);
            hourHand.localRotation = Quaternion.Euler(0, 0, -Clock * .15f - 306);
            flag.localRotation = Quaternion.Euler(0, reducedMotion ? 0 : Mathf.Sin(Clock * 1.5f) * 8, reducedMotion ? 0 : Mathf.Sin(Clock * 2) * 3);
            for (int i = 0; i < fountainDrops.Length; i++)
            {
                fountainDrops[i].gameObject.SetActive(!reducedMotion);
                if (reducedMotion) continue;
                float t = Mathf.Repeat(Clock * .64f + i / (float)fountainDrops.Length, 1);
                float angle = i * 2.39996f;
                float radius = .12f + t * .34f;
                fountainDrops[i].localPosition = new Vector3(Mathf.Cos(angle) * radius, .48f + Mathf.Sin(t * Mathf.PI) * .85f, Mathf.Sin(angle) * radius);
                fountainDrops[i].localScale = new Vector3(.055f, .09f + .035f * Mathf.Sin(t * Mathf.PI), .055f);
            }
            for (int i = 0; i < chimneyPuffs.Length; i++)
            {
                chimneyPuffs[i].gameObject.SetActive(!reducedMotion);
                if (reducedMotion) continue;
                float t = Mathf.Repeat(Clock * .16f + i * .25f, 1);
                chimneyPuffs[i].localPosition = new Vector3((i < 4 ? -.92f : .92f) + t * .48f, 4.05f + t * 1.55f, .44f + t * .28f);
                chimneyPuffs[i].localScale = Vector3.one * Mathf.Lerp(.18f, .60f, t) * Mathf.Min(1, (1 - t) * 5);
            }
            bool celebrating = celebrationLeft > 0;
            for (int i = 0; i < players.Length; i++)
            {
                Neighbour person = players[i];
                float cycle = Clock * .68f + i * 1.6f;
                Vector3 offset = reducedMotion ? Vector3.zero : new Vector3(Mathf.Sin(cycle) * .32f, 0, Mathf.Cos(cycle * .8f) * .23f);
                person.root.localPosition = playerHomes[i] + offset;
                Vector3 look = new Vector3(ball.localPosition.x, 0, ball.localPosition.z) - person.root.localPosition;
                if (look.sqrMagnitude > .01f) person.root.localRotation = Quaternion.LookRotation(look);
                AnimateNeighbour(person, reducedMotion ? 0 : Mathf.Sin(Clock * 5 + i) * 15, celebrating && i < 4, reducedMotion);
            }
            float phase = Mathf.Repeat(Clock, 22);
            if (reducedMotion) ball.localPosition = new Vector3(.15f, .18f, .1f);
            else if (celebrating) ball.localPosition = new Vector3(.20f, .18f, 2.20f);
            else
            {
                float t = Mathf.PingPong(phase * .18f, 1);
                ball.localPosition = Vector3.Lerp(new Vector3(-1.30f, .18f, -1.45f), new Vector3(1.15f, .18f, 1.60f), t);
                ball.localPosition += Vector3.up * Mathf.Abs(Mathf.Sin(Clock * 4)) * .11f;
                ball.localRotation = Quaternion.Euler(Clock * 160, Clock * 85, 0);
            }
            ballShadow.localPosition = new Vector3(ball.localPosition.x, .075f, ball.localPosition.z);
            goalBadge.gameObject.SetActive(celebrating);
            goalBadge.localScale = Vector3.one * (reducedMotion ? 1 : 1 + Mathf.Sin(Clock * 6) * .045f);
            for (int i = 0; i < confetti.Length; i++)
            {
                confetti[i].gameObject.SetActive(celebrating && !reducedMotion);
                if (!celebrating || reducedMotion) continue;
                float t = 1 - celebrationLeft / 2;
                float angle = i * 2.39996f;
                confetti[i].localPosition = new Vector3(Mathf.Cos(angle) * t * 1.65f, .6f + Mathf.Sin(t * Mathf.PI) * (1.0f + (i % 3) * .3f), 1.8f + Mathf.Sin(angle) * t * .75f);
                confetti[i].localRotation = Quaternion.Euler(Clock * 90 + i * 21, i * 32, Clock * 110);
            }
            for (int i = 0; i < neighbours.Length; i++)
                AnimateNeighbour(neighbours[i], reducedMotion ? 0 : Mathf.Sin(Clock * 1.7f + i) * 2, false, reducedMotion);
        }

        static void AnimateNeighbour(Neighbour person, float stride, bool cheer, bool reducedMotion)
        {
            person.leftLeg.localRotation = Quaternion.Euler(stride, 0, 0);
            person.rightLeg.localRotation = Quaternion.Euler(-stride, 0, 0);
            person.leftArm.localRotation = Quaternion.Euler(-stride * .5f, 0, cheer ? -145 : -9);
            person.rightArm.localRotation = Quaternion.Euler(stride * .5f, 0, cheer ? 145 : 9);
            person.body.localPosition = new Vector3(0, cheer && !reducedMotion ? Mathf.Abs(Mathf.Sin(stride * .15f)) * .07f : 0, 0);
        }

        void BuildTownHall()
        {
            Transform hall = Group(architecture, "Pastel town hall", TownHallPosition, -90);
            Box(hall, "Town hall plaza paving", new Vector3(0, .024f, -.45f), new Vector3(5, .035f, 5), ToyGeometry.Hex("E2D6BE"));
            Solid(hall, "Town hall main body", new Vector3(0, 1.21f, 0), new Vector3(4.0f, 2.42f, 2.6f), ToyGeometry.Hex("EAD9AD"));
            Box(hall, "Town hall lower stone band", new Vector3(0, .25f, 0), new Vector3(4.10f, .25f, 2.7f), Cream);
            Box(hall, "Town hall cornice", new Vector3(0, 2.40f, 0), new Vector3(4.22f, .24f, 2.85f), Cream);
            Box(hall, "Town hall teal roof", new Vector3(0, 2.57f, 0), new Vector3(4.30f, .17f, 2.95f), Teal);
            for (int i = 0; i < 4; i++)
            {
                float x = -1.50f + i;
                ToyGeometry.Cylinder(hall, "Town hall symmetrical column", new Vector3(x, 1.12f, -1.59f), new Vector3(.22f, 2.04f, .22f), White);
                Box(hall, "Column square capital", new Vector3(x, 2.12f, -1.59f), new Vector3(.34f, .17f, .34f), Cream);
                Box(hall, "Column stone base", new Vector3(x, .17f, -1.59f), new Vector3(.34f, .19f, .34f), Cream);
            }
            Box(hall, "Town hall portico lintel", new Vector3(0, 2.28f, -1.57f), new Vector3(3.85f, .22f, .55f), White);
            Box(hall, "Town hall porch step", new Vector3(0, .10f, -1.70f), new Vector3(3.8f, .18f, .85f), Cream);
            Box(hall, "Town hall double entrance", new Vector3(0, 1.00f, -1.326f), new Vector3(1.12f, 1.75f, .06f), Teal);
            Box(hall, "Town hall door split", new Vector3(0, .99f, -1.37f), new Vector3(.05f, 1.71f, .035f), Cream);
            for (int i = -1; i <= 1; i += 2)
            {
                Box(hall, "Town hall tall window frame", new Vector3(i * 1.38f, 1.18f, -1.33f), new Vector3(.62f, 1.20f, .06f), Cream);
                Box(hall, "Town hall blue glass", new Vector3(i * 1.38f, 1.18f, -1.37f), new Vector3(.48f, 1.04f, .035f), Glass);
                Box(hall, "Town hall window mullion", new Vector3(i * 1.38f, 1.18f, -1.40f), new Vector3(.04f, 1.04f, .018f), Cream);
            }
            Box(hall, "Central clock tower", new Vector3(0, 3.63f, .25f), new Vector3(1.35f, 2.24f, 1.35f), ToyGeometry.Hex("F4E5BE"));
            Box(hall, "Clock tower stone belt", new Vector3(0, 3.35f, .25f), new Vector3(1.45f, .12f, 1.45f), Cream);
            Box(hall, "Clock tower roof", new Vector3(0, 4.80f, .25f), new Vector3(1.65f, .20f, 1.65f), Teal);
            ToyGeometry.Sphere(hall, "Clock face dark rim", new Vector3(0, 4.05f, -.45f), new Vector3(1.04f, 1.04f, .085f), Teal);
            ToyGeometry.Sphere(hall, "Clock face", new Vector3(0, 4.05f, -.50f), new Vector3(.92f, .92f, .045f), White);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI / 6;
                Transform mark = Box(hall, "Clock hour mark", new Vector3(Mathf.Sin(angle) * .37f, 4.05f + Mathf.Cos(angle) * .37f, -.532f), new Vector3(.035f, .075f, .02f), Ink);
                mark.localRotation = Quaternion.Euler(0, 0, -i * 30);
            }
            Transform movingHall = Group(transform, "Town hall animated details", TownHallPosition, -90);
            minuteHand = Group(movingHall, "Clock minute hand pivot", new Vector3(0, 4.05f, -.558f));
            Box(minuteHand, "Clock minute hand", new Vector3(0, .155f, 0), new Vector3(.035f, .35f, .024f), Ink);
            hourHand = Group(movingHall, "Clock hour hand pivot", new Vector3(0, 4.05f, -.58f));
            Box(hourHand, "Clock hour hand", new Vector3(0, .105f, 0), new Vector3(.055f, .25f, .025f), Ink);
            ToyGeometry.Sphere(movingHall, "Clock pin", new Vector3(0, 4.05f, -.61f), new Vector3(.09f, .09f, .035f), Coral);
            ToyGeometry.Cylinder(hall, "Town flag pole", new Vector3(.53f, 5.40f, .25f), new Vector3(.055f, 1.2f, .055f), Cream);
            ToyGeometry.Sphere(hall, "Town flag pole finial", new Vector3(.53f, 6.03f, .25f), Vector3.one * .13f, ToyGeometry.Hex("ECC477"));
            flag = Group(movingHall, "Soft municipal flag", new Vector3(.53f, 5.85f, .25f));
            Box(flag, "Mint flag", new Vector3(.34f, -.10f, 0), new Vector3(.67f, .35f, .035f), Teal);
            Box(flag, "Cream flag stripe", new Vector3(.34f, -.10f, -.022f), new Vector3(.67f, .08f, .012f), Cream);
            AddLabel(hall.TransformPoint(new Vector3(0, 2.29f, -1.86f)), hall.rotation, "BELEDİYE", "TOWN HALL", .049f);

            Transform basin = Group(architecture, "Town hall fountain", FountainPosition);
            ToyGeometry.Cylinder(basin, "Fountain lower stone bank", new Vector3(0, .12f, 0), new Vector3(1.20f, .24f, 1.20f), ToyGeometry.Hex("D0C5B0"));
            ToyGeometry.Cylinder(basin, "Fountain cream bank", new Vector3(0, .27f, 0), new Vector3(1.08f, .13f, 1.08f), Cream);
            ToyGeometry.Cylinder(basin, "Fountain turquoise water", new Vector3(0, .345f, 0), new Vector3(.91f, .025f, .91f), ToyGeometry.Hex("7DC8CD"));
            ToyGeometry.Cylinder(basin, "Fountain central stem", new Vector3(0, .56f, 0), new Vector3(.13f, .5f, .13f), Cream);
            ToyGeometry.Cylinder(basin, "Fountain upper bowl", new Vector3(0, .78f, 0), new Vector3(.44f, .12f, .44f), Cream);
            ToyGeometry.Sphere(basin, "Fountain steady water plume", new Vector3(0, 1.02f, 0), new Vector3(.07f, .42f, .07f), ToyGeometry.Hex("B0E2E0"));
            Transform water = Group(transform, "Pooled fountain water", FountainPosition);
            for (int i = 0; i < fountainDrops.Length; i++)
                fountainDrops[i] = ToyGeometry.Sphere(water, "Fountain reusable water bead", Vector3.zero, Vector3.one * .07f, ToyGeometry.Hex("B0E2E0"));
            Bench(new Vector3(-10.25f, 0, -10.25f), 90);
            Bench(new Vector3(-10.25f, 0, -13.85f), 90);
        }

        void BuildFactory()
        {
            Transform factory = Group(architecture, "Parcel factory", FactoryPosition, 180);
            Box(factory, "Factory cream foundation", new Vector3(0, .13f, 0), new Vector3(3.5f, .25f, 2.9f), Cream);
            // Exactly the previous destination house's footprint: saved approaches stay clear.
            Solid(factory, "Parcel factory main body", new Vector3(0, 1.18f, 0), new Vector3(3.25f, 2.05f, 2.65f), ToyGeometry.Hex("B8CCD0"));
            Box(factory, "Factory brick lower band", new Vector3(0, .45f, 0), new Vector3(3.30f, .45f, 2.70f), ToyGeometry.Hex("C98F78"));
            Box(factory, "Factory roof rim", new Vector3(0, 2.25f, 0), new Vector3(3.6f, .20f, 2.95f), Teal);
            for (int i = 0; i < 3; i++)
            {
                Transform roof = Box(factory, "Factory sawtooth roof section", new Vector3(-1.15f + i * 1.15f, 2.48f, .05f), new Vector3(1.24f, .13f, 2.78f), ToyGeometry.Hex("739A9B"));
                roof.localRotation = Quaternion.Euler(0, 0, 16);
                Box(factory, "Factory clerestory roof light", new Vector3(-.66f + i * 1.15f, 2.43f, -.72f), new Vector3(.12f, .25f, .75f), Glass);
            }
            for (int i = -1; i <= 1; i += 2)
            {
                ToyGeometry.Cylinder(factory, "Factory tall brick chimney", new Vector3(i * .92f, 3.05f, .44f), new Vector3(.43f, 1.95f, .43f), ToyGeometry.Hex("CB927C"));
                for (int band = 0; band < 4; band++)
                    ToyGeometry.Cylinder(factory, "Factory chimney stone ring", new Vector3(i * .92f, 2.4f + band * .47f, .44f), new Vector3(.47f, .09f, .47f), Cream);
                ToyGeometry.Cylinder(factory, "Factory chimney rim", new Vector3(i * .92f, 4.05f, .44f), new Vector3(.54f, .14f, .54f), ToyGeometry.Hex("A97768"));
                Box(factory, "Factory window frame", new Vector3(i * 1.12f, 1.38f, -1.35f), new Vector3(.63f, .82f, .08f), Cream);
                Box(factory, "Factory blue window", new Vector3(i * 1.12f, 1.38f, -1.40f), new Vector3(.49f, .66f, .035f), Glass);
                Box(factory, "Factory window crossbar", new Vector3(i * 1.12f, 1.38f, -1.43f), new Vector3(.04f, .66f, .02f), Cream);
            }
            Box(factory, "Factory loading door frame", new Vector3(0, .94f, -1.36f), new Vector3(1.15f, 1.67f, .10f), Cream);
            Box(factory, "Factory teal loading door", new Vector3(0, .92f, -1.43f), new Vector3(.98f, 1.48f, .07f), Teal);
            for (int i = 0; i < 5; i++) Box(factory, "Factory loading shutter slat", new Vector3(0, .38f + i * .25f, -1.48f), new Vector3(.90f, .03f, .018f), Ink);
            Box(factory, "Factory canopy", new Vector3(0, 1.92f, -1.65f), new Vector3(1.60f, .13f, .66f), Coral);
            Box(factory, "Factory label board", new Vector3(0, 2.75f, -1.47f), new Vector3(2.15f, .49f, .10f), Cream);
            AddLabel(factory.TransformPoint(new Vector3(0, 2.75f, -1.54f)), factory.rotation, "PAKET FABRİKASI", "PARCEL FACTORY", .033f);
            // A second little roof label faces the camera side of this north-facing address.
            AddLabel(factory.TransformPoint(new Vector3(0, 2.76f, 1.53f)), factory.rotation * Quaternion.Euler(0, 180, 0), "FABRİKA", "FACTORY", .045f);
            Box(factory, "Factory rear label board", new Vector3(0, 2.76f, 1.47f), new Vector3(1.65f, .45f, .10f), Cream);
            Box(factory, "Factory delivery welcome step", new Vector3(0, .10f, -1.72f), new Vector3(1.25f, .18f, .58f), Cream);
            Box(factory, "Factory door path", new Vector3(0, .045f, -2.33f), new Vector3(1.40f, .035f, .83f), ToyGeometry.Hex("E5D8BD"));
            Transform smoke = Group(transform, "Pooled factory steam", FactoryPosition, 180);
            for (int i = 0; i < chimneyPuffs.Length; i++)
                chimneyPuffs[i] = ToyGeometry.Sphere(smoke, "Reusable soft steam puff", Vector3.zero, Vector3.one * .3f, ToyGeometry.Hex("E6E9DA"));
        }

        void BuildFootball()
        {
            Transform pitch = Group(architecture, "Neighborhood five-a-side pitch", FootballPosition);
            Box(pitch, "Football grass inset", new Vector3(0, .04f, 0), new Vector3(5.0f, .065f, 4.8f), ToyGeometry.Hex("78B894"));
            for (int i = 0; i < 6; i++)
                Box(pitch, "Mown football grass band", new Vector3(0, .078f, -2.2f + i * .88f), new Vector3(4.80f, .012f, .44f), ToyGeometry.Hex("80BF9B"));
            for (int i = -1; i <= 1; i += 2)
            {
                Box(pitch, "Football touchline", new Vector3(i * 2.32f, .09f, 0), new Vector3(.035f, .014f, 4.85f), Cream);
                Box(pitch, "Football goal line", new Vector3(0, .09f, i * 2.25f), new Vector3(4.65f, .014f, .035f), Cream);
                Goal(pitch, i * 2.24f);
                Box(pitch, "Penalty area front", new Vector3(0, .092f, i * 1.52f), new Vector3(2.10f, .015f, .035f), Cream);
                for (int side = -1; side <= 1; side += 2)
                    Box(pitch, "Penalty area side", new Vector3(side * 1.05f, .092f, i * 1.96f), new Vector3(.035f, .015f, .88f), Cream);
            }
            Box(pitch, "Football halfway line", new Vector3(0, .091f, 0), new Vector3(4.65f, .015f, .035f), Cream);
            for (int i = 0; i < 20; i++)
            {
                float angle = i * Mathf.PI * 2 / 20;
                Transform line = Box(pitch, "Centre circle painted segment", new Vector3(Mathf.Sin(angle) * .59f, .093f, Mathf.Cos(angle) * .59f), new Vector3(.19f, .014f, .035f), Cream);
                line.localRotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);
            }
            Transform game = Group(transform, "Football neighbors and ball", FootballPosition);
            for (int i = 0; i < players.Length; i++)
            {
                players[i] = Adult(game, "Adult neighborhood football player " + (i + 1), playerHomes[i], i < 3 ? Coral : Teal, i);
                players[i].root.localScale = Vector3.one * .84f;
            }
            ballShadow = ToyGeometry.Sphere(game, "Football soft ground shadow", new Vector3(0, .075f, 0), new Vector3(.34f, .012f, .25f), ToyGeometry.Hex("62977F"));
            ball = ToyGeometry.Sphere(game, "Neighborhood football", new Vector3(.15f, .18f, .1f), Vector3.one * .27f, White);
            for (int i = 0; i < 6; i++)
            {
                Vector3 direction = i < 2 ? (i == 0 ? Vector3.up : Vector3.down) : i < 4 ? (i == 2 ? Vector3.left : Vector3.right) : (i == 4 ? Vector3.forward : Vector3.back);
                ToyGeometry.Sphere(ball, "Football dark panel", direction * .47f, Vector3.one * .30f, Ink);
            }
            goalBadge = Group(game, "Reusable goal celebration badge", new Vector3(0, 2.55f, .45f));
            goalBadge.localRotation = Quaternion.Euler(49, -39, 0);
            ToyGeometry.Sphere(goalBadge, "Goal badge cream bubble", Vector3.zero, new Vector3(1.10f, .58f, .11f), Cream);
            // A ball-and-stars badge is readable without relying on untranslated text.
            ToyGeometry.Sphere(goalBadge, "Goal badge football", new Vector3(0, 0, -.085f), new Vector3(.29f, .29f, .05f), White);
            ToyGeometry.Sphere(goalBadge, "Goal badge football panel", new Vector3(0, 0, -.12f), new Vector3(.11f, .11f, .022f), Ink);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(goalBadge, "Goal sparkle vertical", new Vector3(side * .35f, .01f, -.08f), new Vector3(.045f, .19f, .025f), ToyGeometry.Hex("ECC477"));
                Box(goalBadge, "Goal sparkle horizontal", new Vector3(side * .35f, .01f, -.08f), new Vector3(.17f, .045f, .025f), ToyGeometry.Hex("ECC477"));
            }
            for (int i = 0; i < confetti.Length; i++)
                confetti[i] = Box(game, "Reusable football celebration confetti", Vector3.zero, new Vector3(.06f, .10f, .018f), i % 3 == 0 ? Coral : i % 3 == 1 ? Teal : ToyGeometry.Hex("ECC477"));
            Box(pitch, "Pitch neighborhood sign", new Vector3(-1.50f, .66f, -2.64f), new Vector3(1.18f, .44f, .08f), Cream);
            Box(pitch, "Pitch sign post", new Vector3(-1.50f, .30f, -2.64f), new Vector3(.07f, .60f, .07f), Wood);
            AddLabel(pitch.TransformPoint(new Vector3(-1.50f, .66f, -2.69f)), Quaternion.identity, "MAHALLE KUPASI", "LOCAL CUP", .025f);
        }

        void Goal(Transform pitch, float z)
        {
            float direction = Mathf.Sign(z);
            for (int side = -1; side <= 1; side += 2)
            {
                ToyGeometry.Cylinder(pitch, "Rounded football goal upright", new Vector3(side * .69f, .48f, z), new Vector3(.065f, .82f, .065f), White);
                Box(pitch, "Goal soft net side", new Vector3(side * .69f, .45f, z + direction * .14f), new Vector3(.025f, .73f, .025f), Cream);
            }
            Transform crossbar = ToyGeometry.Cylinder(pitch, "Football goal crossbar", new Vector3(0, .90f, z), new Vector3(.065f, 1.45f, .065f), White);
            crossbar.localRotation = Quaternion.Euler(0, 0, 90);
            for (int i = -3; i <= 3; i++)
                Box(pitch, "Football goal net vertical", new Vector3(i * .21f, .46f, z + direction * .17f), new Vector3(.012f, .79f, .012f), ToyGeometry.Hex("D0DBC7"));
            for (int i = 0; i < 4; i++)
                Box(pitch, "Football goal net horizontal", new Vector3(0, .12f + i * .24f, z + direction * .17f), new Vector3(1.38f, .012f, .012f), ToyGeometry.Hex("D0DBC7"));
        }

        void BuildNeighbours()
        {
            neighbours[0] = Adult(transform, "Adult town hall neighbor", new Vector3(-10.2f, 0, -10.65f), Teal, 6);
            neighbours[0].root.localRotation = Quaternion.Euler(0, 125, 0);
            neighbours[1] = Adult(transform, "Adult parcel factory worker", new Vector3(9.1f, 0, -18.0f), ToyGeometry.Hex("D8AC6E"), 7);
            neighbours[1].root.localRotation = Quaternion.Euler(0, 0, 0);
            // An ordinary hard hat and work apron distinguish the factory job.
            ToyGeometry.Sphere(neighbours[1].body, "Factory worker hard hat", new Vector3(0, 1.52f, .005f), new Vector3(.43f, .17f, .41f), ToyGeometry.Hex("EDC67E"));
            Box(neighbours[1].body, "Factory hard hat brim", new Vector3(0, 1.47f, .025f), new Vector3(.47f, .045f, .45f), ToyGeometry.Hex("EDC67E"));
        }

        Neighbour Adult(Transform parent, string name, Vector3 position, Color shirt, int variant)
        {
            var person = new Neighbour();
            person.root = Group(parent, name, position);
            person.home = position;
            person.body = Group(person.root, "Adult character pose", Vector3.zero);
            Color skin = variant % 3 == 0 ? ToyGeometry.Hex("D9AD88") : variant % 3 == 1 ? ToyGeometry.Hex("EDC9A2") : ToyGeometry.Hex("BC8F70");
            Color hair = variant % 2 == 0 ? ToyGeometry.Hex("795D50") : ToyGeometry.Hex("4F5356");
            ToyGeometry.Capsule(person.body, "Adult casual shirt", new Vector3(0, .91f, 0), new Vector3(.40f, .58f, .30f), shirt);
            ToyGeometry.Sphere(person.body, "Adult cartoon head", new Vector3(0, 1.34f, .025f), new Vector3(.37f, .42f, .35f), skin);
            ToyGeometry.Sphere(person.body, "Adult hair", new Vector3(0, 1.50f, -.015f), new Vector3(.38f, .15f, .35f), hair);
            ToyGeometry.Sphere(person.body, "Adult nose", new Vector3(0, 1.32f, .215f), new Vector3(.075f, .08f, .075f), skin);
            for (int i = -1; i <= 1; i += 2)
                ToyGeometry.Sphere(person.body, "Friendly adult eye", new Vector3(i * .078f, 1.38f, .184f), new Vector3(.035f, .045f, .025f), Ink);
            if ((variant & 1) == 0)
                ToyGeometry.Sphere(person.body, "Adult moustache", new Vector3(0, 1.255f, .192f), new Vector3(.14f, .038f, .035f), hair);
            else
                Box(person.body, "Adult smile", new Vector3(0, 1.24f, .19f), new Vector3(.08f, .018f, .018f), Ink);
            person.leftArm = Limb(person.body, "Adult left arm", new Vector3(-.24f, 1.05f, 0), shirt, skin, true);
            person.rightArm = Limb(person.body, "Adult right arm", new Vector3(.24f, 1.05f, 0), shirt, skin, true);
            person.leftLeg = Limb(person.body, "Adult left leg", new Vector3(-.11f, .64f, 0), Ink, Ink, false);
            person.rightLeg = Limb(person.body, "Adult right leg", new Vector3(.11f, .64f, 0), Ink, Ink, false);
            return person;
        }

        static Transform Limb(Transform parent, string name, Vector3 position, Color cloth, Color skin, bool arm)
        {
            Transform pivot = Group(parent, name, position);
            ToyGeometry.Capsule(pivot, "Soft adult limb", new Vector3(0, arm ? -.18f : -.23f, 0), new Vector3(arm ? .12f : .15f, arm ? .34f : .44f, arm ? .12f : .15f), cloth);
            if (arm) ToyGeometry.Sphere(pivot, "Adult hand", new Vector3(0, -.38f, 0), Vector3.one * .13f, skin);
            else ToyGeometry.Sphere(pivot, "Adult trainer", new Vector3(0, -.52f, .045f), new Vector3(.17f, .12f, .27f), Cream);
            return pivot;
        }

        void Bench(Vector3 position, float yaw)
        {
            Transform bench = Group(architecture, "Fountain plaza bench", position, yaw);
            Box(bench, "Fountain bench seat", new Vector3(0, .38f, 0), new Vector3(1.08f, .12f, .39f), Wood);
            Box(bench, "Fountain bench back", new Vector3(0, .66f, .18f), new Vector3(1.08f, .39f, .08f), Wood);
            for (int i = -1; i <= 1; i += 2)
                Box(bench, "Fountain bench leg", new Vector3(i * .36f, .18f, 0), new Vector3(.08f, .36f, .30f), Teal);
        }

        void AddLabel(Vector3 position, Quaternion rotation, string tr, string en, float size)
        {
            if (labelCount >= labels.Length) return;
            Transform root = Group(transform, "Localized city lettering", position);
            root.rotation = rotation;
            TextMesh text = root.gameObject.AddComponent<TextMesh>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            text.text = GameText.Pick(tr, en);
            text.fontSize = 64;
            text.characterSize = size;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Ink;
            labels[labelCount++] = new Label { text = text, tr = tr, en = en };
        }

        static Transform Group(Transform parent, string name, Vector3 position, float yaw = 0)
        {
            Transform result = ToyGeometry.Group(parent, name, position);
            result.localRotation = Quaternion.Euler(0, yaw, 0);
            return result;
        }
        static Transform Box(Transform parent, string name, Vector3 position, Vector3 size, Color color)
            => ToyGeometry.Box(parent, name, position, size, color);
        static void Solid(Transform parent, string name, Vector3 position, Vector3 size, Color color)
        {
            Transform body = Box(parent, name, position, size, color);
            // ToyGeometry disables and schedules the primitive collider for destruction.
            // The independent box below is the one permanent, unscaled collision envelope.
            Transform envelope = Group(parent, name + " collision", position);
            envelope.gameObject.AddComponent<BoxCollider>().size = size;
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
