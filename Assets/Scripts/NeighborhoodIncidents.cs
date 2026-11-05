using System;
using UnityEngine;

namespace KurmaliKargo
{
    internal enum Incident { Ducks, BurstPipe, Market, Wedding, Wind }

    [Serializable]
    internal sealed class IncidentSave
    {
        public bool enabled;
        public float seconds;
        public float duckWait;
        public bool duckPaid;
    }

    internal sealed class NeighborhoodIncidents
    {
        static readonly Vector3[] Places = {
            new Vector3(-8, 0, 2.9f), new Vector3(8, 0, -5.8f), new Vector3(-16, 0, -4),
            new Vector3(0, 0, 8), new Vector3(8, 0, 8)
        };
        static readonly Color Cream = ToyGeometry.Hex("FFF2D2");
        static readonly Color Yellow = ToyGeometry.Hex("F9C862");
        static readonly Color Orange = ToyGeometry.Hex("E58B54");
        static readonly Color Aqua = ToyGeometry.Hex("84CDE0");
        static readonly Color Pink = ToyGeometry.Hex("DE87A6");
        readonly Transform[] roots = new Transform[Places.Length];
        readonly Transform[] ducks = new Transform[4];
        readonly Transform[] droplets = new Transform[10];
        readonly Transform[] weddingCars = new Transform[3];
        readonly Transform[] streamers = new Transform[6];
        readonly int[] slots = new int[Places.Length];
        IncidentSave state;
        int announced;

        public NeighborhoodIncidents()
        {
            Transform parent = new GameObject("Seeded neighborhood incidents").transform;
            for (int i = 0; i < roots.Length; i++)
                roots[i] = ToyGeometry.Group(parent, ((Incident)i).ToString(), Places[i]);
            BuildDucks(roots[(int)Incident.Ducks]);
            BuildPipe(roots[(int)Incident.BurstPipe]);
            BuildMarket(roots[(int)Incident.Market]);
            BuildWedding(roots[(int)Incident.Wedding]);
            for (int i = 0; i < streamers.Length; i++)
                streamers[i] = ToyGeometry.Box(roots[(int)Incident.Wind], "Wind ribbon", new Vector3((i - 2.5f) * .7f, .4f + i * .11f, 0),
                    new Vector3(.54f, .035f, .085f), i % 2 == 0 ? Aqua : Cream);
            Configure(0, null);
        }

        public void Configure(int seed, IncidentSave saved)
        {
            Array.Fill(slots, -1);
            announced = 0;
            state = saved == null || !saved.enabled ? null : new IncidentSave
                { enabled = true, seconds = saved.seconds, duckWait = saved.duckWait, duckPaid = saved.duckPaid };
            if (state != null)
            {
                var random = new System.Random(seed ^ 0x50748);
                int count = random.Next(1, 4);
                for (int i = 0; i < count; i++)
                {
                    int next = random.Next(slots.Length);
                    while (slots[next] >= 0) next = (next + 1) % slots.Length;
                    slots[next] = i;
                }
            }
            RefreshVisibility();
        }

        public IncidentSave Capture() => state == null ? null : new IncidentSave
            { enabled = true, seconds = state.seconds, duckWait = state.duckWait, duckPaid = state.duckPaid };

        public static bool Valid(IncidentSave saved)
            => saved == null || (saved.seconds >= 0 && saved.seconds <= 86400 && saved.duckWait >= 0 && saved.duckWait <= 1.4f);

        static float Start(int slot) => 4 + slot * 17;
        static float Length(Incident kind) => kind == Incident.Ducks || kind == Incident.Wedding ? 9 : 15;
        float Age(Incident kind) => state.seconds - Start(slots[(int)kind]);
        internal bool Active(Incident kind) => state != null && slots[(int)kind] >= 0 && Age(kind) >= 0 && Age(kind) < Length(kind);
        internal static Vector3 Position(Incident kind) => Places[(int)kind];
        internal int Slot(Incident kind) => slots[(int)kind];

        bool Nearby(Incident kind, Vector3 position, float radius)
            => Active(kind) && (position - Position(kind)).sqrMagnitude < radius * radius;

        public float SpeedScale(Vector3 position)
            => Nearby(Incident.Market, position, 2.65f) ? .72f : 1;
        public float GripScale(Vector3 position)
            => Nearby(Incident.BurstPipe, position, 2.4f) ? .34f : 1;
        public float Gust => Active(Incident.Wind) ? Mathf.Sin(Age(Incident.Wind) * 2.8f) : 0;
        public bool LaneBlocked => Active(Incident.Ducks) || Active(Incident.Wedding);

        public Vector3 ConstrainStep(Vector3 position, Vector3 step, float bodyMargin)
        {
            step = BlockLane(Incident.Ducks, position, step, bodyMargin);
            return BlockLane(Incident.Wedding, position, step, bodyMargin);
        }

        Vector3 BlockLane(Incident kind, Vector3 position, Vector3 step, float bodyMargin)
        {
            if (!Active(kind)) return step;
            Vector3 offset = position - Position(kind);
            bool alongZ = kind == Incident.Ducks;
            float lateral = alongZ ? offset.x + step.x : offset.z + step.z;
            if (Mathf.Abs(lateral) > 1.07f + bodyMargin * .55f) return step;
            float along = alongZ ? offset.z : offset.x;
            float delta = alongZ ? step.z : step.x;
            float gap = .43f + bodyMargin;
            if (along * delta >= 0) return step;
            float permitted = Mathf.Sign(delta) * Mathf.Min(Mathf.Abs(delta), Mathf.Max(0, Mathf.Abs(along) - gap));
            if (alongZ) step.z = permitted;
            else step.x = permitted;
            return step;
        }

        public bool Tick(float dt, Vector3 position, float speed, bool reduced, Action<Incident> announce)
        {
            if (state == null) return false;
            state.seconds = Mathf.Min(86400, state.seconds + dt);
            RefreshVisibility();
            for (int i = 0; i < roots.Length; i++)
            {
                var kind = (Incident)i;
                int bit = 1 << i;
                if ((announced & bit) != 0 || !Active(kind) || (kind != Incident.Wind && !Nearby(kind, position, 7))) continue;
                announced |= bit;
                announce(kind);
            }
            Pose(reduced);
            if (state.duckPaid) return false;
            state.duckWait = Nearby(Incident.Ducks, position, 2.8f) && speed < .65f
                ? Mathf.Min(1.4f, state.duckWait + dt) : 0;
            if (state.duckWait < 1.4f) return false;
            state.duckPaid = true;
            return true;
        }

        void RefreshVisibility()
        {
            for (int i = 0; i < roots.Length; i++)
            {
                bool active = Active((Incident)i);
                if (roots[i].gameObject.activeSelf != active) roots[i].gameObject.SetActive(active);
            }
        }

        void Pose(bool reduced)
        {
            if (Active(Incident.Ducks))
                for (int i = 0; i < ducks.Length; i++)
                {
                    float t = Mathf.Clamp01(Age(Incident.Ducks) / Length(Incident.Ducks));
                    ducks[i].localPosition = new Vector3((t - .5f) * 1.6f + (i - 1.5f) * .39f,
                        reduced ? 0 : Mathf.Abs(Mathf.Sin(t * 63 + i)) * .06f, i % 2 * .27f - .12f);
                }
            if (Active(Incident.BurstPipe))
                for (int i = 0; i < droplets.Length; i++)
                {
                    float t = reduced ? i / (float)droplets.Length : Mathf.Repeat(Age(Incident.BurstPipe) * 1.5f + i * .14f, 1);
                    droplets[i].localPosition = new Vector3(.6f + Mathf.Cos(i * 2.4f) * t * .65f, .16f + Mathf.Sin(t * Mathf.PI) * 1.48f,
                        Mathf.Sin(i * 2.4f) * t * .67f);
                }
            if (Active(Incident.Wedding))
                for (int i = 0; i < weddingCars.Length; i++)
                {
                    float t = Mathf.Clamp01(Age(Incident.Wedding) / Length(Incident.Wedding));
                    weddingCars[i].localPosition = new Vector3((t - .5f) * .8f, 0, (i - 1) * 1.14f);
                }
            if (Active(Incident.Wind))
                for (int i = 0; i < streamers.Length; i++)
                    streamers[i].localRotation = Quaternion.Euler(0, reduced ? 0 : Gust * 25 + i * 15, 0);
        }

        void BuildDucks(Transform root)
        {
            for (int i = 0; i < ducks.Length; i++)
            {
                Transform duck = ToyGeometry.Group(root, "Crossing duck", Vector3.zero);
                ducks[i] = duck;
                if (i > 0) duck.localScale = Vector3.one * .68f;
                ToyGeometry.Sphere(duck, "Duck body", new Vector3(0, .21f, 0), new Vector3(.43f, .31f, .27f), Yellow);
                ToyGeometry.Sphere(duck, "Duck head", new Vector3(.16f, .38f, 0), Vector3.one * .21f, Yellow);
                ToyGeometry.Sphere(duck, "Duck bill", new Vector3(.27f, .35f, 0), new Vector3(.14f, .06f, .12f), Orange);
            }
        }

        void BuildPipe(Transform root)
        {
            ToyGeometry.Sphere(root, "Wide slippery puddle", new Vector3(0, .075f, 0), new Vector3(2.9f, .03f, 2.1f), Aqua);
            ToyGeometry.Cylinder(root, "Burst pipe", new Vector3(.60f, .14f, 0), new Vector3(.34f, .26f, .34f), ToyGeometry.Hex("568CA3"));
            for (int i = 0; i < droplets.Length; i++)
                droplets[i] = ToyGeometry.Sphere(root, "Water jet droplet", Vector3.zero, new Vector3(.11f, .17f, .11f), i % 2 == 0 ? Aqua : Cream);
        }

        void BuildMarket(Transform root)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 1.19f;
                ToyGeometry.Box(root, "Market stall counter", new Vector3(x, .42f, 0), new Vector3(.75f, .68f, 1.62f), Orange);
                for (int stripe = 0; stripe < 6; stripe++)
                {
                    float z = (stripe - 2.5f) * .28f;
                    ToyGeometry.Box(root, "Market awning stripe", new Vector3(x, 1.54f, z), new Vector3(.99f, .11f, .29f), stripe % 2 == 0 ? Pink : Cream);
                    ToyGeometry.Sphere(root, "Market fruit", new Vector3(x, .83f, z * .78f), Vector3.one * .20f, stripe % 2 == 0 ? Yellow : Aqua);
                }
                for (int pole = -1; pole <= 1; pole += 2)
                    ToyGeometry.Box(root, "Market stall post", new Vector3(x + side * .28f, 1.07f, pole * .69f), new Vector3(.06f, .9f, .06f), Cream);
            }
        }

        void BuildWedding(Transform root)
        {
            Color tire = ToyGeometry.Hex("455968");
            for (int i = 0; i < weddingCars.Length; i++)
            {
                Transform car = ToyGeometry.Group(root, "Wedding convoy car", Vector3.zero);
                weddingCars[i] = car;
                ToyGeometry.Box(car, "Toy car body", new Vector3(0, .27f, 0), new Vector3(.92f, .33f, 1.07f), i == 1 ? Pink : Cream);
                ToyGeometry.Box(car, "Toy car windows", new Vector3(0, .52f, -.07f), new Vector3(.70f, .29f, .59f), Aqua);
                ToyGeometry.Box(car, "Wedding ribbon", new Vector3(0, .69f, -.08f), new Vector3(.13f, .04f, .75f), Pink);
                for (int side = -1; side <= 1; side += 2)
                    for (int end = -1; end <= 1; end += 2)
                        ToyGeometry.Sphere(car, "Convoy wheel", new Vector3(side * .42f, .16f, end * .34f), new Vector3(.18f, .29f, .29f), tire);
            }
        }
    }
}
