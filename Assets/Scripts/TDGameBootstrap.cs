using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace NightWatch
{
    public class TDGameBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSetup()
        {
            if (FindFirstObjectByType<GameManager>() != null) return;
            CreateGameRoot();
        }

        public static void CreateGameRoot()
        {
            if (FindFirstObjectByType<GameManager>() != null) return;

            var root = new GameObject("NightWatchGame");
            root.AddComponent<GameManager>();
            root.AddComponent<UIManager>();
            root.AddComponent<TowerInput>();

            SetupCamera();
            SetupLighting();
        }

        static void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
                camGo.AddComponent<AudioListener>();
            }

            cam.transform.position = new Vector3(0f, 34f, -14f);
            cam.transform.rotation = Quaternion.Euler(62f, 0f, 0f);
            cam.fieldOfView = 55f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
        }

        static void SetupLighting()
        {
            if (FindFirstObjectByType<Light>() != null) return;
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightGo.transform.rotation = Quaternion.Euler(55f, -25f, 0f);
        }
    }

    public static class UiEventSetup
    {
        public static void Ensure()
        {
            var es = Object.FindFirstObjectByType<EventSystem>();
            var go = es != null ? es.gameObject : new GameObject("EventSystem");

            if (es == null)
                go.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
            foreach (var module in go.GetComponents<BaseInputModule>())
            {
                if (!(module is InputSystemUIInputModule))
                    Object.Destroy(module);
            }

            var uiModule = go.GetComponent<InputSystemUIInputModule>();
            if (uiModule == null)
                uiModule = go.AddComponent<InputSystemUIInputModule>();

            uiModule.enabled = true;
            if (uiModule.actionsAsset == null)
                uiModule.AssignDefaultActions();
#else
            if (go.GetComponent<StandaloneInputModule>() == null)
                go.AddComponent<StandaloneInputModule>();
#endif
        }

        public static bool IsPointerOverUi()
        {
            if (EventSystem.current == null) return false;

            var data = new PointerEventData(EventSystem.current) { position = GetPointerPosition() };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, results);

            foreach (var hit in results)
            {
                if (hit.gameObject.GetComponentInParent<Canvas>() != null)
                    return true;
            }

            return false;
        }

        /// <summary>Курсор над ігровим полем (нижня частина екрана), не над верхнім UI.</summary>
        public static bool IsPointerOverGameWorld()
        {
            var pos = GetPointerPosition();
            return pos.y < Screen.height * 0.72f;
        }

        public static Vector2 GetPointerPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
            if (Touchscreen.current != null)
                return Touchscreen.current.primaryTouch.position.ReadValue();
            return Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }
    }
}
