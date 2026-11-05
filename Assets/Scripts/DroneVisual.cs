using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>A small parcel helper. Its owner supplies every position and animation frame.</summary>
    public sealed class DroneVisual : MonoBehaviour
    {
        Transform body, parcel;
        readonly Transform[] rotors = new Transform[4];
        static readonly Color Cream = ToyGeometry.Hex("FFF1D5");
        static readonly Color Mint = ToyGeometry.Hex("86C8B8");
        static readonly Color Ink = ToyGeometry.Hex("3B5965");

        public static DroneVisual Create(Transform parent = null)
        {
            Transform root = ToyGeometry.Group(parent, "Little parcel drone", Vector3.zero);
            var drone = root.gameObject.AddComponent<DroneVisual>();
            drone.Build();
            drone.Show(false);
            return drone;
        }

        void Build()
        {
            body = ToyGeometry.Group(transform, "Gentle drone flight pose", Vector3.zero);
            ToyGeometry.Sphere(body, "Mint drone fuselage", Vector3.zero, new Vector3(.56f, .25f, .47f), Mint);
            ToyGeometry.Box(body, "Cream centre stripe", new Vector3(0, .105f, 0), new Vector3(.11f, .045f, .37f), Cream);
            foreach (float angle in new[] { -35f, 35f })
            {
                Transform arm = ToyGeometry.Box(body, "Quadrotor cross arm", Vector3.zero, new Vector3(1.20f, .075f, .085f), Cream);
                arm.localRotation = Quaternion.Euler(0, angle, 0);
            }
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -.49f : .49f;
                float z = i < 2 ? -.34f : .34f;
                Transform motor = ToyGeometry.Group(body, "Rotor motor", new Vector3(x, .035f, z));
                ToyGeometry.Cylinder(motor, "Motor housing", Vector3.zero, new Vector3(.17f, .14f, .17f), Mint);
                rotors[i] = ToyGeometry.Group(motor, "Propeller", new Vector3(0, .093f, 0));
                ToyGeometry.Box(rotors[i], "Cream propeller blade", Vector3.zero, new Vector3(.50f, .025f, .065f), Cream);
                ToyGeometry.Box(rotors[i], "Dark propeller blade", Vector3.zero, new Vector3(.065f, .022f, .50f), Ink);
                ToyGeometry.Sphere(rotors[i], "Propeller centre", new Vector3(0, .02f, 0), new Vector3(.09f, .06f, .09f), Mint);
                // Open octagonal guards leave the propeller silhouette readable.
                for (int j = 0; j < 8; j++)
                {
                    float a = j * Mathf.PI * 2 / 8;
                    Transform guard = ToyGeometry.Box(motor, "Rotor guard", new Vector3(Mathf.Cos(a) * .29f, .075f, Mathf.Sin(a) * .29f), new Vector3(.235f, .035f, .035f), Mint);
                    guard.localRotation = Quaternion.Euler(0, 90 - a * Mathf.Rad2Deg, 0);
                }
            }
            foreach (float x in new[] { -.21f, .21f })
            {
                ToyGeometry.Box(body, "Landing leg", new Vector3(x, -.16f, 0), new Vector3(.045f, .24f, .045f), Ink);
                ToyGeometry.Box(body, "Landing skid", new Vector3(x, -.28f, 0), new Vector3(.06f, .055f, .48f), Cream);
                ToyGeometry.Sphere(body, "Friendly drone eye", new Vector3(x * .48f, .015f, .227f), new Vector3(.055f, .062f, .025f), Ink);
            }
            ToyGeometry.Sphere(body, "Port signal lamp", new Vector3(-.24f, .06f, .16f), new Vector3(.06f, .06f, .045f), ToyGeometry.Hex("E99B87"));
            ToyGeometry.Sphere(body, "Starboard signal lamp", new Vector3(.24f, .06f, .16f), new Vector3(.06f, .06f, .045f), ToyGeometry.Hex("E6CB75"));
            parcel = ToyGeometry.Group(body, "Drone's strapped parcel", Vector3.zero);
            ToyGeometry.Box(parcel, "Cream parcel", new Vector3(0, -.47f, 0), new Vector3(.43f, .34f, .39f), Cream);
            ToyGeometry.Box(parcel, "Parcel golden strap", new Vector3(0, -.47f, -.005f), new Vector3(.09f, .35f, .405f), ToyGeometry.Hex("E5B875"));
            foreach (float x in new[] { -.16f, .16f })
                ToyGeometry.Box(parcel, "Suspension strap", new Vector3(x, -.26f, 0), new Vector3(.025f, .32f, .025f), Ink);
            ToyGeometry.Box(parcel, "Parcel address label", new Vector3(.125f, -.45f, .203f), new Vector3(.12f, .12f, .018f), ToyGeometry.Hex("D9E6D4"));
        }

        public void Show(bool visible) { gameObject.SetActive(visible); }

        public void Animate(Vector3 worldPosition, Vector3 heading, float progress, bool carrying, bool reducedMotion)
        {
            transform.position = worldPosition;
            heading.y = 0;
            if (heading.sqrMagnitude > .0001f) transform.rotation = Quaternion.LookRotation(heading.normalized, Vector3.up);
            float p = Mathf.Clamp01(progress);
            float flutter = reducedMotion ? 0 : Mathf.Sin(p * 48);
            body.localPosition = new Vector3(0, flutter * .025f, 0);
            body.localRotation = Quaternion.Euler(reducedMotion ? 0 : -Mathf.Sin(p * Mathf.PI) * 5, 0, flutter * 1.5f);
            for (int i = 0; i < rotors.Length; i++)
                rotors[i].localRotation = Quaternion.Euler(0, reducedMotion ? i * 45 : p * (i % 2 == 0 ? 14400 : -14400) + i * 27, 0);
            if (parcel.gameObject.activeSelf != carrying) parcel.gameObject.SetActive(carrying);
        }
    }
}
