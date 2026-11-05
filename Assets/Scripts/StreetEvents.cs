using UnityEngine;

namespace KurmaliKargo
{
    [System.Serializable]
    public sealed class StreetState
    {
        public int version = 1;
        public int seed, mask, worksiteIndex, routeIndex;
        public float arrivalDelay, clock, warningLeft, collectionLeft, truckX, truckZ, yaw;
        public bool headingForMidpoint, spawned;
    }

    /// <summary>
    /// A deterministic little collection route and a single, clearly marked road closure.
    /// CourierGame owns the simulation clock; overlays never advance this component.
    /// </summary>
    public sealed class StreetEvents : MonoBehaviour
    {
        public Vector3 TruckPosition => truck ? truck.position : ActiveRoute[0];
        public Vector3 WorksitePosition => Worksites[WorksiteIndex];
        public bool HasTruck { get; private set; }
        public bool HasWorksite { get; private set; }
        public int Seed { get; private set; }
        public int WorksiteIndex { get; private set; }
        public float ArrivalDelay { get; private set; } = 8;
        public float ExtraClearance { get; set; }
        public bool TruckWaiting => HasTruck && (!lastPlaying || !spawned || warningLeft > 0 || collectionLeft > 0 || IsBlockedByPlayer);
        public bool WarningActive => HasTruck && warningLeft > 0;
        public bool IsBlockedByPlayer { get; private set; }
        public float Clock { get; private set; }
        public BoxCollider TruckCollider { get; private set; }
        public BoxCollider WorksiteCollider { get; private set; }

        const float Speed = 2.0f;
        const float SafetyRadius = 2.1f;
        // The inset south lane clears the existing crate at (4,-8.45).
        static readonly Vector3[] MainRoute =
        {
            new Vector3(0, 0, -10.7f), new Vector3(0, 0, 0), new Vector3(8, 0, 0),
            new Vector3(8, 0, -7.35f), new Vector3(0, 0, -7.35f)
        };
        static readonly Vector3[] WestRoute =
        {
            new Vector3(0, 0, -10.7f), new Vector3(0, 0, 0), new Vector3(-7.35f, 0, 0),
            new Vector3(-7.35f, 0, -7.35f), new Vector3(0, 0, -7.35f)
        };
        static readonly Vector3[] AroundCentreRoute =
        {
            new Vector3(0, 0, -10.7f), new Vector3(0, 0, -7.35f), new Vector3(-7.35f, 0, -7.35f),
            new Vector3(-7.35f, 0, 0), new Vector3(8, 0, 0), new Vector3(8, 0, -7.35f), new Vector3(0, 0, -7.35f)
        };
        static readonly Vector3[] Worksites = { new Vector3(-4, 0, 0), new Vector3(4, 0, 0), new Vector3(0, 0, -4) };
        Vector3[] ActiveRoute => SelectRoute(HasWorksite, WorksiteIndex);
        static readonly Color Cream = ToyGeometry.Hex("FFF0CB");
        static readonly Color Teal = ToyGeometry.Hex("69A991");
        static readonly Color Mint = ToyGeometry.Hex("B7DBAF");
        static readonly Color Ink = ToyGeometry.Hex("38545F");
        static readonly Color Orange = ToyGeometry.Hex("F2A45F");
        static readonly Color White = ToyGeometry.Hex("FFFAE8");
        Transform truck, rearLid, worksite;
        readonly Transform[] wheels = new Transform[4];
        readonly Transform[] amberLights = new Transform[2];
        Transform warningPaddle;
        float warningLeft, collectionLeft;
        int routeIndex;
        bool headingForMidpoint, spawned, lastPlaying;

        public static StreetEvents Create()
        {
            var root = new GameObject("Living neighborhood street events");
            var events = root.AddComponent<StreetEvents>();
            events.BuildTruck();
            events.BuildWorksite();
            events.ConfigureFixed(true, true);
            return events;
        }

        public void Configure(int seed)
        {
            var random = new System.Random(seed);
            int mask = random.Next(4);
            Seed = seed;
            HasTruck = (mask & 1) != 0;
            HasWorksite = (mask & 2) != 0;
            WorksiteIndex = random.Next(3);
            float arrival = 8 + random.Next(11);
            ArrivalDelay = seed == -1 ? 8 : arrival; // -1 is reserved for fixed regression fixtures.
            ApplyWorksite();
            Reset();
        }

        public void ConfigureFixed(bool hasTruck, bool hasWorksite, int workIndex = 0)
        {
            Seed = -1;
            HasTruck = hasTruck;
            HasWorksite = hasWorksite;
            WorksiteIndex = Mathf.Clamp(workIndex, 0, 2);
            ArrivalDelay = 8;
            ApplyWorksite();
            Reset();
        }

        void ApplyWorksite()
        {
            if (!worksite) return;
            worksite.position = WorksitePosition;
            worksite.rotation = Quaternion.Euler(0, WorksiteIndex == 2 ? 90 : 0, 0);
            WorksiteCollider.enabled = HasWorksite;
            worksite.gameObject.SetActive(HasWorksite);
        }

        public void Reset()
        {
            Clock = 0;
            warningLeft = HasTruck ? ArrivalDelay : 0;
            collectionLeft = 0;
            routeIndex = 0;
            headingForMidpoint = true;
            spawned = false;
            lastPlaying = false;
            IsBlockedByPlayer = false;
            if (truck)
            {
                if (TruckCollider) TruckCollider.enabled = false;
                Vector3[] route = ActiveRoute;
                truck.position = route[0];
                truck.rotation = Quaternion.LookRotation(route[1] - route[0]);
                truck.gameObject.SetActive(false);
            }
            if (rearLid) rearLid.localRotation = Quaternion.identity;
            ApplyWorksite();
            Physics.SyncTransforms();
        }

        static Vector3[] SelectRoute(bool hasWorksite, int workIndex)
        {
            return !hasWorksite || workIndex == 0 ? MainRoute : workIndex == 1 ? WestRoute : AroundCentreRoute;
        }

        public static StreetState QuietState()
        {
            return new StreetState
            {
                seed = -1, mask = 0, worksiteIndex = 0, arrivalDelay = 8,
                headingForMidpoint = true, truckX = MainRoute[0].x, truckZ = MainRoute[0].z
            };
        }

        public StreetState CaptureState()
        {
            return new StreetState
            {
                seed = Seed, mask = (HasTruck ? 1 : 0) | (HasWorksite ? 2 : 0), worksiteIndex = WorksiteIndex,
                arrivalDelay = ArrivalDelay, clock = Clock, warningLeft = warningLeft, collectionLeft = collectionLeft,
                routeIndex = routeIndex, headingForMidpoint = headingForMidpoint, spawned = spawned,
                truckX = TruckPosition.x, truckZ = TruckPosition.z, yaw = truck ? truck.eulerAngles.y : 0
            };
        }

        public static bool ValidState(StreetState state)
        {
            if (state == null || state.version != 1 || state.mask < 0 || state.mask > 3 || state.worksiteIndex < 0 || state.worksiteIndex > 2) return false;
            if (!Range(state.arrivalDelay, 8, 18) || !Range(state.clock, 0, 10000000) || !Range(state.warningLeft, 0, state.arrivalDelay) ||
                !Range(state.collectionLeft, 0, 4) || !Range(state.truckX, -WorldBuilder.PlayableHalfExtent, WorldBuilder.PlayableHalfExtent) ||
                !Range(state.truckZ, -WorldBuilder.PlayableHalfExtent, WorldBuilder.PlayableHalfExtent) || !Range(state.yaw, 0, 360)) return false;
            if (state.seed == -1)
            {
                if (Mathf.Abs(state.arrivalDelay - 8) > .001f) return false;
            }
            else
            {
                var random = new System.Random(state.seed);
                if (state.mask != random.Next(4) || state.worksiteIndex != random.Next(3) || Mathf.Abs(state.arrivalDelay - (8 + random.Next(11))) > .001f) return false;
            }
            bool hasTruck = (state.mask & 1) != 0;
            Vector3[] route = SelectRoute((state.mask & 2) != 0, state.worksiteIndex);
            if (state.routeIndex < 0 || state.routeIndex >= route.Length) return false;
            Vector3 p = new Vector3(state.truckX, 0, state.truckZ);
            if (!hasTruck)
                return !state.spawned && state.warningLeft == 0 && state.collectionLeft == 0 && state.routeIndex == 0 && state.headingForMidpoint && FlatDistance(p, route[0]) < .02f;
            if (!state.spawned || state.warningLeft > 0)
                return state.warningLeft > 0 && state.collectionLeft == 0 && state.routeIndex == 0 && state.headingForMidpoint && FlatDistance(p, route[0]) < .02f;
            Vector3 midpoint = (route[state.routeIndex] + route[(state.routeIndex + 1) % route.Length]) * .5f;
            if (state.collectionLeft > 0)
                return !state.headingForMidpoint && FlatDistance(p, midpoint) < .025f;
            Vector3 start = state.headingForMidpoint ? route[state.routeIndex] : midpoint;
            Vector3 end = state.headingForMidpoint ? midpoint : route[(state.routeIndex + 1) % route.Length];
            return DistanceToSegment(p, start, end) < .025f;
        }

        public bool RestoreState(StreetState state, Vector3 playerPosition)
        {
            if (!ValidState(state)) return false;
            Seed = state.seed;
            HasTruck = (state.mask & 1) != 0;
            HasWorksite = (state.mask & 2) != 0;
            WorksiteIndex = state.worksiteIndex;
            ArrivalDelay = state.arrivalDelay;
            ApplyWorksite();
            Clock = state.clock;
            warningLeft = state.warningLeft;
            collectionLeft = state.collectionLeft;
            routeIndex = state.routeIndex;
            headingForMidpoint = state.headingForMidpoint;
            spawned = state.spawned;
            lastPlaying = false;
            IsBlockedByPlayer = false;
            TruckCollider.enabled = false;
            truck.gameObject.SetActive(false);
            truck.position = new Vector3(state.truckX, 0, state.truckZ);
            truck.rotation = Quaternion.Euler(0, state.yaw, 0);
            KeepClearOf(playerPosition);
            if (HasTruck && spawned)
            {
                truck.gameObject.SetActive(true);
                TruckCollider.enabled = true;
                AnimateTruck(false);
            }
            Physics.SyncTransforms();
            return true;
        }

        public void KeepClearOf(Vector3 position)
        {
            if (HasTruck && spawned && FlatDistance(position, truck.position) < SafetyRadius + ExtraClearance + .25f)
            {
                Vector3[] route = ActiveRoute;
                spawned = false;
                routeIndex = 0;
                headingForMidpoint = true;
                collectionLeft = 0;
                warningLeft = 8;
                truck.position = route[0];
                truck.rotation = Quaternion.LookRotation(route[1] - route[0]);
                IsBlockedByPlayer = true;
                TruckCollider.enabled = false;
                truck.gameObject.SetActive(false);
                Physics.SyncTransforms();
            }
        }

        static bool Range(float value, float min, float max) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= min && value <= max;

        public void Tick(float dt, bool playing, Vector3 playerPosition, bool reducedMotion)
        {
            lastPlaying = playing;
            if (!playing || float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0) return;
            // Substeps keep a long test tick or resumed frame from jumping through a rider.
            dt = Mathf.Min(dt, 60);
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / .05f));
            float step = dt / steps;
            bool changed = false;
            for (int i = 0; i < steps; i++) changed |= Simulate(step, playerPosition);
            AnimateTruck(reducedMotion);
            if (changed) Physics.SyncTransforms();
        }

        bool Simulate(float dt, Vector3 player)
        {
            Clock += dt;
            IsBlockedByPlayer = false;
            if (!HasTruck) return false;
            Vector3[] route = ActiveRoute;
            if (!spawned)
            {
                if (FlatDistance(player, route[0]) < SafetyRadius + ExtraClearance + .25f)
                {
                    IsBlockedByPlayer = true;
                    return false;
                }
                spawned = true;
                truck.gameObject.SetActive(true);
                TruckCollider.enabled = true;
            }
            if (warningLeft > 0)
            {
                warningLeft = Mathf.Max(0, warningLeft - dt);
                return true;
            }
            if (collectionLeft > 0)
            {
                collectionLeft = Mathf.Max(0, collectionLeft - dt);
                return false;
            }

            int next = (routeIndex + 1) % route.Length;
            Vector3 target = headingForMidpoint ? (route[routeIndex] + route[next]) * .5f : route[next];
            Vector3 delta = target - truck.position;
            delta.y = 0;
            if (delta.magnitude < .012f)
            {
                if (headingForMidpoint) { headingForMidpoint = false; collectionLeft = 4; }
                else { routeIndex = next; headingForMidpoint = true; }
                return false;
            }
            Vector3 direction = delta.normalized;
            Vector3 proposed = truck.position + direction * Mathf.Min(Speed * dt, delta.magnitude);
            // This is wider than the collider + rider capsule even during a turn.
            // The truck holds its pose instead of shoving or trying to steer around the rider.
            if (DistanceToSegment(player, truck.position, proposed) < SafetyRadius + ExtraClearance)
            {
                IsBlockedByPlayer = true;
                return false;
            }
            Quaternion desiredHeading = Quaternion.LookRotation(direction);
            if (Quaternion.Angle(truck.rotation, desiredHeading) > 1)
            {
                // Turning while translating sweeps the long body into roadside
                // crates. Finish the corner on the spot before moving forward.
                truck.rotation = Quaternion.RotateTowards(truck.rotation, desiredHeading, dt * 115);
                return true;
            }
            float distance = Vector3.Distance(truck.position, proposed);
            truck.position = proposed;
            truck.rotation = desiredHeading;
            for (int i = 0; i < wheels.Length; i++) wheels[i].Rotate(Vector3.up, distance * 285, Space.Self);
            return true;
        }

        void AnimateTruck(bool reducedMotion)
        {
            if (!spawned) return;
            bool blink = reducedMotion || Mathf.FloorToInt(Clock * 2.0f) % 2 == 0;
            for (int i = 0; i < amberLights.Length; i++)
                amberLights[i].localScale = Vector3.one * (reducedMotion || blink == (i == 0) ? 1 : .72f);
            warningPaddle.gameObject.SetActive(WarningActive || IsBlockedByPlayer);
            rearLid.localRotation = Quaternion.Euler(collectionLeft > 0 ? (reducedMotion ? -12 : -12 - Mathf.Sin(Clock * 3) * 7) : 0, 0, 0);
        }

        static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0;
            return Vector3.Distance(a, b);
        }

        static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            p.y = a.y = b.y = 0;
            Vector3 segment = b - a;
            float u = segment.sqrMagnitude > .00001f ? Mathf.Clamp01(Vector3.Dot(p - a, segment) / segment.sqrMagnitude) : 0;
            return Vector3.Distance(p, a + segment * u);
        }

        void BuildTruck()
        {
            truck = ToyGeometry.Group(transform, "Hazard_GarbageTruck", MainRoute[0]);
            TruckCollider = truck.gameObject.AddComponent<BoxCollider>();
            TruckCollider.center = new Vector3(0, .67f, 0);
            TruckCollider.size = new Vector3(1.18f, 1.20f, 2.10f);
            TruckCollider.enabled = false;
            ToyGeometry.Box(truck, "Truck chassis", new Vector3(0, .37f, 0), new Vector3(1.10f, .20f, 2.06f), Ink);
            ToyGeometry.Box(truck, "Round little cab", new Vector3(0, .83f, .65f), new Vector3(1.05f, .85f, .78f), Mint);
            ToyGeometry.Sphere(truck, "Cab rounded roof", new Vector3(0, 1.25f, .65f), new Vector3(1.08f, .20f, .82f), Mint);
            ToyGeometry.Box(truck, "Front blue windscreen", new Vector3(0, 1.01f, 1.05f), new Vector3(.81f, .34f, .035f), ToyGeometry.Hex("6C9DA6"));
            ToyGeometry.Box(truck, "Windscreen centre", new Vector3(0, 1.01f, 1.078f), new Vector3(.055f, .36f, .025f), Cream);
            ToyGeometry.Box(truck, "Front cream bumper", new Vector3(0, .48f, 1.08f), new Vector3(1.15f, .16f, .10f), Cream);
            ToyGeometry.Box(truck, "Truck grille", new Vector3(0, .69f, 1.055f), new Vector3(.37f, .16f, .035f), Teal);
            for (int i = 0; i < 3; i++)
                ToyGeometry.Box(truck, "Grille slot", new Vector3(-.11f + i * .11f, .69f, 1.080f), new Vector3(.035f, .11f, .018f), Ink);
            foreach (float x in new[] { -.39f, .39f })
            {
                ToyGeometry.Sphere(truck, "Friendly round headlight", new Vector3(x, .72f, 1.065f), new Vector3(.18f, .18f, .065f), White);
                ToyGeometry.Box(truck, "Side blue window", new Vector3(x < 0 ? -.535f : .535f, 1.02f, .66f), new Vector3(.03f, .31f, .42f), ToyGeometry.Hex("6C9DA6"));
                ToyGeometry.Box(truck, "Side door handle", new Vector3(x < 0 ? -.55f : .55f, .78f, .46f), new Vector3(.03f, .035f, .15f), Ink);
            }
            ToyGeometry.Box(truck, "Green collection body", new Vector3(0, .87f, -.39f), new Vector3(1.12f, .95f, 1.30f), Teal);
            ToyGeometry.Sphere(truck, "Rounded compactor top", new Vector3(0, 1.30f, -.40f), new Vector3(1.14f, .24f, 1.30f), Teal);
            ToyGeometry.Box(truck, "Cream body belt", new Vector3(0, .83f, -.39f), new Vector3(1.145f, .13f, 1.32f), Cream);
            for (int i = 0; i < 3; i++)
                ToyGeometry.Box(truck, "Compactor body rib", new Vector3(0, 1.07f, -.87f + i * .38f), new Vector3(1.15f, .48f, .045f), ToyGeometry.Hex("80B59C"));
            foreach (float x in new[] { -.575f, .575f })
            {
                ToyGeometry.Sphere(truck, "Municipal leaf emblem", new Vector3(x, 1.04f, -.35f), new Vector3(.025f, .27f, .36f), Mint);
                ToyGeometry.Box(truck, "Leaf emblem vein", new Vector3(x < 0 ? x - .017f : x + .017f, 1.04f, -.35f), new Vector3(.018f, .025f, .24f), Cream);
            }
            rearLid = ToyGeometry.Group(truck, "Collection hatch hinge", new Vector3(0, 1.30f, -1.035f));
            ToyGeometry.Box(rearLid, "Rear collection hatch", new Vector3(0, -.39f, 0), new Vector3(1.08f, .78f, .08f), Darken(Teal));
            ToyGeometry.Box(rearLid, "Rear hatch handle", new Vector3(0, -.52f, -.06f), new Vector3(.42f, .065f, .07f), Cream);
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -.54f : .54f;
                float z = i < 2 ? -.68f : .71f;
                wheels[i] = ToyGeometry.Cylinder(truck, "Collection truck tire", new Vector3(x, .28f, z), new Vector3(.40f, .13f, .40f), Ink);
                wheels[i].localRotation = Quaternion.Euler(0, 0, 90);
                Transform hub = ToyGeometry.Cylinder(truck, "Cream hubcap", new Vector3(x < 0 ? x - .075f : x + .075f, .28f, z), new Vector3(.23f, .018f, .23f), Cream);
                hub.localRotation = Quaternion.Euler(0, 0, 90);
            }
            for (int i = 0; i < 2; i++)
            {
                amberLights[i] = ToyGeometry.Group(truck, "Amber warning lamp", new Vector3(i == 0 ? -.33f : .33f, 1.39f, .66f));
                ToyGeometry.Cylinder(amberLights[i], "Amber lamp base", Vector3.zero, new Vector3(.19f, .055f, .19f), Ink);
                ToyGeometry.Sphere(amberLights[i], "Amber beacon dome", new Vector3(0, .08f, 0), new Vector3(.16f, .16f, .16f), Orange);
            }
            warningPaddle = ToyGeometry.Group(truck, "Truck will start shortly", new Vector3(0, 1.90f, 0));
            ToyGeometry.Sphere(warningPaddle, "Cream warning bubble", Vector3.zero, new Vector3(.47f, .50f, .13f), Cream);
            ToyGeometry.Box(warningPaddle, "Warning exclamation", new Vector3(0, .065f, -.08f), new Vector3(.055f, .20f, .03f), Orange);
            ToyGeometry.Sphere(warningPaddle, "Warning exclamation dot", new Vector3(0, -.11f, -.08f), new Vector3(.065f, .065f, .03f), Orange);
        }

        void BuildWorksite()
        {
            Transform work = ToyGeometry.Group(transform, "Hazard_Roadworks", WorksitePosition);
            worksite = work;
            WorksiteCollider = work.gameObject.AddComponent<BoxCollider>();
            WorksiteCollider.center = new Vector3(0, .45f, 0);
            WorksiteCollider.size = new Vector3(2.25f, .90f, 2.65f);
            ToyGeometry.Cylinder(work, "Manhole dark interior", new Vector3(0, .061f, 0), new Vector3(1.35f, .035f, 1.35f), ToyGeometry.Hex("52656A"));
            ToyGeometry.Cylinder(work, "Manhole inner darkness", new Vector3(0, .085f, 0), new Vector3(1.10f, .015f, 1.10f), Ink);
            Transform cover = ToyGeometry.Cylinder(work, "Manhole cover set aside", new Vector3(.62f, .15f, .46f), new Vector3(.97f, .075f, .97f), ToyGeometry.Hex("81958E"));
            cover.localRotation = Quaternion.Euler(0, 0, 12);
            for (int i = 0; i < 4; i++)
                ToyGeometry.Box(cover, "Manhole cover ridge", new Vector3(-.28f + i * .18f, .60f, 0), new Vector3(.045f, .35f, .66f), ToyGeometry.Hex("6F847E"));
            foreach (float x in new[] { -.98f, .98f })
            {
                Barrier(work, new Vector3(x, 0, 0));
                Cone(work, new Vector3(x, 0, -.97f));
                Cone(work, new Vector3(x, 0, .97f));
            }
            // A little toolbox makes the closure readable even from the opposite approach.
            ToyGeometry.Box(work, "Road crew toolbox", new Vector3(-.30f, .23f, .78f), new Vector3(.68f, .40f, .37f), Orange);
            ToyGeometry.Box(work, "Toolbox handle", new Vector3(-.30f, .47f, .78f), new Vector3(.30f, .08f, .08f), Ink);
            ToyGeometry.Box(work, "Toolbox latch", new Vector3(-.30f, .25f, .58f), new Vector3(.07f, .13f, .03f), Cream);
        }

        static void Barrier(Transform parent, Vector3 position)
        {
            Transform b = ToyGeometry.Group(parent, "Striped manhole safety fence", position);
            foreach (float z in new[] { -.73f, .73f })
            {
                ToyGeometry.Box(b, "Fence upright", new Vector3(0, .43f, z), new Vector3(.10f, .86f, .10f), Ink);
                ToyGeometry.Box(b, "Fence foot", new Vector3(0, .08f, z), new Vector3(.48f, .13f, .20f), Ink);
            }
            ToyGeometry.Box(b, "Fence lower rail", new Vector3(0, .30f, 0), new Vector3(.09f, .09f, 1.70f), Cream);
            ToyGeometry.Box(b, "Fence cream safety board", new Vector3(0, .65f, 0), new Vector3(.13f, .30f, 1.75f), White);
            for (int i = 0; i < 5; i++)
            {
                Transform stripe = ToyGeometry.Box(b, "Orange fence stripe", new Vector3(0, .65f, -.70f + i * .35f), new Vector3(.15f, .30f, .15f), Orange);
                stripe.localRotation = Quaternion.Euler(30, 0, 0);
            }
            ToyGeometry.Sphere(b, "Safety fence orange lamp", new Vector3(0, .93f, 0), new Vector3(.19f, .20f, .19f), Orange);
        }

        static void Cone(Transform parent, Vector3 position)
        {
            Transform c = ToyGeometry.Group(parent, "Soft road cone", position);
            ToyGeometry.Box(c, "Cone square foot", new Vector3(0, .07f, 0), new Vector3(.39f, .09f, .39f), Ink);
            ToyGeometry.Cylinder(c, "Cone lower tier", new Vector3(0, .17f, 0), new Vector3(.29f, .16f, .29f), Orange);
            ToyGeometry.Cylinder(c, "Cone reflective band", new Vector3(0, .30f, 0), new Vector3(.23f, .10f, .23f), White);
            ToyGeometry.Cylinder(c, "Cone upper tier", new Vector3(0, .41f, 0), new Vector3(.17f, .13f, .17f), Orange);
            ToyGeometry.Sphere(c, "Cone rounded tip", new Vector3(0, .49f, 0), new Vector3(.13f, .11f, .13f), Orange);
        }

        static Color Darken(Color color) => Color.Lerp(color, Ink, .20f);
    }
}
