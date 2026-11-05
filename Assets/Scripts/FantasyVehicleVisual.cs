using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>Original soft toy couriers. Geometry and a single reusable delivery effect only.</summary>
    public sealed class FantasyVehicleVisual : MonoBehaviour
    {
        public Transform CargoAnchor { get; private set; }
        public bool IsDragon { get; private set; }
        public bool ParachuteVisible => parachute && parachute.gameObject.activeSelf;
        Transform body, leftWing, rightWing, tail, parachute, flames;
        Transform[] wheels;
        readonly Transform[] legs = new Transform[4];
        Material paint;
        float gait, parachuteTime, wingFold, wheelSpin;
        Vector3 parachuteDestination;
        static readonly Color Cream = ToyGeometry.Hex("FFF0D2");
        static readonly Color Ink = ToyGeometry.Hex("365566");
        static readonly Color Gold = ToyGeometry.Hex("EFC777");
        static readonly Color Coral = ToyGeometry.Hex("E99F9A");
        static readonly Color Purple = ToyGeometry.Hex("A69BCB");

        public static FantasyVehicleVisual Create(Transform parent, bool dragon)
        {
            Transform root = ToyGeometry.Group(parent, dragon ? "Cloud parcel dragon" : "Rocket parcel racer", Vector3.zero);
            var creature = root.gameObject.AddComponent<FantasyVehicleVisual>();
            creature.IsDragon = dragon;
            creature.Build();
            return creature;
        }

        void Build()
        {
            paint = new Material(Shader.Find("Standard"));
            paint.name = IsDragon ? "Personal cloud dragon paint" : "Personal rocket racer paint";
            paint.color = ToyGeometry.Hex(IsDragon ? "B1A1D5" : "E8574A");
            paint.SetFloat("_Glossiness", .12f);
            paint.SetFloat("_SpecularHighlights", 0);
            body = ToyGeometry.Group(transform, "Soft courier body", Vector3.zero);
            if (IsDragon) BuildDragon(); else BuildRacer();
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

        // The non-dragon fantasy ride is a cartoon rocket racer: low, wide-wheeled,
        // with a gold spoiler and exhaust flames that grow with speed.
        void BuildRacer()
        {
            Paint(ToyGeometry.Box(body, "Racer body", new Vector3(0, .42f, 0), new Vector3(1.05f, .34f, 2.05f), Cream));
            Paint(ToyGeometry.Sphere(body, "Racer nose cone", new Vector3(0, .42f, 1.02f), new Vector3(.86f, .3f, .72f), Cream));
            Paint(ToyGeometry.Box(body, "Racer side pod", new Vector3(-.5f, .38f, -.15f), new Vector3(.3f, .28f, 1.1f), Cream));
            Paint(ToyGeometry.Box(body, "Racer side pod", new Vector3(.5f, .38f, -.15f), new Vector3(.3f, .28f, 1.1f), Cream));
            ToyGeometry.Box(body, "Racing stripe", new Vector3(-.13f, .6f, .1f), new Vector3(.12f, .02f, 1.9f), Cream);
            ToyGeometry.Box(body, "Racing stripe", new Vector3(.13f, .6f, .1f), new Vector3(.12f, .02f, 1.9f), Cream);
            ToyGeometry.Sphere(body, "Race number roundel", new Vector3(.54f, .45f, .35f), new Vector3(.04f, .26f, .26f), Cream);
            ToyGeometry.Sphere(body, "Race number roundel", new Vector3(-.54f, .45f, .35f), new Vector3(.04f, .26f, .26f), Cream);
            ToyGeometry.Box(body, "Cockpit rim", new Vector3(0, .62f, -.15f), new Vector3(.62f, .1f, .8f), Ink);
            ToyGeometry.Sphere(body, "Windscreen bubble", new Vector3(0, .7f, .3f), new Vector3(.55f, .3f, .3f), ToyGeometry.Hex("BFE3EE"));
            // Wheels: bigger at the back, like a proper racer.
            wheels = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                bool rear = i >= 2;
                float side = i % 2 == 0 ? -1 : 1, size = rear ? .62f : .5f;
                Transform wheel = ToyGeometry.Group(body, "Racer wheel", new Vector3(side * .68f, size * .5f, rear ? -.68f : .72f));
                Transform tyre = ToyGeometry.Cylinder(wheel, "Racer tyre", Vector3.zero, new Vector3(size, .28f, size), Ink);
                tyre.localRotation = Quaternion.Euler(0, 0, 90);
                Transform hub = ToyGeometry.Cylinder(wheel, "Gold hub", new Vector3(side * .15f, 0, 0), new Vector3(size * .45f, .02f, size * .45f), Gold);
                hub.localRotation = Quaternion.Euler(0, 0, 90);
                ToyGeometry.Box(wheel, "Hub spoke", new Vector3(side * .16f, 0, 0), new Vector3(.02f, size * .5f, .06f), Cream);
                wheels[i] = wheel;
            }
            foreach (float side in new[] { -1f, 1f })
                ToyGeometry.Box(body, "Spoiler post", new Vector3(side * .32f, .78f, -.95f), new Vector3(.07f, .36f, .1f), Ink);
            ToyGeometry.Box(body, "Gold rear spoiler", new Vector3(0, .98f, -1.0f), new Vector3(1.3f, .07f, .34f), Gold);
            ToyGeometry.Box(body, "Spoiler end plate", new Vector3(-.66f, .95f, -1.0f), new Vector3(.04f, .2f, .36f), Coral);
            ToyGeometry.Box(body, "Spoiler end plate", new Vector3(.66f, .95f, -1.0f), new Vector3(.04f, .2f, .36f), Coral);
            flames = ToyGeometry.Group(body, "Exhaust flames", new Vector3(0, .42f, -1.08f));
            foreach (float side in new[] { -1f, 1f })
            {
                ToyGeometry.Cylinder(body, "Exhaust pipe", new Vector3(side * .22f, .42f, -1.02f), new Vector3(.14f, .05f, .14f), ToyGeometry.Hex("C9CED3")).localRotation = Quaternion.Euler(90, 0, 0);
                ToyGeometry.Sphere(flames, "Exhaust flame", new Vector3(side * .22f, 0, -.12f), new Vector3(.16f, .16f, .3f), Gold);
                ToyGeometry.Sphere(flames, "Exhaust flame core", new Vector3(side * .22f, 0, -.06f), new Vector3(.09f, .09f, .16f), Coral);
            }
            // A small parcel rack behind the cockpit.
            ToyGeometry.Box(body, "Parcel rack", new Vector3(0, .62f, -.62f), new Vector3(.62f, .06f, .5f), Ink);
            CargoAnchor = ToyGeometry.Group(body, "Racer cargo anchor", new Vector3(0, .66f, -.62f));
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
            float y = IsDragon ? 2.41f : .5f;
            Transform rider = ToyGeometry.Group(body, "Tiny helmeted parcel rider", new Vector3(0, y, IsDragon ? -.72f : -.12f));
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

        public void SetColor(Color color) { if (paint) paint.color = Color.Lerp(color, IsDragon ? Purple : ToyGeometry.Hex("E8574A"), IsDragon ? .38f : .72f); }
        void Paint(Transform part) { part.GetComponent<Renderer>().sharedMaterial = paint; }

        public void Animate(float speed01, float turn01, bool reducedMotion, bool animate)
        {
            float speed = Mathf.Clamp01(speed01);
            if (animate && !reducedMotion) gait += Time.deltaTime * (IsDragon ? 4.8f : 3 + speed * 7);
            float wave = reducedMotion ? 0 : Mathf.Sin(gait);
            body.localPosition = new Vector3(0, reducedMotion ? 0 : wave * (IsDragon ? .065f : .012f * speed), 0);
            body.localRotation = Quaternion.Euler(0, 0, reducedMotion ? 0 : -Mathf.Clamp(turn01, -1, 1) * 3);
            // Wings fold up while riding so the dragon fits the same lane as every
            // other vehicle; they open again when it slows down or stops.
            float targetFold = speed > .15f ? 1 : 0;
            wingFold = reducedMotion || !animate ? targetFold : Mathf.MoveTowards(wingFold, targetFold, Time.deltaTime * 4);
            float flap = reducedMotion ? 0 : wave * 13 * (1 - wingFold);
            if (leftWing)
            {
                leftWing.localRotation = Quaternion.Euler(0, 0, -flap - 72 * wingFold);
                leftWing.localScale = new Vector3(Mathf.Lerp(1, .5f, wingFold), 1, 1);
            }
            if (rightWing)
            {
                rightWing.localRotation = Quaternion.Euler(0, 0, flap + 72 * wingFold);
                rightWing.localScale = new Vector3(Mathf.Lerp(1, .5f, wingFold), 1, 1);
            }
            if (tail) tail.localRotation = Quaternion.Euler(0, reducedMotion ? 0 : wave * 5 * (IsDragon ? 1 : speed), 0);
            if (wheels != null)
            {
                if (animate && !reducedMotion) wheelSpin = Mathf.Repeat(wheelSpin + Time.deltaTime * speed * 1100, 360);
                foreach (Transform wheel in wheels) wheel.localRotation = Quaternion.Euler(wheelSpin, 0, 0);
            }
            if (flames)
                flames.localScale = Vector3.one * (reducedMotion ? .6f : Mathf.Lerp(.3f, 1.35f, speed) * (1 + .18f * Mathf.Sin(Time.time * 41)));
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
