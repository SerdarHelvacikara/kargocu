using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>
    /// A stray dog that hunts the courier during a shift, in every district.
    /// It only comes while a parcel is in hand: if it catches up it bites the
    /// parcel (condition loss, wobble, streak lost); outrunning it pays a bonus.
    /// Dog treats turn it into a friend who brings a tip instead. The flying
    /// dragon is out of reach.
    /// </summary>
    public sealed partial class CourierGame
    {
        enum StrayState { Away, Growl, Chase, Leave }
        StrayState strayState;
        Transform strayDog, strayLegs0, strayLegs1, strayTail;
        float strayTimer, strayNext, strayGait;
        readonly RaycastHit[] strayHits = new RaycastHit[12];
        const float StrayBite = 22, StrayReach = 1.15f;

        public bool StrayDogVisible => shiftActive && strayDog && strayDog.gameObject.activeSelf;
        public bool StrayDogChasing => strayState == StrayState.Chase || strayState == StrayState.Growl;
        public Vector3 StrayDogPosition => strayDog ? strayDog.position : Vector3.zero;
        public int StrayBites { get; private set; }
        public int StrayEscapes { get; private set; }

        void ResetStrayDog()
        {
            strayState = StrayState.Away;
            strayNext = 28;
            StrayBites = StrayEscapes = 0;
            if (strayDog) strayDog.gameObject.SetActive(false);
        }

        void TickStrayDog(float dt)
        {
            if (!shiftActive) return;
            EnsureStrayDog();
            if (strayState == StrayState.Away)
            {
                // Comes only for a parcel in hand, more often as the shift wears on.
                if (shiftElapsed < strayNext || !HasPackage || VehicleIndex == 4 || !SpawnStrayDog()) return;
                strayState = StrayState.Growl;
                strayTimer = .9f;
                PushFeelBubble(strayDog, strayDog.position + Vector3.up * 1.4f, "HIRRR!", "GRRR!", 1.2f, true);
                PlayFeelPair(220, 180);
                return;
            }
            strayTimer -= dt;
            Vector3 toPlayer = player.position - strayDog.position; toPlayer.y = 0;
            float distance = toPlayer.magnitude;
            if (strayState == StrayState.Growl)
            {
                FaceStray(toPlayer, dt * 10);
                if (strayTimer > 0) return;
                strayState = StrayState.Chase;
                strayTimer = 6.5f;
                PushFeelBubble(strayDog, strayDog.position + Vector3.up * 1.4f, "HAV HAV HAV!", "WOOF WOOF WOOF!", 1.6f, true);
                PushFeelBubble(player, player.position + Vector3.up * 2.2f, "EYVAH, KÖPEK!", "UH-OH, A DOG!", 1.3f, true);
                PlayFeelPair(310, 390);
                if (!ReducedMotion) feelShakeLeft = Mathf.Max(feelShakeLeft, .18f);
                return;
            }
            if (strayState == StrayState.Chase)
            {
                // A little slower than a courier riding straight, so good lines escape it.
                float speed = 5.1f * .86f * Mathf.Lerp(1, 1.12f, shiftElapsed / ShiftLength);
                MoveStray(player.position, speed, dt);
                if (distance < StrayReach)
                {
                    if (ShiftDogTreat) FriendlyStray();
                    else if (HasPackage) BiteParcel();
                    strayState = StrayState.Leave;
                    strayTimer = 2.2f;
                    return;
                }
                if (strayTimer <= 0 || !HasPackage)
                {
                    if (HasPackage)
                    {
                        StrayEscapes++;
                        Coins = Mathf.Min(999999, Coins + 10);
                        shiftEarnings += 10;
                        PushFeelBubble(player, player.position + Vector3.up * 2.2f, "+10 KAÇTIM!", "+10 GOT AWAY!", 1.6f, true);
                        PlayNote(990);
                    }
                    PushFeelBubble(strayDog, strayDog.position + Vector3.up * 1.4f, "Hıh!", "Hmph!", 1.0f, false);
                    strayState = StrayState.Leave;
                    strayTimer = 2.2f;
                }
                return;
            }
            // Leave: trot away, then vanish until next time.
            MoveStray(strayDog.position - toPlayer.normalized * 4, 4f, dt);
            if (strayTimer > 0) return;
            strayDog.gameObject.SetActive(false);
            strayState = StrayState.Away;
            strayNext = shiftElapsed + Mathf.Lerp(24, 13, shiftElapsed / ShiftLength);
        }

        void BiteParcel()
        {
            StrayBites++;
            Cargo = Mathf.Max(0, Cargo - StrayBite * ShiftDamageScale);
            comboCount = 0;
            wobble = 1;
            parcelVisual.OnImpact(Cargo);
            PushFeelBubble(strayDog, strayDog.position + Vector3.up * 1.4f, "HAP!", "CHOMP!", 1.4f, true);
            PushFeelBubble(player, player.position + Vector3.up * 2.3f, "PAKETİ ISIRDI!", "IT BIT THE PARCEL!", 1.8f, true);
            Notify(GameText.Pick("Köpek paketi ısırdı! Sağlamlık −", "The dog bit your parcel! Condition −") + Mathf.RoundToInt(StrayBite * ShiftDamageScale));
            PlayFeelPair(180, 140);
            if (!ReducedMotion) feelShakeLeft = Mathf.Max(feelShakeLeft, .3f);
            SpawnFeelParticles(player.position + Vector3.up * 1.6f, true, 6);
        }

        void FriendlyStray()
        {
            Coins = Mathf.Min(999999, Coins + 15);
            shiftEarnings += 15;
            PushFeelBubble(strayDog, strayDog.position + Vector3.up * 1.4f, "Hav! ♥", "Woof! ♥", 1.6f, true);
            PushFeelBubble(player, player.position + Vector3.up * 2.2f, "+15 Mama işe yaradı!", "+15 The treats worked!", 1.6f, true);
            PlayFeelPair(880, 1100);
        }

        // Appears on a street some way off, never inside a building.
        bool SpawnStrayDog()
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                float angle = shiftRandom.Next(360) * Mathf.Deg2Rad;
                Vector3 p = player.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 8.5f;
                // Snap to the nearest street line.
                if (Mathf.Abs(Mathf.Repeat(p.x + 4, 8) - 4) < Mathf.Abs(Mathf.Repeat(p.z + 4, 8) - 4)) p.x = Mathf.Round(p.x / 8) * 8;
                else p.z = Mathf.Round(p.z / 8) * 8;
                float bound = WorldBuilder.PlayableHalfExtent - 1.5f;
                if (Mathf.Abs(p.x) > bound || Mathf.Abs(p.z) > bound) continue;
                if (Physics.CheckSphere(p + Vector3.up * .5f, .5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                strayDog.position = p;
                strayDog.gameObject.SetActive(true);
                return true;
            }
            return false;
        }

        void MoveStray(Vector3 target, float speed, float dt)
        {
            Vector3 delta = target - strayDog.position; delta.y = 0;
            if (delta.magnitude < .05f) return;
            float step = Mathf.Min(speed * dt, delta.magnitude);
            Vector3 direction = delta.normalized;
            // Try straight, then fan out around corners and walls.
            foreach (float angle in new[] { 0f, 35f, -35f, 70f, -70f, 105f, -105f })
            {
                Vector3 d = Quaternion.Euler(0, angle, 0) * direction;
                int hits = Physics.SphereCastNonAlloc(strayDog.position + Vector3.up * .35f, .22f, d, strayHits, step + .1f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                bool blocked = false;
                for (int i = 0; i < hits && !blocked; i++)
                    if (strayHits[i].collider && strayHits[i].collider != controller && !strayHits[i].collider.transform.IsChildOf(player)) blocked = true;
                if (blocked) continue;
                Vector3 next = strayDog.position + d * step;
                float bound = WorldBuilder.PlayableHalfExtent - .4f;
                if (Mathf.Abs(next.x) > bound || Mathf.Abs(next.z) > bound) continue;
                strayDog.position = next;
                FaceStray(d, dt * 14);
                break;
            }
            strayGait += dt * (ReducedMotion ? 0 : 20);
            float swing = Mathf.Sin(strayGait) * 30;
            strayLegs0.localRotation = Quaternion.Euler(swing, 0, 0);
            strayLegs1.localRotation = Quaternion.Euler(-swing, 0, 0);
            strayTail.localRotation = Quaternion.Euler(50, 0, ReducedMotion ? 0 : Mathf.Sin(strayGait * .7f) * 30);
        }

        void FaceStray(Vector3 direction, float amount)
        {
            direction.y = 0;
            if (direction.sqrMagnitude > .001f)
                strayDog.rotation = Quaternion.Slerp(strayDog.rotation, Quaternion.LookRotation(direction), Mathf.Clamp01(amount));
        }

        void EnsureStrayDog()
        {
            if (strayDog) return;
            Color fur = ToyGeometry.Hex("8B6A4F"), patch = ToyGeometry.Hex("F3E2C6"), ink = ToyGeometry.Hex("3D535E");
            strayDog = new GameObject("Shift stray dog").transform;
            strayDog.localScale = Vector3.one * 1.35f;
            Transform body = ToyGeometry.Group(strayDog, "Stray body", Vector3.zero);
            ToyGeometry.Sphere(body, "Scruffy body", new Vector3(0, .5f, -.05f), new Vector3(.5f, .48f, .9f), fur);
            ToyGeometry.Sphere(body, "Chest patch", new Vector3(0, .48f, .25f), new Vector3(.38f, .36f, .28f), patch);
            ToyGeometry.Sphere(body, "Head", new Vector3(0, .8f, .38f), new Vector3(.5f, .48f, .46f), fur);
            ToyGeometry.Sphere(body, "Muzzle", new Vector3(0, .72f, .6f), new Vector3(.32f, .22f, .24f), patch);
            ToyGeometry.Sphere(body, "Nose", new Vector3(0, .76f, .72f), new Vector3(.1f, .08f, .07f), ink);
            ToyGeometry.Sphere(body, "Angry brow", new Vector3(0, .93f, .58f), new Vector3(.34f, .06f, .06f), ink);
            foreach (float x in new[] { -.12f, .12f })
            {
                ToyGeometry.Sphere(body, "Eye", new Vector3(x, .86f, .59f), new Vector3(.07f, .08f, .03f), ink);
                Transform ear = ToyGeometry.Capsule(body, "Pointy ear", new Vector3(x * 1.8f, 1.02f, .32f), new Vector3(.15f, .3f, .12f), ToyGeometry.Hex("6E523C"));
                ear.localRotation = Quaternion.Euler(-10, 0, x < 0 ? 15 : -15);
            }
            ToyGeometry.Sphere(body, "Spiked collar", new Vector3(0, .62f, .32f), new Vector3(.44f, .1f, .2f), ToyGeometry.Hex("C0453A"));
            strayLegs0 = ToyGeometry.Group(body, "Legs", new Vector3(0, .3f, 0));
            strayLegs1 = ToyGeometry.Group(body, "Legs", new Vector3(0, .3f, 0));
            foreach (float z in new[] { -.28f, .26f })
                foreach (float x in new[] { -.17f, .17f })
                    ToyGeometry.Capsule((z > 0) == (x > 0) ? strayLegs0 : strayLegs1, "Leg", new Vector3(x, -.12f, z), new Vector3(.13f, .26f, .13f), fur);
            strayTail = ToyGeometry.Group(body, "Tail", new Vector3(0, .66f, -.46f));
            ToyGeometry.Capsule(strayTail, "Tail", new Vector3(0, .13f, 0), new Vector3(.1f, .32f, .1f), fur);
            strayDog.gameObject.SetActive(false);
        }

        // Test seams (require -smokeTest -shiftTest).
        internal void SummonStrayDogForTest()
        {
            if (!smokeTest || !shiftTest || !shiftActive) return;
            EnsureStrayDog();
            strayNext = shiftElapsed;
        }
        internal bool StrayDogAwayForTest => strayState == StrayState.Away;
        internal void PlaceStrayDogForTest(Vector3 p) { if (smokeTest && shiftTest && strayDog) strayDog.position = p; }
    }
}
