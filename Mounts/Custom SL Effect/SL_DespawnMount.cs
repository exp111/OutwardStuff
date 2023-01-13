using SideLoader;
using System;

namespace Mounts.Custom_SL_Effect
{
    public class SL_DespawnMount : SL_Effect, ICustomModel
    {
        public Type SLTemplateModel => typeof(SL_DespawnMount);
        public Type GameModel => typeof(DespawnMount);

        public override void ApplyToComponent<T>(T component)
        {
        }

        public override void SerializeEffect<T>(T effect)
        {
        }
    }

    public class DespawnMount : Effect, ICustomModel
    {
        public Type SLTemplateModel => typeof(SL_DespawnMount);
        public Type GameModel => typeof(DespawnMount);

        public override void ActivateLocally(Character _affectedCharacter, object[] _infos)
        {
            try
            {
                Mounts.DebugLog($"Despawning mount for {_affectedCharacter}");
                var characterMount = _affectedCharacter.gameObject.GetComponent<CharacterMount>();

                if (characterMount == null)
                {
                    Mounts.DebugLog($"No CharacterMount found for {_affectedCharacter.Name}.");
                    return;
                }
                Mounts.DespawnMount(characterMount);
            }
            catch (Exception e)
            {
                Mounts.Log.LogMessage($"Exception during DespawnMount.ActivateLocally: {e}");
            }
        }
    }
}
