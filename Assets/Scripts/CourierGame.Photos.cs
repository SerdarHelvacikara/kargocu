using UnityEngine;

namespace KurmaliKargo
{
    /// <summary>
    /// A tiny photo studio: renders the real courier model in every vehicle
    /// style into small textures, so shop and garage cards show the actual
    /// ride instead of a drawing. Re-shot whenever the paint changes.
    /// </summary>
    public sealed partial class CourierGame
    {
        const int PhotoLayer = 31;
        static readonly float[] PhotoZoom = { 1.15f, 1.15f, 1.1f, 1.3f, 1.55f, 1.05f };
        readonly RenderTexture[] vehiclePhotos = new RenderTexture[6];
        readonly System.Collections.Generic.List<GameObject> photoParts = new System.Collections.Generic.List<GameObject>();
        readonly System.Collections.Generic.List<int> photoLayers = new System.Collections.Generic.List<int>();
        Camera photoCamera;
        int photoPaint = -1;

        /// <summary>Null until <see cref="RefreshVehiclePhotos"/> has run once.</summary>
        public Texture VehiclePhoto(int index) => index >= 0 && index < vehiclePhotos.Length ? vehiclePhotos[index] : null;

        // Called from Update-time code only (never from OnGUI).
        public void RefreshVehiclePhotos()
        {
            if (photoPaint == ScooterColorIndex && vehiclePhotos[0] || !player || !model) return;
            if (!photoCamera)
            {
                var go = new GameObject("Vehicle photo studio camera");
                photoCamera = go.AddComponent<Camera>();
                photoCamera.enabled = false;
                photoCamera.orthographic = true;
                photoCamera.clearFlags = CameraClearFlags.SolidColor;
                photoCamera.backgroundColor = new Color(0, 0, 0, 0);
                photoCamera.cullingMask = 1 << PhotoLayer;
                photoCamera.nearClipPlane = .1f;
                photoCamera.farClipPlane = 40;
                photoCamera.allowHDR = false;
            }
            for (int i = 0; i < vehiclePhotos.Length; i++)
                if (!vehiclePhotos[i])
                    vehiclePhotos[i] = new RenderTexture(320, 240, 16, RenderTextureFormat.ARGB32) { name = "Vehicle photo " + i, antiAliasing = 4 };

            int keepVehicle = VehicleIndex;
            bool parcelShown = package.gameObject.activeSelf;
            package.gameObject.SetActive(false);
            Vector3 keepModelPosition = model.localPosition;
            Quaternion keepModelRotation = model.localRotation;
            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.identity;
            SetPhotoLayer(true);
            // A three-quarter front view, the same for every ride.
            Vector3 view = player.rotation * new Vector3(-1.1f, .85f, 1.55f).normalized;
            for (int i = 0; i < vehiclePhotos.Length; i++)
            {
                VehicleIndex = i;
                ApplyVehicleStyle();
                AnimateVehicleVisual(0, 0, true);
                Vector3 focus = player.position + Vector3.up * (i == 4 ? 1.85f : i == 3 ? 1.0f : i == 2 ? .75f : i == 5 ? .55f : 1.1f);
                photoCamera.transform.position = focus + view * 20;
                photoCamera.transform.LookAt(focus);
                photoCamera.orthographicSize = PhotoZoom[i];
                photoCamera.aspect = 320f / 240f;
                photoCamera.targetTexture = vehiclePhotos[i];
                photoCamera.Render();
            }
            photoCamera.targetTexture = null;
            SetPhotoLayer(false);
            VehicleIndex = keepVehicle;
            ApplyVehicleStyle();
            model.localPosition = keepModelPosition;
            model.localRotation = keepModelRotation;
            package.gameObject.SetActive(parcelShown);
            photoPaint = ScooterColorIndex;
        }

        void SetPhotoLayer(bool studio)
        {
            if (studio)
            {
                photoParts.Clear(); photoLayers.Clear();
                foreach (Transform t in player.GetComponentsInChildren<Transform>(true))
                {
                    photoParts.Add(t.gameObject);
                    photoLayers.Add(t.gameObject.layer);
                    t.gameObject.layer = PhotoLayer;
                }
                return;
            }
            for (int i = 0; i < photoParts.Count; i++)
                if (photoParts[i]) photoParts[i].layer = photoLayers[i];
        }
    }
}
