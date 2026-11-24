using UnityEngine;

namespace Kotwica.Orbits
{
    /// <summary>
    /// Marks an object that is not part of a ring (player, ship, camera).
    /// When the station's reference frame changes, it is moved together with the world
    /// so the change is invisible to the player.
    /// </summary>
    public class FrameObject : MonoBehaviour
    {
        // Registered in Awake/OnDestroy (not OnEnable) so inactive objects, e.g. the player
        // sitting in the ship, are shifted too
        private void Awake() => StationFrame.Register(this);
        private void OnDestroy() => StationFrame.Unregister(this);

        /// <param name="shift">Position offset of the new frame.</param>
        /// <param name="frameVelocity">Velocity of the new anchor in the old frame (subtracted from bodies).</param>
        public void ChangeFrame(Vector3 origin, Quaternion rotation)
        {
            // CharacterController overrides transform changes while enabled
            var controller = GetComponent<CharacterController>();
            bool controllerWasOn = controller != null && controller.enabled;
            if (controllerWasOn) controller.enabled = false;

            transform.SetPositionAndRotation(rotation * (transform.position - origin), rotation * transform.rotation);

            var body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = transform.position;
                body.rotation = transform.rotation;
            }

            if (controllerWasOn) controller.enabled = true;
        }
    }
}
