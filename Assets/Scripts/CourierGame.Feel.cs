using UnityEngine;

namespace KurmaliKargo
{
    public sealed partial class CourierGame
    {
        public int DeliveryReactionVariant { get; private set; }
        public float FeelClock { get; private set; }
        public float PanicTimeLeft => feelPanicLeft;
        internal bool BarkPulseActive => barkPulseLeft > 0;
        public int EscapeRewardCount { get; private set; }
        public int ActiveComicBubbleCount
        {
            get
            {
                int count = 0;
                if (feelBubbles != null) for (int i = 0; i < feelBubbles.Length; i++) if (feelBubbles[i].remaining > 0) count++;
                return count;
            }
        }
        public float PanicBlendWeight => feelPanicLeft > 0 ? (ReducedMotion ? .20f : .35f) + (ReducedMotion ? .10f : .30f) * Mathf.Sin(Mathf.Clamp01(feelPanicLeft / .85f) * Mathf.PI) : 0;
        public Vector3 FeelCameraOffset => Phase == GamePhase.Playing && !ReducedMotion ? feelCameraOffset : Vector3.zero;

        ComicBubble[] feelBubbles;
        readonly Rect[] drawnComicBubbles = new Rect[3];
        FeelParticle[] feelParticles;
        FeelParticle[] feelDust;
        Transform feelEffects;
        Texture2D feelCircle;
        GUIStyle feelTextStyle;
        readonly System.Random feelRandom = new System.Random(9173);
        bool feelInitialized;
        float feelPanicLeft, feelPanicCooldown, feelShakeLeft, feelSecondNoteLeft;
        int feelPanicDog, feelSecondNote, feelDustNext;
        Vector3 feelPanicDirection, feelCameraOffset;
        Transform barkPulse;
        float barkPulseLeft;

        sealed class ComicBubble
        {
            public readonly GUIContent content = new GUIContent();
            public Transform anchor;
            public Vector3 position;
            public string tr, en;
            public float remaining, duration, age;
            public bool shout, reaction;
            public ParcelKind parcel;
            public float integrity;
            public int variant;
        }

        sealed class FeelParticle
        {
            public Transform root;
            public Vector3 velocity;
            public float remaining, duration;
            public bool sweat;
            public Renderer renderer;
            public Material regularMaterial;
        }

        public void InitializeFeel()
        {
            if (feelInitialized) return;
            feelInitialized = true;
            feelBubbles = new ComicBubble[3];
            for (int i = 0; i < feelBubbles.Length; i++) feelBubbles[i] = new ComicBubble();
            feelParticles = new FeelParticle[32];
            feelEffects = ToyGeometry.Group(transform, "Reusable comic effects", Vector3.zero);
            barkPulse = ToyGeometry.Group(feelEffects, "Comic bark shock ring", Vector3.zero);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI / 6;
                Vector3 spoke = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Transform ray = ToyGeometry.Box(barkPulse, "Amber bark dash", spoke,
                    new Vector3(.17f, .025f, .068f), i % 2 == 0 ? ToyGeometry.Hex("E88C52") : ToyGeometry.Hex("FFD678"));
                ray.localRotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0);
            }
            barkPulse.gameObject.SetActive(false);
            for (int i = 0; i < feelParticles.Length; i++)
            {
                bool sweat = i >= 24;
                Transform visual = sweat
                    ? ToyGeometry.Sphere(feelEffects, "Reusable cartoon sweat bead", Vector3.zero, new Vector3(.07f, .14f, .06f), ToyGeometry.Hex("93C9DB"))
                    : ToyGeometry.Box(feelEffects, "Reusable delivery confetti", Vector3.zero, new Vector3(.065f, .11f, .025f), i % 3 == 0 ? ToyGeometry.Hex("EAA18D") : i % 3 == 1 ? ToyGeometry.Hex("8CC7B1") : ToyGeometry.Hex("EED18A"));
                Renderer renderer = visual.GetComponent<Renderer>();
                feelParticles[i] = new FeelParticle { root = visual, sweat = sweat, renderer = renderer, regularMaterial = renderer.sharedMaterial };
                visual.gameObject.SetActive(false);
            }
            feelDust = new FeelParticle[12];
            for (int i = 0; i < feelDust.Length; i++)
            {
                Transform puff = ToyGeometry.Sphere(feelEffects, "Reusable soft road dust", Vector3.zero, Vector3.one * .14f, ToyGeometry.Hex("D9D9BF"));
                feelDust[i] = new FeelParticle { root = puff };
                puff.gameObject.SetActive(false);
            }
            feelCircle = new Texture2D(24, 24, TextureFormat.RGBA32, false) { name = "Comic bubble rounded corner" };
            feelCircle.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < 24; y++)
                for (int x = 0; x < 24; x++)
                {
                    float alpha = Mathf.Clamp01((11.5f - Vector2.Distance(new Vector2(x, y), new Vector2(11.5f, 11.5f))) * 1.6f);
                    feelCircle.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            feelCircle.Apply(false, true);
            feelTextStyle = new GUIStyle
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),
                wordWrap = true,
                richText = false,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
            feelTextStyle.normal.textColor = ToyGeometry.Hex("345461");
            if (dogs)
            {
                dogs.WarningStarted += FeelDogWarning;
                dogs.ChaseStarted += FeelDogChase;
                dogs.ChaseEnded += FeelDogChaseEnded;
                dogs.Scared += FeelDogScared;
            }
            if (world != null && world.City) world.City.Speech += FeelCitySpeech;
        }

        public void TickFeel(float dt)
        {
            if (!feelInitialized || Phase != GamePhase.Playing || float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0) return;
            dt = Mathf.Min(dt, 60);
            FeelClock += dt;
            feelPanicLeft = Mathf.Max(0, feelPanicLeft - dt);
            if (barkPulseLeft > 0)
            {
                barkPulseLeft = Mathf.Max(0, barkPulseLeft - dt);
                barkPulse.gameObject.SetActive(barkPulseLeft > 0);
                barkPulse.localScale = Vector3.one * (ReducedMotion ? .9f : Mathf.Lerp(.45f, 1.95f, 1 - barkPulseLeft / .65f));
            }
            feelPanicCooldown = Mathf.Max(0, feelPanicCooldown - dt);
            if (feelPanicLeft > 0 && dogs && feelPanicDog >= 0 && feelPanicDog < dogs.DogPositions.Length)
            {
                Vector3 away = player.position - dogs.DogPositions[feelPanicDog]; away.y = 0;
                if (away.sqrMagnitude > .001f) feelPanicDirection = away.normalized;
            }
            if (feelSecondNote > 0)
            {
                feelSecondNoteLeft -= dt;
                if (feelSecondNoteLeft <= 0) { PlayNote(feelSecondNote); feelSecondNote = 0; }
            }
            feelShakeLeft = Mathf.Max(0, feelShakeLeft - dt);
            float shake = !ReducedMotion ? Mathf.Clamp01(feelShakeLeft / .20f) * .085f : 0;
            feelCameraOffset = new Vector3(Mathf.Sin(FeelClock * 73) * shake, Mathf.Cos(FeelClock * 61) * shake * .45f, 0);
            for (int i = 0; i < feelBubbles.Length; i++)
            {
                ComicBubble bubble = feelBubbles[i];
                if (bubble.remaining <= 0) continue;
                bubble.remaining = Mathf.Max(0, bubble.remaining - dt);
                bubble.age += dt;
            }
            for (int i = 0; i < feelParticles.Length; i++)
            {
                FeelParticle particle = feelParticles[i];
                if (particle.remaining <= 0) continue;
                particle.remaining = Mathf.Max(0, particle.remaining - dt);
                if (particle.remaining <= 0 || ReducedMotion)
                {
                    particle.remaining = 0;
                    particle.root.gameObject.SetActive(false);
                    continue;
                }
                particle.velocity += Vector3.down * (particle.sweat ? 2.6f : 3.4f) * dt;
                particle.root.position += particle.velocity * dt;
                if (!particle.sweat) particle.root.Rotate(95 * dt, 120 * dt, 145 * dt, Space.Self);
                float fade = Mathf.Clamp01(particle.remaining / .22f);
                particle.root.localScale = (particle.sweat ? new Vector3(.07f, .14f, .06f) : new Vector3(.065f, .11f, .025f)) * fade;
            }
            for (int i = 0; i < feelDust.Length; i++)
            {
                FeelParticle puff = feelDust[i];
                if (puff.remaining <= 0) continue;
                puff.remaining = Mathf.Max(0, puff.remaining - dt);
                if (puff.remaining <= 0 || ReducedMotion)
                {
                    puff.remaining = 0;
                    puff.root.gameObject.SetActive(false);
                    continue;
                }
                puff.root.position += puff.velocity * dt;
                float age = 1 - puff.remaining / puff.duration;
                puff.root.localScale = Vector3.one * Mathf.Lerp(.14f, .38f, age) * Mathf.Clamp01(puff.remaining / .18f);
            }
        }

        public void ResetFeel()
        {
            if (!feelInitialized) return;
            FeelClock = 0;
            feelPanicLeft = feelPanicCooldown = feelShakeLeft = feelSecondNoteLeft = 0;
            barkPulseLeft = 0;
            barkPulse.gameObject.SetActive(false);
            feelSecondNote = 0;
            feelCameraOffset = Vector3.zero;
            EscapeRewardCount = 0;
            for (int i = 0; i < feelBubbles.Length; i++) feelBubbles[i].remaining = 0;
            for (int i = 0; i < feelParticles.Length; i++)
            {
                feelParticles[i].remaining = 0;
                feelParticles[i].root.gameObject.SetActive(false);
            }
            for (int i = 0; i < feelDust.Length; i++)
            {
                feelDust[i].remaining = 0;
                feelDust[i].root.gameObject.SetActive(false);
            }
        }

        // Only alter the requested input vector. The ordinary controller, body
        // sweeps and accepted vehicle steering still own every centimeter moved.
        public Vector3 BlendPanicInput(Vector3 desired)
        {
            if (!feelInitialized || Phase != GamePhase.Playing || feelPanicLeft <= 0 || DinoScaresDogs) return desired;
            desired.y = 0;
            // Releasing the stick still gives a brief cartoon escape impulse.
            // Active input remains part of the blend throughout the whole panic.
            return Vector3.Lerp(desired, feelPanicDirection * Mathf.Max(.65f, desired.magnitude), PanicBlendWeight);
        }

        public void DeliveryFeel(Transform customer, Vector3 destination, float integrity)
        {
            if (!feelInitialized || Phase != GamePhase.Playing) return;
            int variant = smokeTest || persistenceTest ? 0 : feelRandom.Next(5);
            if (!smokeTest && !persistenceTest && variant == DeliveryReactionVariant) variant = (variant + 1) % 5;
            DeliveryReactionVariant = variant;
            var reaction = CurrentOrder == OrderStyle.Standard ? ("", "") : SpecialOrderReaction(CurrentOrder, integrity);
            ComicBubble reactionBubble = PushFeelBubble(customer, destination + Vector3.up * 2, reaction.Item1, reaction.Item2, 4.8f, false);
            reactionBubble.reaction = CurrentOrder == OrderStyle.Standard;
            reactionBubble.parcel = LastParcel;
            reactionBubble.integrity = integrity;
            reactionBubble.variant = variant;
            reactionBubble.age = .16f;
            string courierTr = "KARGO GELDİİİ!", courierEn = "DELIVERY IS HERE!";
            if (integrity >= 90) { courierTr += "\nKUSURSUZ!"; courierEn += "\nPERFECT!"; }
            if (ComboCount > 1) { courierTr += "  •  SERİ " + ComboCount; courierEn += "  •  STREAK " + ComboCount; }
            ComicBubble courier = PushFeelBubble(player, player.position + Vector3.up * 2.3f, courierTr, courierEn, 2.7f, true);
            courier.age = .16f;
            if (integrity >= 90 || CurrentOrder == OrderStyle.Vip)
                SpawnFeelParticles(destination + Vector3.up * 1.7f, false, 24, CurrentOrder == OrderStyle.Vip);
            feelPanicLeft = 0;
            feelShakeLeft = integrity >= 90 ? .20f : 0;
            feelCameraOffset = Vector3.zero;
        }

        public void ImpactFeel()
        {
            if (!feelInitialized || Phase != GamePhase.Playing) return;
            PushFeelBubble(player, player.position + Vector3.up * 2.1f, "PARDON PARDON!", "OOPS, SORRY!", 1.2f, true);
            feelShakeLeft = .20f;
            SpawnFeelParticles(player.position + Vector3.up * 1.9f, true, 3);
        }

        void FeelDogWarning(int index, Vector3 position)
        {
            if (Phase != GamePhase.Playing) return;
            PushFeelBubble(dogs.GetDogTransform(index), position + Vector3.up * 1.5f, "Hırr?", "Grr?", 1.15f, false);
        }

        void FeelDogChase(int index, Vector3 position)
        {
            if (Phase != GamePhase.Playing) return;
            PushFeelBubble(dogs.GetDogTransform(index), position + Vector3.up * 1.6f, "HAV HAV!", "WOOF WOOF!", 2.0f, true);
            PushFeelBubble(player, player.position + Vector3.up * 2.15f, "AAAA!", "YIKES!", 1.4f, true);
            barkPulse.position = new Vector3(position.x, .13f, position.z);
            barkPulse.localScale = Vector3.one * (ReducedMotion ? .9f : .45f);
            barkPulseLeft = .65f;
            barkPulse.gameObject.SetActive(true);
            if (!ReducedMotion) feelShakeLeft = Mathf.Max(feelShakeLeft, .16f);
            PlayFeelPair(310, 390);
            if (feelPanicCooldown > 0 || DinoScaresDogs) return;
            feelPanicCooldown = 3;
            feelPanicLeft = .85f;
            feelPanicDog = index;
            feelPanicDirection = player.position - position; feelPanicDirection.y = 0;
            feelPanicDirection = feelPanicDirection.sqrMagnitude > .001f ? feelPanicDirection.normalized : -player.forward;
            SpawnFeelParticles(player.position + Vector3.up * 1.95f, true, 8);
        }

        void FeelDogChaseEnded(int index, Vector3 position, bool escaped)
        {
            if (Phase != GamePhase.Playing || !escaped || DinoScaresDogs) return;
            // The dog raises this only on its single Chasing -> Returning edge.
            // Reset, resume, warning cancellation and dinosaur flight give no reward.
            Coins = Coins > 999994 ? 999999 : Coins + 5;
            EscapeRewardCount++;
            PushFeelBubble(player, player.position + Vector3.up * 2.1f, "+5  TEMİZ KAÇIŞ!", "+5  CLEAN ESCAPE!", 2.0f, true);
            PlayNote(880);
            Save();
        }

        void FeelDogScared(int index, Vector3 position)
        {
            if (Phase != GamePhase.Playing) return;
            PushFeelBubble(dogs.GetDogTransform(index), position + Vector3.up * 1.6f, "KAI KAI!", "YIP YIP!", 2.2f, true);
            PlayFeelPair(620, 440);
            feelPanicLeft = 0;
        }

        void FeelCitySpeech(Vector3 position, string tr, string en)
        {
            if (Phase == GamePhase.Playing) PushFeelBubble(null, position, tr, en, 4, false);
        }

        void PlayFeelPair(int first, int second)
        {
            PlayNote(first);
            feelSecondNote = second;
            feelSecondNoteLeft = .12f;
        }

        ComicBubble PushFeelBubble(Transform anchor, Vector3 worldPosition, string tr, string en, float duration, bool shout)
        {
            ComicBubble result = feelBubbles[0];
            for (int i = 0; i < feelBubbles.Length; i++)
            {
                if (feelBubbles[i].remaining <= 0) { result = feelBubbles[i]; break; }
                if (feelBubbles[i].remaining < result.remaining) result = feelBubbles[i];
            }
            result.anchor = anchor;
            result.position = anchor ? worldPosition - anchor.position : worldPosition;
            result.tr = tr; result.en = en;
            result.remaining = result.duration = duration;
            result.age = 0;
            result.shout = shout;
            result.reaction = false;
            return result;
        }

        void SpawnFeelParticles(Vector3 position, bool sweat, int count, bool gold = false)
        {
            if (ReducedMotion) return;
            int start = sweat ? 24 : 0;
            int length = sweat ? 8 : 24;
            count = Mathf.Min(count, length);
            for (int i = 0; i < count; i++)
            {
                FeelParticle particle = feelParticles[start + i];
                particle.renderer.sharedMaterial = gold ? feelParticles[2].regularMaterial : particle.regularMaterial;
                float angle = i * 2.39996f;
                float radius = sweat ? .15f : .24f + (i % 3) * .07f;
                particle.root.position = position + new Vector3(Mathf.Cos(angle) * radius, (i % 4) * .09f, Mathf.Sin(angle) * radius);
                particle.velocity = new Vector3(Mathf.Cos(angle) * (sweat ? .55f : 1.25f), sweat ? .55f : 1.6f + (i % 4) * .20f, Mathf.Sin(angle) * (sweat ? .55f : 1.25f));
                particle.remaining = particle.duration = sweat ? .70f : 1.8f;
                particle.root.localScale = sweat ? new Vector3(.07f, .14f, .06f) : new Vector3(.065f, .11f, .025f);
                particle.root.gameObject.SetActive(true);
            }
        }

        public void EmitFeelDust(Vector3 position)
        {
            if (!feelInitialized || Phase != GamePhase.Playing || ReducedMotion) return;
            FeelParticle puff = feelDust[feelDustNext];
            feelDustNext = (feelDustNext + 1) % feelDust.Length;
            puff.root.position = position;
            puff.root.localScale = Vector3.one * .14f;
            float angle = feelDustNext * 2.39996f;
            puff.velocity = new Vector3(Mathf.Cos(angle) * .08f, .24f, Mathf.Sin(angle) * .08f);
            puff.remaining = puff.duration = .75f;
            puff.root.gameObject.SetActive(true);
        }

        // Called after the HUD restores its matrix, also for the compact branch.
        // World-to-safe-area projection and the matrix both belong to this method.
        public void DrawFeelGUI()
        {
            if (!feelInitialized || !GameCamera || (Phase != GamePhase.Playing && Phase != GamePhase.Result)) return;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.matrix = GuiViewport.Matrix;
            bool compact = GuiViewport.Compact;
            float width = compact ? 185 : 265;
            float top = compact ? 60 : 138;
            float bottom = GuiViewport.LogicalHeight - 14;
            int drawn = 0;
            for (int i = 0; i < feelBubbles.Length; i++)
            {
                ComicBubble bubble = feelBubbles[i];
                if (bubble.remaining <= 0) continue;
                Vector3 worldPosition = bubble.anchor ? bubble.anchor.position + bubble.position : bubble.position;
                if (!ReducedMotion) worldPosition.y += Mathf.Min(bubble.age, 2) * .13f;
                Vector3 projected = GameCamera.WorldToScreenPoint(worldPosition);
                if (projected.z <= 0) continue;
                Vector2 point = GuiViewport.ScreenToDesign(new Vector2(projected.x, projected.y));
                bubble.content.text = bubble.reaction ? GameText.Reaction(bubble.parcel, bubble.integrity, bubble.variant) : GameText.Pick(bubble.tr, bubble.en);
                feelTextStyle.fontSize = bubble.shout ? compact ? 17 : 22 : compact ? 14 : 17;
                feelTextStyle.fontStyle = bubble.shout ? FontStyle.Bold : FontStyle.Normal;
                float bodyWidth = bubble.shout ? width * .86f : width;
                float height = Mathf.Clamp(feelTextStyle.CalcHeight(bubble.content, bodyWidth - 24) + 20, compact ? 36 : 44, compact ? 99 : 118);
                float pop = ReducedMotion ? 1 : .88f + Mathf.Clamp01(bubble.age / .14f) * .12f;
                Rect rect = new Rect(point.x - bodyWidth * .5f, point.y - height, bodyWidth, height);
                rect.x = Mathf.Clamp(rect.x, 8, Mathf.Max(8, GuiViewport.LogicalWidth - rect.width - 8));
                rect.y = Mathf.Clamp(rect.y, top, Mathf.Max(top, bottom - rect.height));
                rect = PlaceComicBubble(rect, drawn, top, bottom);
                drawnComicBubbles[drawn++] = rect;
                rect = new Rect(rect.center.x - rect.width * pop * .5f, rect.center.y - rect.height * pop * .5f, rect.width * pop, rect.height * pop);
                float alpha = Mathf.Clamp01(bubble.remaining / .25f);
                GUI.color = new Color(1, 1, 1, alpha);
                FeelRounded(new Rect(rect.x + 2, rect.y + 3, rect.width, rect.height), new Color(.17f, .28f, .31f, .20f));
                FeelRounded(rect, bubble.shout ? new Color(1, .91f, .70f, .98f) : new Color(1, .975f, .91f, .98f));
                GUI.color = new Color(1, .975f, .91f, alpha);
                float tailX = Mathf.Clamp(point.x, rect.x + 16, rect.xMax - 16);
                GUI.DrawTexture(new Rect(tailX - 7, point.y < rect.y ? rect.y - 9 : rect.yMax - 5, 14, 14), feelCircle);
                GUI.color = new Color(1, 1, 1, alpha);
                GUI.Label(new Rect(rect.x + 12, rect.y + 8, rect.width - 24, rect.height - 16), bubble.content, feelTextStyle);
            }
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        Rect PlaceComicBubble(Rect preferred, int drawn, float top, float bottom)
        {
            const float gap = 12;
            if (ComicBubbleFits(preferred, drawn, top, bottom)) return preferred;
            Rect best = preferred;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < drawn; i++)
            {
                Rect other = drawnComicBubbles[i];
                for (int side = 0; side < 4; side++)
                {
                    Rect candidate = preferred;
                    if (side == 0) candidate.y = other.y - preferred.height - gap;
                    else if (side == 1) candidate.y = other.yMax + gap;
                    else if (side == 2) candidate.x = other.x - preferred.width - gap;
                    else candidate.x = other.xMax + gap;
                    float distance = (candidate.position - preferred.position).sqrMagnitude;
                    if (distance >= bestDistance || !ComicBubbleFits(candidate, drawn, top, bottom)) continue;
                    best = candidate;
                    bestDistance = distance;
                }
            }
            return best;
        }

        bool ComicBubbleFits(Rect rect, int drawn, float top, float bottom)
        {
            if (rect.y < top || rect.yMax > bottom || rect.x < 8 || rect.xMax > GuiViewport.LogicalWidth - 8)
                return false;
            rect.yMin -= 8;
            rect.yMax += 8;
            for (int i = 0; i < drawn; i++)
                if (rect.Overlaps(drawnComicBubbles[i])) return false;
            return true;
        }

        void FeelRounded(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = new Color(color.r, color.g, color.b, color.a * previous.a);
            float diameter = Mathf.Min(22, rect.height * .6f);
            float radius = diameter * .5f;
            GUI.DrawTexture(new Rect(rect.x + radius, rect.y, rect.width - diameter, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y + radius, rect.width, rect.height - diameter), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, diameter, diameter), feelCircle);
            GUI.DrawTexture(new Rect(rect.xMax - diameter, rect.y, diameter, diameter), feelCircle);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - diameter, diameter, diameter), feelCircle);
            GUI.DrawTexture(new Rect(rect.xMax - diameter, rect.yMax - diameter, diameter, diameter), feelCircle);
            GUI.color = previous;
        }
    }
}
