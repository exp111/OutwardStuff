using SideLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Mounts.Custom_SL_Effect
{
    public class SL_IsMounted : SL_EffectCondition, ICustomModel
    {
        public Type SLTemplateModel => typeof(SL_IsMounted);
        public Type GameModel => typeof(IsMounted);

        public override void ApplyToComponent<T>(T component)
        {
        }

        public override void SerializeEffect<T>(T effect)
        {
        }
    }

    public class IsMounted : EffectCondition, ICustomModel
    {
        public Type SLTemplateModel => typeof(SL_IsMounted);
        public Type GameModel => typeof(IsMounted);

        public override string ProcessMessage(string _message)
        {
            return "Not mounted";
        }

        public override bool CheckIsValid(Character _affectedCharacter)
        {
            try
            {
                //Mounts.DebugLog($"IsMounted IsValid {_affectedCharacter}");
                var characterMount = _affectedCharacter.gameObject.GetComponent<CharacterMount>();

                return characterMount != null && // needs mount comp
                    characterMount.HasActiveMount && // needs active mount
                    characterMount.ActiveMount.IsMounted; // needs to be mounted
            }
            catch (Exception e)
            {
                Mounts.Log.LogMessage($"Exception during IsMounted.CheckIsValid: {e}");
            }
            return false;
        }
    }
}
