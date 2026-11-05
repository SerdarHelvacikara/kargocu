using System.Collections.Generic;
using UnityEngine;

namespace KurmaliKargo
{
    public enum ParcelKind { Cake, Plant, TeaSet }

    /// <summary>Three self-contained toy parcels. No asset, rig or particle-system dependency.</summary>
    public sealed partial class ParcelVisual : MonoBehaviour
    {
        public ParcelKind Kind { get; private set; }
        public int ImpactCount { get; private set; }
        public int VisibleLeafCount
        {
            get
            {
                if (Kind != ParcelKind.Plant || leaves == null) return 0;
                int count = 0;
                for (int i = 0; i < leaves.Length; i++) if (leaves[i].gameObject.activeSelf) count++;
                return count;
            }
        }

        Transform pivot, cake, plant, tea, cakeTop, plantPot, plantStem;
        Transform[] leaves, cups;
        Transform teaCrack;
        Face cakeFace, plantFace, teaFace;
        Vector3[] leafPositions;
        Quaternion[] leafRotations;
        Vector3[] cupPositions;
        bool built;
        float pulse, integrity = 100;
        int shownLeaves = -1;
        static readonly Color Carton = ToyGeometry.Hex("D5A977");
        static readonly Color Tape = ToyGeometry.Hex("F7DCA6");
        static readonly Color Cream = ToyGeometry.Hex("FFF1D7");
        static readonly Color Pink = ToyGeometry.Hex("ED91AE");
        static readonly Color Ink = ToyGeometry.Hex("314C59");

        sealed class Face
        {
            public Transform LeftEye, RightEye, Mouth, LeftBrow, RightBrow;
        }

        void Awake() { Build(); Configure(ParcelKind.Cake); }

        void Build()
        {
            if (built) return;
            built = true;
            pivot = ToyGeometry.Group(transform, "Parcel animation pivot", Vector3.zero);
            cake = ToyGeometry.Group(pivot, "Freshly baked cake", Vector3.zero);
            plant = ToyGeometry.Group(pivot, "Little potted plant", Vector3.zero);
            tea = ToyGeometry.Group(pivot, "Three little tea cups", Vector3.zero);
            BuildCake(); BuildPlant(); BuildTea();
            BuildSpecialOrders();
        }

        public void Configure(ParcelKind kind)
        {
            Build();
            Kind = kind;
            ImpactCount = 0;
            integrity = 100;
            pulse = 0;
            shownLeaves = -1;
            SetOrder(OrderStyle.Standard);
            Animate(100, 0, 0, 0);
        }

        void BuildCake()
        {
            Tray(cake);
            ToyGeometry.Cylinder(cake, "Vanilla sponge", new Vector3(0, -.095f, 0), new Vector3(.70f, .23f, .65f), Cream);
            ToyGeometry.Cylinder(cake, "Strawberry filling", new Vector3(0, -.08f, 0), new Vector3(.707f, .045f, .657f), Pink);
            ToyGeometry.Cylinder(cake, "Lower pink icing", new Vector3(0, .038f, 0), new Vector3(.74f, .060f, .69f), Pink);
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2 / 9;
                ToyGeometry.Sphere(cake, "Icing dollop", new Vector3(Mathf.Cos(a) * .285f, .070f, Mathf.Sin(a) * .265f), new Vector3(.12f, .10f, .12f), Cream);
            }
            cakeTop = ToyGeometry.Group(cake, "Wobbly upper tier", new Vector3(0, .055f, 0));
            ToyGeometry.Cylinder(cakeTop, "Upper sponge", new Vector3(0, .095f, 0), new Vector3(.44f, .18f, .42f), Cream);
            ToyGeometry.Cylinder(cakeTop, "Upper pink icing", new Vector3(0, .20f, 0), new Vector3(.47f, .055f, .45f), Pink);
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2 / 5;
                ToyGeometry.Sphere(cakeTop, "Berry", new Vector3(Mathf.Cos(a) * .15f, .265f, Mathf.Sin(a) * .145f), new Vector3(.10f, .11f, .10f), ToyGeometry.Hex("CE687A"));
            }
            ToyGeometry.Cylinder(cakeTop, "Birthday candle", new Vector3(0, .33f, 0), new Vector3(.045f, .17f, .045f), ToyGeometry.Hex("83BBC6"));
            ToyGeometry.Sphere(cakeTop, "Little candle flame", new Vector3(0, .445f, 0), new Vector3(.065f, .105f, .065f), ToyGeometry.Hex("FFCD69"));
            cakeFace = MakeFace(cake, new Vector3(0, -.23f, -.418f), 1);
        }

        void BuildPlant()
        {
            plantPot = ToyGeometry.Group(plant, "Tilting terracotta pot", Vector3.zero);
            ToyGeometry.Cylinder(plantPot, "Terracotta pot", new Vector3(0, -.16f, 0), new Vector3(.55f, .34f, .55f), ToyGeometry.Hex("CE896E"));
            ToyGeometry.Cylinder(plantPot, "Pot bottom", new Vector3(0, -.30f, 0), new Vector3(.43f, .08f, .43f), ToyGeometry.Hex("BD785F"));
            ToyGeometry.Cylinder(plantPot, "Thick pot rim", new Vector3(0, .015f, 0), new Vector3(.64f, .11f, .64f), ToyGeometry.Hex("E1A181"));
            ToyGeometry.Cylinder(plantPot, "Dark soil", new Vector3(0, .075f, 0), new Vector3(.51f, .025f, .51f), ToyGeometry.Hex("7B6757"));
            plantStem = ToyGeometry.Group(plantPot, "Springy plant", new Vector3(0, .055f, 0));
            ToyGeometry.Cylinder(plantStem, "Main stem", new Vector3(0, .21f, 0), new Vector3(.035f, .42f, .035f), ToyGeometry.Hex("4B956A"));
            leaves = new Transform[6]; leafPositions = new Vector3[6]; leafRotations = new Quaternion[6];
            for (int i = 0; i < leaves.Length; i++)
            {
                float a = (i * 137f + 24) * Mathf.Deg2Rad;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                leafPositions[i] = radial * (.14f + (i % 2) * .02f) + Vector3.up * (.13f + .051f * i);
                leafRotations[i] = Quaternion.LookRotation(radial, Vector3.up) * Quaternion.Euler(-22, 0, (i % 2 == 0 ? 10 : -10));
                leaves[i] = ToyGeometry.Sphere(plantStem, "Leaf " + (i + 1), leafPositions[i], new Vector3(.15f, .075f, .37f), i % 2 == 0 ? ToyGeometry.Hex("5CA676") : ToyGeometry.Hex("7DBB7C"));
                leaves[i].localRotation = leafRotations[i];
                ToyGeometry.Sphere(leaves[i], "Leaf vein", new Vector3(0, .33f, 0), new Vector3(.12f, .10f, .76f), ToyGeometry.Hex("9ACD8A"));
            }
            plantFace = MakeFace(plantPot, new Vector3(0, -.15f, -.285f), .8f);
        }

        void BuildTea()
        {
            Tray(tea);
            cups = new Transform[3];
            cupPositions = new[] { new Vector3(-.23f, -.095f, -.13f), new Vector3(.23f, -.095f, -.11f), new Vector3(0, -.095f, .20f) };
            Color blue = ToyGeometry.Hex("719EBC");
            for (int i = 0; i < cups.Length; i++)
            {
                cups[i] = ToyGeometry.Group(tea, "Tea cup " + (i + 1), cupPositions[i]);
                Transform c = cups[i];
                ToyGeometry.Cylinder(c, "Porcelain cup", Vector3.zero, new Vector3(.235f, .28f, .235f), Cream);
                ToyGeometry.Cylinder(c, "Blue porcelain band", new Vector3(0, .025f, 0), new Vector3(.24f, .047f, .24f), blue);
                ToyGeometry.Cylinder(c, "Cup rim", new Vector3(0, .145f, 0), new Vector3(.25f, .03f, .25f), Cream);
                ToyGeometry.Cylinder(c, "Warm tea", new Vector3(0, .164f, 0), new Vector3(.19f, .012f, .19f), ToyGeometry.Hex("B98753"));
                for (int j = 0; j < 9; j++)
                {
                    float angle = j * Mathf.PI * 2 / 9;
                    ToyGeometry.Sphere(c, "Rounded cup handle", new Vector3(.15f + Mathf.Cos(angle) * .06f, .015f + Mathf.Sin(angle) * .075f, 0), new Vector3(.044f, .044f, .044f), Cream);
                }
                ToyGeometry.Cylinder(c, "Little saucer", new Vector3(0, -.155f, 0), new Vector3(.32f, .035f, .32f), Cream);
            }
            teaCrack = ToyGeometry.Group(cups[0], "Cartoon cup crack", new Vector3(0, .03f, -.123f));
            Transform crackA = ToyGeometry.Box(teaCrack, "Crack line", new Vector3(-.01f, .025f, 0), new Vector3(.018f, .09f, .012f), Ink);
            crackA.localRotation = Quaternion.Euler(0, 0, -24);
            Transform crackB = ToyGeometry.Box(teaCrack, "Crack line", new Vector3(.005f, -.045f, 0), new Vector3(.018f, .07f, .012f), Ink);
            crackB.localRotation = Quaternion.Euler(0, 0, 21);
            teaFace = MakeFace(tea, new Vector3(0, -.23f, -.418f), 1);
        }

        static void Tray(Transform parent)
        {
            ToyGeometry.Box(parent, "Open carton bottom", new Vector3(0, -.32f, 0), new Vector3(.93f, .10f, .81f), Carton);
            ToyGeometry.Box(parent, "Carton front", new Vector3(0, -.225f, -.39f), new Vector3(.93f, .20f, .055f), Carton);
            ToyGeometry.Box(parent, "Carton back", new Vector3(0, -.225f, .39f), new Vector3(.93f, .20f, .055f), Carton);
            ToyGeometry.Box(parent, "Carton side", new Vector3(-.44f, -.225f, 0), new Vector3(.055f, .20f, .75f), Carton);
            ToyGeometry.Box(parent, "Carton side", new Vector3(.44f, -.225f, 0), new Vector3(.055f, .20f, .75f), Carton);
            ToyGeometry.Box(parent, "Packing tape", new Vector3(0, -.375f, 0), new Vector3(.12f, .014f, .81f), Tape);
            Transform flap = ToyGeometry.Box(parent, "Folded carton flap", new Vector3(.49f, -.13f, 0), new Vector3(.16f, .025f, .73f), Tape);
            flap.localRotation = Quaternion.Euler(0, 0, -26);
        }

        static Face MakeFace(Transform parent, Vector3 p, float scale)
        {
            Transform f = ToyGeometry.Group(parent, "Parcel face", p);
            f.localScale = Vector3.one * scale;
            var face = new Face();
            face.LeftEye = ToyGeometry.Sphere(f, "Left eye", new Vector3(-.125f, .015f, -.018f), new Vector3(.034f, .049f, .020f), Ink);
            face.RightEye = ToyGeometry.Sphere(f, "Right eye", new Vector3(.125f, .015f, -.018f), new Vector3(.034f, .049f, .020f), Ink);
            face.Mouth = ToyGeometry.Sphere(f, "Little mouth", new Vector3(0, -.037f, -.018f), new Vector3(.085f, .028f, .019f), Ink);
            face.LeftBrow = ToyGeometry.Box(f, "Left eyebrow", new Vector3(-.125f, .058f, -.018f), new Vector3(.065f, .014f, .014f), Ink);
            face.RightBrow = ToyGeometry.Box(f, "Right eyebrow", new Vector3(.125f, .058f, -.018f), new Vector3(.065f, .014f, .014f), Ink);
            return face;
        }

        public void Animate(float currentIntegrity, float speed01, float turn01, float hitShake)
        {
            if (!built) return;
            integrity = Mathf.Clamp(currentIntegrity, 0, 100);
            float damage = 1 - integrity / 100;
            float speed = Mathf.Clamp01(speed01), turn = Mathf.Clamp(turn01, -1, 1);
            pulse = Mathf.MoveTowards(pulse, 0, Time.deltaTime * 2.8f);
            float shake = Mathf.Max(pulse, Mathf.Clamp01(hitShake));
            float time = Time.time;
            pivot.localPosition = new Vector3(Mathf.Sin(time * 31) * shake * .025f, Mathf.Sin(time * 19) * speed * .012f, 0);
            pivot.localRotation = Quaternion.Euler(Mathf.Sin(time * 17) * speed * 1.5f, 0, -turn * 4 + Mathf.Sin(time * 29) * shake * 7);
            if (Kind == ParcelKind.Cake)
            {
                cakeTop.localPosition = new Vector3(damage * .09f, .055f - damage * .035f, damage * .035f);
                cakeTop.localRotation = Quaternion.Euler(damage * 9 + Mathf.Sin(time * 10) * speed * 2, 0, -turn * 12 - damage * 20 + Mathf.Sin(time * 24) * shake * 10);
                SetFace(cakeFace, damage, shake);
            }
            else if (Kind == ParcelKind.Plant)
            {
                plantPot.localRotation = Quaternion.Euler(damage * 5, 0, damage * 13 - turn * 5);
                plantStem.localRotation = Quaternion.Euler(Mathf.Sin(time * 7) * speed * 5, 0, -turn * 16 + Mathf.Sin(time * 9) * speed * 3 + shake * Mathf.Sin(time * 23) * 12);
                int count = Mathf.Clamp(6 - Mathf.FloorToInt((100 - integrity) / 18f), 1, 6);
                if (shownLeaves != count)
                {
                    shownLeaves = count;
                    for (int i = 0; i < leaves.Length; i++) leaves[i].gameObject.SetActive(i < count);
                }
                SetFace(plantFace, damage, shake);
            }
            else
            {
                for (int i = 0; i < cups.Length; i++)
                {
                    bool visible = i != 1 || integrity >= 35;
                    if (cups[i].gameObject.activeSelf != visible) cups[i].gameObject.SetActive(visible);
                    cups[i].localPosition = cupPositions[i] + new Vector3(Mathf.Sin(time * 23 + i) * shake * .025f, Mathf.Abs(Mathf.Sin(time * 24 + i * 2)) * shake * .045f, 0);
                    cups[i].localRotation = Quaternion.Euler(damage * (i == 0 ? 26 : -7), i * damage * 11, -turn * (6 + i * 3) + damage * (i == 0 ? -22 : 13) + Mathf.Sin(time * 27 + i) * shake * 11);
                }
                teaCrack.gameObject.SetActive(integrity < 65);
                SetFace(teaFace, damage, shake);
            }
        }

        static void SetFace(Face face, float damage, float shake)
        {
            float surprise = Mathf.Max(shake, Mathf.InverseLerp(.45f, 1, damage));
            Vector3 eyeSize = new Vector3(.034f + surprise * .006f, .049f + surprise * .018f, .020f);
            face.LeftEye.localScale = eyeSize;
            face.RightEye.localScale = eyeSize;
            face.Mouth.localScale = Vector3.Lerp(new Vector3(.085f, .028f, .019f), new Vector3(.052f, .075f, .021f), surprise);
            face.LeftBrow.localRotation = Quaternion.Euler(0, 0, -damage * 24);
            face.RightBrow.localRotation = Quaternion.Euler(0, 0, damage * 24);
        }

        public void OnImpact(float currentIntegrity)
        {
            Build();
            ImpactCount++;
            integrity = Mathf.Clamp(currentIntegrity, 0, 100);
            pulse = 1;
            if (Kind == ParcelKind.Cake)
            {
                for (int i = 0; i < 3; i++) Fleck(new Vector3((i - 1) * .14f, .15f, -.12f), new Vector3(.065f, .055f, .065f), Pink, i);
            }
            else if (Kind == ParcelKind.Plant)
            {
                Fleck(new Vector3(-.13f, .3f, 0), new Vector3(.14f, .035f, .25f), ToyGeometry.Hex("69AE75"), 0);
                if (integrity < 70) Fleck(new Vector3(.15f, .25f, .06f), new Vector3(.12f, .035f, .22f), ToyGeometry.Hex("83BB7B"), 1);
            }
            else
            {
                Fleck(new Vector3(-.18f, .16f, -.1f), new Vector3(.07f, .07f, .07f), Cream, 0);
                Fleck(new Vector3(.16f, .14f, -.1f), new Vector3(.06f, .06f, .06f), ToyGeometry.Hex("B98753"), 1);
            }
        }

        void Fleck(Vector3 local, Vector3 scale, Color color, int index)
        {
            Transform t = ToyGeometry.Sphere(null, "Parcel impact fleck", transform.TransformPoint(local), scale, color);
            t.rotation = transform.rotation * Quaternion.Euler(15 + index * 35, index * 71, 24);
            var effect = t.gameObject.AddComponent<ParcelFleck>();
            effect.Initialize(transform.right * ((index % 2 == 0 ? -1 : 1) * (.45f + index * .14f)) - transform.forward * .45f + Vector3.up * 1.2f,
                new Vector3(100 + index * 20, 160, 70 - index * 30), .95f + index * .12f);
        }
    }

    internal sealed class ParcelFleck : MonoBehaviour
    {
        Vector3 velocity, spin, scale;
        float remaining, life;
        public void Initialize(Vector3 v, Vector3 rotation, float seconds)
        {
            velocity = v; spin = rotation; remaining = life = seconds; scale = transform.localScale;
        }
        void Update()
        {
            remaining -= Time.deltaTime;
            if (remaining <= 0) { Destroy(gameObject); return; }
            velocity += Vector3.down * (Time.deltaTime * 3.6f);
            transform.position += velocity * Time.deltaTime;
            transform.Rotate(spin * Time.deltaTime, Space.Self);
            transform.localScale = scale * Mathf.Clamp01(remaining / Mathf.Min(life, .32f));
        }
    }

    // Shared primitive authoring helpers used by both parcel and customer visuals.
    internal static class ToyGeometry
    {
        static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();
        public static Transform Group(Transform parent, string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            return go.transform;
        }
        public static Transform Box(Transform parent, string name, Vector3 position, Vector3 size, Color color)
            => Shape(parent, name, position, size, color, PrimitiveType.Cube);
        public static Transform Sphere(Transform parent, string name, Vector3 position, Vector3 size, Color color)
            => Shape(parent, name, position, size, color, PrimitiveType.Sphere);
        public static Transform Cylinder(Transform parent, string name, Vector3 position, Vector3 size, Color color)
            => Shape(parent, name, position, new Vector3(size.x, size.y * .5f, size.z), color, PrimitiveType.Cylinder);
        public static Transform Capsule(Transform parent, string name, Vector3 position, Vector3 size, Color color)
            => Shape(parent, name, position, new Vector3(size.x, size.y * .5f, size.z), color, PrimitiveType.Capsule);
        static Transform Shape(Transform parent, string name, Vector3 position, Vector3 scale, Color color, PrimitiveType primitive)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            Collider collider = go.GetComponent<Collider>();
            if (collider) { collider.enabled = false; Object.Destroy(collider); }
            go.GetComponent<Renderer>().sharedMaterial = Material(color);
            return go.transform;
        }
        static Material Material(Color color)
        {
            Material material;
            if (Materials.TryGetValue(color, out material) && material) return material;
            material = new Material(Shader.Find("Standard"));
            material.name = "Toy " + ColorUtility.ToHtmlStringRGB(color);
            material.color = color;
            material.SetFloat("_Glossiness", .12f);
            material.SetFloat("_SpecularHighlights", 0);
            Materials[color] = material;
            return material;
        }
        public static Color Hex(string hex)
        {
            Color c; ColorUtility.TryParseHtmlString("#" + hex, out c); return c;
        }
    }
}
