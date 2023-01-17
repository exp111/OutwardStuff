using SideLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SkilledAtMeditation
{
    public class SL_Meditation : SL_Effect, ICustomModel
    {
        public Type SLTemplateModel => typeof(SL_Meditation);
        public Type GameModel => typeof(Meditation);

        public override void ApplyToComponent<T>(T component)
        {
        }

        public override void SerializeEffect<T>(T effect)
        {
        }
    }

    public class Meditation : Effect, ICustomModel
    {
        public Type SLTemplateModel => typeof(SL_Meditation);
        public Type GameModel => typeof(Meditation);

        public override void ActivateLocally(Character _affectedCharacter, object[] _infos)
        {
            try
            {
                SkilledAtMeditation.DebugTrace($"Meditation Tick");
                if (_affectedCharacter.CurrentSpellCast != Character.SpellCastType.Sit)
                {
                    SkilledAtMeditation.DebugTrace($"Removing status effect {this}");
                    _affectedCharacter.StatusEffectMngr.CleanseStatusEffect(SkilledAtMeditation.MeditationStatusIdentifier);
                    return;
                }

                void Update(ref float cur, float delta, float max)
                {
                    cur = Mathf.Clamp(cur + delta, 0, max);
                }

                var stats = _affectedCharacter.Stats;
                if (SkilledAtMeditation.EnableBurntRegen.Value)
                {
                    SkilledAtMeditation.DebugTrace("Restoring burnt stats...");
                    Update(ref stats.m_burntStamina, -SkilledAtMeditation.BurntStaminaRegen.Value, stats.MaxStamina);
                    Update(ref stats.m_burntHealth, -SkilledAtMeditation.BurntHealthRegen.Value, stats.MaxHealth);
                    Update(ref stats.m_burntMana, -SkilledAtMeditation.BurntManaRegen.Value, stats.MaxMana);
                }
                if (SkilledAtMeditation.EnableActiveRegen.Value)
                {
                    SkilledAtMeditation.DebugTrace("Restoring active stats...");
                    Update(ref stats.m_stamina, SkilledAtMeditation.ActiveStaminaRegen.Value, stats.ActiveMaxStamina);
                    Update(ref stats.m_health, SkilledAtMeditation.ActiveHealthRegen.Value, stats.ActiveMaxHealth);
                    Update(ref stats.m_mana, SkilledAtMeditation.ActiveManaRegen.Value, stats.ActiveMaxMana);
                }
            }
            catch (Exception e)
            {
                SkilledAtMeditation.Log.LogMessage($"Exception during Meditation.ActivateLocally: {e}");
            }
        }
    }
}
