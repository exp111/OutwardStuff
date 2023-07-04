using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Reflection;
using UnityEngine;

namespace LAN
{
    [BepInPlugin(ID, NAME, VERSION)]
    public class LAN : BaseUnityPlugin
    {
        public const string ID = "com.exp111.LAN";
        public const string NAME = "LAN";
        public const string VERSION = "1.0";

        public static ManualLogSource Log;
        private static Harmony harmony;

        public static ConfigEntry<string> IP;
        public static ConfigEntry<bool> Utilities;
        public static bool IsHosting = false;
        public static bool IsJoining = false;

        public void Awake()
        {
            try
            {
                Log = Logger;
                Log.LogMessage("Awake");
                DebugLog("Using a DEBUG build.");

                SetupConfig();

                harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ID);
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during LAN.Awake: {e}");
            }
        }

        private void SetupConfig()
        {
            //TODO: create join button, that sets IsJoining, disconnects+connects to photon
            IP = Config.Bind("General", "IP", "127.0.0.1", "The local IP to join");
            Utilities = Config.Bind("General", "Utilities", false, new ConfigDescription("Host/join a game lobby",
                null,
                new ConfigurationManagerAttributes { CustomDrawer = CustomDrawer, HideDefaultButton = true }));
        }

        // Config GUI
        public static void CustomDrawer(ConfigEntryBase entry)
        {
            try
            {
                // Button to 
                if (GUILayout.Button("Host", GUILayout.ExpandWidth(true)))
                {
                    Log.LogMessage("Hosting Lobby");
                    HostLobby();
                }
                if (GUILayout.Button("Join", GUILayout.ExpandWidth(true)))
                {
                    Log.LogMessage($"Joining Lobby {IP.Value}");
                    JoinLobby();
                }
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during LAN.SeedDrwaer: {e}");
            }
        }

        public static void HostLobby()
        {
            IsHosting = true;
            IsJoining = false;

            // reconnect from photon
            ConnectPhotonMaster.Instance.DisconnectPhoton();
            ConnectPhotonMaster.Instance.ConnectToPhoton();
        }

        public static void JoinLobby()
        {
            IsHosting = false;
            IsJoining = true;

            // reconnect from photon
            ConnectPhotonMaster.Instance.DisconnectPhoton();
            ConnectPhotonMaster.Instance.ConnectToPhoton();
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

    [HarmonyPatch(typeof(ConnectPhotonMaster), nameof(ConnectPhotonMaster.ConnectToPhoton))]
    public class ConnectPhotonMaster_ConnectToPhoton
    {
        public static bool Prefix(ref ConnectPhotonMaster.AutoTypes ___m_autoType, ref string ___m_autoRoomName)
        {
            if (LAN.IsHosting || LAN.IsJoining)
            {
                string text = "Error";
                if (LAN.IsHosting)
                {
                    text = "127.0.0.1";
                    ___m_autoType = ConnectPhotonMaster.AutoTypes.AutoCreate;
                    ___m_autoRoomName = "!" + GetIP();
                    LAN.IsHosting = false;
                }
                else if (LAN.IsJoining)
                {
                    text = LAN.IP.Value;
                    ___m_autoType = ConnectPhotonMaster.AutoTypes.AutoJoin;
                    ___m_autoRoomName = "!" + text;
                    LAN.IsJoining = false;
                }
                PhotonNetwork.ConnectToMaster(text, 5055, "", "");
                LAN.DebugLog($"ConnectToPhoton({text})");
                return false; // skip
            }
            return true; // dont skip
        }

        public static string GetIP()
        {
            NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            for (int i = 0; i < allNetworkInterfaces.Length; i++)
            {
                foreach (UnicastIPAddressInformation unicastIPAddressInformation in allNetworkInterfaces[i].GetIPProperties().UnicastAddresses)
                {
                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(unicastIPAddressInformation.Address) && IsLocal(unicastIPAddressInformation.Address))
                    {
                        return unicastIPAddressInformation.Address.MapToIPv4().ToString();
                    }
                }
            }
            return null;
        }

        public static bool IsLocal(IPAddress hostIP)
        {
            IPAddress[] hostAddresses = Dns.GetHostAddresses(Dns.GetHostName());
            if (IPAddress.IsLoopback(hostIP))
            {
                return true;
            }
            foreach (IPAddress obj in hostAddresses)
            {
                if (hostIP.Equals(obj))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /* TODO: is this needed?
    [HarmonyPatch(typeof(ConnectPhotonMaster), "OnJoinedLobby")]
    public class ConnectPhotonMaster_OnJoinedLobby
    {
        public static bool Prefix(ConnectPhotonMaster __instance, ref int ___m_connectToLobbyWanted, bool ___m_requestRooms, ref ConnectPhotonMaster.AutoTypes ___m_autoType, ref string ___m_autoRoomName, ref bool ___m_wasOffline)
        {
            LAN.DebugLog("Connected to Photon Lobby");
            ___m_connectToLobbyWanted = 0;
            if (PhotonNetwork.inRoom)
            {
                PhotonNetwork.LeaveRoom(true);
            }
            if (!___m_requestRooms)
            {
                if (__instance.AutoConnect || PhotonNetwork.offlineMode)
                {
                    __instance.CreateOrJoin(__instance.DefaultRoomName);
                }
                if (___m_autoType == ConnectPhotonMaster.AutoTypes.AutoCreate)
                {
                    __instance.CreateOrJoin(___m_autoRoomName);
                }
                else if (___m_autoType == ConnectPhotonMaster.AutoTypes.StoreCreate)
                {
                    StoreManager.Instance.PrepareLobby();
                }
                else if (___m_autoType == ConnectPhotonMaster.AutoTypes.AutoJoin)
                {
                    __instance.ConnectToRoomByName(___m_autoRoomName);
                }
                ___m_wasOffline = PhotonNetwork.offlineMode;
                ___m_autoType = ConnectPhotonMaster.AutoTypes.None;
                ___m_autoRoomName = "";
                return false; // skip
            }
            __instance.StartCoroutine(__instance.SendRoomInfosToAsker());
            //__instance.StartCoroutine((IEnumerator)At.Invoke<ConnectPhotonMaster>(__instance, "SendRoomInfosToAsker", Array.Empty<object>()));
            return false; // skip
        }
    }*/
}
