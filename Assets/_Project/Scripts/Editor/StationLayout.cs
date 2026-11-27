using UnityEngine;

namespace Kotwica.EditorTools
{
    /// <summary>
    /// Shared placement of second docks along a ring hoop, so the station scene (the dock deck and port)
    /// and the district scene (people and props on that deck) agree on where the dock is.
    /// Ring root = main port on the rim; the hoop is a circle of the orbit radius around (0, 0, -radius),
    /// local x is the tangent at the root.
    /// </summary>
    public static class StationLayout
    {
        public const float KeshRadius = 760f;
        /// <summary>Arc length from Kesh's main port to dock B (metres along the hoop, past the end of the street).</summary>
        public const float KeshDockBArc = 300f;

        public static Vector3 HoopPosition(float radius, float arc)
        {
            float angle = arc / radius;
            return new Vector3(radius * Mathf.Sin(angle), 0f, radius * Mathf.Cos(angle) - radius);
        }

        /// <summary>Deck rotation that keeps local x along the hoop and local z pointing outwards.</summary>
        public static Quaternion HoopRotation(float radius, float arc) => Quaternion.Euler(0f, arc / radius * Mathf.Rad2Deg, 0f);

        public const float OrrinRadius = 640f;
        public const float VaroRadius = 520f;
        /// <summary>Second docks of Orrin (trade dock) and Varo (private dock), same distance along their hoops.</summary>
        public const float DockBArc = 300f;

        public static Vector3 KeshDockBPosition => HoopPosition(KeshRadius, KeshDockBArc);
        public static Quaternion KeshDockBRotation => HoopRotation(KeshRadius, KeshDockBArc);
        public static Vector3 OrrinDockBPosition => HoopPosition(OrrinRadius, DockBArc);
        public static Quaternion OrrinDockBRotation => HoopRotation(OrrinRadius, DockBArc);
        public static Vector3 VaroDockBPosition => HoopPosition(VaroRadius, DockBArc);
        public static Quaternion VaroDockBRotation => HoopRotation(VaroRadius, DockBArc);

        /// <summary>A point given in dock-deck coordinates (same layout as the main deck) in ring coordinates.</summary>
        public static Vector3 KeshDockB(Vector3 local) => KeshDockBPosition + KeshDockBRotation * local;
        public static Vector3 OrrinDockB(Vector3 local) => OrrinDockBPosition + OrrinDockBRotation * local;
        public static Vector3 VaroDockB(Vector3 local) => VaroDockBPosition + VaroDockBRotation * local;
    }
}
