using UnityEngine;

namespace ProfessorSprat.Gameplay.Config
{
    /// <summary>TDD §B KeyMaterialisation tuning.</summary>
    [CreateAssetMenu(menuName = "ProfessorSprat/Config/AccessKey", fileName = "AccessKeyConfig")]
    public sealed class AccessKeyConfig : ScriptableObject
    {
        [SerializeField] private float _ceremonyDuration = 1.2f;
        [SerializeField] private float _amberIntensity = 4.0f;
        [SerializeField] private float _amberRange = 6.0f;
        [SerializeField] private float _amberRampDuration = 1.2f;
        [SerializeField] private float _pickupRadius = 1.5f;
        [SerializeField] private float _doorUnlockRadius = 1.5f;
        [SerializeField] private float _doorOpenDuration = 0.8f;

        public float CeremonyDuration => _ceremonyDuration;
        public float AmberIntensity => _amberIntensity;
        public float AmberRange => _amberRange;
        public float AmberRampDuration => _amberRampDuration;
        public float PickupRadius => _pickupRadius;
        public float DoorUnlockRadius => _doorUnlockRadius;
        public float DoorOpenDuration => _doorOpenDuration;
    }
}
