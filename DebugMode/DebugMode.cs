using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace DebugMode
{
    [BepInPlugin(ID, NAME, VERSION)]
    public class DebugMode : BaseUnityPlugin
    {
        // Choose a GUID for your project. Change "myname" and "mymod".
        public const string ID = "com.exp111.DebugMode";
        // Choose a NAME for your project, generally the same as your Assembly Name.
        public const string NAME = "DebugMode";
        // Increment the VERSION when you release a new version of your mod.
        public const string VERSION = "1.1";

        public static ConfigEntry<bool> EnableDebug;
        public static ConfigEntry<bool> ShowHierarchyViewer;
        public static ConfigEntry<bool> ShowPhotonStats;
        public static ConfigEntry<bool> OverwriteAreaSwitchNames;
        public static ConfigEntry<bool> EnableMapActions;
        public static ConfigEntry<bool> DisableVideoCamera;

        public static ManualLogSource Log;
        private static Harmony harmony;

        // Runtime vars
        public static int TPMarkerID = -1;
        public static Sprite TPMarkerTexture;

        void Awake()
        {
            try
            {
                Log = Logger;
                Log.LogMessage("Awake");

                // Setup Config
                SetupConfig();
                // Load sprites
                LoadTextures();

                // Harmony
                harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ID);
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during DebugMode.Awake: {e}");
            }
        }

        private void SetupConfig()
        {
            EnableDebug = Config.Bind("General", "Enable Debug", true);
            ShowHierarchyViewer = Config.Bind("General", "Show Hierarchy Viewer", false);
            ShowPhotonStats = Config.Bind("General", "Show Photon Stats", false);
            OverwriteAreaSwitchNames = Config.Bind("General", "Overwrite Area Switch Names", true, "Make the Area Names in the F2 menu human-readable.");
            OverwriteAreaSwitchNames.SettingChanged += OverwriteAreaSwitchNames_SettingChanged;
            EnableMapActions = Config.Bind("General", "Enable Map Actions", true, "Enables the map actions that can be used when right-clicking the map like teleport (restart to hide marker).");
            DisableVideoCamera = Config.Bind("General", "Disable Video Camera", true, "Disables the video camera that pops up when you press the semicolon.");

            Config.SettingChanged += (_, e) => ApplyConfig();
        }
        private void LoadTextures()
        {
            try
            {
                DebugLog($"Location: {Info.Location}, BepInExRootPath: {Paths.BepInExRootPath}");
                var directory = Path.GetDirectoryName(Info.Location);
                if (directory == null)
                    directory = Path.Combine(Paths.BepInExRootPath, "scripts");
                TPMarkerTexture = LoadSprite(Path.Combine(directory, "tpMarker.png"));
                Log.LogMessage($"Loaded sprites from {directory}");
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during DebugMode.LoadTextures: {e}");
            }
        }

        private Texture2D LoadTexture(string path)
        {
            try
            {
                var tex = new Texture2D(4, 4);
                var file = File.ReadAllBytes(path);
                tex.LoadImage(file);
                return tex;
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception while loading texture from path {path}! Message: {e}");
            }
            return null;
        }
        private Sprite LoadSprite(string path)
        {
            var tex = LoadTexture(path);
            if (!tex)
                return null;

            return Sprite.Create(tex,
                new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        private void OverwriteAreaSwitchNames_SettingChanged(object sender, EventArgs e)
        {
            var debugPanels = GameObject.Find("DebugPanels");
            if (!debugPanels)
            {
                Log.LogMessage("No DebugPanels object found");
                return;
            }
            var manager = debugPanels.GetComponent<DeveloperToolManager>();
            if (!manager)
            {
                Log.LogMessage("No DeveloperToolManager component found");
                return;
            }
            CharacterCheats_InitAreaSwitches.Overwrite(manager.CheatPanel);
        }

        public static void ApplyConfig()
        {
            try
            {
                Global.CheatsEnabled = EnableDebug.Value;
                SetHierarchyViewer(ShowHierarchyViewer.Value);
                SetPhotonStats(ShowPhotonStats.Value);
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during DebugMode.ApplyConfig: {e}");
            }
        }

        public static void SetHierarchyViewer(bool val)
        {
            var hierarchy = GameObject.Find("HierarchyViewer");
            if (val)
            {
                if (hierarchy == null)
                {
                    GameObject gameObject = new GameObject("HierarchyViewer");
                    DontDestroyOnLoad(gameObject);
                    gameObject.AddComponent<SceneHierarchyViewer>();
                }
            }
            else if (hierarchy != null)
            {
                Destroy(hierarchy);
            }
        }

        public static void SetPhotonStats(bool val)
        {
            var photon = GameObject.Find("PhotonStats");
            if (val)
            {
                if (photon == null)
                {
                    GameObject gameObject2 = new GameObject("PhotonStats");
                    DontDestroyOnLoad(gameObject2);
                    gameObject2.AddComponent<PhotonStatsGui>().statsWindowOn = true;
                }
            }
            else if (photon != null)
            {
                Destroy(photon);
            }
        }

        public void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }

        [Conditional("DEBUG")]
        public static void DebugLog(string message)
        {
            Log.LogMessage(message);
        }

        [Conditional("TRACE")]
        public static void DebugTrace(string message)
        {
            Log.LogMessage(message);
        }
    }

    [HarmonyPatch(typeof(Global), nameof(Global.Awake))] // Can't use ProcessDebug cause it may not be called if we dont have a DEBUG.txt
    class Global_Awake_Patch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
                // Set cheats
                DebugMode.ApplyConfig();
            }
            catch (Exception e)
            {
                DebugMode.Log.LogMessage($"Exception during Global.Awake hook: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(Global), nameof(Global.ProcessDebug))]
    class Global_ProcessDebug_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            // Skip original debug function if its called
            return false;
        }
    }

    // Called in the main menu on load
    [HarmonyPatch(typeof(DT_CharacterCheats), nameof(DT_CharacterCheats.InitAreaSwitches))]
    public class CharacterCheats_InitAreaSwitches
    {
        [HarmonyPostfix]
        public static void Postfix(DT_CharacterCheats __instance)
        {
            try
            {
                DebugMode.DebugLog($"CharacterCheats.InitAreaSwitches start: {__instance}");
                // This is only run on launch, so if this isnt enabled, no need to overwrite as the scene names are used by default
                if (!DebugMode.OverwriteAreaSwitchNames.Value)
                    return;

                Overwrite(__instance);
            }
            catch (Exception e)
            {
                DebugMode.Log.LogMessage($"Exception during CharacterCheats.InitAreaSwitches: {e}");
            }
        }

        public static void Overwrite(DT_CharacterCheats __instance)
        {
            try
            {
                DebugMode.DebugLog($"Starting to overwrite {__instance}");
                DebugMode.DebugTrace($"Scenes: {__instance.m_activeScenes}");
                DebugMode.DebugTrace($"Dropdowns: {__instance.m_ddFamilies}");
                // Go through all areas
                for (var familyIndex = 0; familyIndex < __instance.m_activeScenes.Length; familyIndex++)
                {
                    var family = __instance.m_activeScenes[familyIndex];
                    if (family == null)
                        continue;

                    DebugMode.DebugTrace($"- Family: {family}");
                    // then iterate through all scenes in that area
                    for (var sceneIndex = 0; sceneIndex < family.Count; sceneIndex++)
                    {
                        var sceneName = family[sceneIndex]; // this should always be the internal SceneName that also gets called to areaswitch
                        if (sceneName == null)
                            continue;

                        // get the related area object (O(n))
                        var area = AreaManager.Instance.GetAreaFromSceneName(sceneName);
                        if (area == null)
                        {
                            DebugMode.Log.LogMessage($"Scene with name {sceneName} not found");
                            continue;
                        }
                        // if enabled use readable name (ie Blister Burrow), else use scene name (ie Chersonese_Dungeon2)
                        var text = sceneName;
                        if (DebugMode.OverwriteAreaSwitchNames.Value)
                        {
                            var name = area.GetName();
                            if (!string.IsNullOrEmpty(name))
                            {
                                if (name.Length + sceneName.Length > 30) // only show the 
                                    text = name;
                                else
                                    text = $"{name} ({sceneName})";
                            }
                        }
                        // +1 cause 0 is the empty/back options
                        __instance.m_ddFamilies[familyIndex].options[sceneIndex + 1].text = text;
                    }
                }
                DebugMode.DebugLog($"Finished overwriting {__instance}");
            }
            catch (Exception e)
            {
                DebugMode.Log.LogMessage($"Exception during CharacterCheats.InitAreaSwitches.Overwrite: {e}");
            }
        }
    }

    //FIXME: find other function that is only called once (or less often than update), as AwakeInit and StartInit aren't called for some reason
    [HarmonyPatch(typeof(MapDisplay), nameof(MapDisplay.Update))]
    public class MapDisplay_Update
    {
        [HarmonyPostfix]
        public static void Postfix(MapDisplay __instance)
        {
            try
            {
                // check for cheats //TODO: remove when cheats are disabled again?
                if (!Global.CheatsEnabled || !DebugMode.EnableMapActions.Value || DebugMode.TPMarkerID != -1)
                    return;

                // get markerselector
                DebugMode.DebugLog($"MapDisplay.AwakeInit start: {__instance}");
                var selector = __instance.m_markerSelector;
                DebugMode.DebugLog($"Selector: {selector}");
                var firstChild = selector.m_items[0].gameObject;
                // copy marker and append to parent
                var tpMarker = GameObject.Instantiate(firstChild, selector.transform);
                DebugMode.DebugLog($"New Marker: {tpMarker}");
                // change sprite
                var item = tpMarker.GetComponent<RadialSelectorItem>();
                if (!DebugMode.TPMarkerTexture)
                {
                    DebugMode.Log.LogMessage("Texture not found!");
                }
                else
                {
                    DebugMode.DebugLog($"Sprite: {DebugMode.TPMarkerTexture}");
                    item.Image.overrideSprite = DebugMode.TPMarkerTexture;
                }

                // update selector
                selector.Refresh();
                // save the id
                DebugMode.TPMarkerID = item.ID;
            }
            catch (Exception e)
            {
                DebugMode.Log.LogMessage($"Exception during MapDisplay.AwakeInit: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(MapDisplay), nameof(MapDisplay.OnMarkerSelected))]
    public class MapDisplay_OnMarkerSelected
    {
        [HarmonyPrefix]
        public static bool Prefix(MapDisplay __instance, int _id)
        {
            try
            {
                DebugMode.DebugLog($"MapDisplay.OnMarkerSelected start: {__instance}, id: {_id}");
                if (!DebugMode.EnableMapActions.Value)
                    return true; // dont skip

                if (_id == DebugMode.TPMarkerID)
                {
                    if (!Global.CheatsEnabled)
                        return true; // dont skip

                    // tp to marker
                    var newPos = Vector3.zero;
                    var pos = __instance.m_markerSelector.transform.localPosition;
                    var zoom = __instance.m_zoomLevelSmooth * MapDisplay.BASE_MARKER_ZOOM;
                    var scene = __instance.CurrentMapScene;
                    // reverse calculation from MapWorldMarker.CalculateMapPosition
                    newPos.x = ((pos.x / zoom) - scene.MarkerOffset.x) / scene.MarkerScale.x;
                    newPos.z = ((pos.y / zoom) - scene.MarkerOffset.y) / scene.MarkerScale.y;
                    newPos.y = 300; // placeholder y pos, in the sky //TODO: what if y is higher than 100? also what if there is a ceiling? raycast from the ground?
                    // get y pos by raycasting to the ground
                    RaycastHit raycastHit;
                    if (Physics.Raycast(newPos, Vector3.down, out raycastHit, 350f, Global.FullEnvironmentMask))
                    {
                        newPos = raycastHit.point;
                    }
                    var rot = __instance.LocalCharacter.transform.rotation;
                    __instance.LocalCharacter.Teleport(newPos, rot);
                    // hide selector
                    __instance.m_markerSelector.SetActiveWithAnim(false);
                    return false; // skip
                }
                return true; // dont skip
            }
            catch (Exception e)
            {
                DebugMode.Log.LogMessage($"Exception during MapDisplay.OnMarkerSelected: {e}");
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(VideoCamera), nameof(VideoCamera.Update))]
    class VideoCamera_Update
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            // return false to skip => if option is true, skip
            return !DebugMode.DisableVideoCamera.Value;
        }
    }
}