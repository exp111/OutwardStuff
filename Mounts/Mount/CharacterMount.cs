using SideLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mounts
{

    /// <summary>
    /// This simply holds a reference to the characters mount instance, might not need, IDK yet.
    /// </summary>
    public class CharacterMount : MonoBehaviour
    {
        public BasicMountController ActiveMount
        {
            get; private set;
        }

        public bool HasActiveMount
        {
            get
            {
                return ActiveMount != null;
            }
        }

        public Character Character => GetComponent<Character>();

        public void SetActiveMount(BasicMountController newMount)
        {
            ActiveMount = newMount;
        }
    }
}
