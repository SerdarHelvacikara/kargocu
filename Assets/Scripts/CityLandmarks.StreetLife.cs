using UnityEngine;

namespace KurmaliKargo
{
    public sealed partial class CityLandmarks
    {
        internal static readonly Vector3 PigeonPlaza = new Vector3(-10.3f, .10f, -9.65f);
        static readonly Color Brass = ToyGeometry.Hex("EAC276");
        static readonly Color BicycleRubber = ToyGeometry.Hex("45535E");
        static readonly Color Pastry = ToyGeometry.Hex("BB763E");
        readonly Neighbour[] walkers = new Neighbour[3];
        readonly Transform[] pigeons = new Transform[5];
        readonly Transform[,] pigeonWings = new Transform[5, 2];
        readonly Transform[] laundry = new Transform[3];
        readonly Transform[] bicycleWheels = new Transform[2];
        readonly Vector3[] walkerEnds = {
            new Vector3(-6.5f, 0, 8.9f), new Vector3(9.5f, 0, -5.2f), new Vector3(-13.4f, 0, 9.4f)
        };
        Transform streetLife, bicycle, cat, catTail;
        float streetLifeTime, birdFlight, streetChatter, bicycleTime;
        float bicyclePullout;
        bool bicycleReturning;
        readonly RaycastHit[] bicycleClearance = new RaycastHit[8];
        static readonly Vector3 BicycleStart = new Vector3(16.9f, 0, -11.3f);
        static readonly Vector3 BicycleEnd = new Vector3(16.9f, 0, 4.2f);
        bool pigeonsArmed = true;

        internal Transform StreetLifeRoot => streetLife;
        internal Vector3 FirstWalkerPosition => walkers[0].root.position;
        internal bool PigeonsFlying => birdFlight > 0;
        internal int PigeonTakeoffs { get; private set; }
        internal Vector3 BicyclePosition => bicycle.position;

        void BuildStreetLife()
        {
            streetLife = Group(transform, "Neighborhood street life", Vector3.zero);
            walkers[0] = Adult(streetLife, "Pedestrian in apricot", new Vector3(-6.5f, 0, .8f), Coral, 8);
            walkers[1] = Adult(streetLife, "Pedestrian in teal", new Vector3(9.5f, 0, 5.7f), Teal, 9);
            walkers[2] = Adult(streetLife, "Pedestrian with a shopping bag", new Vector3(-16.9f, 0, 9.4f), Glass, 10);
            Box(walkers[2].rightArm, "Paper shopping bag", new Vector3(.02f, -.44f, .03f), new Vector3(.19f, .23f, .14f), Brass);

            bicycle = Group(streetLife, "Neighborhood bicycle", BicycleStart, 0);
            for (int i = 0; i < bicycleWheels.Length; i++)
            {
                float z = i == 0 ? -.43f : .43f;
                Transform wheel = Group(bicycle, "Bicycle rolling wheel", new Vector3(0, .28f, z));
                bicycleWheels[i] = wheel;
                Transform tire = ToyGeometry.Cylinder(wheel, "Bicycle tire", Vector3.zero, new Vector3(.53f, .065f, .53f), BicycleRubber);
                tire.localRotation = Quaternion.Euler(0, 0, 90);
                Transform rim = ToyGeometry.Cylinder(wheel, "Bicycle rim", Vector3.zero, new Vector3(.43f, .07f, .43f), White);
                rim.localRotation = tire.localRotation;
                Box(wheel, "Bicycle spoke", Vector3.zero, new Vector3(.078f, .037f, .39f), BicycleRubber);
            }
            Box(bicycle, "Bicycle crossbar", new Vector3(0, .51f, 0), new Vector3(.085f, .085f, .83f), Coral);
            Box(bicycle, "Bicycle handle stem", new Vector3(0, .57f, .39f), new Vector3(.07f, .56f, .075f), Glass);
            Box(bicycle, "Bicycle handlebars", new Vector3(0, .85f, .39f), new Vector3(.47f, .055f, .07f), Ink);
            Box(bicycle, "Bicycle saddle", new Vector3(0, .69f, -.19f), new Vector3(.20f, .08f, .21f), Ink);
            Neighbour rider = Adult(bicycle, "Adult cyclist", new Vector3(0, .20f, -.12f), Brass, 11);
            rider.root.localScale = Vector3.one * .8f;
            rider.leftLeg.localRotation = Quaternion.Euler(-35, 0, 0);
            rider.rightLeg.localRotation = Quaternion.Euler(-35, 0, 0);
            rider.leftArm.localRotation = Quaternion.Euler(-65, 0, 0);
            rider.rightArm.localRotation = Quaternion.Euler(-65, 0, 0);
            ToyGeometry.Sphere(rider.body, "Cyclist helmet", new Vector3(0, 1.51f, .01f), new Vector3(.44f, .17f, .39f), Teal);

            BuildStreetCat();
            for (int i = 0; i < pigeons.Length; i++)
            {
                Transform pigeon = Group(streetLife, "Plaza pigeon", PigeonHome(i), i * 63);
                pigeons[i] = pigeon;
                ToyGeometry.Sphere(pigeon, "Pigeon body", new Vector3(0, .12f, 0), new Vector3(.23f, .20f, .34f), Glass);
                ToyGeometry.Sphere(pigeon, "Pigeon head", new Vector3(0, .23f, .13f), Vector3.one * .14f, Ink);
                Box(pigeon, "Pigeon beak", new Vector3(0, .23f, .21f), new Vector3(.05f, .04f, .065f), Brass);
                for (int side = 0; side < 2; side++)
                {
                    int sign = side == 0 ? -1 : 1;
                    Transform wing = Group(pigeon, "Pigeon wing", new Vector3(sign * .075f, .16f, -.035f));
                    pigeonWings[i, side] = wing;
                    ToyGeometry.Sphere(wing, "Blue gray flight feathers", new Vector3(sign * .083f, 0, -.055f),
                        new Vector3(.24f, .057f, .23f), ToyGeometry.Hex("8297AD"));
                }
            }
            BuildPorchAndCart();
        }

        void BuildStreetCat()
        {
            cat = Group(streetLife, "Ginger street cat", new Vector3(-6.15f, 0, -6.8f));
            Color coat = ToyGeometry.Hex("D89359");
            ToyGeometry.Sphere(cat, "Cat body", new Vector3(0, .25f, -.09f), new Vector3(.32f, .32f, .61f), coat);
            ToyGeometry.Sphere(cat, "Cat head", new Vector3(0, .37f, .23f), new Vector3(.32f, .32f, .29f), coat);
            for (int side = -1; side <= 1; side += 2)
            {
                Transform ear = Box(cat, "Cat ear", new Vector3(side * .103f, .52f, .23f), new Vector3(.096f, .135f, .08f), coat);
                ear.localRotation = Quaternion.Euler(0, 0, side * -15);
                ToyGeometry.Sphere(cat, "Cat eye", new Vector3(side * .071f, .395f, .357f), new Vector3(.032f, .046f, .03f), Ink);
                for (int leg = -1; leg <= 1; leg += 2)
                    ToyGeometry.Sphere(cat, "Cat paw", new Vector3(side * .096f, .098f, leg * .17f), new Vector3(.11f, .17f, .13f), coat);
            }
            ToyGeometry.Sphere(cat, "Cat muzzle", new Vector3(0, .326f, .36f), new Vector3(.12f, .085f, .066f), Cream);
            catTail = Group(cat, "Cat tail pivot", new Vector3(0, .28f, -.34f));
            Transform tail = ToyGeometry.Capsule(catTail, "Curled ginger tail", new Vector3(0, .14f, -.1f), new Vector3(.09f, .39f, .09f), coat);
            tail.localRotation = Quaternion.Euler(-36, 0, 0);
        }

        void BuildPorchAndCart()
        {
            Transform porch = Group(streetLife, "Porch tea break", new Vector3(10.1f, 0, 7.7f), -90);
            ToyGeometry.Cylinder(porch, "Tea table", new Vector3(0, .57f, 0), new Vector3(.65f, .11f, .65f), Wood);
            ToyGeometry.Cylinder(porch, "Tea table stand", new Vector3(0, .28f, 0), new Vector3(.13f, .55f, .13f), Teal);
            for (int side = -1; side <= 1; side += 2)
            {
                ToyGeometry.Cylinder(porch, "Tea saucer", new Vector3(side * .15f, .641f, 0), new Vector3(.19f, .03f, .16f), White);
                ToyGeometry.Cylinder(porch, "Tulip tea glass", new Vector3(side * .15f, .707f, 0), new Vector3(.095f, .12f, .095f), Pastry);
                Neighbour guest = Adult(porch, "Neighbor taking tea", new Vector3(side * .66f, 0, .22f), side < 0 ? Teal : Glass, side < 0 ? 12 : 14);
                guest.root.localScale = Vector3.one * .83f;
                guest.root.localRotation = Quaternion.Euler(0, side * -104, 0);
                guest.rightArm.localRotation = Quaternion.Euler(-45, 0, 9);
            }
            Transform line = Group(streetLife, "Balcony laundry", new Vector3(11.5f, 2.7f, 5.49f), 90);
            Box(line, "Laundry cord", Vector3.zero, new Vector3(1.9f, .018f, .018f), Wood);
            for (int i = 0; i < laundry.Length; i++)
            {
                laundry[i] = Group(line, "Laundry cloth pivot", new Vector3(-.57f + i * .56f, 0, 0));
                Box(laundry[i], "Soft hanging cloth", new Vector3(0, -.2f, 0), new Vector3(.38f, .4f, .039f), i == 0 ? Coral : i == 1 ? Cream : Teal);
            }
            Transform cart = Group(streetLife, "Simit cart", new Vector3(-10.45f, 0, -7.2f), 18);
            Box(cart, "Simit cart base", new Vector3(0, .39f, 0), new Vector3(1.05f, .61f, .56f), Coral);
            Box(cart, "Simit display board", new Vector3(0, .73f, 0), new Vector3(1.13f, .085f, .64f), Cream);
            Box(cart, "Simit cart canopy", new Vector3(0, 1.37f, 0), new Vector3(1.17f, .07f, .69f), Teal);
            for (int side = -1; side <= 1; side += 2)
                Box(cart, "Simit canopy strut", new Vector3(side * .47f, 1.04f, 0), new Vector3(.047f, .59f, .047f), Wood);
            for (int row = 0; row < 2; row++)
                for (int bread = 0; bread < 3; bread++)
                    for (int piece = 0; piece < 6; piece++)
                    {
                        float angle = piece * Mathf.PI / 3;
                        Vector3 p = new Vector3((bread - 1) * .29f + Mathf.Cos(angle) * .08f, .81f, (row - .5f) * .25f + Mathf.Sin(angle) * .08f);
                        ToyGeometry.Sphere(cart, "Ring of toasted simit", p, new Vector3(.11f, .065f, .11f), Pastry);
                    }
            AddLabel(cart.TransformPoint(new Vector3(0, .53f, -.30f)), cart.rotation, "SİMİT", "SIMIT", .041f);
            Adult(streetLife, "Neighborhood simit seller", new Vector3(-10.85f, 0, -6.72f), Brass, 15);
        }

        static Vector3 PigeonHome(int i)
            => PigeonPlaza + new Vector3((i % 3 - 1) * .31f, 0, (i / 3 - .5f) * .35f);

        void TickStreetLife(float dt, Vector3 observer, float courierMargin, bool reducedMotion)
        {
            streetLifeTime += dt * (reducedMotion ? .45f : 1);
            for (int i = 0; i < walkers.Length; i++)
            {
                Neighbour walker = walkers[i];
                WalkStreetRoute(walker.root, walker.home, walkerEnds[i], streetLifeTime * .22f + i * 1.9f);
                AnimateNeighbour(walker, reducedMotion ? 0 : Mathf.Sin(streetLifeTime * 8 + i) * 28, false, reducedMotion);
            }
            TickBicycle(dt, observer, courierMargin, reducedMotion);
            if (!reducedMotion)
                foreach (Transform wheel in bicycleWheels) wheel.localRotation = Quaternion.Euler(bicycleTime * 260, 0, 0);
            WalkStreetRoute(cat, new Vector3(-6.15f, 0, -6.8f), new Vector3(-6.15f, 0, -9.1f), streetLifeTime * .31f);
            catTail.localRotation = Quaternion.Euler(0, reducedMotion ? 0 : Mathf.Sin(streetLifeTime * 3) * 24, 0);
            for (int i = 0; i < laundry.Length; i++)
                laundry[i].localRotation = Quaternion.Euler(reducedMotion ? 0 : Mathf.Sin(streetLifeTime * 2.3f + i) * 13, 0, 0);

            TickPigeons(dt, observer, reducedMotion);
            streetChatter -= dt;
            if (streetChatter > 0) return;
            for (int i = 0; i < walkers.Length; i++)
            {
                Vector3 p = walkers[i].root.position;
                if ((p - observer).sqrMagnitude > 9) continue;
                streetChatter = 24;
                if (i == 1) Speech?.Invoke(p + Vector3.up * 1.7f, "Kargom da bu sokakta mı?", "Is my parcel on this street too?");
                else Speech?.Invoke(p + Vector3.up * 1.7f, "Yavaş evladım, pasta sallanıyor!", "Easy there, that cake is wobbling!");
                break;
            }
        }

        void TickBicycle(float dt, Vector3 observer, float courierMargin, bool reduced)
        {
            float clearance = courierMargin + .56f;
            Vector3 current = bicycle.position;
            if (bicyclePullout != 0)
            {
                Vector3 lane = new Vector3(BicycleStart.x, current.y, current.z);
                if ((lane - observer).sqrMagnitude > 12) bicycleReturning = true;
                if (bicycleReturning && (lane - observer).sqrMagnitude < clearance * clearance)
                    bicycleReturning = false;
                float target = bicycleReturning ? lane.x : bicyclePullout;
                current.x = Mathf.MoveTowards(current.x, target, dt * 2.9f);
                bicycle.position = current;
                if (bicycleReturning && Mathf.Abs(current.x - lane.x) < .005f)
                {
                    bicycleReturning = false;
                    bicyclePullout = 0;
                }
                return;
            }
            float nextTime = bicycleTime + dt * (reduced ? .45f : 1);
            Vector3 next = Vector3.Lerp(BicycleStart, BicycleEnd, .5f - .5f * Mathf.Cos(nextTime * .28f));
            if ((next - observer).sqrMagnitude >= clearance * clearance ||
                (next - observer).sqrMagnitude >= (current - observer).sqrMagnitude)
            {
                bicycleTime = nextTime;
                WalkStreetRoute(bicycle, BicycleStart, BicycleEnd, bicycleTime * .28f);
                return;
            }
            // Pull onto a clear shoulder so the two vehicles do not wait face to face.
            float preferred = observer.x <= current.x ? 1 : -1;
            if (!TryBicycleShoulder(current, preferred, observer, clearance))
                TryBicycleShoulder(current, -preferred, observer, clearance);
        }

        bool TryBicycleShoulder(Vector3 current, float side, Vector3 observer, float clearance)
        {
            float targetX = Mathf.Clamp(current.x + side * 2.65f, -19.15f, 19.15f);
            float distance = Mathf.Abs(targetX - current.x);
            int hits = Physics.SphereCastNonAlloc(current + Vector3.up * .65f, .27f, Vector3.right * side,
                bicycleClearance, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (hits == bicycleClearance.Length) return false;
            for (int i = 0; i < hits; i++) distance = Mathf.Min(distance, Mathf.Max(0, bicycleClearance[i].distance - .045f));
            targetX = current.x + side * distance;
            if (Mathf.Abs(targetX - observer.x) < clearance + .08f) return false;
            bicyclePullout = targetX;
            return true;
        }

        internal void PlaceBicycleAtNorthEndForTest()
        {
            bicycleTime = Mathf.PI / .28f + .55f;
            bicyclePullout = 0;
            bicycleReturning = false;
            WalkStreetRoute(bicycle, BicycleStart, BicycleEnd, bicycleTime * .28f);
        }

        internal Vector3 ClampAtCyclist(Vector3 rider, Vector3 step, float vehicleMargin)
        {
            Vector3 from = rider - bicycle.position;
            from.y = 0;
            float radius = vehicleMargin + .56f;
            float a = step.sqrMagnitude;
            float b = Vector3.Dot(from, step);
            if (a < .0000001f || b >= 0) return step;
            float c = from.sqrMagnitude - radius * radius;
            if (c <= 0) return Vector3.zero;
            float discriminant = b * b - a * c;
            if (discriminant <= 0) return step;
            float hit = (-b - Mathf.Sqrt(discriminant)) / a;
            return hit >= 1 ? step : step * Mathf.Max(0, hit - .012f / Mathf.Sqrt(a));
        }

        void TickPigeons(float dt, Vector3 observer, bool reducedMotion)
        {
            float distance = (PigeonPlaza - observer).sqrMagnitude;
            if (distance > 16) pigeonsArmed = true;
            if (!reducedMotion && pigeonsArmed && birdFlight <= 0 && distance < 6.25f)
            {
                pigeonsArmed = false;
                birdFlight = 2;
                PigeonTakeoffs++;
            }
            birdFlight = reducedMotion ? 0 : Mathf.Max(0, birdFlight - dt);
            float height = Mathf.Sin(birdFlight * Mathf.PI * .5f);
            for (int i = 0; i < pigeons.Length; i++)
            {
                float angle = i * 2.39996f;
                Vector3 drift = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                pigeons[i].localPosition = PigeonHome(i) + height * (Vector3.up * (1.5f + i * .13f) + drift * .95f);
                pigeons[i].localRotation = Quaternion.Euler(0, i * 63, height * 13);
                float flap = birdFlight > 0 ? Mathf.Sin(birdFlight * 24 + i) * 46 : -63;
                pigeonWings[i, 0].localRotation = Quaternion.Euler(0, 0, -flap);
                pigeonWings[i, 1].localRotation = Quaternion.Euler(0, 0, flap);
            }
        }

        static void WalkStreetRoute(Transform actor, Vector3 from, Vector3 to, float phase)
        {
            actor.localPosition = Vector3.Lerp(from, to, .5f - .5f * Mathf.Cos(phase));
            actor.localRotation = Quaternion.LookRotation(Mathf.Sin(phase) >= 0 ? to - from : from - to);
        }
    }
}
