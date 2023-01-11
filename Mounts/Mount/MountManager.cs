using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;

namespace Mounts
{
    /// <summary>
    /// Manages Mount Instances so they can be destroyed when needed along with their ui.
    /// </summary>
    public class MountManager
    {
        private List<MountSpecies> SpeciesData = new();

        public Dictionary<Character, BasicMountController> MountControllers
        {
            get; private set;
        }

        private string RootFolder;
        private string SpeciesFolder;

        public MountManager(string rootFolder)
        {
            RootFolder = rootFolder;
            SpeciesFolder = Path.Combine(RootFolder, "MountSpecies");
            
            Mounts.Log.LogMessage($"Initalising MountManager at: {RootFolder}");
            MountControllers = new Dictionary<Character, BasicMountController>();
            LoadAllSpeciesDataFiles();
        }

        private void LoadAllSpeciesDataFiles()
        {
            Mounts.Log.LogMessage($"MountManager Initalising Species Definitions..");
            SpeciesData.Clear();

            if (!Directory.Exists(SpeciesFolder))
            {
                Mounts.Log.LogMessage($"MountManager MountSpecies Folder {SpeciesFolder} does not exist");
                return;
            }

            string[] filePaths = Directory.GetFiles(SpeciesFolder, "*.xml");
            Mounts.DebugTrace($"MountManager MountSpecies Definitions [{filePaths.Length}] Found.");


            foreach (var item in filePaths)
            {
                Mounts.DebugTrace($"MountManager MountSpecies Reading {item} data.");
                MountSpecies mountSpecies = DeserializeFromXML<MountSpecies>(item);

                if (!HasSpeciesDefinition(mountSpecies.SpeciesName))
                {
                    SpeciesData.Add(mountSpecies);
                    Mounts.DebugTrace($"MountManager MountSpecies Added {mountSpecies.SpeciesName} data. ({mountSpecies.SLPackName}/{mountSpecies.AssetBundleName}/{mountSpecies.PrefabName})");
                }           
            }
            Mounts.Log.LogMessage($"MountManager read {SpeciesData.Count} Species..");
        }

        public bool HasSpeciesDefinition(string SpeciesName)
        {
            return SpeciesData.Find(x => x.SpeciesName == SpeciesName) != null ? true : false;
        }

        public MountSpecies GetSpeciesDefinitionByName(string SpeciesName)
        {
            if (SpeciesData != null)
            {
                return SpeciesData.Find(x => x.SpeciesName == SpeciesName);
            }

            return null;
        }

        public static T DeserializeFromXML<T>(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            StreamReader reader = new StreamReader(path);
            T deserialized = (T)serializer.Deserialize(reader.BaseStream);
            reader.Close();
            return deserialized;
        }

        /// <summary>
        /// Creates a new Mount in the scene next to the Owner Player from the XML Definition.
        /// </summary>
        /// <param name="_affectedCharacter"></param>
        /// <param name="MountSpecies"></param>
        /// <param name="Position"></param>
        /// <param name="Rotation"></param>
        /// <returns></returns>
        public BasicMountController CreateMountFromSpecies(Character _affectedCharacter, MountSpecies MountSpecies, Vector3 Position, Quaternion Rotation, string bagUID = "")
        {
            GameObject Prefab = OutwardHelpers.GetFromAssetBundle<GameObject>(MountSpecies.SLPackName, MountSpecies.AssetBundleName, MountSpecies.PrefabName);
            GameObject MountInstance = null;

            if (Prefab == null)
            {
                Mounts.Log.LogMessage($"CreateMountForCharacter PrefabName: {MountSpecies.PrefabName} from AssetBundle ({MountSpecies.SLPackName}/{MountSpecies.AssetBundleName}) was null.");
                return null;
            }

            if (MountInstance == null)
            {
                MountInstance = GameObject.Instantiate(Prefab, Position, Rotation);
                GameObject.DontDestroyOnLoad(MountInstance);

                BasicMountController basicMountController = MountInstance.AddComponent<BasicMountController>();

                basicMountController.SetOwner(_affectedCharacter);
                basicMountController.SetSpecies(MountSpecies);

                basicMountController.MaxCarryWeight = MountSpecies.MaximumCarryWeight;
                basicMountController.EncumberenceSpeedModifier = MountSpecies.EncumberenceSpeedModifier;

                CharacterMount characterMount = _affectedCharacter.gameObject.GetComponent<CharacterMount>();

                if (characterMount)
                {
                    characterMount.SetActiveMount(basicMountController);
                }

                MountControllers.Add(_affectedCharacter, basicMountController);

                basicMountController.Teleport(Position, Rotation);
                basicMountController.MountCharacter(_affectedCharacter);
                return basicMountController;
            }

            return null;
        }

        public bool CharacterHasMount(Character character)
        {
            if (MountControllers.ContainsKey(character))
            {
                return true;
            }

            return false;
        }
        public BasicMountController GetActiveMountForCharacter(Character _affectedCharacter)
        {
            if (MountControllers.ContainsKey(_affectedCharacter))
            {
                return MountControllers[_affectedCharacter];
            }

            return null;
        }
        public void DestroyAllMountInstances()
        {
            Mounts.DebugLog($"Destroying All Mount Instances...");

            if (MountControllers != null)
            {
                foreach (var mount in MountControllers.ToList())
                {
                    Mounts.DebugTrace($"Destroying and unregistering from UI for {mount.Value} of {mount.Key.Name}");
                    DestroyMount(mount.Key, mount.Value);
                }

                MountControllers.Clear();

                Mounts.DebugLog($"All Mount Instances Destroyed Successfully.");
            }
        }
        public void DestroyActiveMount(Character character)
        {
            Mounts.DebugLog($"Destroying Active Mount instance for {character.Name}");

            if (MountControllers != null)
            {
                if (MountControllers.ContainsKey(character))
                {
                    DestroyMount(character, MountControllers[character]);
                }
            }
        }

        private void DestroyMount(Character character, BasicMountController basicMountController)
        {
            if (basicMountController.IsMounted)
            {
                Mounts.DebugLog($"Dismounting {character} from {basicMountController}");
                basicMountController.DismountCharacter(character);
            }
            // Check if we have any characters as children
            if (basicMountController.gameObject.GetComponentInChildren<Character>() != null)
            {
                Mounts.Log.LogMessage($"Warning: We are destroying a mount that contains a character ({character})!");
            }
            Mounts.DebugLog($"Destroying Mount instance for {character.Name}");
            GameObject.Destroy(basicMountController.gameObject);
            MountControllers.Remove(character);
        }
    }
}
