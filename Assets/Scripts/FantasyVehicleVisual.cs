using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>Original soft toy couriers. Geometry and a single reusable delivery effect only.</summary>
    public sealed class FantasyVehicleVisual : MonoBehaviour
    {
        public Transform CargoAnchor { get; private set; }
        public bool IsDragon { get; private set; }
        public bool ParachuteVisible => parachute && parachute.gameObject.activeSelf;
        Transform body, leftWing, rightWing, tail, parachute;
        readonly Transform[] legs = new Transform[4];
        Material paint;
        float gait, parachuteTime;
        Vector3 parachuteDestination;
        static readonly Color Cream = ToyGeometry.Hex("FFF0D2");
        static readonly Color Ink = ToyGeometry.Hex("365566");
        static readonly Color Gold = ToyGeometry.Hex("EFC777");
        static readonly Color Coral = ToyGeometry.Hex("E99F9A");
        static readonly Color Purple = ToyGeometry.Hex("A69BCB");

        public static FantasyVehicleVisual Create(Transform parent, bool dragon)
        {
            Transform root = ToyGeometry.Group(parent, dragon ? "Cloud parcel dragon" : "Gentle parcel dinosaur", Vector3.zero);
            var creature = root.gameObject.AddComponent<FantasyVehicleVisual>();
            creature.IsDragon = dragon;
            creature.Build();
            return creature;
        }

        void Build()
        {
            paint = new Material(Shader.Find("Standard"));
            paint.name = IsDragon ? "Personal cloud dragon paint" : "Personal gentle dinosaur paint";
            paint.color = ToyGeometry.Hex(IsDragon ? "B1A1D5" : "A5C89E");
            paint.SetFloat("_Glossiness", .12f);
            paint.SetFloat("_SpecularHighlights", 0);
            body = ToyGeometry.Group(transform, "Soft courier body", Vector3.zero);
            if (IsDragon) BuildDragon(); else BuildDinosaur();
            BuildRider();
            if (IsDragon) BuildParachute();
        }

        void BuildDragon()
        {
            Paint(ToyGeometry.Sphere(body, "Pear-shaped flying body", new Vector3(0, 1.96f, -.12f), new Vector3(1.12f, .80f, 1.48f), Cream));
            ToyGeometry.Sphere(body, "Cream dragon belly", new Vector3(0, 1.80f, .34f), new Vector3(.79f, .45f, .74f), Cream);
            Paint(ToyGeometry.Sphere(body, "Friendly dragon head", new Vector3(0, 2.20f, .75f), new Vector3(.87f, .76f, .83f), Cream));
            Paint(ToyGeometry.Sphere(body, "Rounded dragon muzzle", new Vector3(0, 2.06f, 1.10f), new Vector3(.81f, .43f, .53f), Cream));
            Face(body, 2.22f, 1.075f, .245f);
            foreach (float side in new[] { -1f, 1f })
            {
                ToyGeometry.Sphere(body, "Tiny nostril", new Vector3(side * .20f, 2.15f, 1.355f), new Vector3(.065f, .045f, .025f), Ink);
                Transform horn = ToyGeometry.Capsule(body, "Soft cream dragon horn", new Vector3(side * .28f, 2.65f, .58f), new Vector3(.13f, .28f, .13f), Cream);
                horn.localRotation = Quaternion.Euler(-16, 0, side * -19);
                ToyGeometry.Sphere(body, "Rosy cheek", new Vector3(side * .35f, 2.06f, 1.13f), new Vector3(.16f, .085f, .03f), Coral);
                Transform foot = ToyGeometry.Group(body, "Tucked flying foot", new Vector3(side * .39f, 1.64f, -.18f));
                Paint(ToyGeometry.Capsule(foot, "Tucked dragon leg", Vector3.zero, new Vector3(.24f, .34f, .28f), Cream));
                ToyGeometry.Sphere(foot, "Cream little claws", new Vector3(0, -.16f, .10f), new Vector3(.27f, .10f, .23f), Cream);
            }
            tail = ToyGeometry.Group(body, "Curled dragon tail", new Vector3(0, 1.98f, -.74f));
            Paint(ToyGeometry.Capsule(tail, "Soft tail base", new Vector3(0, -.02f, -.29f), new Vector3(.36f, .72f, .36f), Cream));
            tail.GetChild(0).localRotation = Quaternion.Euler(80, 0, 0);
            Paint(ToyGeometry.Sphere(tail, "Curled tail tip", new Vector3(0, .07f, -.70f), new Vector3(.26f, .28f, .44f), Cream));
            for (int i = 0; i < 4; i++)
                ToyGeometry.Sphere(body, "Golden soft back scale", new Vector3(0, 2.37f - i * .05f, .10f - i * .29f), new Vector3(.16f, .22f, .19f), Gold);
            leftWing = Wing(-1);
            rightWing = Wing(1);
            Saddle(new Vector3(0, 2.31f, -.28f), .84f, .82f);
        }

        Transform Wing(float side)
        {
            Transform wing = ToyGeometry.Group(body, "Soft dragon wing", new Vector3(side * .43f, 2.18f, -.12f));
            Transform upper = ToyGeometry.Capsule(wing, "Cream wing leading edge", new Vector3(side * .39f, .16f, -.03f), new Vector3(.12f, .90f, .12f), Cream);
            upper.localRotation = Quaternion.Euler(0, 0, side * -66);
            ToyGeometry.Sphere(wing, "Pastel wing membrane", new Vector3(side * .59f, .05f, -.23f), new Vector3(.92f, .16f, .77f), Purple);
            Transform tip = ToyGeometry.Capsule(wing, "Rounded wing tip", new Vector3(side * .94f, .12f, -.28f), new Vector3(.12f, .39f, .12f), Cream);
            tip.localRotation = Quaternion.Euler(30, 0, side * -66);
            return wing;
        }

        void BuildDinosaur()
        {
            Paint(ToyGeometry.Sphere(body, "Squishy dinosaur body", new Vector3(0, 1.03f, -.10f), new Vector3(1.27f, 1.13f, 1.57f), Cream));
            ToyGeometry.Sphere(body, "Cream dinosaur belly", new Vector3(0, .89f, .45f), new Vector3(.90f, .81f, .49f), Cream);
            Paint(ToyGeometry.Capsule(body, "Soft upright neck", new Vector3(0, 1.48f, .57f), new Vector3(.59f, .85f, .61f), Cream));
            Paint(ToyGeometry.Sphere(body, "Big gentle dinosaur head", new Vector3(0, 1.98f, .78f), new Vector3(.97f, .80f, .97f), Cream));
            ToyGeometry.Sphere(body, "Cream dinosaur snout", new Vector3(0, 1.81f, 1.13f), new Vector3(.86f, .40f, .54f), Cream);
            Face(body, 2.03f, 1.196f, .27f);
            foreach (float side in new[] { -1f, 1f })
            {
                ToyGeometry.Sphere(body, "Dinosaur nostril", new Vector3(side * .21f, 1.91f, 1.40f), new Vector3(.065f, .045f, .025f), Ink);
                ToyGeometry.Sphere(body, "Rosy dinosaur cheek", new Vector3(side * .38f, 1.87f, 1.18f), new Vector3(.15f, .10f, .045f), Coral);
                Transform arm = ToyGeometry.Capsule(body, "Tiny helpful dinosaur arm", new Vector3(side * .59f, 1.20f, .36f), new Vector3(.22f, .45f, .25f), Cream);
                arm.localRotation = Quaternion.Euler(-35, 0, side * 22);
                for (int i = 0; i < 2; i++)
                {
                    Transform leg = ToyGeometry.Group(body, "Sturdy soft dinosaur foot", new Vector3(side * .43f, .40f, i == 0 ? -.48f : .40f));
                    legs[(side < 0 ? 0 : 2) + i] = leg;
                    Paint(ToyGeometry.Capsule(leg, "Chunky dinosaur leg", Vector3.zero, new Vector3(.37f, .62f, .43f), Cream));
                    ToyGeometry.Sphere(leg, "Cream dinosaur toe", new Vector3(0, -.26f, .13f), new Vector3(.40f, .18f, .48f), Cream);
                }
            }
            tail = ToyGeometry.Group(body, "Dinosaur wagging tail", new Vector3(0, 1.03f, -.72f));
            Transform baseTail = ToyGeometry.Capsule(tail, "Plump dinosaur tail", new Vector3(0, -.04f, -.32f), new Vector3(.48f, .85f, .44f), Cream);
            baseTail.localRotation = Quaternion.Euler(80, 0, 0);
            Paint(baseTail);
            Paint(ToyGeometry.Sphere(tail, "Round dinosaur tail tip", new Vector3(0, .02f, -.77f), new Vector3(.28f, .24f, .48f), Cream));
            for (int i = 0; i < 5; i++)
                ToyGeometry.Sphere(body, "Coral soft back plate", new Vector3(0, 1.58f - i * .035f, .08f - i * .26f), new Vector3(.16f, .27f, .23f), Coral);
            Saddle(new Vector3(0, 1.60f, -.33f), 1.00f, .95f);
            ToyGeometry.Sphere(body, "Cream courier helmet", new Vector3(0, 2.39f, .74f), new Vector3(.80f, .18f, .73f), Cream);
            ToyGeometry.Box(body, "Helmet parcel badge", new Vector3(0, 2.39f, 1.11f), new Vector3(.18f, .12f, .035f), Gold);
        }

        static void Face(Transform parent, float y, float z, float spacing)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                ToyGeometry.Sphere(parent, "Gentle eye white", new Vector3(side * spacing, y, z), new Vector3(.19f, .23f, .09f), Cream);
                ToyGeometry.Sphere(parent, "Curious eye", new Vector3(side * spacing, y + .008f, z + .046f), new Vector3(.085f, .12f, .035f), Ink);
                ToyGeometry.Sphere(parent, "Eye glint", new Vector3(side * spacing - .018f, y + .035f, z + .065f), new Vector3(.025f, .029f, .012f), Color.white);
            }
        }

        void Saddle(Vector3 p, float width, float length)
        {
            ToyGeometry.Box(body, "Cream parcel saddle", p, new Vector3(width, .15f, length), Cream);
            ToyGeometry.Box(body, "Gold saddle cushion", p + Vector3.up * .10f, new Vector3(width * .84f, .11f, length * .83f), Gold);
            CargoAnchor = ToyGeometry.Group(body, "Guarded fantasy cargo anchor", p + new Vector3(0, .18f, 0));
            foreach (float side in new[] { -1f, 1f })
            {
                ToyGeometry.Box(body, "Parcel saddle side", p + new Vector3(side * width * .47f, .27f, 0), new Vector3(.065f, .55f, length), Ink);
                ToyGeometry.Box(body, "Gold cargo strap", p + new Vector3(side * .31f, .43f, 0), new Vector3(.045f, .76f, .055f), Gold);
            }
            ToyGeometry.Box(body, "Gold strap bridge", p + Vector3.up * .82f, new Vector3(.67f, .045f, .055f), Gold);
        }

        void BuildRider()
        {
            float y = IsDragon ? 2.41f : 1.68f;
            Transform rider = ToyGeometry.Group(body, "Tiny helmeted parcel rider", new Vector3(0, y, -.72f));
            ToyGeometry.Capsule(rider, "Courier jacket", new Vector3(0, .22f, 0), new Vector3(.35f, .38f, .32f), Coral);
            ToyGeometry.Sphere(rider, "Courier face", new Vector3(0, .52f, .035f), new Vector3(.31f, .33f, .30f), ToyGeometry.Hex("EDBB99"));
            ToyGeometry.Sphere(rider, "Cream courier helmet", new Vector3(0, .67f, 0), new Vector3(.38f, .18f, .35f), Cream);
            foreach (float side in new[] { -1f, 1f })
                ToyGeometry.Sphere(rider, "Rider eye", new Vector3(side * .061f, .54f, .183f), new Vector3(.033f, .046f, .018f), Ink);
        }

        void BuildParachute()
        {
            parachute = new GameObject("Reusable dragon parcel parachute").transform;
            ToyGeometry.Sphere(parachute, "Soft cream parachute canopy", new Vector3(0, 1.05f, 0), new Vector3(1.12f, .37f, .98f), Cream);
            ToyGeometry.Sphere(parachute, "Coral parachute centre", new Vector3(0, 1.14f, 0), new Vector3(.43f, .23f, .99f), Coral);
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -.35f : .35f, z = i < 2 ? -.27f : .27f;
                Transform cord = ToyGeometry.Cylinder(parachute, "Parachute cord", new Vector3(x * .6f, .59f, z * .6f), new Vector3(.015f, .72f, .015f), Ink);
                cord.localRotation = Quaternion.Euler(z * 32, 0, -x * 34);
            }
            ToyGeometry.Box(parachute, "Floating delivery parcel", new Vector3(0, .16f, 0), new Vector3(.44f, .36f, .40f), Gold);
            ToyGeometry.Box(parachute, "Floating parcel tape", new Vector3(0, .16f, 0), new Vector3(.065f, .37f, .41f), Cream);
            parachute.gameObject.SetActive(false);
        }

        public void TriggerParachute(Vector3 destination)
        {
            if (!IsDragon || !parachute) return;
            parachuteDestination = destination;
            parachuteTime = 0;
            parachute.position = destination + Vector3.up * 2.3f;
            parachute.rotation = Quaternion.identity;
            parachute.gameObject.SetActive(true);
        }

        public void StopParachute() { if (parachute) parachute.gameObject.SetActive(false); }

        public void SetColor(Color color) { if (paint) paint.color = Color.Lerp(color, IsDragon ? Purple : ToyGeometry.Hex("B9D5A1"), .38f); }
        void Paint(Transform part) { part.GetComponent<Renderer>().sharedMaterial = paint; }

        public void Animate(float speed01, float turn01, bool reducedMotion, bool animate)
        {
            float speed = Mathf.Clamp01(speed01);
            if (animate && !reducedMotion) gait += Time.deltaTime * (IsDragon ? 4.8f : 3 + speed * 7);
            float wave = reducedMotion ? 0 : Mathf.Sin(gait);
            body.localPosition = new Vector3(0, reducedMotion ? 0 : wave * (IsDragon ? .065f : .024f * speed), 0);
            body.localRotation = Quaternion.Euler(0, 0, reducedMotion ? 0 : -Mathf.Clamp(turn01, -1, 1) * 3);
            if (leftWing) leftWing.localRotation = Quaternion.Euler(0, 0, reducedMotion ? 0 : -wave * 13);
            if (rightWing) rightWing.localRotation = Quaternion.Euler(0, 0, reducedMotion ? 0 : wave * 13);
            if (tail) tail.localRotation = Quaternion.Euler(0, reducedMotion ? 0 : wave * 5 * (IsDragon ? 1 : speed), 0);
            for (int i = 0; i < legs.Length; i++)
                if (legs[i]) legs[i].localRotation = Quaternion.Euler(reducedMotion ? 0 : Mathf.Sin(gait + (i % 3 == 0 ? 0 : Mathf.PI)) * speed * 12, 0, 0);
            if (!parachute || !parachute.gameObject.activeSelf) return;
            if (reducedMotion) { parachute.gameObject.SetActive(false); return; }
            if (!animate) return;
            parachuteTime += Time.deltaTime;
            float t = Mathf.Clamp01(parachuteTime / 1.75f);
            parachute.position = parachuteDestination + new Vector3(Mathf.Sin(t * Mathf.PI * 2) * .10f, Mathf.Lerp(2.3f, .04f, t), 0);
            if (t >= 1) parachute.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (paint) Destroy(paint);
            if (parachute) Destroy(parachute.gameObject);
        }
    }
}
