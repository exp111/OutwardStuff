using SideLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TextCore;
using Random = UnityEngine.Random;

namespace Mounts
{
    public class BasicMountController : MonoBehaviour
    {
        public Character CharacterOwner
        {
            get; private set;
        }
        public Animator Animator
        {
            get; private set;
        }
        public CharacterController Controller
        {
            get; private set;
        }
        public Item BagContainer
        {
            get; private set;
        }

        public MountSpecies MountSpecies
        {
            get; private set;
        }

        public string SLPackName
        {
            get; set;
        }

        public string AssetBundleName
        {
            get; set;
        }

        public string PrefabName
        {
            get; set;
        }

        public string MountUID
        {
            get; set;
        }

        // Mount Movement Settings
        public float MoveSpeed { get; private set; }
        public float ActualMoveSpeed => WeightAsNormalizedPercent > WeightEncumberenceLimit ? MoveSpeed * EncumberenceSpeedModifier : MoveSpeed;
        public float RotateSpeed { get; private set; }

        //weight
        public float CurrentCarryWeight = 0;
        //no idea on a reasonable number for any of this
        public float MaxCarryWeight = 90f;
        public float WeightEncumberenceLimit = 0.75f;
        public float EncumberenceSpeedModifier = 0.5f;
        public float WeightAsNormalizedPercent => CurrentCarryWeight / MaxCarryWeight;

        public Vector3 MountedCameraOffset;
        private Vector3 OriginalPlayerCameraOffset;

        public bool IsMounted;
        public float MountTotalWeight => BagContainer != null && BagContainer.ParentContainer != null ? BagContainer.ParentContainer.TotalContentWeight : 0;

        //Input - tidy up doesnt need this many calls or new v3s
        public Vector3 BaseInput => new Vector3(ControlsInput.MoveHorizontal(CharacterOwner.OwnerPlayerSys.PlayerID), 0, ControlsInput.MoveVertical(CharacterOwner.OwnerPlayerSys.PlayerID));
        public Vector3 CameraRelativeInput => Camera.main.transform.TransformDirection(BaseInput);
        public Vector3 CameraRelativeInputNoY => new Vector3(CameraRelativeInput.x, 0, CameraRelativeInput.z);

        public void Awake()
        {
            Animator = GetComponent<Animator>();
            Controller = GetComponent<CharacterController>();

            MountUID = Guid.NewGuid().ToString();
        }

        public void SetOwner(Character mountTarget)
        {
            CharacterOwner = mountTarget;
        }

        public void SetSpecies(MountSpecies mountSpecies)
        {
            MountSpecies = mountSpecies;
            MoveSpeed = MountSpecies.MoveSpeed;
            RotateSpeed = MountSpecies.RotateSpeed;
            MountedCameraOffset = MountSpecies.CameraOffset;
        }

        public void SetCharacterCameraOffset(Character _affectedCharacter, Vector3 NewOffset)
        {
            _affectedCharacter.CharacterCamera.Offset = NewOffset;
        }

        private void UpdateCurrentWeight(float newWeight)
        {
            CurrentCarryWeight = newWeight;
        }
        /// <summary>
        /// Can the mount carry weightToCarry as well as it's own current weight
        /// </summary>
        /// <param name="weightToCarry"></param>
        /// <returns></returns>
        public bool CanCarryWeight(float weightToCarry)
        {
            return Mounts.EnableWeightLimit.Value ? this.MountTotalWeight + weightToCarry < MaxCarryWeight : true;
        }

        public void Update()
        {
            if (CharacterOwner == null || !IsMounted)
            {
                return;
            }

            try
            {
                this.transform.forward = Vector3.RotateTowards(this.transform.forward, this.transform.forward + this.CameraRelativeInputNoY, this.RotateSpeed * Time.deltaTime, 6f);
                this.Controller.SimpleMove(this.CameraRelativeInput.normalized * this.ActualMoveSpeed);

                UpdateAnimator(this);

            }
            catch (Exception e)
            {
                Mounts.Log.LogMessage($"Exception during MountController.Update: {e}");
            }
        }

        public void UpdateAnimator(BasicMountController MountController)
        {
            MountController.Animator.SetFloat("Move X", MountController.BaseInput.x, 5f, 5f);
            MountController.Animator.SetFloat("Move Z", MountController.BaseInput.z != 0 ? 1f : 0f, 5f, 5f);
        }

        public bool CanMount(Character character)
        {
            if (!CanCarryWeight(character.Inventory.TotalWeight) && Mounts.EnableWeightLimit.Value)
            {
                DisplayNotification($"You are carrying too much weight to mount.");
                return false;
            }

            return true;
        }
        public void MountCharacter(Character _affectedCharacter)
        {
            PrepareCharacter(_affectedCharacter);
            IsMounted = true;
            //UpdateCurrentWeight(_affectedCharacter.Inventory.TotalWeight);
        }

        /// <summary>
        /// Prepares a Character for mounting to the MountGameObject
        /// </summary>
        /// <param name="character"></param>
        private void PrepareCharacter(Character character)
        {
            character.CharMoveBlockCollider.enabled = false;
            character.CharacterController.enabled = false;
            //character.CharacterControl.enabled = false;
            character.CharacterControl.InputLocked = true;

            //cancel movement in animator
            character.SetAnimMove(0, 0);
            
            TryToParent(character, gameObject);
            OriginalPlayerCameraOffset = character.CharacterCamera.Offset;
            SetCharacterCameraOffset(character, OriginalPlayerCameraOffset + MountedCameraOffset);

            // Sit //TODO: call sit while dismounting instead of relying on the skill to use that anim?
            character.SpellCastAnim(Character.SpellCastType.Sit, Character.SpellCastModifier.Immobilized, 1);
        }

        public void DismountCharacter(Character _affectedCharacter)
        {
            IsMounted = false;

            _affectedCharacter.enabled = true;
            _affectedCharacter.CharMoveBlockCollider.enabled = true;
            _affectedCharacter.CharacterController.enabled = true;
            //_affectedCharacter.CharacterControl.enabled = true;
            _affectedCharacter.CharacterControl.InputLocked = false;

            _affectedCharacter.Animator.enabled = true;
            _affectedCharacter.Animator.Update(Time.deltaTime);

            _affectedCharacter.transform.parent = null;
            _affectedCharacter.transform.position = transform.position;
            _affectedCharacter.transform.rotation = transform.rotation;
            //TODO: unsit
            SetCharacterCameraOffset(_affectedCharacter, OriginalPlayerCameraOffset);
        }
        public void DisplayNotification(string text)
        {
            if (CharacterOwner != null)
            {
                CharacterOwner.CharacterUI.ShowInfoNotification(text);
            }
        }

        public void DisplayImportantNotification(string text)
        {
            if (CharacterOwner != null)
            {
                CharacterOwner.CharacterUI.NotificationPanel.ShowNotification(text);
            }
        }

        public void PlayTriggerAnimation(string name)
        {
            Animator.SetTrigger(name);
        }

        public void PlayMountAnimation(MountAnimations animation)
        {
            switch (animation)
            {
                case MountAnimations.MOUNT_HAPPY:
                    PlayTriggerAnimation("DoMountHappy");
                    break;
                case MountAnimations.MOUNT_ANGRY:
                    PlayTriggerAnimation("DoMountAngry");
                    break;
                case MountAnimations.MOUNT_SPECIAL:
                    PlayTriggerAnimation("DoMountSpecial");
                    break;
                case MountAnimations.MOUNT_ATTACK:
                    PlayTriggerAnimation("DoMountAttack");
                    break;
                case MountAnimations.MOUNT_HITREACT:
                    PlayTriggerAnimation("DoMountHitReact");
                    break;
            }
        }

        public void Teleport(Vector3 Position, Quaternion Rotation)
        {
            Mounts.DebugLog($"Teleporting to {Position}, {Rotation}");
            transform.SetPositionAndRotation(Position, Rotation);
            Mounts.DebugLog($"Teleported to {Position}, {Rotation}");
        }

        private void TryToParent(Character _affectedCharacter, GameObject MountInstance)
        {
            //probably insanely inefficient, or uses some bizzare form of windings to find the transform, who knows with extension methods :shrug:
            Transform mountPointTransform = transform.FindInAllChildren("SL_MOUNTPOINT");

            if (mountPointTransform != null)
            {
                _affectedCharacter.transform.parent = mountPointTransform;
                _affectedCharacter.transform.localPosition = Vector3.zero;
                _affectedCharacter.transform.localEulerAngles = Vector3.zero;
            }
            else
            {
                _affectedCharacter.transform.parent = MountInstance.transform;
                _affectedCharacter.transform.localPosition = Vector3.zero;
                _affectedCharacter.transform.localEulerAngles = Vector3.zero;

            }
        }
    }

    public enum MountAnimations
    {
        MOUNT_HAPPY,
        MOUNT_ANGRY,
        MOUNT_SPECIAL,
        MOUNT_ATTACK,
        MOUNT_HITREACT
    }
}
