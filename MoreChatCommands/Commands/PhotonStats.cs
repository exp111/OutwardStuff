using UnityEngine;

namespace MoreChatCommands
{
    public class PhotonStats : CustomDebugCmd
    {
        public override string Command => "togglePhoton";

        public override string HelpText => "Toggles the photon stats overlay.";

        public override string Usage => "/togglePhoton";

        public override bool Cheat => true;

        public override bool Run(string[] args)
        {
            var photon = GameObject.Find("PhotonStats");
            if (photon == null)
            {
                GameObject gameObject = new GameObject("PhotonStats");
                Object.DontDestroyOnLoad(gameObject);
                gameObject.AddComponent<PhotonStatsGui>().statsWindowOn = true;
            }
            else
            {
                Object.Destroy(photon);
            }
            return true;
        }
    }
}
