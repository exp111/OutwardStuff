using SideLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Mounts.Custom_SL_Effect
{
    public class SL_ToggleMount : SL_Effect, ICustomModel
    {
        public Type SLTemplateModel => typeof(SL_ToggleMount);
        public Type GameModel => typeof(ToggleMount);

        public string SpeciesName;

        public override void ApplyToComponent<T>(T component)
        {
            var comp = component as ToggleMount;
            comp.SpeciesName = SpeciesName;
        }

        public override void SerializeEffect<T>(T effect)
        {
            //TODO: do we need to set this?
            // SpeciesName = (effect as ToggleMount).SpeciesName;
        }
    }

    public class ToggleMount : Effect, ICustomModel
    {
        public Type SLTemplateModel => typeof(SL_ToggleMount);
        public Type GameModel => typeof(ToggleMount);

        public string SpeciesName;

        public override void ActivateLocally(Character _affectedCharacter, object[] _infos)
        {
            try
            {
                Mounts.Log.LogMessage($"Toggling mount {SpeciesName}");
                Mounts.ToggleMount(_affectedCharacter);
            }
            catch (Exception e)
            {
                Mounts.Log.LogMessage($"Exception during ToggleMount.ActivateLocally: {e}");
            }
        }
    }
}
