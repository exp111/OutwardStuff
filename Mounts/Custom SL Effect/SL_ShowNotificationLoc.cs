using SideLoader;
using System;

namespace Mounts.Custom_SL_Effect
{
    public class SL_ShowNotificationLoc : SL_Effect, ICustomModel
    {
        public Type SLTemplateModel => typeof(SL_ShowNotificationLoc);
        public Type GameModel => typeof(ShowNotificationLoc);

        public string Message;

        public override void ApplyToComponent<T>(T component)
        {
            var comp = component as ShowNotificationLoc;
            comp.Message = Message;
        }

        public override void SerializeEffect<T>(T effect)
        {
        }
    }

    public class ShowNotificationLoc : Effect, ICustomModel
    {
        public Type SLTemplateModel => typeof(SL_ShowNotificationLoc);
        public Type GameModel => typeof(ShowNotificationLoc);

        public string Message;

        public override void ActivateLocally(Character _affectedCharacter, object[] _infos)
        {
            try
            {
                Mounts.DebugLog($"Showing Notification {Message} for {_affectedCharacter}");
                if (_affectedCharacter && _affectedCharacter.CharacterUI)
                {
                    _affectedCharacter.CharacterUI.ShowInfoNotificationLoc(Message);
                }
            }
            catch (Exception e)
            {
                Mounts.Log.LogMessage($"Exception during DespawnMount.ActivateLocally: {e}");
            }
        }
    }
}
