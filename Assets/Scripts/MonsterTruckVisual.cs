using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>Original toy parcel truck. Geometry and animation only; CourierGame owns all collision and rewards.</summary>
    public sealed class MonsterTruckVisual : MonoBehaviour
    {
        public Transform CargoAnchor { get; private set; }
        public float WheelRotation => wheelAngle;
        Transform body;
        readonly Transform[] wheels = new Transform[4];
        Material paint;
        float wheelAngle;
        static readonly Color Cream = ToyGeometry.Hex("FFF0CE");
        static readonly Color Ink = ToyGeometry.Hex("344D5B");
        static readonly Color Rubber = ToyGeometry.Hex("38454E");
        static readonly Color Tread = ToyGeometry.Hex("46515B");
        static readonly Color Glass = ToyGeometry.Hex("8CC4CF");
        static readonly Color Gold = ToyGeometry.Hex("F3BD50");
        static readonly Color Coral = ToyGeometry.Hex("E9917A");

        public static MonsterTruckVisual Create(Transform parent)
        {
            Transform root = ToyGeometry.Group(parent, "Big-wheel parcel courier", Vector3.zero);
            var truck = root.gameObject.AddComponent<MonsterTruckVisual>();
            truck.Build();
            return truck;
        }

        void Build()
        {
            paint = new Material(Shader.Find("Standard"));
            paint.name = "Personal big-wheel truck paint";
            paint.color = ToyGeometry.Hex("86C8B8");
            paint.SetFloat("_Glossiness", .13f);
            paint.SetFloat("_SpecularHighlights", 0);
            body = ToyGeometry.Group(transform, "Bouncy toy truck suspension", Vector3.zero);

            // Raised frame and four visible springs sit between oversized wheels.
            ToyGeometry.Box(body, "Raised chassis", new Vector3(0, .91f, -.03f), new Vector3(1.22f, .15f, 1.86f), Ink);
            for (int axle = 0; axle < 2; axle++)
            {
                float z = axle == 0 ? -.75f : .75f;
                Transform bar = ToyGeometry.Cylinder(transform, "Solid toy axle", new Vector3(0, .51f, z), new Vector3(.10f, 1.52f, .10f), Ink);
                bar.localRotation = Quaternion.Euler(0, 0, 90);
                foreach (float side in new[] { -1f, 1f })
                {
                    ToyGeometry.Cylinder(body, "Suspension rod", new Vector3(side * .46f, .73f, z), new Vector3(.055f, .46f, .055f), Cream);
                    for (int spring = 0; spring < 3; spring++)
                        ToyGeometry.Cylinder(body, "Chunky gold spring", new Vector3(side * .46f, .58f + spring * .145f, z), new Vector3(.17f, .026f, .17f), Gold);
                }
            }

            Paint(ToyGeometry.Box(body, "Rounded pickup sill", new Vector3(0, 1.11f, -.04f), new Vector3(1.20f, .30f, 1.91f), Cream));
            Paint(ToyGeometry.Sphere(body, "Soft bonnet", new Vector3(0, 1.30f, .72f), new Vector3(1.19f, .32f, .61f), Cream));
            Paint(ToyGeometry.Box(body, "Tall friendly cab", new Vector3(0, 1.49f, .29f), new Vector3(1.15f, .54f, .77f), Cream));
            Paint(ToyGeometry.Sphere(body, "Squishy cab roof", new Vector3(0, 1.77f, .27f), new Vector3(1.22f, .24f, .85f), Cream));
            ToyGeometry.Box(body, "Front wide windscreen", new Vector3(0, 1.53f, .689f), new Vector3(.91f, .32f, .035f), Glass);
            ToyGeometry.Box(body, "Windscreen centre stripe", new Vector3(.13f, 1.53f, .712f), new Vector3(.045f, .33f, .018f), Cream);
            ToyGeometry.Box(body, "Cream toy roof stripe", new Vector3(0, 1.895f, .27f), new Vector3(.21f, .025f, .67f), Cream);
            ToyGeometry.Sphere(body, "Roof delivery light", new Vector3(0, 1.96f, .32f), new Vector3(.20f, .14f, .20f), Gold);

            ToyGeometry.Box(body, "Cream front bumper", new Vector3(0, 1.00f, 1.045f), new Vector3(1.37f, .19f, .14f), Cream);
            ToyGeometry.Box(body, "Smiling grille", new Vector3(0, 1.245f, 1.035f), new Vector3(.43f, .14f, .025f), Ink);
            for (int i = 0; i < 3; i++)
                ToyGeometry.Box(body, "Grille tooth", new Vector3(-.13f + .13f * i, 1.27f, 1.054f), new Vector3(.055f, .065f, .018f), Cream);
            foreach (float x in new[] { -.43f, .43f })
            {
                ToyGeometry.Sphere(body, "Big round headlight", new Vector3(x, 1.28f, 1.025f), new Vector3(.24f, .26f, .08f), Cream);
                ToyGeometry.Sphere(body, "Headlight warm centre", new Vector3(x, 1.28f, 1.073f), new Vector3(.12f, .14f, .02f), Gold);
            }

            // Open rear carrier makes the strapped parcel visible from the isometric camera.
            ToyGeometry.Box(body, "Rear parcel tray", new Vector3(0, 1.265f, -.57f), new Vector3(1.14f, .11f, 1.03f), Cream);
            foreach (float side in new[] { -1f, 1f })
            {
                Paint(ToyGeometry.Box(body, "Pickup side panel", new Vector3(side * .57f, 1.40f, -.56f), new Vector3(.10f, .30f, 1.03f), Cream));
                ToyGeometry.Box(body, "Cream cargo side rail", new Vector3(side * .59f, 1.57f, -.56f), new Vector3(.13f, .065f, 1.06f), Cream);
                ToyGeometry.Box(body, "Cab side window", new Vector3(side * .585f, 1.53f, .29f), new Vector3(.025f, .31f, .52f), Glass);
                ToyGeometry.Box(body, "Cab door handle", new Vector3(side * .605f, 1.26f, .17f), new Vector3(.025f, .035f, .16f), Ink);
                ToyGeometry.Box(body, "Mirror stalk", new Vector3(side * .625f, 1.50f, .62f), new Vector3(.08f, .03f, .03f), Ink);
                ToyGeometry.Sphere(body, "Cream mirror", new Vector3(side * .68f, 1.55f, .62f), new Vector3(.075f, .15f, .12f), Cream);
                ToyGeometry.Sphere(body, "Door parcel badge", new Vector3(side * .625f, 1.25f, -.56f), new Vector3(.025f, .22f, .27f), Cream);
                ToyGeometry.Box(body, "Door badge parcel", new Vector3(side * .644f, 1.25f, -.56f), new Vector3(.018f, .12f, .14f), Gold);
            }
            Paint(ToyGeometry.Box(body, "Little tailgate", new Vector3(0, 1.39f, -1.045f), new Vector3(1.10f, .28f, .10f), Cream));
            ToyGeometry.Box(body, "Rear cream bumper", new Vector3(0, 1.01f, -1.10f), new Vector3(1.36f, .17f, .12f), Cream);
            foreach (float x in new[] { -.44f, .44f })
                ToyGeometry.Box(body, "Rear light", new Vector3(x, 1.37f, -1.102f), new Vector3(.13f, .12f, .025f), Coral);

            CargoAnchor = ToyGeometry.Group(body, "Strapped rear parcel anchor", new Vector3(0, 1.36f, -.55f));
            // These two rounded tie-down frames visually protect the standard parcel envelope.
            foreach (float z in new[] { -.77f, -.34f })
            {
                ToyGeometry.Box(body, "Cargo strap upright", new Vector3(-.44f, 1.70f, z), new Vector3(.045f, .76f, .055f), Gold);
                ToyGeometry.Box(body, "Cargo strap upright", new Vector3(.44f, 1.70f, z), new Vector3(.045f, .76f, .055f), Gold);
                ToyGeometry.Box(body, "Cargo strap bridge", new Vector3(0, 2.09f, z), new Vector3(.92f, .045f, .055f), Gold);
            }

            // A friendly courier face stays visible through the stylized front glass.
            Color skin = ToyGeometry.Hex("F1BC93");
            ToyGeometry.Sphere(body, "Courier face", new Vector3(-.22f, 1.54f, .716f), new Vector3(.22f, .22f, .055f), skin);
            ToyGeometry.Sphere(body, "Courier cap", new Vector3(-.22f, 1.65f, .727f), new Vector3(.25f, .10f, .06f), Cream);
            foreach (float x in new[] { -.26f, -.18f })
                ToyGeometry.Sphere(body, "Courier eye", new Vector3(x, 1.56f, .749f), new Vector3(.026f, .034f, .015f), Ink);

            for (int i = 0; i < 4; i++) BuildWheel(i);
        }

        void BuildWheel(int index)
        {
            float side = index % 2 == 0 ? -1 : 1;
            float z = index < 2 ? -.75f : .75f;
            Transform wheel = ToyGeometry.Group(transform, "Monster wheel " + (index + 1), new Vector3(side * .76f, .51f, z));
            wheels[index] = wheel;
            Transform tyre = ToyGeometry.Cylinder(wheel, "Oversized soft tyre", Vector3.zero, new Vector3(.98f, .34f, .98f), Rubber);
            tyre.localRotation = Quaternion.Euler(0, 0, 90);
            Transform hub = ToyGeometry.Cylinder(wheel, "Cream monster hub", new Vector3(side * .177f, 0, 0), new Vector3(.51f, .022f, .51f), Cream);
            hub.localRotation = Quaternion.Euler(0, 0, 90);
            Transform cap = ToyGeometry.Cylinder(wheel, "Gold axle cap", new Vector3(side * .205f, 0, 0), new Vector3(.18f, .016f, .18f), Gold);
            cap.localRotation = Quaternion.Euler(0, 0, 90);
            for (int i = 0; i < 14; i++)
            {
                float angle = i * Mathf.PI * 2 / 14;
                Vector3 point = new Vector3(0, Mathf.Sin(angle) * .477f, Mathf.Cos(angle) * .477f);
                Transform lug = ToyGeometry.Box(wheel, "Chunky tyre tread", point, new Vector3(.345f, .077f, .16f), Tread);
                lug.localRotation = Quaternion.Euler(90 - angle * Mathf.Rad2Deg, 0, 0);
            }
        }

        void Paint(Transform part) { part.GetComponent<Renderer>().sharedMaterial = paint; }
        public void SetColor(Color color) { if (paint) paint.color = color; }

        public void Animate(float speed01, float turn01, bool reducedMotion)
        {
            float speed = Mathf.Clamp01(speed01);
            body.localPosition = new Vector3(0, reducedMotion ? 0 : Mathf.Sin(Time.time * 12) * .018f * speed, 0);
            body.localRotation = Quaternion.Euler(reducedMotion ? 0 : Mathf.Sin(Time.time * 8) * speed * .6f, 0,
                reducedMotion ? 0 : -Mathf.Clamp(turn01, -1, 1) * 3.2f);
            if (speed <= .001f) return;
            wheelAngle = Mathf.Repeat(wheelAngle + speed * Time.deltaTime * 560, 360);
            foreach (Transform wheel in wheels) if (wheel) wheel.localRotation = Quaternion.Euler(wheelAngle, 0, 0);
        }

        void OnDestroy() { if (paint) Destroy(paint); }
    }
}
