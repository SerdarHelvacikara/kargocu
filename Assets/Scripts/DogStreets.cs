using System;
using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>Two optional, signed shortcuts with visible dogs and short, bite-free cartoon chases.</summary>
    public sealed class DogStreets : MonoBehaviour
    {
        public int Seed { get; private set; }
        public int PresenceMask { get; private set; }
        public int ActiveCount => (PresenceMask & 1) + ((PresenceMask >> 1) & 1);
        public bool AnyChasing => dogs[0].phase == DogPhase.Chasing || dogs[1].phase == DogPhase.Chasing;
        public bool AnyWarning => dogs[0].phase == DogPhase.Warning || dogs[1].phase == DogPhase.Warning;
        public float SlowMultiplier => !ScaredOfCourier && slowLeft > 0 ? .78f : 1;
        public float StandOffDistance { get; set; } = .95f;
        public bool ScaredOfCourier { get; set; }
        public float Clock { get; private set; }
        public int ChaseCount { get; private set; }
        public int NudgeCount { get; private set; }
        public int EscapedChaseCount { get; private set; }
        public int ScaredCount { get; private set; }
        public Vector3[] DogPositions { get; } = new Vector3[2];
        public event Action FunnyCue;
        public event Action<float> Slowed;
        public event Action<int, Vector3> WarningStarted;
        public event Action<int, Vector3> ChaseStarted;
        public event Action<int, Vector3, bool> ChaseEnded;
        public event Action<int, Vector3> Scared;

        public static readonly Vector3[] ShortcutCentres = { new Vector3(-4, 0, 13.75f), new Vector3(13.75f, 0, 4) };
        static readonly Vector3[] Homes = { new Vector3(-4, .10f, 14.25f), new Vector3(14.25f, .10f, 4) };
        readonly Dog[] dogs = new Dog[2];
        float slowLeft;
        Camera view;
        enum DogPhase { Idle, Warning, Chasing, Fleeing, Returning }

        sealed class Dog
        {
            public Transform root, model, tail, tongue, warning;
            public Transform[] legs;
            public DogPhase phase;
            public float timer, cooldown, trailClock;
            public bool waitingForClear, nudged;
            public readonly Vector3[] trail = new Vector3[32];
            public readonly RaycastHit[] hits = new RaycastHit[12];
            public int trailCount, returnIndex;
        }

        public static DogStreets Create()
        {
            var root = new GameObject("Playful shortcut dogs");
            var scene = root.AddComponent<DogStreets>();
            scene.dogs[0] = scene.BuildDog(0, ToyGeometry.Hex("C7996D"));
            scene.dogs[1] = scene.BuildDog(1, ToyGeometry.Hex("B5B4AA"));
            scene.ConfigureFixed(0);
            return scene;
        }

        public Transform GetDogTransform(int index)
            => index >= 0 && index < dogs.Length ? dogs[index].root : null;

        public void Configure(int seed)
        {
            Seed = seed;
            PresenceMask = seed == -1 ? 0 : new System.Random(seed ^ 0x49C3).Next(4);
            Reset();
        }

        public void ConfigureFixed(int mask)
        {
            Seed = -1;
            PresenceMask = Mathf.Clamp(mask, 0, 3);
            Reset();
        }

        public void Reset()
        {
            Clock = slowLeft = 0;
            ChaseCount = NudgeCount = EscapedChaseCount = ScaredCount = 0;
            for (int i = 0; i < dogs.Length; i++)
            {
                Dog dog = dogs[i];
                dog.phase = DogPhase.Idle;
                dog.timer = dog.cooldown = dog.trailClock = 0;
                dog.trailCount = 0;
                dog.returnIndex = -1;
                dog.waitingForClear = true;
                dog.nudged = false;
                dog.root.position = Homes[i];
                dog.root.rotation = Quaternion.LookRotation(i == 0 ? Vector3.back : Vector3.left);
                dog.root.gameObject.SetActive(false);
                dog.warning.gameObject.SetActive(false);
                dog.tongue.gameObject.SetActive(false);
                DogPositions[i] = dog.root.position;
            }
        }

        // Called after restoring a player position: no dog materializes beside a saved rider.
        public void KeepClearOf(Vector3 player)
        {
            slowLeft = 0;
            for (int i = 0; i < dogs.Length; i++)
            {
                Dog dog = dogs[i];
                if (Distance(player, dog.root.position) >= 3.2f && Distance(player, Homes[i]) >= 3.2f) continue;
                dog.phase = DogPhase.Idle;
                dog.timer = 0; dog.cooldown = 3;
                dog.waitingForClear = true;
                dog.root.position = Homes[i];
                dog.root.gameObject.SetActive(false);
                dog.warning.gameObject.SetActive(false);
                dog.tongue.gameObject.SetActive(false);
                DogPositions[i] = Homes[i];
            }
        }

        // The owner calls this only during Playing. There is deliberately no Update method.
        public void Tick(Vector3 player, float dt, bool reducedMotion)
        {
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0 || !Finite(player.x) || !Finite(player.z)) return;
            dt = Mathf.Min(dt, 60);
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / .05f));
            float step = dt / steps;
            for (int j = 0; j < steps; j++)
            {
                Clock += step;
                slowLeft = Mathf.Max(0, slowLeft - step);
                for (int i = 0; i < dogs.Length; i++) StepDog(i, player, step);
            }
            if (!view) view = Camera.main;
            for (int i = 0; i < dogs.Length; i++)
            {
                Dog dog = dogs[i];
                DogPositions[i] = dog.root.position;
                if (!dog.root.gameObject.activeSelf) continue;
                bool running = dog.phase == DogPhase.Chasing || dog.phase == DogPhase.Fleeing || dog.phase == DogPhase.Returning;
                float stride = reducedMotion ? 0 : Mathf.Sin(Clock * (running ? 18 : 3));
                dog.model.localPosition = Vector3.up * (running && !reducedMotion ? Mathf.Abs(stride) * .035f : stride * .006f);
                for (int leg = 0; leg < dog.legs.Length; leg++)
                    dog.legs[leg].localRotation = Quaternion.Euler(running ? stride * (leg % 2 == 0 ? 23 : -23) : 0, 0, 0);
                dog.tail.localRotation = Quaternion.Euler(58, 0, reducedMotion ? 0 : Mathf.Sin(Clock * 11) * 23);
                dog.tongue.gameObject.SetActive(dog.phase == DogPhase.Chasing);
                dog.warning.gameObject.SetActive(dog.phase == DogPhase.Warning);
                if (view && dog.phase == DogPhase.Warning)
                    dog.warning.rotation = Quaternion.LookRotation(-view.transform.forward, view.transform.up);
            }
        }

        void StepDog(int index, Vector3 player, float dt)
        {
            Dog dog = dogs[index];
            if ((PresenceMask & (1 << index)) == 0)
            {
                if (dog.root.gameObject.activeSelf) dog.root.gameObject.SetActive(false);
                return;
            }
            if (dog.waitingForClear)
            {
                if (Distance(player, Homes[index]) < 3.2f) return;
                dog.waitingForClear = false;
                dog.root.position = Homes[index];
                dog.root.gameObject.SetActive(true);
            }
            dog.cooldown = Mathf.Max(0, dog.cooldown - dt);
            if (dog.phase == DogPhase.Idle)
            {
                Vector3 p = player - ShortcutCentres[index];
                bool entered = index == 0 ? Mathf.Abs(p.x) < 2.3f && Mathf.Abs(p.z) < 1.0f : Mathf.Abs(p.z) < 2.3f && Mathf.Abs(p.x) < 1.0f;
                if (dog.cooldown <= 0 && entered)
                {
                    if (ScaredOfCourier) { BeginFlee(index); return; }
                    dog.phase = DogPhase.Warning; dog.timer = 1.1f;
                    Face(dog, player - dog.root.position, dt * 12);
                    WarningStarted?.Invoke(index, dog.root.position);
                    FunnyCue?.Invoke();
                }
                return;
            }
            if (dog.phase == DogPhase.Warning)
            {
                if (ScaredOfCourier) { BeginFlee(index); return; }
                dog.timer -= dt;
                Face(dog, player - dog.root.position, dt * 10);
                if (dog.timer <= 0)
                {
                    if (Distance(player, Homes[index]) > 4.8f) { dog.phase = DogPhase.Idle; dog.cooldown = 8; return; }
                    dog.phase = DogPhase.Chasing; dog.timer = 2.7f; dog.nudged = false;
                    dog.trailCount = 1; dog.trail[0] = dog.root.position; dog.trailClock = 0;
                    ChaseCount++;
                    ChaseStarted?.Invoke(index, dog.root.position);
                }
                return;
            }
            if (dog.phase == DogPhase.Chasing)
            {
                if (ScaredOfCourier) { BeginFlee(index); return; }
                dog.timer -= dt;
                Vector3 away = dog.root.position - player; away.y = 0;
                float standOff = Finite(StandOffDistance) ? Mathf.Clamp(StandOffDistance, .95f, 3f) : .95f;
                Vector3 goal = player + (away.sqrMagnitude > .001f ? away.normalized : Vector3.back) * standOff;
                MoveSafely(dog, goal, 3.35f, dt, false);
                dog.trailClock += dt;
                if (dog.trailClock >= .14f && dog.trailCount < dog.trail.Length)
                {
                    dog.trailClock = 0;
                    dog.trail[dog.trailCount++] = dog.root.position;
                }
                if (!dog.nudged && Distance(player, dog.root.position) < standOff + .33f)
                {
                    dog.nudged = true;
                    slowLeft = Mathf.Max(slowLeft, 2);
                    NudgeCount++;
                    Slowed?.Invoke(.78f);
                    FunnyCue?.Invoke();
                }
                if (dog.timer <= 0)
                {
                    dog.phase = DogPhase.Returning; dog.timer = 8;
                    dog.returnIndex = dog.trailCount - 1;
                    if (!dog.nudged) EscapedChaseCount++;
                    ChaseEnded?.Invoke(index, dog.root.position, !dog.nudged);
                }
                return;
            }
            if (dog.phase == DogPhase.Fleeing)
            {
                dog.timer -= dt;
                Vector3 away = dog.root.position - player; away.y = 0;
                if (away.sqrMagnitude < .01f) away = index == 0 ? Vector3.right : Vector3.back;
                MoveSafely(dog, dog.root.position + away.normalized * 1.7f, 3.2f, dt, true);
                dog.trailClock += dt;
                if (dog.trailClock >= .14f && dog.trailCount < dog.trail.Length)
                {
                    dog.trailClock = 0;
                    dog.trail[dog.trailCount++] = dog.root.position;
                }
                if (dog.timer <= 0)
                {
                    dog.phase = DogPhase.Returning; dog.timer = 8;
                    dog.returnIndex = dog.trailCount - 1;
                }
                return;
            }
            // Retrace only positions actually traversed during the chase, rather
            // than teleporting or walking through the alley wall to get home.
            dog.timer -= dt;
            while (dog.returnIndex >= 0 && Distance(dog.root.position, dog.trail[dog.returnIndex]) < .16f) dog.returnIndex--;
            if (dog.returnIndex >= 0) MoveSafely(dog, dog.trail[dog.returnIndex], 3.1f, dt, true);
            else if (Distance(dog.root.position, Homes[index]) > .16f) MoveSafely(dog, Homes[index], 3.1f, dt, true);
            else { dog.phase = DogPhase.Idle; dog.cooldown = 12; }
            if (dog.timer <= 0)
            {
                dog.phase = DogPhase.Idle; dog.cooldown = 12;
                dog.waitingForClear = true;
                dog.root.gameObject.SetActive(false);
            }
        }

        void BeginFlee(int index)
        {
            Dog dog = dogs[index];
            dog.phase = DogPhase.Fleeing; dog.timer = 1.4f;
            dog.trailCount = 1; dog.trail[0] = dog.root.position; dog.trailClock = 0;
            dog.warning.gameObject.SetActive(false);
            slowLeft = 0;
            ScaredCount++;
            Scared?.Invoke(index, dog.root.position);
        }

        static void Face(Dog dog, Vector3 direction, float amount)
        {
            direction.y = 0;
            if (direction.sqrMagnitude > .001f) dog.root.rotation = Quaternion.Slerp(dog.root.rotation, Quaternion.LookRotation(direction), Mathf.Clamp01(amount));
        }

        static void MoveSafely(Dog dog, Vector3 target, float speed, float dt, bool returning)
        {
            Vector3 delta = target - dog.root.position; delta.y = 0;
            if (delta.magnitude < .035f) return;
            float distance = Mathf.Min(speed * dt, delta.magnitude);
            Vector3 direction = delta.normalized;
            for (int i = 0; i < 5; i++)
            {
                float angle = i == 0 ? 0 : i == 1 ? 35 : i == 2 ? -35 : i == 3 ? 70 : -70;
                Vector3 d = Quaternion.Euler(0, angle, 0) * direction;
                int hitCount = Physics.SphereCastNonAlloc(dog.root.position + Vector3.up * .36f, .18f, d, dog.hits, distance + .05f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                bool blocked = hitCount == dog.hits.Length;
                for (int hit = 0; hit < hitCount && !blocked; hit++)
                    if (returning || !(dog.hits[hit].collider is CharacterController)) blocked = true;
                if (blocked) continue;
                Vector3 next = dog.root.position + d * distance;
                if (Mathf.Abs(next.x) > WorldBuilder.PlayableHalfExtent - .3f || Mathf.Abs(next.z) > WorldBuilder.PlayableHalfExtent - .3f) continue;
                dog.root.position = next;
                Face(dog, d, dt * 12);
                break;
            }
        }

        static float Distance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        Dog BuildDog(int index, Color fur)
        {
            Color cream = ToyGeometry.Hex("FFF1D8"), ink = ToyGeometry.Hex("3D535E");
            var dog = new Dog();
            dog.root = ToyGeometry.Group(transform, "Shortcut dog " + (index + 1), Homes[index]);
            dog.model = ToyGeometry.Group(dog.root, "Bouncy dog model", Vector3.zero);
            ToyGeometry.Sphere(dog.model, "Soft little dog body", new Vector3(0, .49f, -.03f), new Vector3(.46f, .46f, .84f), fur);
            ToyGeometry.Sphere(dog.model, "Cream chest", new Vector3(0, .49f, .23f), new Vector3(.36f, .35f, .26f), cream);
            ToyGeometry.Sphere(dog.model, "Round dog head", new Vector3(0, .75f, .35f), new Vector3(.49f, .48f, .44f), fur);
            ToyGeometry.Sphere(dog.model, "Cream muzzle", new Vector3(0, .68f, .57f), new Vector3(.31f, .21f, .22f), cream);
            ToyGeometry.Sphere(dog.model, "Button nose", new Vector3(0, .72f, .69f), new Vector3(.10f, .075f, .065f), ink);
            foreach (float x in new[] { -.115f, .115f })
            {
                ToyGeometry.Sphere(dog.model, "Friendly dog eye", new Vector3(x, .81f, .555f), new Vector3(.054f, .063f, .025f), ink);
                Transform ear = ToyGeometry.Capsule(dog.model, "Floppy dog ear", new Vector3(x * 1.85f, .89f, .27f), new Vector3(.17f, .32f, .15f), Color.Lerp(fur, ink, .13f));
                ear.localRotation = Quaternion.Euler(-18, 0, x < 0 ? -20 : 20);
            }
            ToyGeometry.Box(dog.model, "Mint collar", new Vector3(0, .55f, .28f), new Vector3(.42f, .08f, .15f), ToyGeometry.Hex("79BBAA"));
            ToyGeometry.Sphere(dog.model, "Golden collar tag", new Vector3(0, .48f, .37f), new Vector3(.09f, .10f, .035f), ToyGeometry.Hex("EDC57C"));
            dog.tongue = ToyGeometry.Sphere(dog.model, "Cartoon happy tongue", new Vector3(0, .60f, .65f), new Vector3(.09f, .12f, .035f), ToyGeometry.Hex("EBA0AA"));
            dog.legs = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                dog.legs[i] = ToyGeometry.Group(dog.model, "Dog leg", new Vector3(i % 2 == 0 ? -.17f : .17f, .30f, i < 2 ? -.25f : .24f));
                ToyGeometry.Capsule(dog.legs[i], "Short furry leg", new Vector3(0, -.10f, 0), new Vector3(.13f, .25f, .13f), fur);
                ToyGeometry.Sphere(dog.legs[i], "Cream paw", new Vector3(0, -.23f, .025f), new Vector3(.15f, .12f, .20f), cream);
            }
            dog.tail = ToyGeometry.Group(dog.model, "Wagging tail", new Vector3(0, .66f, -.43f));
            ToyGeometry.Capsule(dog.tail, "Curious tail", new Vector3(0, .13f, 0), new Vector3(.11f, .35f, .11f), fur);
            dog.warning = ToyGeometry.Group(dog.root, "Dog chase warning", new Vector3(0, 1.55f, 0));
            ToyGeometry.Sphere(dog.warning, "Cream warning bubble", Vector3.zero, new Vector3(.48f, .57f, .12f), cream);
            ToyGeometry.Box(dog.warning, "Warning exclamation", new Vector3(0, .065f, .075f), new Vector3(.06f, .22f, .025f), ToyGeometry.Hex("DA9772"));
            ToyGeometry.Sphere(dog.warning, "Warning dot", new Vector3(0, -.13f, .075f), new Vector3(.075f, .075f, .025f), ToyGeometry.Hex("DA9772"));
            return dog;
        }
    }
}
