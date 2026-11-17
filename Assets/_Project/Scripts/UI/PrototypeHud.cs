using Kotwica.Gameplay;
using Kotwica.Player;
using Kotwica.Ship;
using UnityEngine;

namespace Kotwica.UI
{
    /// <summary>
    /// Quick prototype HUD drawn with IMGUI: money, job, prompts, flight data, target marker.
    /// Throw-away code: the real UI will be built with UI Toolkit in the game's visual style.
    /// </summary>
    public class PrototypeHud : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private ShipController ship;
        [SerializeField] private Camera viewCamera;

        private GUIStyle panelStyle, textStyle, bigStyle, smallStyle, markerStyle;

        private void OnGUI()
        {
            EnsureStyles();

            DrawStatusPanel();
            DrawMessage();
            DrawPrompt();
            if (ship != null && ship.IsPiloted) DrawFlightPanel();
            DrawTargetMarker();
            DrawHelp();
        }

        private void DrawStatusPanel()
        {
            CourierJob job = GameState.ActiveJob;
            string jobText = job != null ? $"{job.Cargo}\n→ {job.Destination.LocationName} ({job.Reward} kr)" : "brak (tablica zleceń na pokładzie)";

            GUI.Box(new Rect(16, 16, 380, 96), GUIContent.none, panelStyle);
            GUI.Label(new Rect(28, 24, 360, 24), $"Kredyty: {GameState.Credits} kr", textStyle);
            GUI.Label(new Rect(28, 50, 360, 56), $"Zlecenie: {jobText}", textStyle);
        }

        private void DrawMessage()
        {
            if (string.IsNullOrEmpty(GameState.LastMessage) || Time.time - GameState.LastMessageTime > 4f) return;
            GUI.Label(new Rect(0, Screen.height * 0.16f, Screen.width, 36), GameState.LastMessage, bigStyle);
        }

        private void DrawPrompt()
        {
            string prompt = null;

            if (ship != null && ship.IsPiloted)
            {
                if (ship.IsAutoDocking) prompt = "Komputer dokujący prowadzi statek...";
                else if (ship.IsDocked) prompt = "[Spacja] Odcumuj     [F] Wyjdź ze statku";
                else if (ship.PortInRange != null)
                {
                    prompt = ship.CanAutoDock
                        ? $"[F] Automatyczne dokowanie: {ship.PortInRange.Ring.DisplayName}"
                        : $"Zbyt szybko do dokowania (max {ship.AutoDockMaxSpeed:0} m/s), puść W lub przytrzymaj X";
                }
            }
            else if (interactor != null && interactor.isActiveAndEnabled && interactor.Current != null)
            {
                prompt = interactor.Current.CanInteract ? $"[F] {interactor.Current.Prompt}" : interactor.Current.Prompt;
            }

            if (!string.IsNullOrEmpty(prompt))
            {
                GUI.Label(new Rect(0, Screen.height - 130, Screen.width, 36), prompt, bigStyle);
            }
        }

        private void DrawFlightPanel()
        {
            // Reference = nearest ring (what the flight assist holds us against)
            DockingPort reference = ship.ReferencePort;
            string referenceName = reference != null ? reference.Ring.DisplayName : "-";
            float relativeSpeed = ship.VelocityRelativeTo(reference).magnitude;

            CourierJob job = GameState.ActiveJob;
            DockingPort target = job != null && job.Destination != null ? job.Destination.Port : null;
            string targetText = target != null
                ? $"{target.Ring.DisplayName}, {Vector3.Distance(ship.transform.position, target.transform.position):0} m"
                : "brak";

            GUI.Box(new Rect(Screen.width - 336, 16, 320, 122), GUIContent.none, panelStyle);
            GUI.Label(new Rect(Screen.width - 324, 24, 300, 24), $"Asystent lotu [Z]: {(ship.FlightAssist ? "WŁ" : "WYŁ")}", textStyle);
            GUI.Label(new Rect(Screen.width - 324, 50, 300, 24), $"Odniesienie: {referenceName}", textStyle);
            GUI.Label(new Rect(Screen.width - 324, 76, 300, 24), $"Prędkość wzgl.: {relativeSpeed:0.0} m/s", textStyle);
            GUI.Label(new Rect(Screen.width - 324, 102, 300, 24), $"Cel: {targetText}", textStyle);
        }

        private void DrawTargetMarker()
        {
            CourierJob job = GameState.ActiveJob;
            DockingPort target = job != null && job.Destination != null ? job.Destination.Port : null;
            if (target == null || viewCamera == null) return;

            Vector3 screen = viewCamera.WorldToScreenPoint(target.transform.position);
            float distance = Vector3.Distance(viewCamera.transform.position, target.transform.position);
            string text = $"[ {target.Ring.DisplayName}  {distance:0} m ]";

            if (screen.z > 0f)
            {
                // IMGUI y axis goes down, screen y goes up
                GUI.Label(new Rect(screen.x - 120, Screen.height - screen.y - 14, 240, 28), text, markerStyle);
            }
            else
            {
                GUI.Label(new Rect(0, Screen.height - 170, Screen.width, 28), $"Cel za tobą: {text}", markerStyle);
            }
        }

        private void DrawHelp()
        {
            string help = ship != null && ship.IsPiloted
                ? "W/S naprzód/wstecz · A/D na boki · Spacja/Ctrl góra/dół · mysz obrót · Q/E przechył · X stop · Z asystent · F dokowanie"
                : "WASD ruch · Shift bieg · Spacja skok · F interakcja · Esc kursor";
            GUI.Label(new Rect(16, Screen.height - 32, Screen.width - 32, 24), help, smallStyle);
        }

        private void EnsureStyles()
        {
            if (panelStyle != null) return;

            var dark = new Texture2D(1, 1);
            dark.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.55f));
            dark.Apply();

            panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = dark } };
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = new Color(0.85f, 0.9f, 0.95f) } };
            bigStyle = new GUIStyle(textStyle) { fontSize = 22, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            smallStyle = new GUIStyle(textStyle) { fontSize = 13, normal = { textColor = new Color(0.7f, 0.75f, 0.8f) } };
            markerStyle = new GUIStyle(textStyle) { fontSize = 15, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 0.75f, 0.3f) } };
        }
    }
}
