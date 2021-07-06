using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AnimationSnooping
{
    // Logs the current animation state periodically, may not catch all states
    [BepInPlugin(ID, NAME, VERSION)]
    public class AnimationSnooping : BaseUnityPlugin
    {
        const string ID = "com.exp111.animationsnooping";
        const string NAME = "AnimationSnooping";
        const string VERSION = "1.0";

        public void Awake()
        {
            Logger.LogInfo("Awake");
        }

        public void OnGUI()
        {
            if (SplitScreenManager.Instance.LocalPlayerCount < 1)
                return;

            if (SplitScreenManager.Instance.LocalPlayers[0].AssignedCharacter == null)
                return;

            // is this the proper way to get the main character?
            var anim = SplitScreenManager.Instance.LocalPlayers[0].AssignedCharacter.Animator;
            //FieldInfo animator = typeof(Character).GetField("m_animator", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            //var anim = (Animator)animator.GetValue(SplitScreenManager.Instance.LocalPlayers[0].AssignedCharacter);
            var state = anim.GetCurrentAnimatorStateInfo(0);
            var label = $"FullPath: {(uint)state.fullPathHash}, ShortName: {(uint)state.shortNameHash}, Name: {(uint)state.nameHash}";
            Logger.LogInfo(label);
            GUI.Label(new Rect(new Vector2(100, 100), new Vector2(150, 200)), label);
        }
    }
}
