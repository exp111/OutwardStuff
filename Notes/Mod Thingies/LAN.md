host: https://flobuk.gitlab.io/assets/docs/tanksmp/guides/photon-server-for-lan, configure: https://doc.photonengine.com/pun/current/getting-started/initial-setup#photon_server_v5

Found raid mode lan version
Starts photon server with
`start /d deploy\bin_Win64 PhotonSocketServer.exe /debug LoadBalancing`

Mod:
Reconnects to photon if our room name is "LAN"
```c#
// Token: 0x02000005 RID: 5
[HarmonyPatch(typeof (StoreManager), "OpenRoomToNetwork")]
public class StoreManager_OpenRoomToNetwork {
  // Token: 0x06000027 RID: 39 RVA: 0x00002B54 File Offset: 0x00000D54
  public static bool Prefix(string _roomName) {
    StoreManager_JoinRequestFromRoomName.IP = null;
    if (_roomName == "LAN") {
      StoreManager_OpenRoomToNetwork.LAN = true;
      NetworkLevelLoader.Instance.PauseGameplay("OpenConnection", ConnectPhotonMaster.Instance, LocalizationManager.Instance.GetLoc("Loading_OpeningConnection"));
      ConnectPhotonMaster.Instance.DisconnectPhoton();
      ConnectPhotonMaster.Instance.ConnectToPhoton();
      return false;
    }
    StoreManager_OpenRoomToNetwork.LAN = false;
    return true;
  }

  // Token: 0x04000005 RID: 5
  public static bool LAN;
}
```
Connects to local photon server if LAN is enabled
```c#
[HarmonyPatch(typeof (ConnectPhotonMaster), "ConnectToPhoton")]
public class ConnectPhotonMaster_ConnectToPhoton {
  // Token: 0x06000054 RID: 84 RVA: 0x00003A54 File Offset: 0x00001C54
  public static bool Prefix(ref ConnectPhotonMaster.AutoTypes ___m_autoType, ref string ___m_autoRoomName) {
    if (StoreManager_OpenRoomToNetwork.LAN || StoreManager_JoinRequestFromRoomName.IP != null) {
      string text;
      if (StoreManager_OpenRoomToNetwork.LAN) {
        text = "127.0.0.1";
        ___m_autoType = ConnectPhotonMaster.AutoTypes.AutoCreate;
        ___m_autoRoomName = "!" + ConnectPhotonMaster_ConnectToPhoton.GetIP();
      } else {
        text = StoreManager_JoinRequestFromRoomName.IP;
        ___m_autoType = ConnectPhotonMaster.AutoTypes.AutoJoin;
        ___m_autoRoomName = "!" + text;
      }
      PhotonNetwork.ConnectToMaster(text, 5055, "", "");
      Debug.LogFormat("ConnectToPhoton({0})", new object[] {
        text
      });
      return false;
    }
    return true;
  }
}
```
(GetIP just gets the first local ip from the network interfaces)
=> how does joining work?
=> allows joining to a IP by using room name `!<IP>`
```c#
[HarmonyPatch(typeof (StoreManager), "JoinRequestFromRoomName")]
public class StoreManager_JoinRequestFromRoomName {
  // Token: 0x06000029 RID: 41 RVA: 0x00002BC2 File Offset: 0x00000DC2
  public static bool Prefix(string _roomName) {
    StoreManager_OpenRoomToNetwork.LAN = false;
    if (_roomName[0] == '!') {
      StoreManager_JoinRequestFromRoomName.IP = _roomName.Remove(0, 1);
      ConnectPhotonMaster.Instance.DisconnectPhoton();
      ConnectPhotonMaster.Instance.ConnectToPhoton();
      return false;
    }
    StoreManager_JoinRequestFromRoomName.IP = null;
    return true;
  }

  // Token: 0x04000006 RID: 6
  public static string IP;
}
```