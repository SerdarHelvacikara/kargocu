using UnityEngine;

namespace KurmaliKargo
{
    public sealed partial class ParcelVisual
    {
        Transform vase, iceCream, gift;

        void BuildSpecialOrders()
        {
            Color glass = ToyGeometry.Hex("91D2D8");
            vase = ToyGeometry.Group(pivot, "Fragile glass vase", Vector3.zero);
            Tray(vase);
            ToyGeometry.Sphere(vase, "Rounded vase belly", new Vector3(0, .04f, 0), new Vector3(.50f, .47f, .50f), glass);
            ToyGeometry.Cylinder(vase, "Glass vase neck", new Vector3(0, .28f, 0), new Vector3(.23f, .25f, .23f), glass);
            ToyGeometry.Cylinder(vase, "Glass vase lip", new Vector3(0, .40f, 0), new Vector3(.34f, .055f, .34f), Cream);
            ToyGeometry.Sphere(vase, "Glass reflection", new Vector3(-.14f, .10f, .219f), new Vector3(.09f, .22f, .035f), Cream);

            iceCream = ToyGeometry.Group(pivot, "Insulated ice cream parcel", Vector3.zero);
            ToyGeometry.Box(iceCream, "Frosted box", new Vector3(0, -.01f, 0), new Vector3(.70f, .33f, .57f), glass);
            ToyGeometry.Box(iceCream, "Insulation rim", new Vector3(0, .15f, 0), new Vector3(.73f, .08f, .60f), Cream);
            ToyGeometry.Cylinder(iceCream, "Wafer cone", new Vector3(0, .25f, 0), new Vector3(.25f, .22f, .25f), Carton);
            ToyGeometry.Sphere(iceCream, "Strawberry ice cream scoop", new Vector3(0, .40f, 0), new Vector3(.33f, .27f, .32f), Pink);

            gift = ToyGeometry.Group(pivot, "Wrapped surprise parcel", Vector3.zero);
            ToyGeometry.Box(gift, "Violet surprise box", new Vector3(0, .02f, 0), new Vector3(.64f, .46f, .54f), ToyGeometry.Hex("AB9BD3"));
            ToyGeometry.Box(gift, "Golden parcel ribbon", new Vector3(0, .024f, 0), new Vector3(.15f, .47f, .56f), Tape);
            for (int side = -1; side <= 1; side += 2)
            {
                Transform loop = ToyGeometry.Sphere(gift, "Gift bow", new Vector3(side * .09f, .28f, 0), new Vector3(.24f, .07f, .13f), Tape);
                loop.localRotation = Quaternion.Euler(0, 0, side * 23);
            }
            vase.gameObject.SetActive(false);
            iceCream.gameObject.SetActive(false);
            gift.gameObject.SetActive(false);
        }

        internal void SetOrder(OrderStyle style)
        {
            bool standard = style != OrderStyle.Fragile && style != OrderStyle.Chilled && style != OrderStyle.Surprise;
            cake.gameObject.SetActive(standard && Kind == ParcelKind.Cake);
            plant.gameObject.SetActive(standard && Kind == ParcelKind.Plant);
            tea.gameObject.SetActive(standard && Kind == ParcelKind.TeaSet);
            vase.gameObject.SetActive(style == OrderStyle.Fragile);
            iceCream.gameObject.SetActive(style == OrderStyle.Chilled);
            gift.gameObject.SetActive(style == OrderStyle.Surprise);
        }
    }
}
