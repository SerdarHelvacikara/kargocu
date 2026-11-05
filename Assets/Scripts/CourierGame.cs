using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KurmaliKargo
{
    // New values go at the end: old run snapshots store the phase as an int.
    public enum GamePhase { Menu, Playing, Result, Shop, Garage, Settings, Privacy, Paused, Tutorial, ShiftCard, ShiftEnd }

    /// <summary>Small, entirely local delivery loop. No networking or paid assets.</summary>
    public sealed partial class CourierGame : MonoBehaviour
    {
        public GamePhase Phase { get; private set; }
        public int Coins { get; private set; }
        public bool IsPlaytest { get; private set; }
        public int BestScore { get; private set; }
        public int Completed { get; private set; }
        public float Cargo { get; private set; } = 100;
        public float TimeLeft { get; private set; } = 55;
        public bool HasPackage { get; private set; }
        public string TargetName => IsParcelDropped ? GameText.Pick("Düşen paketin", "Your dropped parcel") : HasPackage ? StopName(AwaitingOrderCheckpoint ? ChainCheckpoint : ActiveStopIndex) : PickupName;
        public string Notice { get; private set; } = "";
        public float NoticeTime { get; private set; }
        public string ResultTitle => Completed == 3 ? GameText.Pick("Mahallenin yıldızı!", "Neighborhood star!") :
            LastIntegrity >= 90 ? GameText.Pick("Mis gibi teslimat!", "Lovely delivery!") : GameText.Pick("Paket yerine ulaştı!", "Delivery made!");
        public string ResultLine => LastOrderReaction() + (Completed == 3 ? "\n" +
            GameText.Pick("Üç teslimat tamam! Tur kazancın: ", "Three deliveries done! Tour earnings: ") + runScore + GameText.Pick(" jeton.", " coins.") : "");
        public int LastPayout { get; private set; }
        public int LastSpeedBonus { get; private set; }
        public int LastCareBonus { get; private set; }
        public int ScooterColorIndex { get; private set; }
        public Vector3 TargetPosition => IsParcelDropped ? DroppedParcelPosition : HasPackage ? world.Stops[AwaitingOrderCheckpoint ? ChainCheckpoint : ActiveStopIndex] : shiftActive ? ShiftPickupTarget : PickupPosition;
        public Transform PlayerTransform => player;
        public Camera GameCamera { get; private set; }
        public bool IsMuted { get; private set; }
        public bool IsTurkish => GameText.Turkish;
        public bool ShopReturnsToResult => shopReturn == GamePhase.Result;
        public bool ShopReturnsToGarage => shopReturn == GamePhase.Garage;
        public int GarageDecorationCount => garageVisual ? garageVisual.VisibleDecorCount : 0;
        public ParcelKind ActiveParcel { get; private set; }
        public ParcelKind LastParcel { get; private set; }
        public float LastIntegrity { get; private set; } = 100;
        public string ParcelName => IsHeavyDelivery ? GameText.Pick("Fabrikanın zor yükü", "Factory heavy haul") : OrderName(CurrentOrder, HasPackage ? ActiveParcel : (ParcelKind)Mathf.Min(Completed, 2));
        public string ParcelHint => IsParcelDropped ? GameText.Pick("Geri dönüp ışıklı halkadan al; teslimata devam et.", "Go back to the glowing ring, collect it and carry on.") : OrderHint(HasPackage ? ActiveParcel : (ParcelKind)Mathf.Min(Completed, 2));
        public float HandlingSpeed => (!HasPackage ? 5.1f : ActiveParcel == ParcelKind.Plant ? 4.6f : ActiveParcel == ParcelKind.TeaSet ? 5.3f : 5.1f) * VehicleSpeedScale * (dogs && streetEnabled ? dogs.SlowMultiplier : 1) * (HasPackage && IsHeavyDelivery ? .75f : 1) * incidents.SpeedScale(player.position) * ShiftSpeedScale;
        public float HandlingAcceleration => (!HasPackage ? 32f : ActiveParcel == ParcelKind.Plant ? 13f : ActiveParcel == ParcelKind.Cake ? 24f : 28f) * VehicleAccelerationScale * (HasPackage && IsHeavyDelivery ? .7f : 1);
        public float Fragility => (ActiveParcel == ParcelKind.Plant ? .75f : ActiveParcel == ParcelKind.TeaSet ? 1.5f : 1.15f) * VehicleFragilityScale * OrderFragility;
        public ParcelVisual CargoVisual => parcelVisual;

        public static readonly Color[] ScooterColors = { new Color(.06f, .69f, .65f), new Color(.98f, .38f, .31f), new Color(1f, .73f, .18f) };
        public bool IsColorUnlocked(int index) => index == 0 || (unlockedColors & (1 << index)) != 0;
        public int ColorPrice(int index) => index == 0 ? 0 : index == 1 ? 120 : 180;

        WorldData world;
        internal CityLandmarks CityScene => world.City;
        internal GarageVisual GarageRoom => garageVisual;
        internal GarageCanvas GarageScreen { get; private set; }
        CourierHUD hud;
        ReleasePanels releasePanels;
        GarageVisual garageVisual;
        ParcelVisual parcelVisual;
        CustomerVisual[] customers;
        Transform player, model, package, marker, arrow, steering, frontWheel, backWheel;
        CharacterController controller;
        Material scooterPaint, markerMaterial;
        AudioSource audioSource;
        readonly Dictionary<int, AudioClip> notes = new Dictionary<int, AudioClip>();
        int unlockedColors = 1, runScore;
        bool pointerHeld, smokeTest, persistenceTest;
        Vector2 stickOrigin, stickPosition;
        Vector3 velocity, menuCameraTarget;
        float hitCooldown, pickupDelay, wobble, confettiTime, nextExhaust, previousYaw, nextAutosave;
        float deliveryCelebration;
        readonly HashSet<Collider> damageContacts = new HashSet<Collider>();
        readonly List<Collider> contactsToRemove = new List<Collider>();
        GamePhase shopReturn = GamePhase.Menu;
        Texture2D circle;
        static readonly Dictionary<Color, Material> Paints = new Dictionary<Color, Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindAnyObjectByType<CourierGame>() == null)
                new GameObject("Kurmalı Kargo").AddComponent<CourierGame>();
        }

        void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = 85;
            QualitySettings.shadowResolution = ShadowResolution.High;
            Time.timeScale = 1;
            smokeTest = Array.Exists(Environment.GetCommandLineArgs(), a => a == "-smokeTest");
            bool persistenceWrite = Array.Exists(Environment.GetCommandLineArgs(), a => a == "-persistenceCheckWrite");
            persistenceTest = persistenceWrite || Array.Exists(Environment.GetCommandLineArgs(), a => a == "-persistenceCheckRead");
            shiftTest = smokeTest && Array.Exists(Environment.GetCommandLineArgs(), a => a == "-shiftTest");
            IsPlaytest = !smokeTest && !persistenceTest && Array.Exists(Environment.GetCommandLineArgs(), a => a == "-playtest");
            LocalProgress.Configure(persistenceTest, persistenceWrite);
            GameText.Initialize(smokeTest);
            Coins = smokeTest ? 0 : LocalProgress.GetInt("coins", IsPlaytest ? 50000 : 0);
            BestScore = smokeTest ? 0 : LocalProgress.GetInt("best", 0);
            unlockedColors = smokeTest ? 1 : LocalProgress.GetInt("colors", 1);
            ScooterColorIndex = smokeTest ? 0 : Mathf.Clamp(LocalProgress.GetInt("paint", 0), 0, 2);
            if (!IsColorUnlocked(ScooterColorIndex)) ScooterColorIndex = 0;
            IsMuted = !smokeTest && LocalProgress.GetInt("mute", 0) == 1;
            LoadProgressExtras();
            BuildLighting();
            world = WorldBuilder.Build();
            districts[0] = world;
            streetEvents = StreetEvents.Create();
            incidents = new NeighborhoodIncidents();
            announceIncident = AnnounceIncident;
            streetEvents.ConfigureFixed(false, false);
            streetEnabled = !smokeTest;
            streetEvents.gameObject.SetActive(streetEnabled);
            CreateScooter();
            droneVisual = DroneVisual.Create();
            dogs = DogStreets.Create();
            dogs.FunnyCue += () => Notify(GameText.Pick("Mahallenin bekçisi peşinde! Açık sokağa çık.", "The neighborhood guard is on your tail! Find the open street."));
            dogs.Configure(0);
            customers = new CustomerVisual[world.Stops.Length];
            for (int i = 0; i < customers.Length; i++)
                customers[i] = CustomerVisual.Create(world.Stops[i] + new Vector3(-.7f, 0, 1.6f), world.StopColors[i], new Vector3(.63f, 0, -.77f));
            CreateCamera();
            CreateMarker();
            garageVisual = GarageVisual.Create();
            GarageScreen = gameObject.AddComponent<GarageCanvas>();
            GarageScreen.Initialize(this);
            RefreshGarage();
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.volume = .24f * Volume;
            InitializeStreetMusic();
            hud = gameObject.AddComponent<CourierHUD>();
            hud.Game = this;
            hud.OwnsColor = IsColorUnlocked;
            releasePanels = gameObject.AddComponent<ReleasePanels>();
            releasePanels.Game = this;
            gameObject.AddComponent<ShiftHUD>().Game = this;
            Lobby = gameObject.AddComponent<LobbyUI>();
            Lobby.Game = this;
            Phase = GamePhase.Menu;
            LoadRun();
            InitializeCommerce();
            InitializeFeel();
            if (shiftTest) gameObject.AddComponent<CourierShiftTest>();
            else if (smokeTest && Array.Exists(Environment.GetCommandLineArgs(), a => a == "-v08Test")) gameObject.AddComponent<CourierV08Test>();
            else if (smokeTest) gameObject.AddComponent<CourierSmokeTest>();
            else if (persistenceTest) gameObject.AddComponent<CourierPersistenceTest>();
        }

        void BuildLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.78f, .87f, 1f);
            RenderSettings.ambientEquatorColor = new Color(.66f, .78f, .76f);
            RenderSettings.ambientGroundColor = new Color(.45f, .60f, .53f);
            RenderSettings.ambientIntensity = 1.0f;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.66f, .83f, .78f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 45;
            RenderSettings.fogEndDistance = 95;
            var light = new GameObject("Afternoon sunshine").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(48, -28, 0);
            light.color = new Color(1f, .93f, .79f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = .36f;
            light.shadowBias = .04f;
            light.shadowNormalBias = .25f;
        }

        void CreateCamera()
        {
            var cameraObject = new GameObject("Neighborhood camera");
            GameCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            GameCamera.orthographic = true;
            GameCamera.orthographicSize = 19.0f;
            GameCamera.backgroundColor = ToyGeometry.Hex("F1CBA9");
            GameCamera.clearFlags = CameraClearFlags.SolidColor;
            GameCamera.nearClipPlane = .1f;
            GameCamera.farClipPlane = 120;
            GameCamera.allowHDR = false;
            GameCamera.transform.rotation = Quaternion.Euler(49f, -39f, 0);
            menuCameraTarget = new Vector3(-2, 0, 1);
            GameCamera.transform.position = menuCameraTarget - GameCamera.transform.forward * 42;
            cameraObject.AddComponent<AudioListener>();
        }

        static Material Paint(Color color)
        {
            if (Paints.TryGetValue(color, out Material cached) && cached) return cached;
            var m = new Material(Shader.Find("Standard"));
            m.color = color;
            m.SetFloat("_Glossiness", .14f);
            m.SetFloat("_SpecularHighlights", 0);
            Paints[color] = m;
            return m;
        }

        static Transform Part(Transform parent, string name, PrimitiveType primitive, Vector3 pos, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            var collider = go.GetComponent<Collider>();
            if (collider) Destroy(collider);
            return go.transform;
        }

        void CreateScooter()
        {
            player = new GameObject("Courier scooter").transform;
            player.position = world.Depot;
            controller = player.gameObject.AddComponent<CharacterController>();
            controller.height = .9f;
            controller.radius = .35f;
            controller.center = new Vector3(0, .48f, 0);
            controller.stepOffset = .08f;
            controller.skinWidth = .025f;
            player.gameObject.AddComponent<ScooterCollision>().Game = this;
            model = new GameObject("Scooter and rider").transform;
            model.SetParent(player, false);
            scooterPaint = new Material(Paint(ScooterColors[ScooterColorIndex]));
            var tire = Paint(new Color(.16f, .22f, .28f));
            var cream = Paint(new Color(1f, .94f, .79f));
            var navy = Paint(new Color(.19f, .26f, .37f));
            var metal = Paint(new Color(.72f, .80f, .81f));
            var skin = Paint(new Color(1f, .73f, .49f));
            var coral = Paint(new Color(1f, .41f, .31f));
            var gold = Paint(new Color(1f, .76f, .25f));
            backWheel = Part(model, "Back wheel", PrimitiveType.Cylinder, new Vector3(0, .29f, -.51f), new Vector3(.55f, .13f, .55f), tire);
            backWheel.localRotation = Quaternion.Euler(0, 0, 90);
            frontWheel = Part(model, "Front wheel", PrimitiveType.Cylinder, new Vector3(0, .29f, .62f), new Vector3(.55f, .12f, .55f), tire);
            frontWheel.localRotation = Quaternion.Euler(0, 0, 90);
            foreach (float z in new[]{-.51f,.62f})
                foreach (float x in new[]{-.14f,.14f})
                {
                    var hub = Part(model, "Cream wheel hub", PrimitiveType.Cylinder, new Vector3(x, .29f, z), new Vector3(.29f, .02f, .29f), cream);
                    hub.localRotation = Quaternion.Euler(0, 0, 90);
                }
            Part(model, "Footboard", PrimitiveType.Cube, new Vector3(0, .43f, .02f), new Vector3(.55f, .15f, 1.16f), scooterPaint);
            Part(model, "Rounded rear body", PrimitiveType.Sphere, new Vector3(0, .58f, -.42f), new Vector3(.72f, .63f, .78f), scooterPaint);
            Part(model, "Seat", PrimitiveType.Sphere, new Vector3(0, .91f, -.27f), new Vector3(.59f, .18f, .62f), navy);
            steering = Part(model, "Front shield", PrimitiveType.Sphere, new Vector3(0, .82f, .49f), new Vector3(.65f, .94f, .28f), scooterPaint);
            Part(model, "Headlight", PrimitiveType.Sphere, new Vector3(0, 1.14f, .66f), new Vector3(.29f, .26f, .13f), cream);
            Part(model, "Handlebars", PrimitiveType.Cube, new Vector3(0, 1.24f, .43f), new Vector3(.86f, .075f, .09f), metal);
            Part(model, "Rear light", PrimitiveType.Sphere, new Vector3(0, .69f, -.81f), new Vector3(.19f, .12f, .075f), coral);
            // Exaggerated simple courier silhouette, with no rigging dependency.
            Part(model, "Uniform", PrimitiveType.Capsule, new Vector3(0, 1.28f, -.15f), new Vector3(.43f, .38f, .40f), coral);
            Part(model, "Face", PrimitiveType.Sphere, new Vector3(0, 1.85f, -.02f), new Vector3(.48f, .48f, .47f), skin);
            Part(model, "Helmet", PrimitiveType.Sphere, new Vector3(0, 2.01f, -.065f), new Vector3(.57f, .36f, .55f), scooterPaint);
            Part(model, "Helmet brim", PrimitiveType.Cube, new Vector3(0, 1.99f, .19f), new Vector3(.52f, .055f, .30f), cream);
            foreach (float x in new[]{-.105f,.105f})
            {
                Part(model, "Eye", PrimitiveType.Sphere, new Vector3(x, 1.86f, .196f), new Vector3(.043f, .06f, .025f), navy);
                var arm = Part(model, "Sleeve", PrimitiveType.Capsule, new Vector3(x*2.3f, 1.27f, .10f), new Vector3(.14f, .22f, .14f), coral);
                arm.localRotation = Quaternion.Euler(57, 0, x > 0 ? -12 : 12);
                Part(model, "Hand", PrimitiveType.Sphere, new Vector3(x*3f, 1.25f, .37f), new Vector3(.14f, .13f, .13f), skin);
                var leg = Part(model, "Trouser leg", PrimitiveType.Capsule, new Vector3(x*2.3f, .85f, -.04f), new Vector3(.15f, .23f, .16f), navy);
                leg.localRotation = Quaternion.Euler(-23, 0, 0);
                Part(model, "Shoe", PrimitiveType.Sphere, new Vector3(x*2.5f, .58f, .12f), new Vector3(.21f, .15f, .33f), cream);
            }
            package = new GameObject("Parcel").transform;
            package.SetParent(model, false);
            package.localPosition = new Vector3(0, 1.18f, -.76f);
            parcelVisual = package.gameObject.AddComponent<ParcelVisual>();
            parcelVisual.Configure(ParcelKind.Cake);
            package.gameObject.SetActive(false);
            player.rotation = Quaternion.Euler(0, 45, 0);
            BuildVehicleDetails();
        }

        void CreateMarker()
        {
            marker = new GameObject("Delivery destination beacon").transform;
            markerMaterial = new Material(Paint(new Color(1f, .77f, .18f)));
            for (int i=0; i<24; i++)
            {
                float angle = i * Mathf.PI * 2 / 24;
                var p = new Vector3(Mathf.Cos(angle)*1.0f, .13f, Mathf.Sin(angle)*1.0f);
                var t = Part(marker, "Destination ring", PrimitiveType.Cube, p, new Vector3(.23f,.05f,.11f), markerMaterial);
                t.localRotation = Quaternion.Euler(0,-angle*Mathf.Rad2Deg,0);
            }
            arrow = new GameObject("Floating parcel marker").transform;
            arrow.SetParent(marker, false);
            Part(arrow, "Parcel icon", PrimitiveType.Cube, new Vector3(0,2.7f,0), Vector3.one*.51f, markerMaterial);
            Part(arrow, "Parcel tape", PrimitiveType.Cube, new Vector3(0,2.96f,0), new Vector3(.12f,.015f,.52f), Paint(Color.white));
            var tip = Part(arrow, "Pointer", PrimitiveType.Cube, new Vector3(0,2.21f,0), Vector3.one*.21f, markerMaterial);
            tip.localRotation = Quaternion.Euler(0,0,45);
        }

        void Update()
        {
            GamePhase inputPhase = Phase;
            if (Phase == GamePhase.Playing && deliveryCelebration > 0)
            {
                deliveryCelebration = Mathf.Max(0, deliveryCelebration - Time.deltaTime);
                if (deliveryCelebration == 0) { Phase = GamePhase.Result; Save(); }
            }
            if (settingsSaveAt >= 0 && Time.unscaledTime >= settingsSaveAt) { settingsSaveAt = -1; Save(); }
            TickDrone(Time.deltaTime);
            TickCommerce(Time.deltaTime);
            TickFeel(Time.deltaTime);
            TickUrgency(Time.deltaTime);
            if (world.City) world.City.Tick(Time.deltaTime, Phase == GamePhase.Playing, ReducedMotion, player.position, TrafficMargin);
            TickStreetMusic();
            TickIncidents(Time.deltaTime);
            TickDogs(Time.deltaTime);
            if (streetEnabled) streetEvents.Tick(Time.deltaTime, Phase == GamePhase.Playing, player.position, ReducedMotion);
            NoticeTime = Mathf.Max(0, NoticeTime - Time.deltaTime);
            hitCooldown -= Time.deltaTime;
            pickupDelay -= Time.deltaTime;
            wobble = Mathf.MoveTowards(wobble, 0, Time.deltaTime * 2.8f);
            // In a shift, waiting orders have their own rings; the main beacon marks the drop-off.
            marker.gameObject.SetActive(Phase == GamePhase.Playing && deliveryCelebration <= 0 && (!shiftActive || HasPackage || IsParcelDropped));
            TickBoardVisuals();
            if (Phase == GamePhase.Playing)
            {
                marker.position = TargetPosition;
                float wave = ReducedMotion ? 0 : Mathf.Sin(Time.time * 3);
                arrow.localPosition = new Vector3(0, wave * .14f, 0);
                arrow.localRotation = Quaternion.Euler(0, ReducedMotion ? 0 : Time.time * 35, 0);
                markerMaterial.color = HasPackage || IsParcelDropped ? new Color(1f,.77f,.18f) : new Color(.12f,.78f,.69f);
                if (HasPackage || IsParcelDropped) TimeLeft = Mathf.Max(0, TimeLeft - Time.deltaTime);
                TickOrder(Time.deltaTime);
                TickShift(Time.deltaTime);
                if (Phase == GamePhase.Playing && !smokeTest && !persistenceTest) MoveFromInput();
                if (Phase == GamePhase.Playing && deliveryCelebration <= 0 && pickupDelay <= 0 && Vector3.Distance(Flat(player.position), Flat(TargetPosition)) < ReachRadius)
                {
                    if (!HasPackage)
                    {
                        bool recovering = IsParcelDropped;
                        if (!shiftActive || recovering) Pickup();
                        else if (ClaimNearestShiftOrder()) { Pickup(); ShiftPickedUp(); }
                    }
                    else if (!TryOrderCheckpoint())
                    {
                        if (shiftActive) CompleteShiftDelivery(); else CompleteDelivery();
                    }
                }
                if (Time.unscaledTime >= nextAutosave) { nextAutosave = Time.unscaledTime + 8; Save(); }
            }
            else { pointerHeld = false; velocity = Vector3.zero; }
            if (Phase == inputPhase && Input.GetKeyDown(KeyCode.Escape))
            {
                if (Phase == GamePhase.Playing) PauseGame();
                else if (Phase == GamePhase.Tutorial) FinishTutorial();
                else if (Phase != GamePhase.Menu) CloseOverlay();
            }
            foreach (var customer in customers) customer.Tick(Time.deltaTime, Phase == GamePhase.Playing, ReducedMotion);
            AnimateScooter();
        }

        static Vector3 Flat(Vector3 p) => new Vector3(p.x,0,p.z);

        void MoveFromInput()
        {
            Vector2 input = Vector2.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) input.y += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) input.y -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input.x += 1;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input.x -= 1;
            Vector2 screen = Input.mousePosition;
            bool down = Input.GetMouseButtonDown(0), held = Input.GetMouseButton(0), up = Input.GetMouseButtonUp(0);
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                screen = t.position;
                down = t.phase == TouchPhase.Began;
                held = t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary || down;
                up = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
            }
            if (down && !hud.BlocksPointer(screen) && !releasePanels.BlocksPointer(screen)) { pointerHeld = true; stickOrigin = screen; stickPosition = screen; }
            if (pointerHeld && held)
            {
                var delta = screen - stickOrigin;
                float radius = Mathf.Max(54, Screen.height * .085f);
                stickPosition = stickOrigin + Vector2.ClampMagnitude(delta, radius);
                if (delta.magnitude > 9) input += Vector2.ClampMagnitude(delta / radius, 1);
            }
            if (up || !held) pointerHeld = false;
            input = Vector2.ClampMagnitude(input, 1);
            Vector3 right = Flat(GameCamera.transform.right).normalized;
            Vector3 forward = Flat(GameCamera.transform.up).normalized;
            MoveWorld(right * input.x + forward * input.y);
        }

        public void MoveWorld(Vector3 direction)
        {
            if (Phase != GamePhase.Playing || deliveryCelebration > 0) return;
            direction.y = 0;
            direction = Vector3.ClampMagnitude(direction, 1);
            direction = BlendPanicInput(direction);
            if (UsesPhysicalSteering) { MoveMonsterWorld(direction); return; }
            float acceleration = (direction.sqrMagnitude < .01f ? 40 : HandlingAcceleration) * incidents.GripScale(player.position);
            velocity = Vector3.MoveTowards(velocity, direction * HandlingSpeed, Time.deltaTime * acceleration);
            ReleaseSeparatedContacts();
            Vector3 step = ConstrainTrafficStep(velocity * Time.deltaTime);
            if (UsesWideVehicleBody) step = ConstrainVanMovement(step);
            controller.Move(step);
            Vector3 p = player.position;
            float bound = WorldBuilder.PlayableHalfExtent - .9f;
            p.x = Mathf.Clamp(p.x, -bound, bound);
            p.z = Mathf.Clamp(p.z, -bound, bound);
            p.y = 0;
            player.position = p;
            if (velocity.sqrMagnitude > .12f)
            {
                Quaternion nextRotation = Quaternion.Slerp(player.rotation, Quaternion.LookRotation(velocity), Time.deltaTime * VehicleTurnRate);
                if (!UsesWideVehicleBody || VanCanTurn(nextRotation)) player.rotation = nextRotation;
            }
        }

        void MoveMonsterWorld(Vector3 direction)
        {
            float dt = Time.deltaTime;
            Vector3 heading = player.forward;
            float speed = Vector3.Dot(velocity, heading);
            bool hasInput = direction.sqrMagnitude > .01f;
            Vector3 requested = hasInput ? direction.normalized : Vector3.zero;
            bool reversing = hasInput && Vector3.Dot(heading, requested) < -.7f;
            float requestedSign = reversing ? -1 : 1;
            bool changingDirection = hasInput && speed * requestedSign < -.01f;

            // A backward command backs straight out. Changing travel direction
            // first stops the truck, so neither release nor forward input can
            // turn its body toward a lingering backward velocity.
            if (hasInput && !reversing && !changingDirection)
            {
                Quaternion next = Quaternion.RotateTowards(player.rotation, Quaternion.LookRotation(requested), VehicleSteeringDegreesPerSecond * dt);
                if (VanCanTurn(next)) player.rotation = next;
            }
            heading = player.forward;
            float alignment = reversing ? 1 : Mathf.Max(0, Vector3.Dot(heading, requested));
            float targetSpeed = hasInput && !changingDirection ? requestedSign * HandlingSpeed * direction.magnitude * alignment : 0;
            float acceleration = hasInput && !changingDirection && Mathf.Abs(targetSpeed) > .01f ? HandlingAcceleration : 40;
            speed = Mathf.MoveTowards(speed, targetSpeed, acceleration * incidents.GripScale(player.position) * dt);
            velocity = heading * speed;

            ReleaseSeparatedContacts();
            // Both the sweep and movement use the accepted body heading. A
            // rejected turn therefore cannot leave sideways velocity behind.
            controller.Move(ConstrainVanMovement(ConstrainTrafficStep(velocity * dt)));
            Vector3 p = player.position;
            float bound = WorldBuilder.PlayableHalfExtent - .9f;
            p.x = Mathf.Clamp(p.x, -bound, bound);
            p.z = Mathf.Clamp(p.z, -bound, bound);
            p.y = 0;
            player.position = p;
        }

        void ReleaseSeparatedContacts()
        {
            if (damageContacts.Count == 0) return;
            Vector3 center = player.position + controller.center;
            float separation = VehicleContactSeparation + controller.skinWidth + .12f;
            contactsToRemove.Clear();
            foreach (Collider obstacle in damageContacts)
                if (!obstacle || Vector3.Distance(center, obstacle.ClosestPoint(center)) > separation)
                    contactsToRemove.Add(obstacle);
            foreach (Collider obstacle in contactsToRemove) damageContacts.Remove(obstacle);
        }

        void AnimateScooter()
        {
            float speed = Phase == GamePhase.Playing ? Mathf.Clamp01(velocity.magnitude/HandlingSpeed) : 0;
            float bob = ReducedMotion ? 0 : Mathf.Sin(Time.time*20)*.025f*speed;
            model.localPosition = new Vector3(0,bob,0);
            model.localRotation = Quaternion.Euler(0,0,ReducedMotion ? 0 : Mathf.Sin(Time.time*27)*wobble*13);
            if (HasPackage)
            {
                float turn = Mathf.Clamp(Mathf.DeltaAngle(previousYaw, player.eulerAngles.y) / Mathf.Max(.001f, Time.deltaTime * 150), -1, 1);
                float gust = !ReducedMotion && ActiveParcel != ParcelKind.Plant && !IsHeavyDelivery ? incidents.Gust * .35f : 0;
                parcelVisual.Animate(Cargo, ReducedMotion ? 0 : speed, ReducedMotion ? 0 : turn + gust, ReducedMotion ? 0 : wobble);
            }
            AnimateVehicleVisual(speed, 0, ReducedMotion);
            previousYaw = player.eulerAngles.y;
            if (Phase == GamePhase.Playing && !ReducedMotion && speed > .35f && Time.time > nextExhaust)
            {
                nextExhaust = Time.time + .19f;
                EmitFeelDust(player.position-player.forward*.85f+Vector3.up*.15f);
            }
        }

        void LateUpdate()
        {
            bool inGarage = Phase == GamePhase.Garage || (Phase == GamePhase.Shop && shopReturn == GamePhase.Garage);
            bool close = Phase == GamePhase.Playing || Phase == GamePhase.Result || Phase == GamePhase.Paused ||
                Phase == GamePhase.ShiftCard || Phase == GamePhase.ShiftEnd;
            Vector3 focus = inGarage ? garageVisual.Focus + GameCamera.transform.right * 2.3f : close ? player.position + new Vector3(0,0,1.5f) : menuCameraTarget;
            Vector3 target = focus - GameCamera.transform.forward * 42 + FeelCameraOffset;
            float desiredZoom = inGarage ? 7.5f : close ? (Screen.width/(float)Screen.height < 1.3f ? 11.8f : 10.3f) : 26f;
            bool cut = ReducedMotion || cameraInGarage != inGarage;
            GameCamera.transform.position = cut ? target : Vector3.Lerp(GameCamera.transform.position,target,1-Mathf.Exp(-Time.deltaTime*3.5f));
            GameCamera.orthographicSize = cut ? desiredZoom : Mathf.Lerp(GameCamera.orthographicSize,desiredZoom,1-Mathf.Exp(-Time.deltaTime*3));
            cameraInGarage = inGarage;
        }

        public void StartRun()
        {
            if (ShiftModeEnabled) StartShift(); else StartTour();
        }

        // The original three-delivery tour. Smoke and persistence tests still drive it.
        void StartTour()
        {
            deliveryCelebration = 0;
            ResetFeel();
            ClearDroppedParcel();
            if (streetEvents) BeginStreetTour();
            incidents.Configure(streetEvents.Seed, smokeTest || persistenceTest ? null : new IncidentSave { enabled = true });
            BeginRoute();
            BeginPickupPlan();
            commerce.activeTour++;
            ConfigureDistrictDogs(streetEvents.Seed);
            Completed = 0;
            BeginOrders();
            runScore = 0;
            ActiveParcel = ParcelKind.Cake;
            LastParcel = ParcelKind.Cake;
            LastIntegrity = 100;
            foreach (var customer in customers) customer.ResetPose();
            RefreshVipCustomer();
            HasPackage = false;
            package.gameObject.SetActive(false);
            Cargo = 100; PrepareDeliveryClock();
            controller.enabled = false;
            player.position = world.Depot + new Vector3(-.1f,0,-.2f);
            controller.enabled = true;
            player.rotation = Quaternion.Euler(0,0,0);
            previousYaw = 0;
            hitCooldown = 0;
            wobble = 0;
            damageContacts.Clear();
            velocity = Vector3.zero;
            pickupDelay = .7f;
            Phase = GamePhase.Playing;
            MarkRunStarted();
            Notify(PickupName + GameText.Pick(": ışıklı halkadan paketini al.", ": collect your parcel from the glowing circle."));
            PlayNote(660);
        }

        void Pickup()
        {
            if (IsParcelDropped) { RecoverDroppedParcel(); return; }
            HasPackage = true;
            ActiveParcel = (ParcelKind)Mathf.Min(Completed, 2);
            parcelVisual.Configure(ActiveParcel);
            parcelVisual.SetOrder(CurrentOrder);
            package.gameObject.SetActive(true);
            Cargo = 100;
            PrepareDeliveryClock();
            hitCooldown = 0;
            damageContacts.Clear();
            pickupDelay = LoadingDelay;
            Notify(ParcelName + " → " + TargetName + ". " + ParcelHint);
            PlayNote(880);
        }

        public void HitObstacle(Collider obstacle, Vector3 normal)
        {
            if (Phase != GamePhase.Playing || !HasPackage || !obstacle) return;
            if (DragonCanFlyOver(obstacle)) return;
            // A single continuous contact is one crash. Steering into a wall while
            // already resting against it must not drain the parcel every second.
            if (Vector3.Dot(velocity, -normal) < 1.2f || !damageContacts.Add(obstacle)) return;
            if (hitCooldown > 0) return;
            bool hazard = obstacle.name.StartsWith("Hazard_", StringComparison.Ordinal);
            hitCooldown = 1.15f;
            Cargo = Mathf.Max(0,Cargo-(hazard ? 18 : 10) * Fragility * VehicleImpactScale(Vector3.Dot(velocity, -normal)) * DepotImpactScale * ShiftDamageScale);
            wobble = 1;
            ImpactFeel();
            parcelVisual.OnImpact(Cargo);
            bool dropped = TryDropParcel(normal, Vector3.Dot(velocity, -normal));
            if (!dropped)
            {
                if (obstacle.name == "Hazard_Roadworks") Notify(GameText.Pick("Yol çalışması var. Diğer sokaktan dolaş!", "Roadworks ahead. Try the next street!"));
                else if (obstacle.name == "Hazard_GarbageTruck") Notify(GameText.Pick("Kamyon çöp topluyor. Biraz mesafe bırak!", "The truck is collecting. Give it a little room!"));
                else Notify(hazard ? GameText.Pick("Hop! Paket bu tümseği hiç sevmedi.", "Bump! Your parcel did not enjoy that.") :
                    GameText.Pick("Ufak bir çarpışma! Paketi biraz sarstın.", "A little knock! Easy on the parcel."));
            }
            if (ActiveParcel == ParcelKind.TeaSet) StartCoroutine(TeaRattle()); else PlayNote(ActiveParcel == ParcelKind.Plant ? 210 : 180);
        }

        void CompleteDelivery()
        {
            if (Phase != GamePhase.Playing || !HasPackage) return;
            LastSpeedBonus = Mathf.RoundToInt(25*Mathf.Clamp01(TimeLeft/timeBudget));
            LastCareBonus = Mathf.RoundToInt(35*Cargo/100);
            LastBaseFee = DeliveryBaseFee + DistrictSurcharge + (IsHeavyDelivery ? 80 : 0) + OrderFee(CurrentOrder) + OpenSurprise();
            LastMultiplier = DeliveryMultiplier;
            comboCount = Cargo >= 85 && TimeLeft > 0 ? Mathf.Min(3, comboCount + 1) : 0;
            LastComboBonus = Mathf.Max(0, comboCount - 1) * 5;
            LastPayout = (LastBaseFee + LastSpeedBonus + LastCareBonus) * LastMultiplier + LastComboBonus;
            Coins += LastPayout;
            runScore += LastPayout;
            LastParcel = ActiveParcel;
            LastIntegrity = Cargo;
            customers[ActiveStopIndex].React(Cargo);
            DeliveryFeel(customers[ActiveStopIndex].transform, world.Stops[ActiveStopIndex], Cargo);
            TriggerDragonDelivery(world.Stops[ActiveStopIndex]);
            RecordBusinessDelivery();
            Completed++;
            BestScore = Mathf.Max(BestScore,runScore);
            deliveryCelebration = ReducedMotion ? .01f : .75f;
            HasPackage = false;
            MarkRunResult();
            CommerceDeliveryFinished();
            package.gameObject.SetActive(false);
            pointerHeld = false;
            velocity = Vector3.zero;
            Save();
            StartCoroutine(DeliveryJingle());
        }

        IEnumerator DeliveryJingle()
        {
            PlayNote(660); yield return new WaitForSecondsRealtime(.1f);
            PlayNote(880); yield return new WaitForSecondsRealtime(.12f);
            PlayNote(1100);
        }

        IEnumerator TeaRattle()
        {
            PlayNote(1320); yield return new WaitForSecondsRealtime(.045f);
            PlayNote(1760); yield return new WaitForSecondsRealtime(.04f);
            PlayNote(1480);
        }

        public void ContinueDelivery()
        {
            if (Completed >= 3) { RestartRun(); return; }
            if (Phase != GamePhase.Result) return;
            Phase = GamePhase.Playing;
            MarkRunPlaying();
            pickupDelay = .6f;
            BeginOrderLeg();
            RefreshVipCustomer();
            Cargo = 100; PrepareDeliveryClock();
            Notify(GameText.Pick("Sıradaki paket: ", "Next pickup: ") + PickupName);
        }
        public void RestartRun() { if (!TryInterstitial(StartRun)) StartRun(); }
        public void ShowMenu() { if (!TryInterstitial(ReturnToMenu)) ReturnToMenu(); }
        void ReturnToMenu() { CloseAdPreferences(); Save(); ClearOverlays(); Phase = GamePhase.Menu; pointerHeld = false; velocity = Vector3.zero; }
        public void OpenShop()
        {
            if (Phase == GamePhase.Shop) return;
            shopReturn = Phase;
            OpenOverlay(GamePhase.Shop);
        }
        public void CloseShop()
        {
            if (Phase != GamePhase.Shop) return;
            CloseOverlay();
            NoticeTime = 0;
            Save();
        }
        public void SelectColor(int index)
        {
            if (index<0 || index>=ScooterColors.Length) return;
            if (!IsColorUnlocked(index))
            {
                int price = ColorPrice(index);
                if (Coins < price) { Notify(GameText.Pick("Bu renk için biraz daha teslimat yapmalısın.", "A few more deliveries will unlock this color.")); PlayNote(240); return; }
                SpendCoins(price);
                unlockedColors |= 1 << index;
            }
            ScooterColorIndex = index;
            scooterPaint.color = ScooterColors[index];
            SetVehiclePaint(ScooterColors[index]);
            RefreshGarage();
            Notify(GameText.Pick("Yeni rengin hazır. Mahalle seni bekliyor!", "Fresh paint! The neighborhood is waiting."));
            PlayNote(880); Save();
        }
        public void ToggleMute() { IsMuted = !IsMuted; ApplyAudioSettings(); Save(); }
        public void ToggleLanguage() { GameText.Toggle(); NoticeTime = 0; }
        string StopName(int index)
        {
            if (GameText.Turkish) return world.StopNames[index];
            return world.StopNamesEn[index];
        }
        void Save()
        {
            if (smokeTest) return;
            LocalProgress.SetInt("coins",Coins); LocalProgress.SetInt("best",BestScore);
            LocalProgress.SetInt("colors",unlockedColors); LocalProgress.SetInt("paint",ScooterColorIndex);
            LocalProgress.SetInt("mute",IsMuted?1:0);
            SaveProgressExtras();
            LocalProgress.Save();
        }
        void OnApplicationPause(bool paused) { if(paused) { if(!smokeTest && !persistenceTest) PauseGame(); pointerHeld=false; velocity=Vector3.zero; Save(); } }
        void OnApplicationFocus(bool focus) { if(!focus) { if(!smokeTest && !persistenceTest) PauseGame(); pointerHeld=false; velocity=Vector3.zero; Save(); } }
        void OnApplicationQuit() => Save();
        void Notify(string text) { Notice = text; NoticeTime = 4.5f; }

        void PlayNote(int frequency)
        {
            if (IsMuted || Volume <= 0 || !audioSource) return;
            if (!notes.TryGetValue(frequency,out AudioClip clip))
            {
                int count=6615; float[] samples = new float[count];
                for(int i=0;i<count;i++) { float t=i/44100f; samples[i] = Mathf.Sin(t*frequency*Mathf.PI*2)*Mathf.Exp(-t*28)*.65f; }
                clip=AudioClip.Create("Soft chime "+frequency,count,1,44100,false); clip.SetData(samples,0); notes.Add(frequency,clip);
            }
            audioSource.PlayOneShot(clip);
        }

        void OnGUI()
        {
            if (!pointerHeld || Phase != GamePhase.Playing) return;
            if (!circle)
            {
                circle = new Texture2D(64,64,TextureFormat.RGBA32,false);
                for(int y=0;y<64;y++) for(int x=0;x<64;x++)
                    circle.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01((31-Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f)))*.8f)));
                circle.Apply();
            }
            float radius=Mathf.Max(54,Screen.height*.085f);
            var old=GUI.color;
            GUI.color=new Color(.08f,.20f,.26f,.18f);
            GUI.DrawTexture(new Rect(stickOrigin.x-radius,Screen.height-stickOrigin.y-radius,radius*2,radius*2),circle);
            GUI.color=new Color(1f,.97f,.87f,.88f);
            GUI.DrawTexture(new Rect(stickPosition.x-25,Screen.height-stickPosition.y-25,50,50),circle);
            GUI.color=old;
        }

        // Test seam bypasses input only; movement, collisions, proximity pickup and rewards stay real.
        public void PlaceForTest(Vector3 p)
        {
            if(!smokeTest) return;
            controller.enabled=false; player.position=p; controller.enabled=true; velocity=Vector3.zero;
        }
    }

    public sealed class ScooterCollision : MonoBehaviour
    {
        public CourierGame Game;
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y < .65f) Game.HitObstacle(hit.collider, hit.normal);
        }
    }
}
