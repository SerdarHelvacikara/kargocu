using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>Soft delivery van model only. CourierGame owns movement and its collision capsule.</summary>
    public sealed class MinivanVisual : MonoBehaviour
    {
        public Transform CargoAnchor { get; private set; }
        Transform body;
        readonly Transform[] wheels = new Transform[4];
        Material paint;
        float wheelAngle;
        static readonly Color Cream = ToyGeometry.Hex("FFF1D5");
        static readonly Color Ink = ToyGeometry.Hex("385462");
        static readonly Color Glass = ToyGeometry.Hex("87B8C2");

        public static MinivanVisual Create(Transform parent)
        {
            Transform root = ToyGeometry.Group(parent, "Soft little delivery minivan", Vector3.zero);
            var van = root.gameObject.AddComponent<MinivanVisual>();
            van.Build();
            return van;
        }

        void Build()
        {
            paint = new Material(Shader.Find("Standard"));
            paint.name = "Personal minivan paint";
            paint.color = ToyGeometry.Hex("86C8B8");
            paint.SetFloat("_Glossiness", .15f);
            paint.SetFloat("_SpecularHighlights", 0);
            body = ToyGeometry.Group(transform, "Soft van suspension", Vector3.zero);
            Paint(ToyGeometry.Box(body, "Lower van body", new Vector3(0, .64f, -.04f), new Vector3(1.12f, .64f, 1.78f), Cream));
            Paint(ToyGeometry.Sphere(body, "Rounded rear corners", new Vector3(0, .71f, -.52f), new Vector3(1.15f, .74f, .93f), Cream));
            Paint(ToyGeometry.Box(body, "Tall cargo compartment", new Vector3(0, 1.00f, -.38f), new Vector3(1.10f, .52f, 1.05f), Cream));
            Paint(ToyGeometry.Sphere(body, "Soft cargo roof", new Vector3(0, 1.23f, -.38f), new Vector3(1.13f, .22f, 1.06f), Cream));
            Paint(ToyGeometry.Box(body, "Cab upper body", new Vector3(0, 1.045f, .50f), new Vector3(1.10f, .44f, .69f), Cream));
            Paint(ToyGeometry.Sphere(body, "Rounded cab roof", new Vector3(0, 1.25f, .50f), new Vector3(1.12f, .18f, .72f), Cream));
            ToyGeometry.Box(body, "Cream lower body belt", new Vector3(0, .55f, -.03f), new Vector3(1.14f, .12f, 1.81f), Cream);
            ToyGeometry.Box(body, "Front windscreen", new Vector3(0, 1.05f, .86f), new Vector3(.87f, .33f, .035f), Glass);
            ToyGeometry.Box(body, "Windscreen centre", new Vector3(.12f, 1.05f, .885f), new Vector3(.045f, .34f, .025f), Cream);
            ToyGeometry.Box(body, "Cream front bumper", new Vector3(0, .43f, .975f), new Vector3(1.18f, .14f, .09f), Cream);
            ToyGeometry.Box(body, "Van front grille", new Vector3(0, .66f, .909f), new Vector3(.37f, .15f, .025f), Ink);
            ToyGeometry.Box(body, "Van grille shine", new Vector3(0, .68f, .928f), new Vector3(.25f, .025f, .015f), Cream);
            foreach (float x in new[] { -.405f, .405f })
                ToyGeometry.Sphere(body, "Big friendly headlight", new Vector3(x, .68f, .922f), new Vector3(.20f, .23f, .065f), Cream);
            foreach (float side in new[] { -1f, 1f })
            {
                ToyGeometry.Box(body, "Cab side window", new Vector3(side * .56f, 1.04f, .50f), new Vector3(.025f, .31f, .49f), Glass);
                ToyGeometry.Box(body, "Window sill", new Vector3(side * .575f, .86f, .50f), new Vector3(.035f, .045f, .54f), Cream);
                ToyGeometry.Box(body, "Cab door handle", new Vector3(side * .579f, .77f, .30f), new Vector3(.025f, .035f, .15f), Ink);
                ToyGeometry.Box(body, "Cargo sliding door seam", new Vector3(side * .565f, .99f, -.58f), new Vector3(.015f, .43f, .025f), Cream);
                ToyGeometry.Box(body, "Cargo door handle", new Vector3(side * .581f, .95f, -.20f), new Vector3(.025f, .035f, .17f), Ink);
                ToyGeometry.Sphere(body, "Parcel emblem disc", new Vector3(side * .579f, 1.05f, -.40f), new Vector3(.025f, .28f, .32f), Cream);
                ToyGeometry.Box(body, "Parcel emblem box", new Vector3(side * .598f, 1.05f, -.40f), new Vector3(.018f, .14f, .17f), ToyGeometry.Hex("D5AC77"));
                ToyGeometry.Box(body, "Mirror stalk", new Vector3(side * .575f, 1.02f, .78f), new Vector3(.075f, .025f, .025f), Ink);
                ToyGeometry.Sphere(body, "Little rear-view mirror", new Vector3(side * .61f, 1.07f, .78f), new Vector3(.055f, .14f, .11f), Cream);
            }
            ToyGeometry.Box(body, "Rear door seam", new Vector3(0, .91f, -.921f), new Vector3(.025f, .64f, .018f), Cream);
            ToyGeometry.Box(body, "Rear bumper", new Vector3(0, .43f, -.976f), new Vector3(1.16f, .13f, .09f), Cream);
            foreach (float x in new[] { -.45f, .45f })
                ToyGeometry.Sphere(body, "Rear red light", new Vector3(x, .66f, -.942f), new Vector3(.10f, .18f, .05f), ToyGeometry.Hex("E38E7F"));
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -.54f : .54f;
                float z = i < 2 ? -.62f : .65f;
                wheels[i] = ToyGeometry.Cylinder(transform, "Van wheel", new Vector3(x, .29f, z), new Vector3(.42f, .13f, .42f), Ink);
                wheels[i].localRotation = Quaternion.Euler(0, 0, 90);
                Transform hub = ToyGeometry.Cylinder(transform, "Cream van hubcap", new Vector3(x < 0 ? x - .074f : x + .074f, .29f, z), new Vector3(.25f, .025f, .25f), Cream);
                hub.localRotation = Quaternion.Euler(0, 0, 90);
                ToyGeometry.Sphere(body, "Cream wheel arch", new Vector3(x * .99f, .43f, z), new Vector3(.12f, .19f, .53f), Cream);
            }
            // Visible through the front glass: a tiny cap, friendly eyes, and a steering wheel.
            Color skin = ToyGeometry.Hex("F2BD95");
            ToyGeometry.Sphere(body, "Courier face in cab", new Vector3(-.22f, 1.035f, .898f), new Vector3(.20f, .215f, .065f), skin);
            ToyGeometry.Sphere(body, "Courier cream cap", new Vector3(-.22f, 1.145f, .90f), new Vector3(.22f, .10f, .075f), Cream);
            foreach (float x in new[] { -.26f, -.18f })
                ToyGeometry.Sphere(body, "Courier eye", new Vector3(x, 1.055f, .936f), new Vector3(.024f, .032f, .015f), Ink);
            ToyGeometry.Sphere(body, "Courier smile", new Vector3(-.22f, .996f, .936f), new Vector3(.055f, .015f, .013f), Ink);
            ToyGeometry.Sphere(body, "Steering wheel", new Vector3(-.22f, .938f, .931f), new Vector3(.15f, .075f, .018f), Ink);
            ToyGeometry.Box(body, "Roof parcel rack", new Vector3(0, 1.365f, -.40f), new Vector3(.91f, .045f, .72f), Cream);
            CargoAnchor = ToyGeometry.Group(body, "Roof cargo anchor", new Vector3(0, 1.45f, -.40f));
        }

        void Paint(Transform part) { part.GetComponent<Renderer>().sharedMaterial = paint; }
        public void SetColor(Color color) { if (paint) paint.color = color; }

        public void Animate(float speed01, float turn01, bool reducedMotion)
        {
            float speed = Mathf.Clamp01(speed01);
            body.localPosition = new Vector3(0, reducedMotion ? 0 : Mathf.Sin(Time.time * 15) * speed * .012f, 0);
            body.localRotation = Quaternion.Euler(0, 0, reducedMotion ? 0 : -Mathf.Clamp(turn01, -1, 1) * 2.5f);
            if (speed > .001f)
            {
                wheelAngle = Mathf.Repeat(wheelAngle + speed * Time.deltaTime * 650, 360);
                for (int i = 0; i < wheels.Length; i++)
                    wheels[i].localRotation = Quaternion.Euler(0, 0, 90) * Quaternion.Euler(0, wheelAngle, 0);
            }
        }

        void OnDestroy() { if (paint) Destroy(paint); }
    }
}
