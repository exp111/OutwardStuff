using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Buttplug.Client;
using Buttplug.Client.Connectors.WebsocketConnector;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Inward
{
    [BepInPlugin(ID, NAME, VERSION)]
    public class Inward : BaseUnityPlugin
    {
        public const string ID = "com.exp111.Inward";
        public const string NAME = "Inward";
        public const string VERSION = "1.0";

        public static ManualLogSource Log;
        private static Harmony Harmony;
        private static ButtplugClient ButtplugClient;

        public static ConfigEntry<string> ServerURL;

        public static ConfigEntry<bool> Rewarding;
        public static ConfigEntry<bool> Punishing;
        public static ConfigEntry<float> RewardFactor;
        public static ConfigEntry<float> PunishmentFactor;

        public void Awake()
        {
            try
            {
                Log = Logger;
                Log.LogMessage("Awake");
                DebugLog("Using a DEBUG build.");
                SetupConfig();

                // Init Bp
                ButtplugClient = new ButtplugClient("Inward");
                Connect();
                // Init Harmony
                Harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ID);
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during Inward.Awake: {e}");
            }
        }

        private void SetupConfig()
        {
            ServerURL = Config.Bind("Connection", "URL", "ws://localhost:12345", "The server url which to join");
            Config.Bind("Connection", "Connect", false, new ConfigDescription("Scan/reconnect",
                null,
                new ConfigurationManagerAttributes { CustomDrawer = ConnectDrawer, HideDefaultButton = true }));

            Rewarding = Config.Bind("Gameplay", "Rewarding", true, "If rewarding actions (hitting things) should trigger vibrations");
            Punishing = Config.Bind("Gameplay", "Punishing", false, "If punishing actions (getting hit) should trigger vibrations");
            RewardFactor = Config.Bind("Gameplay", "Reward Factor", 0.5f, "The reward multiplier");
            PunishmentFactor = Config.Bind("Gameplay", "Punishment Factor", 2f, "The punishment multiplier");
        }

        private void Connect()
        {
            try
            {
                DebugLog($"Connecting to {ServerURL.Value}");
                var connector = new ButtplugWebsocketConnector(new Uri(ServerURL.Value));
                ButtplugClient.ConnectAsync(connector);
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during Inward.Connect: {e}");
            }
        }

        private void Disconnect()
        {
            try
            {
                DebugLog($"Disconnecting");
                ButtplugClient.DisconnectAsync();
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during Inward.Disconnect: {e}");
            }
        }

        public void ConnectDrawer(ConfigEntryBase entry)
        {
            try
            {
                //TODO: connection status + devices
                GUILayout.TextArea($"Status: {(ButtplugClient.Connected ? "Connected" : "Disconnected")}", GUILayout.ExpandWidth(true));
                GUILayout.TextArea($"#Devices: {ButtplugClient.Devices.Length}", GUILayout.ExpandWidth(true));
                // Buttons //TODO: disable/hide if connected/disconnected
                if (GUILayout.Button("Connect", GUILayout.ExpandWidth(true)))
                {
                    DebugLog("Connecting");
                    Connect();
                }
                if (GUILayout.Button("Scan", GUILayout.ExpandWidth(true)))
                {
                    DebugLog("Scanning");
                    ButtplugClient.StartScanningAsync();
                }
                if (GUILayout.Button("Disconnect", GUILayout.ExpandWidth(true)))
                {
                    DebugLog($"Disconnecting");
                    Disconnect();
                }
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during Inward.CustomDrawer: {e}");
            }
        }

        public class Vibration
        {
            public float Intensity;
            public Vibration(float intensity)
            { 
                Intensity = intensity;
            }
        }
        public static List<Vibration> Vibrations = new List<Vibration>();
        public static void Vibrate(float strength)
        {
            var str = Math.Max(0, Math.Min(1f, strength));
            Vibrations.Add(new Vibration(str));
            SendVibrate(str);
        }

        public void Update()
        {
            try
            {
                var deltaTime = Time.deltaTime;
                if (Vibrations.Count == 0)
                {
                    return;
                }

                // Fade the vibration out
                foreach (var t in Vibrations)
                {
                    t.Intensity = Mathf.Lerp(t.Intensity, 0, 0.5f * deltaTime);
                }

                // Remove all done vibrations
                Vibrations.RemoveAll(p => p.Intensity <= 0f);

                // Vibration this tick is the sum of all current vibrations
                var sum = Vibrations.Sum(item => item.Intensity);

                SendVibrate(sum);
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during Inward.Update: {e}");
            }
        }

        public static void SendVibrate(float strength)
        {
            //TODO: specify which device
            foreach (var device in ButtplugClient.Devices)
            {
                device.VibrateAsync(strength);
            }
        }

        public void OnDestroy()
        {
            // Delete your stuff
            Harmony?.UnpatchSelf();
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

    [HarmonyPatch(typeof(Character), nameof(Character.OnReceiveHit))]
    class Character_OnReceiveHit_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Character __instance, Weapon _weapon, float _damage, Character _dealerChar)
        {
            try
            {
                //TODO: differentiate between local players
                var received = __instance.IsLocalPlayer;
                var dealt = _dealerChar.IsLocalPlayer;
                if (Inward.Rewarding.Value && dealt)
                {
                    var strength = Inward.RewardFactor.Value * (_damage / __instance.ActiveMaxHealth);
                    Inward.Vibrate(strength);
                }
                if (Inward.Punishing.Value && received)
                {
                    var strength = Inward.PunishmentFactor.Value * (_damage / __instance.ActiveMaxHealth);
                    Inward.Vibrate(strength);
                }
            }
            catch (Exception e)
            {
                Inward.Log.LogMessage($"Exception during Character.OnReceiveHit hook: {e}");
            }
        }
    }
}
