using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;

namespace Mounts
{
    [Serializable]
    public class MountSpecies
    {
        public string SpeciesName;
        public string SLPackName;
        public string AssetBundleName;
        public string PrefabName;

        //movement
        public float MoveSpeed;
        //used by nav mesh agent
        public float Acceleration;
        public float RotateSpeed;

        public Vector3 CameraOffset;

        //weight
        public float MaximumCarryWeight;
        public float CarryWeightEncumberenceLimit;
        public float EncumberenceSpeedModifier;
    }
}
