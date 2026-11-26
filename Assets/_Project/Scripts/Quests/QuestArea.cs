using Kotwica.Core;
using UnityEngine;

namespace Kotwica.Quests
{
    /// <summary>Trigger volume that reports "player reached this place" (objective Idź).</summary>
    [RequireComponent(typeof(Collider))]
    public class QuestArea : MonoBehaviour
    {
        [SerializeField] private string targetId = "miejsce";

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<Kotwica.Player.PlayerController>() != null)
                GameServices.Report(ObjectiveType.Idz, targetId);
        }
    }
}
