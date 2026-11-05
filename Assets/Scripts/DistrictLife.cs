using System;
using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>
    /// People strolling around the blocks of a district, with a line now and then.
    /// No colliders and no Update: CourierGame ticks it while the district is shown.
    /// </summary>
    public sealed class DistrictLife : MonoBehaviour
    {
        public event Action<Vector3, string, string> Speech;
        public int WalkerCount => walkers.Length;

        sealed class Walker
        {
            public Transform root, body, leftLeg, rightLeg;
            public Vector3 block;
            public float t, speed, half;
        }

        Walker[] walkers = new Walker[0];
        float clock, nextSpeech = 6;
        int district, speechIndex;

        static readonly string[][] Lines =
        {
            new string[0],
            new[] { "Taze baharat, taze kargo!", "Fresh spices, fresh parcels!", "Bu kumaşı kim getirdi? Harika!", "Who brought this fabric? Lovely!",
                    "Çarşıda pazarlık serbest, kargo değil!", "Haggling is fine, parcels aren't!" },
            new[] { "Çayınız demlendi, kargocu!", "Your tea is ready, courier!", "Fenerler bu akşam yanacak!", "The lanterns light up tonight!",
                    "Erişteler sıcak, kargo hızlı!", "Hot noodles, quick parcels!" },
            new[] { "Toplantıya kargo yetişir mi?", "Will the parcel make the meeting?", "Asansör yine dolu!", "The lift is full again!",
                    "Kahvem nerede kaldı?", "Where's my coffee?" }
        };

        public static DistrictLife Create(int district)
        {
            var life = new GameObject("District " + district + " street life").AddComponent<DistrictLife>();
            life.district = district;
            Color[] outfits = district == 1 ? new[] { ToyGeometry.Hex("E2634F"), ToyGeometry.Hex("5CAE7E"), ToyGeometry.Hex("EAB63E"), ToyGeometry.Hex("A66BB0") } :
                district == 2 ? new[] { ToyGeometry.Hex("C0453A"), ToyGeometry.Hex("2F7C6E"), ToyGeometry.Hex("F5C671"), ToyGeometry.Hex("6B3F5E") } :
                new[] { ToyGeometry.Hex("2E4E63"), ToyGeometry.Hex("4C7FB0"), ToyGeometry.Hex("5B6770"), ToyGeometry.Hex("F0A98E") };
            Color[] skins = { ToyGeometry.Hex("F2C49B"), ToyGeometry.Hex("D9A06F"), ToyGeometry.Hex("A8704A"), ToyGeometry.Hex("FFDDBB") };
            var random = new System.Random(4021 + district);
            life.walkers = new Walker[12];
            for (int i = 0; i < life.walkers.Length; i++)
            {
                var w = new Walker();
                w.root = ToyGeometry.Group(life.transform, "Strolling neighbour", Vector3.zero);
                w.body = ToyGeometry.Group(w.root, "Neighbour body", Vector3.zero);
                Color outfit = outfits[i % outfits.Length];
                ToyGeometry.Capsule(w.body, "Neighbour coat", new Vector3(0, .62f, 0), new Vector3(.34f, .58f, .3f), outfit);
                ToyGeometry.Sphere(w.body, "Neighbour head", new Vector3(0, 1.08f, 0), new Vector3(.3f, .3f, .3f), skins[i % skins.Length]);
                ToyGeometry.Sphere(w.body, "Neighbour hair", new Vector3(0, 1.17f, .03f), new Vector3(.31f, .17f, .3f), ToyGeometry.Hex(i % 3 == 0 ? "3D3027" : i % 3 == 1 ? "7A4B2E" : "C9A26B"));
                if (district == 3 && i % 2 == 0)
                    ToyGeometry.Box(w.body, "Briefcase", new Vector3(.22f, .45f, 0), new Vector3(.08f, .2f, .26f), ToyGeometry.Hex("5A3E2B"));
                else if (district == 1 && i % 3 == 0)
                    ToyGeometry.Sphere(w.body, "Shopping bag", new Vector3(.22f, .45f, 0), new Vector3(.18f, .22f, .14f), ToyGeometry.Hex("F4D35E"));
                else if (district == 2 && i % 3 == 0)
                    ToyGeometry.Sphere(w.body, "Paper lantern", new Vector3(.24f, .7f, 0), new Vector3(.16f, .2f, .16f), ToyGeometry.Hex("E2473B"));
                w.leftLeg = ToyGeometry.Group(w.root, "Leg", new Vector3(-.08f, .32f, 0));
                ToyGeometry.Capsule(w.leftLeg, "Leg", new Vector3(0, -.14f, 0), new Vector3(.11f, .3f, .11f), ToyGeometry.Hex("405C68"));
                w.rightLeg = ToyGeometry.Group(w.root, "Leg", new Vector3(.08f, .32f, 0));
                ToyGeometry.Capsule(w.rightLeg, "Leg", new Vector3(0, -.14f, 0), new Vector3(.11f, .3f, .11f), ToyGeometry.Hex("405C68"));
                w.block = new Vector3(-12 + random.Next(4) * 8, 0, -12 + random.Next(4) * 8);
                w.half = 2.02f;
                w.t = (float)random.NextDouble() * 4;
                w.speed = (district == 3 ? .34f : .22f) * (.8f + (float)random.NextDouble() * .4f) * (i % 2 == 0 ? 1 : -1);
                life.walkers[i] = w;
            }
            life.Pose(0, false);
            life.gameObject.SetActive(false);
            return life;
        }

        public void Tick(float dt, bool playing, bool reducedMotion)
        {
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0) return;
            dt = Mathf.Min(dt, .1f);
            clock += dt;
            if (!reducedMotion)
                foreach (Walker w in walkers) w.t = Mathf.Repeat(w.t + w.speed * dt, 4);
            Pose(dt, reducedMotion);
            if (!playing || clock < nextSpeech || Lines[district].Length == 0 || walkers.Length == 0) return;
            nextSpeech = clock + 13;
            Walker speaker = walkers[speechIndex % walkers.Length];
            int line = speechIndex % (Lines[district].Length / 2) * 2;
            speechIndex++;
            Speech?.Invoke(speaker.root.position + Vector3.up * 1.7f, Lines[district][line], Lines[district][line + 1]);
        }

        // Each walker loops the sidewalk ring of its block: t in [0, 4) is one lap.
        void Pose(float dt, bool reducedMotion)
        {
            foreach (Walker w in walkers)
            {
                int side = Mathf.FloorToInt(w.t) % 4;
                float f = w.t - Mathf.Floor(w.t);
                Vector3[] corners =
                {
                    new Vector3(-w.half, 0, -w.half), new Vector3(w.half, 0, -w.half),
                    new Vector3(w.half, 0, w.half), new Vector3(-w.half, 0, w.half)
                };
                Vector3 a = corners[side], b = corners[(side + 1) % 4];
                Vector3 pos = w.block + Vector3.Lerp(a, b, f);
                w.root.position = pos;
                Vector3 dir = (b - a) * Mathf.Sign(w.speed);
                if (dir.sqrMagnitude > .001f) w.root.rotation = Quaternion.LookRotation(dir);
                float swing = reducedMotion ? 0 : Mathf.Sin(clock * 9 + w.block.x) * 28;
                w.leftLeg.localRotation = Quaternion.Euler(swing, 0, 0);
                w.rightLeg.localRotation = Quaternion.Euler(-swing, 0, 0);
                w.body.localPosition = new Vector3(0, reducedMotion ? 0 : Mathf.Abs(Mathf.Sin(clock * 9 + w.block.x)) * .04f, 0);
            }
        }
    }
}
