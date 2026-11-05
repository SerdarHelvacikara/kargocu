using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KurmaliKargo
{
    internal sealed class GarageCanvas : MonoBehaviour
    {
        static readonly Color Ink = ToyGeometry.Hex("203C52");
        static readonly Color Muted = ToyGeometry.Hex("557188");
        static readonly Color Night = ToyGeometry.Hex("20324A");
        static readonly Color Ivory = ToyGeometry.Hex("FFF8E9");
        static readonly Color Teal = ToyGeometry.Hex("197E79");
        static readonly Color Coral = ToyGeometry.Hex("AD4B43");
        static readonly Color Gold = ToyGeometry.Hex("F5CB72");
        static readonly Color Violet = ToyGeometry.Hex("70599C");
        static readonly Color[] Accents = { Teal, Gold, Violet, Coral };
        CourierGame game;
        Canvas canvas;
        Camera previewCamera;
        RenderTexture previewTexture;
        RectTransform safe, body, tabs, items, preview, confirmation;
        ScrollRect scroll;
        Text wallet, toast, previewLabel;
        Font font;
        Sprite rounded;
        Texture2D roundedPixels;
        int tab, previewVehicle = -1, displayedCoins, lastBalance, lastWidth, lastHeight;
        bool lastLanguage, lastReady, lastAdReady, roomOnly;
        float previewAngle = 136;
        Rect lastSafe;
        GamePhase shownPhase;
        internal bool Visible => canvas && canvas.gameObject.activeSelf;
        internal int CardCount { get; private set; }
        internal int SelectedTab => tab;
        internal bool CatalogFitsViewport => Visible && items && scroll && items.rect.width > 0 &&
            Mathf.Abs(items.rect.width - scroll.viewport.rect.width) <= .5f && Mathf.Abs(items.anchoredPosition.x) <= .5f;

        internal void Initialize(CourierGame courier)
        {
            game = courier;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildSprite();
            var root = new GameObject("Garage and store canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 540);
            scaler.matchWidthOrHeight = .5f;
            if (!EventSystem.current)
            {
                var events = new GameObject("Garage input events", typeof(EventSystem), typeof(StandaloneInputModule));
                events.transform.SetParent(transform, false);
            }
            var cameraObject = new GameObject("Garage model camera");
            cameraObject.transform.SetParent(transform, false);
            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.enabled = false;
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = 4.0f;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = ToyGeometry.Hex("AED6CB");
            previewCamera.nearClipPlane = .1f;
            previewCamera.farClipPlane = 32;
            previewTexture = new RenderTexture(512, 384, 16, RenderTextureFormat.ARGB32) { name = "Live garage preview" };
            previewCamera.targetTexture = previewTexture;
            root.SetActive(false);
        }

        void Update()
        {
            if (!game || !canvas) return;
            bool show = game.Phase == GamePhase.Garage || game.Phase == GamePhase.Shop;
            if (!show)
            {
                if (!Visible) return;
                canvas.gameObject.SetActive(false);
                previewCamera.enabled = false;
                previewVehicle = -1;
                roomOnly = false;
                game.RefreshGarage();
                game.GarageRoom.TurnPreview(136);
                return;
            }
            if (!Visible || shownPhase != game.Phase)
            {
                shownPhase = game.Phase;
                if (shownPhase == GamePhase.Shop) tab = 1;
                canvas.gameObject.SetActive(true);
                previewCamera.enabled = true;
                displayedCoins = game.Coins;
                Rebuild();
            }
            if (lastWidth != Screen.width || lastHeight != Screen.height || lastSafe != Screen.safeArea || lastLanguage != game.IsTurkish)
                Rebuild();
            if (lastReady != game.StoreReady || lastAdReady != game.AdsReady || lastBalance != game.Coins) RefreshCards(false);
            if (displayedCoins != game.Coins)
            {
                int delta = game.Coins - displayedCoins;
                int step = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(delta) * Time.unscaledDeltaTime * 12));
                displayedCoins = game.ReducedMotion || Mathf.Abs(delta) <= step ? game.Coins : displayedCoins + Math.Sign(delta) * step;
                wallet.text = CoinText(displayedCoins);
            }
            if (toast)
            {
                bool showNotice = game.NoticeTime > 0 && !string.IsNullOrEmpty(game.Notice);
                if (toast.gameObject.activeSelf != showNotice) toast.gameObject.SetActive(showNotice);
                if (showNotice && toast.text != game.Notice) toast.text = game.Notice;
            }
            previewAngle = game.ReducedMotion ? 136 : Mathf.Repeat(previewAngle + Time.unscaledDeltaTime * 17, 360);
            game.GarageRoom.TurnPreview(previewAngle);
        }

        void OnDestroy()
        {
            if (previewTexture) { previewTexture.Release(); Destroy(previewTexture); }
            if (rounded) Destroy(rounded);
            if (roundedPixels) Destroy(roundedPixels);
        }

        void BuildSprite()
        {
            const int edge = 36, radius = 12;
            roundedPixels = new Texture2D(edge, edge, TextureFormat.RGBA32, false);
            var pixels = new Color32[edge * edge];
            for (int y = 0; y < edge; y++)
                for (int x = 0; x < edge; x++)
                {
                    float dx = Mathf.Max(0, radius - Mathf.Min(x + .5f, edge - x - .5f));
                    float dy = Mathf.Max(0, radius - Mathf.Min(y + .5f, edge - y - .5f));
                    pixels[y * edge + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.Clamp01(radius + .5f - Mathf.Sqrt(dx * dx + dy * dy))));
                }
            roundedPixels.SetPixels32(pixels);
            roundedPixels.Apply(false, true);
            rounded = Sprite.Create(roundedPixels, new Rect(0, 0, edge, edge), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
        }

        void Rebuild()
        {
            lastWidth = Screen.width; lastHeight = Screen.height; lastSafe = Screen.safeArea; lastLanguage = game.IsTurkish;
            bool portrait = lastWidth < lastHeight;
            if (safe) { safe.gameObject.SetActive(false); Destroy(safe.gameObject); }
            RectTransform root = (RectTransform)canvas.transform;
            safe = Rect("Garage safe area", root);
            Place(safe, new Vector2(lastSafe.xMin / lastWidth, lastSafe.yMin / lastHeight),
                new Vector2(lastSafe.xMax / lastWidth, lastSafe.yMax / lastHeight), Vector2.zero, Vector2.zero);
            Plate(safe, Night, false);
            RectTransform header = Rect("Garage header", safe);
            Place(header, new Vector2(0, 1), Vector2.one, new Vector2(12, -64), new Vector2(-12, -8));
            Button back = ActionButton(header, L("‹ Geri", "‹ Back"), Ivory, Ink, () =>
            {
                if (roomOnly) { roomOnly = false; Rebuild(); }
                else if (game.Phase == GamePhase.Shop) game.CloseShop();
                else game.CloseOverlay();
            });
            Place((RectTransform)back.transform, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(78, 0));
            Text title = Caption(header, roomOnly ? L("Garajın", "Your garage") : L("Garaj ve şehir", "Garage & city"), portrait ? 20 : 29, Ivory, true);
            Place(title.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(88, 0), new Vector2(-117, 0));
            wallet = Caption(header, CoinText(displayedCoins), 17, Gold, true, TextAnchor.MiddleRight);
            Place(wallet.rectTransform, new Vector2(1, 0), Vector2.one, new Vector2(-108, 0), Vector2.zero);

            body = Rect("Garage content", safe);
            Place(body, Vector2.zero, Vector2.one, new Vector2(12, 49), new Vector2(-12, -74));
            preview = Rect("Live 3D display", body);
            RectTransform list = Rect("Shop and inventory", body);
            if (roomOnly) Fill(preview);
            else if (portrait)
            {
                Place(preview, new Vector2(0, .63f), Vector2.one, Vector2.zero, Vector2.zero);
                Place(list, Vector2.zero, new Vector2(1, .63f), Vector2.zero, new Vector2(0, -8));
            }
            else
            {
                Place(preview, Vector2.zero, new Vector2(.38f, 1), Vector2.zero, Vector2.zero);
                Place(list, new Vector2(.38f, 0), Vector2.one, new Vector2(10, 0), Vector2.zero);
            }
            Plate(preview, ToyGeometry.Hex("314E63"));
            RectTransform image = Rect("RenderTexture view", preview);
            Place(image, Vector2.zero, Vector2.one, new Vector2(8, 58), new Vector2(-8, -8));
            RawImage liveImage = image.gameObject.AddComponent<RawImage>();
            liveImage.texture = previewTexture;
            liveImage.raycastTarget = false;
            previewLabel = Caption(preview, PreviewName(), 18, Ivory, true);
            Place(previewLabel.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(12, 8), new Vector2(roomOnly ? -12 : -104, 52));
            if (!roomOnly)
            {
                Button room = ActionButton(preview, L("Odayı aç", "View room"), Ivory, Ink, () => { roomOnly = true; Rebuild(); });
                Place((RectTransform)room.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-102, 8), new Vector2(-8, 53));
                BuildList(list, portrait);
            }
            else list.gameObject.SetActive(false);
            toast = Caption(safe, "", 15, Ivory, true, TextAnchor.MiddleCenter);
            Place(toast.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(10, 3), new Vector2(-10, 44));
            toast.gameObject.SetActive(false);
            Vector3 focus = game.GarageRoom.Focus + new Vector3(.25f, .02f, -.40f);
            Quaternion orientation = Quaternion.Euler(34, 32, 0);
            previewCamera.transform.SetPositionAndRotation(focus - orientation * Vector3.forward * 16, orientation);
            previewCamera.orthographicSize = roomOnly ? 4.8f : 3.9f;
            Canvas.ForceUpdateCanvases();
            previewCamera.aspect = image.rect.width / Mathf.Max(1, image.rect.height);
        }

        void BuildList(RectTransform list, bool portrait)
        {
            tabs = Rect("Store navigation", list);
            Place(tabs, new Vector2(0, 1), Vector2.one, new Vector2(0, -102), Vector2.zero);
            RectTransform view = Rect("Catalog viewport", list);
            Place(view, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -111));
            Plate(view, ToyGeometry.Hex("314E63"));
            scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 48;
            RectTransform clip = Rect("Clipped cards", view);
            Place(clip, Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -5));
            clip.gameObject.AddComponent<RectMask2D>();
            items = Rect("Scrollable product cards", clip);
            items.anchorMin = new Vector2(0, 1); items.anchorMax = Vector2.one; items.pivot = new Vector2(.5f, 1);
            items.sizeDelta = Vector2.zero;
            items.anchoredPosition = Vector2.zero;
            var stack = items.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(5, 5, 4, 6);
            stack.spacing = 8;
            stack.childControlHeight = true; stack.childControlWidth = true; stack.childForceExpandHeight = false;
            var size = items.gameObject.AddComponent<ContentSizeFitter>();
            size.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = clip;
            scroll.content = items;
            RefreshTabs();
            RefreshCards(true);
        }

        void RefreshTabs()
        {
            Clear(tabs);
            string[] names = { L("Araçlar", "Rides"), L("Boyalar", "Paint"), L("Dekor", "Decor"), "Drone",
                L("İşler", "Jobs"), L("Mağaza", "Store"), L("Bölgeler", "Districts"), L("Depo", "Depot") };
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                Button button = ActionButton(tabs, names[i], tab == i ? Gold : Ivory, Ink, () => SelectTab(index));
                int col = i % 4, row = i / 4;
                Place((RectTransform)button.transform, new Vector2(col * .25f, 1 - (row + 1) * .5f),
                    new Vector2((col + 1) * .25f, 1 - row * .5f), new Vector2(2, 2), new Vector2(-2, -2));
            }
        }

        void SelectTab(int index)
        {
            tab = index;
            RefreshTabs();
            RefreshCards(true);
        }

        void RefreshCards(bool jumpTop)
        {
            if (!items || roomOnly) return;
            float oldPosition = scroll.verticalNormalizedPosition;
            lastReady = game.StoreReady;
            lastAdReady = game.AdsReady;
            lastBalance = game.Coins;
            Clear(items);
            CardCount = 0;
            if (tab == 0) Vehicles();
            else if (tab == 1) Paints();
            else if (tab == 2) Decorations();
            else if (tab == 3) Drone();
            else if (tab == 4) Jobs();
            else if (tab == 5) Store();
            else if (tab == 6) Districts();
            else Depot();
            LayoutRebuilder.ForceRebuildLayoutImmediate(items);
            scroll.verticalNormalizedPosition = jumpTop ? 1 : Mathf.Clamp01(oldPosition);
        }

        void Vehicles()
        {
            for (int i = 0; i < game.VehicleCount; i++)
            {
                int index = i;
                bool selected = game.VehicleIndex == i;
                bool owned = game.VehicleOwned(i), accessible = game.VehicleAccessible(i);
                string action = selected ? L("Seçili", "Selected") : accessible ? L("Seç", "Equip") : CoinText(game.VehiclePrice(i));
                string rarity = i >= 4 ? L("EFSANEVİ", "LEGENDARY") : i >= 2 ? L("NADİR", "RARE") : L("ŞEHİR SERİSİ", "CITY SERIES");
                RectTransform card = Card(game.VehicleNameFor(i), rarity, game.VehicleHintFor(i), i >= 4 ? Violet : Teal, action,
                    !selected && game.CanChangeVehicle && (accessible || game.Coins >= game.VehiclePrice(i)),
                    () => Act(() => game.SelectVehicle(index)));
                AddSmallAction(card, L("3D incele", "Inspect 3D"), () => PreviewVehicle(index));
                if (i >= 4 && !owned && (game.AdsReady || game.CommerceIsMock))
                    Card(L("3 dakikalık deneme", "3-minute trial"), rarity, game.VehicleNameFor(i),
                        Violet, game.CommerceIsMock ? L("TEST reklam", "TEST ad") : L("Reklam izle", "Watch ad"),
                        game.CanRequestRental(i), () => Act(() => game.RentWithAd(index)));
            }
        }

        void Paints()
        {
            string[] names = { L("Turkuaz", "Turquoise"), L("Mercan", "Coral"), L("Altın", "Gold") };
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                bool owned = game.IsColorUnlocked(i), selected = game.ScooterColorIndex == i;
                RectTransform card = Card(names[i], L("GÖVDE BOYASI", "BODY PAINT"), L("Renk tüm araçlarda görünür.", "Paint applies to every ride."),
                    i == 1 ? Coral : Teal, selected ? L("Seçili", "Selected") : owned ? L("Uygula", "Apply") : CoinText(game.ColorPrice(i)),
                    !selected && (owned || game.Coins >= game.ColorPrice(i)), () => Act(() => game.SelectColor(index)));
                AddSmallAction(card, L("3D dene", "Preview"), () =>
                {
                    game.GarageRoom.SetScooterColor(CourierGame.ScooterColors[index]);
                    previewLabel.text = names[index];
                });
            }
        }

        void Decorations()
        {
            for (int i = 0; i < game.DecorCount; i++)
            {
                int index = i;
                bool owned = game.DecorOwned(i);
                Card(game.DecorName(i), i >= 9 ? L("KOLEKSİYON", "COLLECTION") : L("GARAJ", "GARAGE"),
                    L("Garajda kendi yerinde sergilenir.", "Displayed in its own spot in the garage."),
                    i >= 9 ? Violet : Teal, owned ? game.DecorEquipped(i) ? L("Gizle", "Hide") : L("Göster", "Show") : CoinText(game.DecorPrice(i)),
                    owned || game.Coins >= game.DecorPrice(i), () => Act(() => game.SelectDecor(index)));
            }
        }

        void Drone()
        {
            Card(L("Kargo drone’u", "Parcel drone"), L("YARDIMCI", "HELPER"), game.DroneStatus, Teal,
                !game.DroneOwned ? CoinText(game.DronePrice) : game.DroneBusy ? L("Görevde", "In flight") : L("Gönder", "Send"),
                game.DroneOwned ? game.CanLaunchDrone : game.Coins >= game.DronePrice,
                () => Act(() => { if (game.DroneOwned) game.LaunchDrone(); else game.BuyDrone(); }));
        }

        void Jobs()
        {
            for (int i = 0; i < game.BusinessCount; i++)
            {
                int index = i;
                Card(game.BusinessName(i), game.BusinessProgress(i) + " / " + game.BusinessTarget(i), game.BusinessHint(i),
                    Teal, "+" + CoinText(game.BusinessReward(i)), game.CanClaimBusiness(i), () => Act(() => game.ClaimBusiness(index)));
            }
        }

        void Store()
        {
            Card(L("Satın alma hakları", "Purchase entitlements"), game.CommerceIsMock ? L("TEST MODU", "TEST MODE") : L("CİHAZ MAĞAZASI", "DEVICE STORE"),
                game.CommerceIsMock ? L("Deneme satın alımları ücret almaz.", "Test purchases do not charge you.") :
                    L("Fiyat ve ödeme cihaz mağazası üzerinden gelir.", "Your device store supplies pricing and payment."),
                Violet, L("Geri yükle", "Restore"), game.StoreReady, () => Act(game.RestorePurchases));
            var products = game.StoreProducts;
            for (int i = 0; i < products.Count; i++)
            {
                StoreProduct item = products[i];
                if (item == null) continue;
                var definition = CommerceConfiguration.Find(item.Key);
                string title = definition == null ? item.Title : definition.Title;
                bool owned = item.Key == CommerceConfiguration.RemoveAds ? game.NoInterstitialAds :
                    item.Key == CommerceConfiguration.Dragon ? game.RootPermanentVehicleOwned(4) :
                    item.Key == CommerceConfiguration.Dino ? game.RootPermanentVehicleOwned(5) :
                    item.Key == CommerceConfiguration.Fleet && game.RootPermanentVehicleOwned(4) && game.RootPermanentVehicleOwned(5);
                bool enabled = game.Phase == GamePhase.Garage && game.StoreReady && item.Available && !owned && !string.IsNullOrEmpty(item.LocalizedPrice);
                string detail = item.Consumable ? "+" + CoinText(item.CoinAmount) : L("Mağaza hesabına bağlı kalıcı hak.", "Permanent entitlement linked to your store account.");
                string price = owned ? L("Sende var", "Owned") : enabled ? item.LocalizedPrice : L("Kapalı", "Unavailable");
                Card(title, item.Consumable ? L("JETON PAKETİ", "COIN PACK") : L("KALICI", "PERMANENT"), detail, Violet,
                    price, enabled, () => Confirm(title, detail + "\n" + item.LocalizedPrice, () => Act(() => game.PurchaseProduct(item.Key))));
            }
        }

        void Districts()
        {
            for (int i = 0; i < CourierGame.DistrictCount; i++)
            {
                int index = i;
                bool owned = game.DistrictOwned(i);
                string detail = CourierGame.DistrictHint(i);
                if (i > 0) detail += "  +" + CoinText(game.DistrictFee(i)) + L(" temel ücret", " base fee");
                Card(CourierGame.DistrictName(i), owned ? L("AÇIK BÖLGE", "OPEN DISTRICT") : L("KİLİTLİ BÖLGE", "LOCKED DISTRICT"),
                    detail, Accents[i], owned ? L("Tura başla", "Start tour") : CoinText(game.DistrictPrice(i)),
                    game.Phase == GamePhase.Garage && (owned || game.Coins >= game.DistrictPrice(i)), () =>
                    {
                        if (!owned) Confirm(CourierGame.DistrictName(index), CoinText(game.DistrictPrice(index)),
                            () => Act(() => game.UnlockDistrict(index)));
                        else if (game.HasSavedRun) Confirm(CourierGame.DistrictName(index),
                            L("Bu tur mevcut teslimatın yerine geçer.", "This tour replaces your current delivery."),
                            () => game.StartDistrict(index));
                        else game.StartDistrict(index);
                    });
            }
        }

        void Depot()
        {
            Card(L("Kargo deposu", "Parcel depot"), L("SEVİYE ", "LEVEL ") + game.DepotLevel + " / " + CourierGame.MaxDepotLevel,
                L("Her kademe yüklemeyi ve drone uçuşunu hızlandırır, darbe hasarını azaltır.", "Each level speeds up loading and drone flights and cushions impacts."),
                Coral, game.DepotLevel < CourierGame.MaxDepotLevel ? CoinText(game.DepotPrice) : L("Tamamlandı", "Complete"),
                game.Phase == GamePhase.Garage && game.DepotLevel < CourierGame.MaxDepotLevel && game.Coins >= game.DepotPrice,
                () => Confirm(L("Depoyu yükselt", "Upgrade depot"), CoinText(game.DepotPrice), () => Act(game.UpgradeDepot)));
        }

        RectTransform Card(string title, string badge, string detail, Color accent, string action, bool enabled, Action click)
        {
            CardCount++;
            RectTransform row = Rect(title, items);
            LayoutElement size = row.gameObject.AddComponent<LayoutElement>();
            size.preferredHeight = 113;
            Plate(row, Ivory);
            RectTransform strip = Rect("Card accent", row);
            Place(strip, Vector2.zero, new Vector2(0, 1), new Vector2(4, 8), new Vector2(10, -8));
            Plate(strip, accent);
            Text flag = Caption(row, badge, 12, accent, true);
            Place(flag.rectTransform, Vector2.zero, Vector2.one, new Vector2(18, 89), new Vector2(-124, -5));
            Text name = Caption(row, title, 19, Ink, true);
            Place(name.rectTransform, Vector2.zero, Vector2.one, new Vector2(18, 53), new Vector2(-123, -23));
            Text summary = Caption(row, detail, 14, Muted, false);
            Place(summary.rectTransform, Vector2.zero, Vector2.one, new Vector2(18, 4), new Vector2(-121, -58));
            Button button = ActionButton(row, action, enabled ? Teal : ToyGeometry.Hex("DCE6E4"), enabled ? Color.white : Ink, click);
            button.interactable = enabled;
            Place((RectTransform)button.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-112, 9), new Vector2(-8, 57));
            return row;
        }

        void AddSmallAction(RectTransform card, string label, Action action)
        {
            Button button = ActionButton(card, label, ToyGeometry.Hex("E5DDF1"), Ink, action);
            Place((RectTransform)button.transform, new Vector2(1, 1), Vector2.one, new Vector2(-112, -54), new Vector2(-8, -6));
        }

        void Confirm(string title, string detail, Action accepted)
        {
            if (confirmation) return;
            confirmation = Rect("Purchase or tour confirmation", safe);
            Fill(confirmation);
            Plate(confirmation, new Color(.06f, .11f, .18f, .82f), false);
            RectTransform box = Rect("Confirmation panel", confirmation);
            Place(box, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-156, -106), new Vector2(156, 106));
            Plate(box, Ivory);
            Text head = Caption(box, title, 22, Ink, true, TextAnchor.MiddleCenter);
            Place(head.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 148), new Vector2(-10, -14));
            Text info = Caption(box, detail, 17, Muted, false, TextAnchor.MiddleCenter);
            Place(info.rectTransform, Vector2.zero, Vector2.one, new Vector2(14, 68), new Vector2(-14, -65));
            Button no = ActionButton(box, L("Vazgeç", "Cancel"), ToyGeometry.Hex("DCE6E4"), Ink, DismissConfirmation);
            Button yes = ActionButton(box, L("Onayla", "Confirm"), Teal, Color.white, () => { DismissConfirmation(); accepted(); });
            Place((RectTransform)no.transform, Vector2.zero, new Vector2(.5f, 0), new Vector2(12, 10), new Vector2(-4, 61));
            Place((RectTransform)yes.transform, new Vector2(.5f, 0), new Vector2(1, 0), new Vector2(4, 10), new Vector2(-12, 61));
        }

        void DismissConfirmation()
        {
            if (!confirmation) return;
            confirmation.gameObject.SetActive(false);
            Destroy(confirmation.gameObject);
            confirmation = null;
        }

        void Act(Action action)
        {
            action();
            previewVehicle = -1;
            game.RefreshGarage();
            if (previewLabel) previewLabel.text = PreviewName();
            RefreshCards(false);
        }

        string PreviewName() => game.VehicleNameFor(previewVehicle >= 0 ? previewVehicle : game.VehicleIndex);
        void PreviewVehicle(int index)
        {
            previewVehicle = index;
            game.GarageRoom.SetVehicleStyle(index);
            previewLabel.text = PreviewName();
        }

        internal void SelectTabForTest(int oldTab)
        {
            tab = oldTab == 1 ? 3 : oldTab == 3 ? 4 : oldTab == 4 ? 5 : oldTab;
            if (Visible && !roomOnly) SelectTab(tab);
        }

        internal void PreviewRoomForTest(bool value)
        {
            roomOnly = value;
            if (Visible) Rebuild();
        }

        internal bool ClickForTest(string name)
        {
            if (!Visible) return false;
            foreach (Button button in safe.GetComponentsInChildren<Button>())
            {
                if (button.name != name || !button.interactable) continue;
                ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
                return true;
            }
            return false;
        }

        static string L(string tr, string en) => GameText.Pick(tr, en);
        static string CoinText(int count) => count + L(" jeton", " coins");
        static void Clear(RectTransform group)
        {
            for (int i = group.childCount - 1; i >= 0; i--)
            {
                GameObject child = group.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }
        static RectTransform Rect(string name, Transform parent)
        {
            var node = new GameObject(name, typeof(RectTransform));
            node.transform.SetParent(parent, false);
            return (RectTransform)node.transform;
        }
        static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 start, Vector2 end)
        { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = start; rect.offsetMax = end; }
        static void Fill(RectTransform rect) => Place(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image Plate(RectTransform rect, Color tint, bool corners = true)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = tint;
            if (corners) { image.sprite = rounded; image.type = Image.Type.Sliced; }
            return image;
        }
        Text Caption(Transform parent, string value, int size, Color tint, bool bold, TextAnchor align = TextAnchor.MiddleLeft)
        {
            Text label = Rect("Label", parent).gameObject.AddComponent<Text>();
            label.font = font; label.fontSize = size; label.text = value; label.color = tint;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; label.alignment = align;
            label.supportRichText = false; label.raycastTarget = false;
            return label;
        }
        Button ActionButton(Transform parent, string label, Color fill, Color ink, Action action)
        {
            RectTransform rect = Rect(label, parent);
            Image image = Plate(rect, fill);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.pressedColor = ToyGeometry.Hex("C6D9DA");
            colors.highlightedColor = ToyGeometry.Hex("F4F9F7");
            colors.disabledColor = Color.white;
            colors.fadeDuration = game.ReducedMotion ? 0 : .12f;
            button.colors = colors;
            button.onClick.AddListener(() => action());
            Text text = Caption(rect, label, 15, ink, true, TextAnchor.MiddleCenter);
            Place(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(3, 2), new Vector2(-3, -2));
            return button;
        }
    }
}
