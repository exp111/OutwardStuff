using SideLoader;
using System.Collections.Generic;
using UnityEngine;

namespace Mounts
{
    public static class OutwardHelpers
    {
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
    }
}
