using SideLoader;
using System;

namespace Mounts.Custom_SL_Effect
{
    public class SL_SpawnMount : SL_Effect, ICustomModel
    {
        public Type SLTemplateModel => typeof(SL_SpawnMount);
        public Type GameModel => typeof(SpawnMount);

        public string SpeciesName;

        public override void ApplyToComponent<T>(T component)
        {
            var comp = component as SpawnMount;
            comp.SpeciesName = SpeciesName;
        }

        public override void SerializeEffect<T>(T effect)
        {
        }
    }

    public class SpawnMount : Effect, ICustomModel
    {
        public Type SLTemplateModel => typeof(SL_SpawnMount);
        public Type GameModel => typeof(SpawnMount);

        public string SpeciesName;

        public override void ActivateLocally(Character _affectedCharacter, object[] _infos)
        {
            try
            {
                if (_affectedCharacter == null)
                {
                    Mounts.DebugLog($"No character {_affectedCharacter.Name}");
                    return;
                }
                Mounts.DebugLog($"Spawning mount {SpeciesName} for {_affectedCharacter}");
                var characterMount = _affectedCharacter.gameObject.GetComponent<CharacterMount>();

                if (characterMount == null)
                {
                    Mounts.DebugLog($"No CharacterMount found for {_affectedCharacter.Name}. Creating");
                    characterMount = _affectedCharacter.gameObject.AddComponent<CharacterMount>();
                }
                Mounts.SpawnMount(characterMount, SpeciesName);
            }
            catch (Exception e)
            {
                Mounts.Log.LogMessage($"Exception during SpawnMount.ActivateLocally: {e}");
            }
        }
    }
}
