Spawnable https://www.nexusmods.com/outward/mods/216: saves enemy prefabs in assetbundle

can we load the prefabs directly and spawn those? => saved in `Assets/PrefabInstance`
wendigo: `Wendigo_v.prefab`
=> this is only the visual...

Can access `Assets/Resources/` with Resources.Load => prefabs arent in Resources folder though
Player prefab is spawned like that `Transform transform = UnityEngine.Object.Instantiate<Transform>(Resources.Load("_Characters/" + text, typeof(Transform)) as Transform, vector, rotation)`

AssetBundle.LoadFromFile? => where are the enemy prefabs saved

ideas:
- load enemy asset bundle if it exists
- copy enemy prefabs WITHOUT assets into own asset bundles (does this work?) => bundlekit
- load scenes, steal prefabs from there => load as disabled to get better performance? (https://github.com/Nebby1999/R2API/blob/master/R2API.SceneAsset/SceneAssetAPI.cs)
- create new enemy template and just enter the stuff we need (model, armor, stats)