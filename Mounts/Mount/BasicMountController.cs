using SideLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Mounts
{
    public class BasicMountController : MonoBehaviour
    {
        #region Properties
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

        #endregion

        //Mount Movement Settings
        public float MoveSpeed { get; private set; }
        public float ActualMoveSpeed => WeightAsNormalizedPercent > WeightEncumberenceLimit ? MoveSpeed * EncumberenceSpeedModifier : MoveSpeed;
        public float RotateSpeed { get; private set; }
        public float LeashDistance = 6f;
        //A Point is randomly chosen in LeashPointRadius around player to leash to.
        public float LeashPointRadius = 2.3f;
        public float TargetStopDistance = 1.4f;
        public float MoveToRayCastDistance = 20f;
        public LayerMask MoveToLayerMask => LayerMask.GetMask("LargeTerrainEnvironment", "WorldItems");

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
        public bool IsMoving;
        public float MountTotalWeight => BagContainer != null && BagContainer.ParentContainer != null ? BagContainer.ParentContainer.TotalContentWeight : 0;

        //Input - tidy up doesnt need this many calls or new v3s
        public Vector3 BaseInput => new Vector3(ControlsInput.MoveHorizontal(CharacterOwner.OwnerPlayerSys.PlayerID), 0, ControlsInput.MoveVertical(CharacterOwner.OwnerPlayerSys.PlayerID));
        public Vector3 CameraRelativeInput => Camera.main.transform.TransformDirection(BaseInput);
        public Vector3 CameraRelativeInputNoY => new Vector3(CameraRelativeInput.x, 0, CameraRelativeInput.z);
        public float DistanceToOwner => CharacterOwner != null ? Vector3.Distance(transform.position, CharacterOwner.transform.position) : 0f;

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

        #region Bag & Weight
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
        #endregion

        public void Update()
        {
            /*if (MountedCharacter == null)
            {
                Mounts.Log.LogMessage($"we somehow ended up in the MountedState without a Mounted Character dismounting character owner and popping state.");
                MountController.DismountCharacter(MountController.CharacterOwner);
                //if we somehow ended up in the MountedState without a Mounted Character
                Parent.PopState();
                return;
            }*/

            /*if (CustomKeybindings.GetKeyDown(Mounts.MOUNT_DISMOUNT_KEY))
            {
                MountController.DismountCharacter(MountedCharacter);
                Parent.PopState();
            }*/

            if (CharacterOwner == null || !IsMounted)
            {
                return;
            }

            try
            {

                this.transform.forward = Vector3.RotateTowards(this.transform.forward, this.transform.forward + this.CameraRelativeInputNoY, this.RotateSpeed * Time.deltaTime, 6f);
                this.Controller.SimpleMove(this.CameraRelativeInput.normalized * this.ActualMoveSpeed);

                UpdateAnimator(this);
                UpdateMenuInputs(this);

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

        private void UpdateMenuInputs(BasicMountController MountController)
        {
            bool flag = false;
            int playerID = MountController.CharacterOwner.OwnerPlayerSys.PlayerID;
            if (MountController.CharacterOwner != null && MountController.CharacterOwner.CharacterUI != null && !MenuManager.Instance.InFade)
            {
                if ((MountController.CharacterOwner.CharacterUI.IsMenuFocused || MountController.CharacterOwner.CharacterUI.IsDialogueInProgress) && ControlsInput.MenuCancel(playerID))
                {
                    MountController.CharacterOwner.CharacterUI.CancelMenu();
                }
                if (!MountController.CharacterOwner.CharacterUI.IgnoreMenuInputs)
                {
                    if (!MountController.CharacterOwner.CurrentlyChargingAttack && !MountController.CharacterOwner.CharacterUI.IsDialogueInProgress && !MountController.CharacterOwner.CharacterUI.IsMenuJustToggled && !MountController.CharacterOwner.CharacterUI.IsOptionPanelDisplayed)
                    {
                        if (ControlsInput.ToggleInventory(playerID))
                        {
                            MountController.CharacterOwner.CharacterUI.ToggleMenu(CharacterUI.MenuScreens.Inventory, true);
                            flag = true;
                        }
                        if (ControlsInput.ToggleEquipment(playerID))
                        {
                            MountController.CharacterOwner.CharacterUI.ToggleMenu(CharacterUI.MenuScreens.Equipment, true);
                            flag = true;
                        }
                        if (ControlsInput.ToggleQuestLog(playerID))
                        {
                            MountController.CharacterOwner.CharacterUI.ToggleMenu(CharacterUI.MenuScreens.QuestLog, true);
                            flag = true;
                        }
                        if (ControlsInput.ToggleSkillMenu(playerID))
                        {
                            MountController.CharacterOwner.CharacterUI.ToggleMenu(CharacterUI.MenuScreens.Skills, true);
                            flag = true;
                        }
                        if (ControlsInput.ToggleCharacterStatusMenu(playerID))
                        {
                            MountController.CharacterOwner.CharacterUI.ToggleMenu(CharacterUI.MenuScreens.CharacterStatus, true);
                            flag = true;
                        }
                        if (ControlsInput.ToggleEffectMenu(playerID))
                        {
                            MountController.CharacterOwner.CharacterUI.ToggleMenu(CharacterUI.MenuScreens.PlayerEffects, true);
                            flag = true;
                        }
                        if (ControlsInput.ToggleQuickSlotMenu(playerID))
                        {
                            MountController.CharacterOwner.CharacterUI.ToggleMenu(CharacterUI.MenuScreens.QuickSlotAssignation, true);
                            flag = true;
                        }
                        if (ControlsInput.ToggleCraftingMenu(playerID))
                        {
                            MountController.CharacterOwner.CharacterUI.ToggleMenu(CharacterUI.MenuScreens.Crafting, true);
                            flag = true;
                        }
                        if (ControlsInput.ToggleMap(playerID) && (!ControlsInput.IsLastActionGamepad(playerID) || !MountController.CharacterOwner.CharacterUI.IsMenuFocused || MountController.CharacterOwner.CharacterUI.IsMapDisplayed))
                        {
                            MenuManager.Instance.ToggleMap(MountController.CharacterOwner.CharacterUI);
                        }
                    }
                    if (ControlsInput.ExitContainer(playerID))
                    {
                        MountController.CharacterOwner.CharacterUI.CloseContainer();
                    }
                    if (ControlsInput.TakeAll(playerID))
                    {
                        MountController.CharacterOwner.CharacterUI.TakeAllItemsInput();
                    }
                    if (MountController.CharacterOwner.CharacterUI.IsMenuFocused)
                    {
                        if (!MountController.CharacterOwner.CharacterUI.IsMenuJustToggled)
                        {
                            if (ControlsInput.InfoInput(playerID))
                            {
                                MountController.CharacterOwner.CharacterUI.InfoInputMenu();
                            }
                            if (ControlsInput.MenuShowDetails(playerID))
                            {
                                MountController.CharacterOwner.CharacterUI.OptionInputMenu();
                            }
                        }
                        if (ControlsInput.GoToPreviousMenu(playerID))
                        {
                            MountController.CharacterOwner.CharacterUI.GoToPreviousTab();
                        }
                        if (ControlsInput.GoToNextMenu(playerID))
                        {
                            MountController.CharacterOwner.CharacterUI.GoToNextTab();
                        }
                    }
                }
                if (ControlsInput.ToggleChatMenu(playerID) && !MountController.CharacterOwner.CurrentlyChargingAttack && !MountController.CharacterOwner.CharacterUI.IsMenuFocused && !MountController.CharacterOwner.CharacterUI.IsDialogueInProgress && !MountController.CharacterOwner.CharacterUI.ChatPanel.IsChatFocused && !MountController.CharacterOwner.CharacterUI.ChatPanel.JustUnfocused)
                {
                    MountController.CharacterOwner.CharacterUI.ShowAndFocusChat();
                    flag = true;
                }
                if (ControlsInput.ToggleHelp(playerID) && !MenuManager.Instance.IsConnectionScreenDisplayed && !MountController.CharacterOwner.CharacterUI.IsMenuJustToggled && !MountController.CharacterOwner.CharacterUI.IsInputFieldJustUnfocused && !MountController.CharacterOwner.Deploying && ((!MountController.CharacterOwner.CharacterUI.IsMenuFocused && !MountController.CharacterOwner.CharacterUI.IsDialogueInProgress) || MountController.CharacterOwner.CharacterUI.GetIsMenuDisplayed(CharacterUI.MenuScreens.PauseMenu)))
                {
                    MountController.CharacterOwner.CharacterUI.ToggleMenu(CharacterUI.MenuScreens.PauseMenu, true);
                }
            }
            if (flag && MountController.CharacterOwner.Deploying)
            {
                MountController.CharacterOwner.DeployInput(-1);
            }
        }

        #region Public Methods

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

        public void DismountCharacter(Character _affectedCharacter)
        {
            IsMounted = false;

            _affectedCharacter.enabled = true;
            _affectedCharacter.CharMoveBlockCollider.enabled = true;
            _affectedCharacter.CharacterController.enabled = true;
            _affectedCharacter.CharacterControl.enabled = true;
            _affectedCharacter.Animator.enabled = true;
            _affectedCharacter.Animator.Update(Time.deltaTime);

            _affectedCharacter.transform.parent = null;
            _affectedCharacter.transform.position = transform.position;
            _affectedCharacter.transform.rotation = transform.rotation;

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
            //StartCoroutine(DelayTeleport(Position, Rotation));
            //transform.position = Position;
            //transform.rotation = Quaternion.Euler(Rotation);
        }

        private IEnumerator DelayTeleport(Vector3 Position, Vector3 Rotation)
        {
            Mounts.Log.LogMessage($"Teleporting {this} to {Position} {Rotation}");
            yield return null;
            yield return null;

            transform.position = Position;
            transform.rotation = Quaternion.Euler(Rotation);
            yield break;
        }

        #endregion

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

        /// <summary>
        /// Prepares a Character for mounting to the MountGameObject
        /// </summary>
        /// <param name="character"></param>
        private void PrepareCharacter(Character character)
        {
            character.CharMoveBlockCollider.enabled = false;
            character.CharacterController.enabled = false;
            character.CharacterControl.enabled = false;
            //cancel movement in animator
            character.SetAnimMove(0, 0);
            character.SpellCastAnim(Character.SpellCastType.Sit, Character.SpellCastModifier.Immobilized, 1);
            TryToParent(character, gameObject);
            OriginalPlayerCameraOffset = character.CharacterCamera.Offset;
            SetCharacterCameraOffset(character, OriginalPlayerCameraOffset + MountedCameraOffset);
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
