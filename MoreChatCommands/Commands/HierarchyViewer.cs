using UnityEngine;

namespace MoreChatCommands
{
    public class HierarchyViewer : CustomDebugCmd
    {
        public override string Command => "toggleHierarchy";

        public override string HelpText => "Toggles the hierarchy viewer overlay.";

        public override string Usage => "/toggleHierarchy";

        public override bool Cheat => true;

        public override bool Run(string[] args)
        {
            var hierarchy = GameObject.Find("HierarchyViewer");
            if (hierarchy == null)
            {
                GameObject gameObject = new GameObject("HierarchyViewer");
                Object.DontDestroyOnLoad(gameObject);
                gameObject.AddComponent<SceneHierarchyViewer>();
            }
            else
            {
                Object.Destroy(hierarchy);
            }
            return true;
        }
    }
}
