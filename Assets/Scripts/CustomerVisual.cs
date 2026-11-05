using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>A lightweight cartoon customer with hand-authored reaction poses.</summary>
    public sealed class CustomerVisual : MonoBehaviour
    {
        public bool ReducedMotion { get; set; }
        public float AnimationClock => elapsed;
        public int ReactionTier => reaction;
        Transform model, head, leftArm, rightArm, mouth, leftEye, rightEye, leftBrow, rightBrow;
        Transform popup, happyIcon, mildIcon, shockedIcon;
        Transform chefHat, mayorSash;
        Camera view;
        float remaining, elapsed;
        int reaction;
        float armBlend;

        public static CustomerVisual Create(Vector3 position, Color shirt, Vector3 lookDirection)
        {
            var go = new GameObject("Friendly neighborhood customer");
            go.transform.position = position;
            lookDirection.y = 0;
            if (lookDirection.sqrMagnitude > .001f) go.transform.rotation = Quaternion.LookRotation(lookDirection);
            var customer = go.AddComponent<CustomerVisual>();
            customer.Build(shirt);
            return customer;
        }

        internal void SetVipRole(int role)
        {
            if (role == 1 && !chefHat)
            {
                Color ivory = ToyGeometry.Hex("FFF1D7");
                chefHat = ToyGeometry.Group(head, "Chef's toque", new Vector3(0, .25f, 0));
                ToyGeometry.Cylinder(chefHat, "Chef hat band", new Vector3(0, .05f, 0), new Vector3(.43f, .15f, .40f), ivory);
                for (int i = -1; i <= 1; i++)
                    ToyGeometry.Sphere(chefHat, "Chef hat puff", new Vector3(i * .13f, .20f, 0), new Vector3(.22f, .26f, .34f), ivory);
            }
            if (role == 2 && !mayorSash)
            {
                mayorSash = ToyGeometry.Box(model, "Mayor ceremonial sash", new Vector3(0, .81f, .195f),
                    new Vector3(.12f, .53f, .055f), ToyGeometry.Hex("C890AD"));
                mayorSash.localRotation = Quaternion.Euler(0, 0, -38);
                ToyGeometry.Sphere(mayorSash, "Mayor gold pin", new Vector3(0, .17f, .035f), Vector3.one * .085f, ToyGeometry.Hex("EAC276"));
            }
            if (chefHat) chefHat.gameObject.SetActive(role == 1);
            if (mayorSash) mayorSash.gameObject.SetActive(role == 2);
        }

        void Start() { view = Camera.main; }

        void Build(Color shirt)
        {
            Color cream = ToyGeometry.Hex("FFF1D7");
            Color navy = ToyGeometry.Hex("3E5967");
            Color skin = ToyGeometry.Hex("F1BC95");
            float hue, saturation, value;
            Color.RGBToHSV(shirt, out hue, out saturation, out value);
            Color hair = hue < .2f ? ToyGeometry.Hex("88624F") : hue < .6f ? ToyGeometry.Hex("6C655E") : ToyGeometry.Hex("805D50");
            model = ToyGeometry.Group(transform, "Breathing cartoon model", Vector3.zero);
            foreach (float x in new[] { -.12f, .12f })
            {
                ToyGeometry.Capsule(model, "Trouser leg", new Vector3(x, .33f, 0), new Vector3(.16f, .48f, .18f), navy);
                ToyGeometry.Sphere(model, "Soft shoe", new Vector3(x, .10f, .045f), new Vector3(.22f, .17f, .33f), cream);
            }
            ToyGeometry.Capsule(model, "Cozy shirt", new Vector3(0, .81f, 0), new Vector3(.51f, .65f, .37f), shirt);
            ToyGeometry.Sphere(model, "Shirt button", new Vector3(0, .83f, .19f), new Vector3(.035f, .035f, .025f), cream);
            ToyGeometry.Sphere(model, "Shirt button", new Vector3(0, .72f, .19f), new Vector3(.035f, .035f, .025f), cream);
            ToyGeometry.Cylinder(model, "Neck", new Vector3(0, 1.08f, 0), new Vector3(.15f, .17f, .15f), skin);
            head = ToyGeometry.Group(model, "Expressive head", new Vector3(0, 1.30f, 0));
            ToyGeometry.Sphere(head, "Face", Vector3.zero, new Vector3(.49f, .51f, .46f), skin);
            ToyGeometry.Sphere(head, "Hair cap", new Vector3(0, .16f, -.035f), new Vector3(.52f, .28f, .48f), hair);
            ToyGeometry.Sphere(head, "Hair curl", new Vector3(-.14f, .19f, .09f), new Vector3(.22f, .17f, .20f), hair);
            ToyGeometry.Sphere(head, "Hair curl", new Vector3(.025f, .225f, .065f), new Vector3(.21f, .17f, .20f), hair);
            foreach (float x in new[] { -.245f, .245f })
                ToyGeometry.Sphere(head, "Ear", new Vector3(x, -.01f, 0), new Vector3(.095f, .14f, .10f), skin);
            leftEye = ToyGeometry.Sphere(head, "Left eye", new Vector3(-.085f, .035f, .214f), new Vector3(.039f, .056f, .024f), navy);
            rightEye = ToyGeometry.Sphere(head, "Right eye", new Vector3(.085f, .035f, .214f), new Vector3(.039f, .056f, .024f), navy);
            ToyGeometry.Sphere(head, "Nose", new Vector3(0, -.01f, .238f), new Vector3(.07f, .065f, .065f), skin);
            mouth = ToyGeometry.Sphere(head, "Reaction mouth", new Vector3(0, -.095f, .215f), new Vector3(.09f, .025f, .023f), navy);
            foreach (float x in new[] { -.15f, .15f })
                ToyGeometry.Sphere(head, "Rosy cheek", new Vector3(x, -.065f, .192f), new Vector3(.075f, .035f, .02f), ToyGeometry.Hex("E8A28C"));
            leftBrow = ToyGeometry.Box(head, "Left brow", new Vector3(-.085f, .104f, .20f), new Vector3(.085f, .018f, .018f), hair);
            rightBrow = ToyGeometry.Box(head, "Right brow", new Vector3(.085f, .104f, .20f), new Vector3(.085f, .018f, .018f), hair);
            leftArm = Arm(model, "Left arm", -.28f, shirt, skin);
            rightArm = Arm(model, "Right arm", .28f, shirt, skin);
            BuildPopup();
            ResetPose();
        }

        static Transform Arm(Transform parent, string name, float x, Color shirt, Color skin)
        {
            Transform arm = ToyGeometry.Group(parent, name, new Vector3(x, .98f, 0));
            ToyGeometry.Capsule(arm, "Sleeve", new Vector3(0, -.12f, 0), new Vector3(.17f, .31f, .18f), shirt);
            ToyGeometry.Capsule(arm, "Forearm", new Vector3(0, -.26f, .005f), new Vector3(.13f, .21f, .14f), skin);
            ToyGeometry.Sphere(arm, "Hand", new Vector3(0, -.36f, .01f), new Vector3(.15f, .15f, .15f), skin);
            return arm;
        }

        void BuildPopup()
        {
            Color cream = ToyGeometry.Hex("FFF6DF");
            Color ink = ToyGeometry.Hex("3C5560");
            popup = ToyGeometry.Group(transform, "Customer reaction bubble", new Vector3(0, 1.98f, 0));
            ToyGeometry.Sphere(popup, "Soft speech bubble", Vector3.zero, new Vector3(.52f, .42f, .15f), cream);
            Transform tail = ToyGeometry.Box(popup, "Speech tail", new Vector3(-.10f, -.18f, -.015f), new Vector3(.11f, .11f, .10f), cream);
            tail.localRotation = Quaternion.Euler(0, 0, 35);
            happyIcon = ToyGeometry.Group(popup, "Happy heart", new Vector3(0, 0, .09f));
            Color pink = ToyGeometry.Hex("E88E9F");
            ToyGeometry.Sphere(happyIcon, "Heart lobe", new Vector3(-.065f, .035f, 0), new Vector3(.16f, .16f, .04f), pink);
            ToyGeometry.Sphere(happyIcon, "Heart lobe", new Vector3(.065f, .035f, 0), new Vector3(.16f, .16f, .04f), pink);
            Transform heart = ToyGeometry.Box(happyIcon, "Heart point", new Vector3(0, -.035f, 0), new Vector3(.16f, .16f, .04f), pink);
            heart.localRotation = Quaternion.Euler(0, 0, 45);
            mildIcon = ToyGeometry.Group(popup, "Considering delivery", new Vector3(0, 0, .09f));
            for (int i = 0; i < 3; i++)
                ToyGeometry.Sphere(mildIcon, "Considering dot", new Vector3((i - 1) * .115f, 0, 0), new Vector3(.06f, .06f, .04f), ink);
            shockedIcon = ToyGeometry.Group(popup, "Oh no", new Vector3(0, 0, .09f));
            ToyGeometry.Box(shockedIcon, "Exclamation stroke", new Vector3(0, .055f, 0), new Vector3(.055f, .16f, .04f), ink);
            ToyGeometry.Sphere(shockedIcon, "Exclamation dot", new Vector3(0, -.09f, 0), new Vector3(.06f, .06f, .04f), ink);
            popup.gameObject.SetActive(false);
        }

        public void React(float integrity)
        {
            if (!model) return;
            reaction = integrity >= 90 ? 1 : integrity >= 60 ? 2 : 3;
            remaining = 2.2f;
            elapsed = 0;
            armBlend = 1;
            popup.gameObject.SetActive(true);
            happyIcon.gameObject.SetActive(reaction == 1);
            mildIcon.gameObject.SetActive(reaction == 2);
            shockedIcon.gameObject.SetActive(reaction == 3);
            // Results pause the simulation. The reaction must already be visible
            // in this frame, without relying on a later autonomous Update.
            ApplyPose(false);
        }

        public void ResetPose()
        {
            remaining = 0; elapsed = 0; reaction = 0; armBlend = 0;
            if (!model) return;
            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.identity;
            head.localRotation = Quaternion.identity;
            leftArm.localRotation = Quaternion.Euler(-8, 0, -10);
            rightArm.localRotation = Quaternion.Euler(-8, 0, 10);
            mouth.localScale = new Vector3(.09f, .025f, .023f);
            mouth.localRotation = Quaternion.identity;
            leftEye.localScale = rightEye.localScale = new Vector3(.039f, .056f, .024f);
            leftBrow.localRotation = rightBrow.localRotation = Quaternion.identity;
            if (popup) popup.gameObject.SetActive(false);
        }

        public void Tick(float dt, bool playing, bool reducedMotion)
        {
            if (!model || !playing || float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0) return;
            ReducedMotion = reducedMotion;
            dt = Mathf.Min(dt, 60);
            elapsed += dt;
            remaining = Mathf.Max(0, remaining - dt);
            bool active = remaining > 0;
            armBlend = Mathf.MoveTowards(armBlend, active ? 1 : 0, dt * (active ? 5 : 3));
            ApplyPose(true);
        }

        void ApplyPose(bool animate)
        {
            bool active = remaining > 0;
            float jump = animate && !ReducedMotion && reaction == 1 && active && elapsed < .60f ? Mathf.Sin(elapsed / .60f * Mathf.PI) * .18f : 0;
            float breathe = !animate || ReducedMotion || (reaction == 1 && active) ? 0 : Mathf.Sin(elapsed * 2.2f) * .012f;
            model.localPosition = new Vector3(0, breathe + jump, 0);
            float arms = reaction == 1 ? 148 : reaction == 2 ? 30 : 160;
            float pitch = reaction == 3 ? -18 : reaction == 1 ? -8 : -14;
            leftArm.localRotation = Quaternion.Euler(Mathf.Lerp(-8, pitch, armBlend), 0, -Mathf.Lerp(10, arms, armBlend));
            rightArm.localRotation = Quaternion.Euler(Mathf.Lerp(-8, pitch, armBlend), 0, Mathf.Lerp(10, arms, armBlend));
            float headTilt = reaction == 2 ? -15 : reaction == 1 ? 7 : ReducedMotion || !animate ? 0 : Mathf.Sin(elapsed * 13) * 4;
            head.localRotation = Quaternion.Euler(reaction == 3 ? -armBlend * 8 : 0, 0, headTilt * armBlend);
            Vector3 normalMouth = new Vector3(.09f, .025f, .023f);
            Vector3 reactionMouth = reaction == 3 ? new Vector3(.082f, .115f, .026f) : reaction == 1 ? new Vector3(.105f, .065f, .026f) : new Vector3(.065f, .027f, .024f);
            mouth.localScale = Vector3.Lerp(normalMouth, reactionMouth, armBlend);
            mouth.localRotation = Quaternion.Euler(0, 0, reaction == 2 ? -armBlend * 13 : 0);
            Vector3 eye = Vector3.Lerp(new Vector3(.039f, .056f, .024f), reaction == 1 ? new Vector3(.055f, .025f, .024f) : reaction == 3 ? new Vector3(.05f, .08f, .024f) : new Vector3(.039f, .046f, .024f), armBlend);
            leftEye.localScale = rightEye.localScale = eye;
            leftBrow.localRotation = Quaternion.Euler(0, 0, (reaction == 3 ? -25 : reaction == 2 ? 15 : -8) * armBlend);
            rightBrow.localRotation = Quaternion.Euler(0, 0, (reaction == 3 ? 25 : reaction == 2 ? 0 : 8) * armBlend);
            if (popup)
            {
                if (popup.gameObject.activeSelf != active) popup.gameObject.SetActive(active);
                if (active)
                {
                    if (!view) view = Camera.main;
                    if (view) popup.rotation = Quaternion.LookRotation(-view.transform.forward, view.transform.up);
                    float pop = ReducedMotion || !animate ? 1 : .92f + Mathf.Clamp01(elapsed * 7) * .08f;
                    popup.localScale = Vector3.one * (pop * (1 + (ReducedMotion || !animate ? 0 : Mathf.Sin(elapsed * 8) * .035f)));
                }
            }
        }
    }
}
