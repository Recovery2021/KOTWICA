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

        private float panelBottom;
        private GUIStyle textStyle, bigStyle, smallStyle, markerStyle;

        private void OnGUI()
        {
            var previous = GUI.matrix;
            GUI.matrix = previous * Matrix4x4.Scale(Vector3.one * HudLayout.Scale);
            EnsureStyles();

            DrawStatusPanel();

            DrawPrompt();
            if (ship != null && ship.IsPiloted) DrawFlightPanel();
            DrawMessage();
            DrawTargetMarker();
            DrawHelp();
            GUI.matrix = previous;
        }

        private void DrawStatusPanel()
        {
            CourierJob job = GameState.ActiveJob;
            string jobText = job != null ? $"{job.Cargo}\n→ {job.Destination.LocationName} ({job.Reward} kr)" : "brak (tablica zleceń na pokładzie)";

            if (job?.Home != null && GameState.Mission != GameState.Stage.Complete)
            {
                var t = Kotwica.Orbits.OrbitalClock.Now;
                jobText += $"\nDeklaracja: {job.DeclaredCargo} · powrót za {job.ReturnBy - t:0} s";
                if (GameState.AtClinic)
                {
                    jobText += GameState.Mission == GameState.Stage.Waiting
                        ? $"\nOdbiór przy bramce za {GameState.ApprovalRemaining:0} s"
                        : "\nKlinika: interkom przy bramce lub przejście techniczne [C].";
                    float homeFlight = Vector3.Distance(job.Home.Port.PositionAt(t), job.Destination.Port.PositionAt(t)) / ship.CruiseSpeed;
                    jobText += $"\nLot na Kesh ~{homeFlight:0} s. Możesz wrócić bez leku.";
                }
                if (GameState.Mission == GameState.Stage.Return)
                    jobText += GameState.Cleared ? "\nZanieś przesyłkę dokerce." :
                        "\nRadio Kesh: po dokowaniu zgłoś ładunek do odprawy." +
                        (GameState.Package == GameState.Medicine.Undeclared ? "\nZgłoszenie: −30 kr. Dokerka najpierw: kontrola następnego kursu." : "");
                var origin = ship.DockedAt;
                var target = job.Destination.Port;
                if (origin != null && origin != target)
                {
                    float now = Vector3.Distance(origin.PositionAt(t), target.PositionAt(t)) / ship.CruiseSpeed;
                    float later = Vector3.Distance(origin.PositionAt(t + 30), target.PositionAt(t + 30)) / ship.CruiseSpeed;
                    jobText += $"\nLot teraz ~{now:0} s · po 30 s oczekiwania ~{later:0} s";
                }
            }
            else if (GameState.Mission == GameState.Stage.Repair && ship != null && ship.RequiresPermit)
                jobText = "Napraw podnośnik przy zielonej tablicy na Kesh.";
            else if (job == null && GameState.Outcome != null)
            {
                // The mission summary stays on screen for a moment, then gives way (it lives on in the journal)
                const float outcomeSeconds = 20f;
                if (Time.time - GameState.OutcomeTime < outcomeSeconds) jobText = GameState.Outcome;
                else if (GameState.NeedsMedicineRun) jobText = "brak · opcjonalnie: kurs po lek z tablicy na Kesh";
                else jobText = "brak · kolejne zadania w dzienniku (Tab)";
            }

            float width = Mathf.Min(500, HudLayout.Width * 0.51f - 20);
            panelBottom = HudLayout.Panel(12, 12, width, $"Kredyty: {GameState.Credits} kr\nZlecenie: {jobText}", textStyle);

        }

        private void DrawMessage()
        {
            if (string.IsNullOrEmpty(GameState.LastMessage) || Time.time - GameState.LastMessageTime > 4f) return;
            HudLayout.Caption(HudLayout.Width / 2, panelBottom + 6, GameState.LastMessage, bigStyle);
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
                    prompt = !ship.PortAllowed ? (ShipController.PortLockedDown(ship.PortInRange, 3) ? "Port zablokowany: alarm na tym pierścieniu" : "Brak zgody na dokowanie w tym porcie") : ship.CanAutoDock
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
                GUI.Label(new Rect(0, HudLayout.Height - 130, HudLayout.Width, 36), prompt, bigStyle);
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

            float width = Mathf.Min(425, HudLayout.Width * 0.49f - 20);
            string data = $"Asystent lotu [Z]: {(ship.FlightAssist ? "WŁ" : "WYŁ")}\nOdniesienie: {referenceName}\nPrędkość wzgl.: {relativeSpeed:0.0} m/s\nCel: {targetText}";
            panelBottom = Mathf.Max(panelBottom, HudLayout.Panel(HudLayout.Width - width - 12, 12, width, data, textStyle));

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
                HudLayout.Caption(screen.x / HudLayout.Scale, HudLayout.Height - screen.y / HudLayout.Scale, text, markerStyle);
            }
            else
            {
                HudLayout.Caption(HudLayout.Width / 2, HudLayout.Height - 175, $"Cel za tobą: {text}", markerStyle);
            }
        }

        private void DrawHelp()
        {
            string help = ship != null && ship.IsPiloted
                ? "W/S naprzód/wstecz · A/D na boki · Spacja/Ctrl góra/dół · mysz obrót · Q/E przechył · X stop · Z asystent · F dokowanie"
                : "WASD ruch · Shift bieg · C kucanie · PPM/LPM celowanie i strzał · R przeładowanie · F interakcja · Tab dziennik";
            GUI.Label(new Rect(16, HudLayout.Height - 32, HudLayout.Width - 32, 24), help, smallStyle);
        }

        private void EnsureStyles()
        {
            if (textStyle != null) return;
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true, normal = { textColor = Color.white } };
            bigStyle = new GUIStyle(textStyle) { fontSize = 22, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            smallStyle = new GUIStyle(textStyle) { fontSize = 15, normal = { textColor = new Color(0.7f, 0.75f, 0.8f) } };
            markerStyle = new GUIStyle(textStyle) { fontSize = 19, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 0.75f, 0.3f) } };
        }
    }

    internal static class HudLayout
    {
        public static float Scale => Mathf.Max(0.75f, Mathf.Min(Screen.width / 1100f, Screen.height / 720f));
        public static float Width => Screen.width / Scale;
        public static float Height => Screen.height / Scale;

        public static float Panel(float x, float y, float width, string text, GUIStyle style)
        {
            float inner = width - 20;
            float height = Mathf.Ceil(style.CalcHeight(new GUIContent(text), inner));
            var old = GUI.color;
            GUI.color = new Color(0.025f, 0.035f, 0.05f, 0.94f);
            GUI.DrawTexture(new Rect(x, y, width, height + 12), Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(new Rect(x + 10, y + 6, inner, height), text, style);
            return y + height + 12;
        }

        public static void Caption(float center, float top, string text, GUIStyle style)
        {
            float width = Mathf.Min(Width - 24, Mathf.Ceil(style.CalcSize(new GUIContent(text)).x) + 20);
            Panel(Mathf.Clamp(center - width / 2, 12, Width - width - 12), top, width, text, style);
        }
    }
}
