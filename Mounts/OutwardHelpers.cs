using SideLoader;
using System.Collections.Generic;
using UnityEngine;

namespace Mounts
{
    public static class OutwardHelpers
    {
        public static string EMISSION_VALUE = "_EmissionColor";

        public static void UpdateWeaponDamage(Weapon WeaponInstance, DamageList newDamageList)
        {
            WeaponInstance.Damage.Clear();
            //just fucken nuke everything 
            WeaponInstance.Stats.BaseDamage = newDamageList;
            WeaponInstance.m_baseDamage = WeaponInstance.Stats.BaseDamage.Clone();
            WeaponInstance.m_activeBaseDamage = WeaponInstance.Stats.BaseDamage.Clone();
            WeaponInstance.baseDamage = WeaponInstance.Stats.BaseDamage.Clone();
            WeaponInstance.Stats.Attacks = SL_WeaponStats.GetScaledAttackData(WeaponInstance);
            //ta-da updated weapon damage
        }

        public static Renderer TryGetWeaponRenderer(Weapon weaponGameObject)
        {
            return weaponGameObject.LoadedVisual.GetComponentInChildren<BoxCollider>().GetComponent<Renderer>();
        }

        //TODO: instead just give the pack into here?
        public static T GetFromAssetBundle<T>(string SLPackName, string AssetBundle, string key) where T : UnityEngine.Object
        {
            if (!SL.PacksLoaded)
            {
                return default(T);
            }

            return SL.GetSLPack(SLPackName).AssetBundles[AssetBundle].LoadAsset<T>(key);
        }

        public static Vector3 GetPositionAroundCharacter(Character _affectedCharacter, Vector3 PositionOffset = default(Vector3))
        {
            return _affectedCharacter.transform.position + PositionOffset;
        }


        public static List<T> GetTypeFromColliders<T>(Collider[] colliders) where T : Component
        {
            List<T> list = new List<T>();
            foreach (var col in colliders)
            {
                T type = col.GetComponentInChildren<T>();
                if (type != null)
                {
                    list.Add(type);
                }
            }
            return list;
        }

        public static void AddDamageType(DamageList Damage, DamageType.Types damageType, float value)
        {

            if (!HasDamageType(Damage, damageType))
            {
                Damage.Add(damageType);
                Damage[damageType].Damage = value;
            }
        }
        public static void RemoveDamageType(DamageList Damage, DamageType.Types damageType)
        {
            if (HasDamageType(Damage, damageType))
            {
                Damage.Remove(damageType);
            }
        }
        public static DamageType GetDamageTypeDamage(DamageList Damage, DamageType.Types damageType)
        {
            return Damage[damageType];
        }

        public static void AddDamageToDamageType(DamageList Damage, DamageType.Types damageType, float value, bool addIfNotExist = true)
        {
            if (HasDamageType(Damage, damageType))
            {
                GetDamageTypeDamage(Damage, damageType).Damage += value;
            }
            else
            {
                if (addIfNotExist) AddDamageType(Damage, damageType, value);
            }
        }
        public static void RemoveDamageFromDamageType(DamageList Damage, DamageType.Types damageType, float value)
        {
            if (HasDamageType(Damage, damageType))
            {
                GetDamageTypeDamage(Damage, damageType).Damage -= value;

                if (Damage[damageType].Damage <= 0)
                {
                    RemoveDamageType(Damage, damageType);
                }
            }
        }

        public static bool HasDamageType(DamageList Damage, DamageType.Types damageType)
        {
            return Damage[damageType] != null;
        }

        public static Tag GetTagDefinition(string TagName)
        {
            foreach (var item in TagSourceManager.Instance.m_tags)
            {

                if (item.TagName == TagName)
                {
                    return item;
                }
            }

            return default(Tag);
        }
    }
}
