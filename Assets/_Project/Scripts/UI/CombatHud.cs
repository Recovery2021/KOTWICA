using Kotwica.Combat;
using Kotwica.Core;
using Kotwica.Gameplay;
using Kotwica.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kotwica.UI
{
    /// <summary>
    /// Prototype combat HUD (IMGUI): crosshair + hit marker, health, ammo, enemy suspicion,
    /// damage flash, prompts, death / victory screens with restart (R).
    /// Throw-away: the real HUD comes later in UI Toolkit.
    /// </summary>
    public class CombatHud : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private PlayerShooter shooter;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private AirlockController airlock;

        private GUIStyle textStyle, bigStyle, smallStyle, centerStyle;
        private Texture2D white;
        private float endTime = -1f;

        private bool Victory => Health.CountAlive(Team.Enemy) == 0;
        private bool Defeat => playerHealth != null && playerHealth.IsDead;

        private void Update()
        {
            if ((Victory || Defeat) && endTime < 0f) endTime = Time.time;

            // Restart after the round is over (short delay so a reload press does not restart by accident)
            if (endTime > 0f && Time.time - endTime > 1f && GameInput.ReloadPressed())
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnGUI()
        {
            EnsureStyles();
            float w = Screen.width, h = Screen.height;

            DrawDamageFlash(w, h);
            if (!Defeat) DrawCrosshair(w, h);
            DrawHealth(h);
            DrawAmmo(w, h);
            DrawSuspicion(w);
            DrawPrompt(w, h);
            DrawMessage(w, h);
            DrawAirlock(w, h);
            DrawEndScreen(w, h);

            GUI.Label(new Rect(16, h - 30, w - 32, 22),
                "WASD ruch · Shift bieg · C kucanie · PPM celowanie · LPM strzał · R przeładowanie · F interakcja · Esc kursor", smallStyle);
        }

        private void DrawCrosshair(float w, float h)
        {
            bool aiming = playerController != null && playerController.IsAiming;
            float gap = aiming ? 4f : 10f;
            float len = aiming ? 6f : 8f;
            var c = new Vector2(w * 0.5f, h * 0.5f);
            Color color = new Color(1f, 1f, 1f, 0.85f);

            Fill(new Rect(c.x - gap - len, c.y - 1, len, 2), color);
            Fill(new Rect(c.x + gap, c.y - 1, len, 2), color);
            Fill(new Rect(c.x - 1, c.y - gap - len, 2, len), color);
            Fill(new Rect(c.x - 1, c.y + gap, 2, len), color);

            // Hit marker: red when the shot killed
            if (shooter != null && Time.time - shooter.LastHitTime < 0.15f)
            {
                Color hit = shooter.LastHitKilled ? new Color(1f, 0.25f, 0.2f) : Color.white;
                GUI.Label(new Rect(c.x - 20, c.y - 20, 40, 40), "×", new GUIStyle(bigStyle) { normal = { textColor = hit }, fontSize = 34 });
            }
        }

        private void DrawHealth(float h)
        {
            if (playerHealth == null) return;
            float k = playerHealth.Current / playerHealth.Max;
            var back = new Rect(20, h - 70, 260, 18);
            Fill(back, new Color(0f, 0f, 0f, 0.55f));
            Fill(new Rect(back.x + 2, back.y + 2, (back.width - 4) * k, back.height - 4),
                Color.Lerp(new Color(0.85f, 0.15f, 0.1f), new Color(0.75f, 0.85f, 0.9f), k));
            GUI.Label(new Rect(20, h - 96, 260, 24), $"Zdrowie {Mathf.CeilToInt(playerHealth.Current)}", textStyle);
            if (playerController != null && playerController.IsAnchored)
                GUI.Label(new Rect(170, h - 96, 160, 24), "[trzymasz uchwyt]", smallStyle);
            else if (playerController != null && playerController.IsCrouched)
                GUI.Label(new Rect(170, h - 96, 120, 24), "[kucasz]", smallStyle);
        }

        private void DrawAmmo(float w, float h)
        {
            if (shooter == null) return;
            string text = shooter.IsReloading ? "PRZEŁADOWANIE..." : $"{shooter.Ammo} / {shooter.Reserve}";
            GUI.Label(new Rect(w - 220, h - 80, 200, 40), text, new GUIStyle(bigStyle) { alignment = TextAnchor.MiddleRight });
        }

        private void DrawSuspicion(float w)
        {
            // Stealth feedback: how close the most alert patrolling enemy is to spotting you
            float suspicion = 0f;
            foreach (EnemyAI enemy in EnemyAI.All) suspicion = Mathf.Max(suspicion, enemy.Suspicion);
            if (suspicion <= 0.02f) return;

            var bar = new Rect(w * 0.5f - 60, 60, 120, 8);
            Fill(bar, new Color(0f, 0f, 0f, 0.5f));
            Fill(new Rect(bar.x, bar.y, bar.width * suspicion, bar.height), new Color(1f, 0.75f, 0.2f, 0.9f));
            GUI.Label(new Rect(0, 30, w, 26), "Ktoś coś zauważył...", centerStyle);
        }

        private void DrawDamageFlash(float w, float h)
        {
            if (playerHealth == null) return;
            float since = Time.time - playerHealth.LastDamageTime;
            float alpha = Mathf.Clamp01(0.45f - since) ;
            if (Defeat) alpha = 0.35f;
            if (alpha > 0f) Fill(new Rect(0, 0, w, h), new Color(0.6f, 0f, 0f, alpha));
        }

        private void DrawPrompt(float w, float h)
        {
            if (interactor == null || !interactor.isActiveAndEnabled || interactor.Current == null || Defeat) return;
            IInteractable current = interactor.Current;
            string prompt = current.CanInteract ? $"[F] {current.Prompt}" : current.Prompt;
            GUI.Label(new Rect(0, h - 150, w, 30), prompt, centerStyle);
        }

        private void DrawMessage(float w, float h)
        {
            if (string.IsNullOrEmpty(GameState.LastMessage) || Time.time - GameState.LastMessageTime > 3.5f) return;
            GUI.Label(new Rect(0, h * 0.2f, w, 36), GameState.LastMessage, bigStyle);
        }

        private void DrawAirlock(float w, float h)
        {
            if (airlock == null || Defeat) return;
            bool safe = playerController != null && playerController.IsAnchored;

            if (airlock.CountdownRemaining > 0f)
            {
                string line = safe ? "Trzymasz się. Śluza otwiera się za" : "ZŁAP UCHWYT [F]! Śluza otwiera się za";
                GUI.Label(new Rect(0, h * 0.28f, w, 40), $"{line} {Mathf.CeilToInt(airlock.CountdownRemaining)}",
                    new GUIStyle(bigStyle) { normal = { textColor = new Color(1f, 0.35f, 0.25f) } });
            }
            else if (airlock.Venting)
            {
                GUI.Label(new Rect(0, h * 0.28f, w, 40), safe ? "DEKOMPRESJA. Trzymaj się!" : "DEKOMPRESJA! CIĄGNIE CIĘ!",
                    new GUIStyle(bigStyle) { normal = { textColor = new Color(1f, 0.35f, 0.25f) } });
            }
        }

        private void DrawEndScreen(float w, float h)
        {
            string title = null, sub = null;
            if (Defeat)
            {
                title = "NIE ŻYJESZ";
                sub = playerHealth.DeathCause;
            }
            else if (Victory)
            {
                title = "KORYTARZ CZYSTY";
                sub = "Wszyscy przeciwnicy wyeliminowani";
            }
            if (title == null) return;

            Fill(new Rect(0, h * 0.38f, w, 120), new Color(0f, 0f, 0f, 0.6f));
            GUI.Label(new Rect(0, h * 0.38f + 14, w, 44), title, new GUIStyle(bigStyle) { fontSize = 36 });
            GUI.Label(new Rect(0, h * 0.38f + 60, w, 26), sub, centerStyle);
            if (Time.time - endTime > 1f) GUI.Label(new Rect(0, h * 0.38f + 88, w, 24), "[R] zagraj ponownie", centerStyle);
        }

        private void Fill(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, white);
            GUI.color = old;
        }

        private void EnsureStyles()
        {
            if (textStyle != null) return;
            white = Texture2D.whiteTexture;
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = new Color(0.85f, 0.9f, 0.95f) } };
            bigStyle = new GUIStyle(textStyle) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            centerStyle = new GUIStyle(textStyle) { alignment = TextAnchor.MiddleCenter };
            smallStyle = new GUIStyle(textStyle) { fontSize = 13, normal = { textColor = new Color(0.65f, 0.7f, 0.75f) } };
        }
    }
}
