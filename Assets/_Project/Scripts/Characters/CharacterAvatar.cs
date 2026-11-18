using UnityEngine;

namespace Kotwica.Characters
{
    /// <summary>
    /// Replaces the grey placeholder body with a real animated humanoid at scene start:
    /// the preset chosen in the creator (player) or the enemy preset (enemies).
    /// If presets are not set up yet, the placeholder simply stays, so nothing breaks.
    /// </summary>
    public class CharacterAvatar : MonoBehaviour
    {
        [SerializeField] private bool isPlayer = true;
        [Tooltip("Capsule body hidden once the real model is in place.")]
        [SerializeField] private GameObject placeholder;

        [Header("Weapon in the right hand (local to the hand bone)")]
        [SerializeField] private Vector3 weaponPosition = new Vector3(0.05f, 0.1f, 0.03f);
        [SerializeField] private Vector3 weaponRotation = new Vector3(0f, 90f, 90f);

        public Animator Animator { get; private set; }
        public Transform WeaponMuzzle { get; private set; }

        private void Awake()
        {
            CharacterRoster roster = CharacterRoster.Load();
            if (roster == null) return;

            CharacterPreset preset = isPlayer ? roster.GetPlayerPreset(CharacterSelection.PresetIndex) : roster.EnemyPreset;
            if (preset == null || preset.Model == null) return;

            // Spawn the model as a child: feet at our pivot, facing our forward
            GameObject model = Instantiate(preset.Model, transform);
            model.name = "Model: " + preset.DisplayName;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * preset.Scale;

            Animator = model.GetComponent<Animator>();
            if (Animator == null) Animator = model.AddComponent<Animator>();
            Animator.runtimeAnimatorController = roster.AnimatorController;
            Animator.applyRootMotion = false;                       // movement comes from gameplay code
            Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            if (placeholder != null) placeholder.SetActive(false);

            var driver = GetComponent<CharacterAnimator>();
            if (driver == null) driver = gameObject.AddComponent<CharacterAnimator>();
            driver.Bind(Animator);

            AttachWeapon(roster.WeaponPrefab);
        }

        private void AttachWeapon(GameObject weaponPrefab)
        {
            Transform hand = Animator.isHuman ? Animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
            if (hand == null) return;

            GameObject weapon;
            if (weaponPrefab != null)
            {
                weapon = Instantiate(weaponPrefab, hand);
            }
            else
            {
                // Placeholder rifle: a long dark box until a proper model exists
                weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(weapon.GetComponent<Collider>());
                weapon.transform.SetParent(hand, false);
                weapon.transform.localScale = new Vector3(0.06f, 0.1f, 0.75f);
                var renderer = weapon.GetComponent<Renderer>();
                var bodyRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
                if (bodyRenderer != null) renderer.sharedMaterial = bodyRenderer.sharedMaterial;
                renderer.material.color = new Color(0.12f, 0.12f, 0.13f);
            }

            weapon.name = "Broń";
            weapon.transform.localPosition = weaponPosition;
            weapon.transform.localRotation = Quaternion.Euler(weaponRotation);

            WeaponMuzzle = new GameObject("Wylot lufy").transform;
            WeaponMuzzle.SetParent(weapon.transform, false);
            WeaponMuzzle.localPosition = new Vector3(0f, 0f, 0.5f);
        }
    }
}
