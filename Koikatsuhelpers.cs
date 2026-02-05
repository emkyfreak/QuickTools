// KoikatsuHelpers.cs
// Helper extensions and utility methods for Koikatsu Party Studio
// Add this to your project for easier development

using System;
using System.Collections.Generic;
using System.Linq;
using Studio;
using UnityEngine;

namespace StudioCustomButton
{
    /// <summary>
    /// Extension methods for Transform to help find bones and objects
    /// </summary>
    public static class TransformExtensions
    {
        /// <summary>
        /// Recursively searches for a child transform by name
        /// Compatible with .NET 3.5
        /// </summary>
        public static Transform FindLoop(this Transform self, string name)
        {
            if (self.name == name)
                return self;

            foreach (Transform child in self)
            {
                Transform result = child.FindLoop(name);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// Get all children recursively
        /// </summary>
        public static List<Transform> GetAllChildren(this Transform self)
        {
            List<Transform> children = new List<Transform>();
            GetAllChildrenRecursive(self, children);
            return children;
        }

        private static void GetAllChildrenRecursive(Transform parent, List<Transform> list)
        {
            foreach (Transform child in parent)
            {
                list.Add(child);
                GetAllChildrenRecursive(child, list);
            }
        }
    }

    /// <summary>
    /// Koikatsu Studio API reference and helper methods
    /// </summary>
    public static class StudioAPI
    {
        // ====================================================================
        // CHARACTER OPERATIONS
        // ====================================================================

        /// <summary>
        /// Get all characters in the current scene
        /// </summary>
        public static List<OCIChar> GetAllCharacters()
        {
            return Studio.Studio.Instance.dicObjectCtrl.Values
                .OfType<OCIChar>()
                .ToList();
        }

        /// <summary>
        /// Get all male characters (sex == 0)
        /// </summary>
        public static List<OCIChar> GetMaleCharacters()
        {
            return GetAllCharacters()
                .Where(x => x.sex == 0)
                .ToList();
        }

        /// <summary>
        /// Get all female characters (sex == 1)
        /// </summary>
        public static List<OCIChar> GetFemaleCharacters()
        {
            return GetAllCharacters()
                .Where(x => x.sex == 1)
                .ToList();
        }

        /// <summary>
        /// Get the OCIChar wrapper for a ChaControl
        /// </summary>
        public static OCIChar GetOCIChar(ChaControl character)
        {
            return Studio.Studio.Instance.dicObjectCtrl.Values
                .OfType<OCIChar>()
                .FirstOrDefault(x => x.charInfo == character);
        }

        // ====================================================================
        // ANIMATION OPERATIONS
        // ====================================================================

        /// <summary>
        /// Load animation on a character
        /// Group: 0-20, Category: 0-99, ID: 0-99
        /// </summary>
        public static void LoadAnimation(OCIChar character, int group, int category, int id)
        {
            if (character == null) return;

            // Access Studio's animation info
            var animeInfo = Studio.Info.Instance.dicAnimeLoadInfo;

            if (animeInfo.ContainsKey(group))
            {
                var categoryInfo = animeInfo[group];
                if (categoryInfo.ContainsKey(category))
                {
                    var animeList = categoryInfo[category];
                    if (id >= 0 && id < animeList.Count)
                    {
                        character.LoadAnime(group, category, id);
                    }
                }
            }
        }

        /// <summary>
        /// Play current animation
        /// </summary>
        public static void PlayAnimation(OCIChar character)
        {
            if (character == null) return;
            character.animeSpeed = 1f;
            character.animePattern = 0;
        }

        /// <summary>
        /// Stop animation
        /// </summary>
        public static void StopAnimation(OCIChar character)
        {
            if (character == null) return;
            character.animeSpeed = 0f;
        }

        // ====================================================================
        // CLOTHING OPERATIONS
        // ====================================================================

        /// <summary>
        /// Set clothing state for all parts
        /// State: 0 = On, 1 = Half, 2 = Off
        /// </summary>
        public static void SetAllClothingState(ChaControl character, byte state)
        {
            if (character == null) return;

            // There are 8 clothing parts in Koikatsu
            for (int i = 0; i < 8; i++)
            {
                character.SetClothesState(i, state);
            }
        }

        /// <summary>
        /// Clothing state presets
        /// </summary>
        public enum ClothingState
        {
            Clothed = 0,    // All on
            Partial = 1,    // Half off
            Nude = 2        // All off
        }

        /// <summary>
        /// Set clothing to preset state
        /// </summary>
        public static void SetClothingPreset(ChaControl character, ClothingState state)
        {
            SetAllClothingState(character, (byte)state);
        }

        /// <summary>
        /// Toggle specific clothing part
        /// Part indices: 0-7 (top, bottom, bra, underwear, gloves, pantyhose, socks, shoes)
        /// </summary>
        public static void ToggleClothingPart(ChaControl character, int partIndex)
        {
            if (character == null || partIndex < 0 || partIndex >= 8) return;

            byte currentState = character.fileStatus.clothesState[partIndex];
            byte newState = (byte)((currentState + 1) % 3); // Cycle 0->1->2->0
            character.SetClothesState(partIndex, newState);
        }

        // ====================================================================
        // OUTFIT OPERATIONS
        // ====================================================================

        /// <summary>
        /// Change to next outfit (0-3)
        /// </summary>
        public static void NextOutfit(ChaControl character)
        {
            if (character == null) return;

            int current = character.fileStatus.coordinateType;
            int next = (current + 1) % 4;
            character.ChangeCoordinateType((ChaFileDefine.CoordinateType)next);
        }

        /// <summary>
        /// Change to previous outfit (0-3)
        /// </summary>
        public static void PreviousOutfit(ChaControl character)
        {
            if (character == null) return;

            int current = character.fileStatus.coordinateType;
            int prev = (current - 1 + 4) % 4;
            character.ChangeCoordinateType((ChaFileDefine.CoordinateType)prev);
        }

        // ====================================================================
        // FACIAL EXPRESSION OPERATIONS
        // ====================================================================

        /// <summary>
        /// Cycle facial expression pattern
        /// </summary>
        public static void CycleFacialPattern(ChaControl character, FacialPart part, int direction)
        {
            if (character == null) return;

            int maxPatterns = GetMaxFacialPatterns(part);
            int current = GetCurrentFacialPattern(character, part);
            int next = (current + direction + maxPatterns) % maxPatterns;

            SetFacialPattern(character, part, next);
        }

        public enum FacialPart
        {
            Eyebrows,
            Eyes,
            Mouth
        }

        private static int GetCurrentFacialPattern(ChaControl character, FacialPart part)
        {
            switch (part)
            {
                case FacialPart.Eyebrows:
                    return character.fileStatus.eyebrowPtn;
                case FacialPart.Eyes:
                    return character.fileStatus.eyesPtn;
                case FacialPart.Mouth:
                    return character.fileStatus.mouthPtn;
                default:
                    return 0;
            }
        }

        private static void SetFacialPattern(ChaControl character, FacialPart part, int pattern)
        {
            switch (part)
            {
                case FacialPart.Eyebrows:
                    character.ChangeEyebrowPtn(pattern);
                    break;
                case FacialPart.Eyes:
                    character.ChangeEyesPtn(pattern);
                    break;
                case FacialPart.Mouth:
                    character.ChangeMouthPtn(pattern);
                    break;
            }
        }

        private static int GetMaxFacialPatterns(FacialPart part)
        {
            // These are approximate - actual values may vary
            switch (part)
            {
                case FacialPart.Eyebrows:
                    return 32; // Typical max eyebrow patterns
                case FacialPart.Eyes:
                    return 40; // Typical max eye patterns
                case FacialPart.Mouth:
                    return 35; // Typical max mouth patterns
                default:
                    return 10;
            }
        }

        // ====================================================================
        // EYE LOOK OPERATIONS
        // ====================================================================

        /// <summary>
        /// Set eye look target
        /// </summary>
        public static void SetEyeLookTarget(ChaControl character, Transform target)
        {
            if (character == null) return;

            var eyeLookCtrl = character.eyeLookCtrl;
            if (eyeLookCtrl != null)
            {
                eyeLookCtrl.target = target;
            }
        }

        /// <summary>
        /// Look at camera
        /// </summary>
        public static void LookAtCamera(ChaControl character)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                SetEyeLookTarget(character, cam.transform);
            }
        }

        /// <summary>
        /// Look away from camera
        /// </summary>
        public static void LookAway(ChaControl character)
        {
            if (character == null) return;

            // Create a target point opposite to camera
            GameObject lookAwayTarget = new GameObject("LookAwayTarget_" + character.name);
            lookAwayTarget.transform.SetParent(character.transform);

            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 awayDirection = character.transform.position - cam.transform.position;
                lookAwayTarget.transform.position = character.transform.position + awayDirection.normalized * 2f;
                SetEyeLookTarget(character, lookAwayTarget.transform);
            }
        }

        /// <summary>
        /// Reset eye look to default
        /// </summary>
        public static void ResetEyeLook(ChaControl character)
        {
            SetEyeLookTarget(character, null);
        }

        // ====================================================================
        // ITEM OPERATIONS
        // ====================================================================

        /// <summary>
        /// Get all items in scene
        /// </summary>
        public static List<OCIItem> GetAllItems()
        {
            return Studio.Studio.Instance.dicObjectCtrl.Values
                .OfType<OCIItem>()
                .ToList();
        }

        /// <summary>
        /// Find items by name pattern
        /// </summary>
        public static List<OCIItem> FindItemsByName(string namePattern)
        {
            return GetAllItems()
                .Where(x => x.treeNodeObject.textName.Contains(namePattern))
                .ToList();
        }

        /// <summary>
        /// Spawn item by group/category/item numbers
        /// Uses reflection to call non-public AddItem method
        /// </summary>
        public static bool SpawnItem(int group, int category, int item)
        {
            try
            {
                Type studioType = typeof(Studio.Studio);

                object[] args = new object[] { group, category, item };

                studioType.InvokeMember("AddItem",
                    System.Reflection.BindingFlags.InvokeMethod |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance,
                    null,
                    Studio.Studio.Instance,
                    args);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ====================================================================
        // CAMERA OPERATIONS
        // ====================================================================

        /// <summary>
        /// Check if running in VR mode
        /// </summary>
        public static bool IsVRMode()
        {
            return GameObject.Find("VRGIN_Camera (origin)") != null;
        }

        /// <summary>
        /// Get the active camera transform (VR or Desktop)
        /// </summary>
        public static Transform GetActiveCamera()
        {
            Transform vrCam = GameObject.Find("VRGIN_Camera (origin)")?.transform;
            if (vrCam != null)
                return vrCam;

            Camera mainCam = Camera.main;
            if (mainCam != null)
                return mainCam.transform;

            return null;
        }

        /// <summary>
        /// Set camera position and rotation
        /// Works in both VR and Desktop mode
        /// </summary>
        public static void SetCameraTransform(Vector3 position, Quaternion rotation)
        {
            Transform cam = GetActiveCamera();
            if (cam != null)
            {
                cam.position = position;
                cam.rotation = rotation;
            }
        }

        // ====================================================================
        // BONE OPERATIONS
        // ====================================================================

        /// <summary>
        /// Find head bone using multiple fallback methods
        /// </summary>
        public static Transform FindHeadBone(ChaControl character)
        {
            if (character == null) return null;

            // Method 1: Use Animator's HumanBodyBones
            Animator animator = character.animBody;
            if (animator != null)
            {
                Transform headBone = animator.GetBoneTransform(HumanBodyBones.Head);
                if (headBone != null) return headBone;
            }

            // Method 2: Search by common bone names
            string[] headBoneNames = new string[]
            {
                "cf_J_Head",
                "cf_j_head",
                "Head",
                "head",
                "N_Head"
            };

            foreach (string boneName in headBoneNames)
            {
                Transform bone = character.transform.FindLoop(boneName);
                if (bone != null) return bone;
            }

            return null;
        }

        /// <summary>
        /// Hide/Show head and accessories
        /// </summary>
        public static void SetHeadVisibility(ChaControl character, bool visible)
        {
            if (character == null) return;

            Transform headBone = FindHeadBone(character);
            if (headBone == null) return;

            // Hide/show head mesh renderers
            Renderer[] renderers = headBone.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                string name = renderer.name.ToLower();
                if (name.Contains("head") || name.Contains("cf_o_head"))
                {
                    renderer.enabled = visible;
                }
            }

            // Hide/show accessories
            for (int i = 0; i < character.objAccessory.Length; i++)
            {
                GameObject accObj = character.objAccessory[i];
                if (accObj != null)
                {
                    ChaFileAccessory.PartsInfo accInfo = character.nowCoordinate.accessory.parts[i];
                    if (accInfo.type == 0) // Head accessories
                    {
                        accObj.SetActive(visible);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Reflection helpers for accessing private fields
    /// Used for Dynamic Bone toggle and other closed-source features
    /// </summary>
    public static class ReflectionHelper
    {
        /// <summary>
        /// Get private field value using reflection
        /// </summary>
        public static T GetPrivateField<T>(object obj, string fieldName)
        {
            if (obj == null) return default(T);

            Type type = obj.GetType();
            System.Reflection.FieldInfo field = type.GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public);

            if (field != null)
            {
                object value = field.GetValue(obj);
                if (value is T)
                    return (T)value;
            }

            return default(T);
        }

        /// <summary>
        /// Set private field value using reflection
        /// </summary>
        public static bool SetPrivateField(object obj, string fieldName, object value)
        {
            if (obj == null) return false;

            Type type = obj.GetType();
            System.Reflection.FieldInfo field = type.GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public);

            if (field != null)
            {
                field.SetValue(obj, value);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Toggle boolean private field
        /// Returns new value after toggle, or null if failed
        /// </summary>
        public static bool? TogglePrivateField(object obj, string fieldName)
        {
            bool currentValue = GetPrivateField<bool>(obj, fieldName);
            bool newValue = !currentValue;

            if (SetPrivateField(obj, fieldName, newValue))
                return newValue;

            return null;
        }
    }
}