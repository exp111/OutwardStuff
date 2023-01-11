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
                Mounts.DebugTrace($"Spawning Mount {SpeciesName} for {_affectedCharacter}");
                var characterMount = _affectedCharacter.gameObject.GetComponent<CharacterMount>();

                if (characterMount == null)
                {
                    Mounts.Log.LogMessage($"No CharacterMount found for {_affectedCharacter.Name}.");
                    return;
                }

                Mounts.Log.LogMessage("mount");
                Mounts.DebugLog("mount");
                //TODO: or Mounts.MountManager.CharacterHasMount(_affectedCharacter)?
                if (characterMount.HasActiveMount) // despawn mount
                {
                    Mounts.DebugLog($"Destroying Active Mount {characterMount.ActiveMount}");
                    Mounts.MountManager.DestroyActiveMount(_affectedCharacter);
                    characterMount.SetActiveMount(null);
                }
                else // spawn mount
                {
                    Mounts.DebugLog($"Spawning mount {SpeciesName}");
                    MountSpecies mountSpecies = Mounts.MountManager.GetSpeciesDefinitionByName(SpeciesName);

                    //TODO: mount
                    if (mountSpecies != null)
                    {
                        BasicMountController basicMountController = Mounts.MountManager.CreateMountFromSpecies(_affectedCharacter, mountSpecies, OutwardHelpers.GetPositionAroundCharacter(_affectedCharacter), _affectedCharacter.transform.rotation);
                    }
                    else
                    {
                        Mounts.Log.LogMessage($"Could not find Species with SpeciesName: {SpeciesName}, in the list of definitions.");
                    }
                }
            }
            catch (Exception e)
            {
                Mounts.Log.LogMessage($"Exception during ToggleMount: {e}");
            }
        }
    }
}
