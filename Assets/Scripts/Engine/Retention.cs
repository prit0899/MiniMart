using System;
using UnityEngine;
using UnityEngine.UI;

namespace MiniMart.Engine
{
    /// <summary>
    /// Lightweight floating toast on the HUD canvas — used by the daily bonus,
    /// goal completions, and the first-thief tip. Self-destructs after a few
    /// seconds; safe to call from anywhere (no-ops without a canvas).
    /// </summary>
    public static class Toast
    {
        public static void Show(string message, float seconds = 3.5f)
        {
            var canvasGO = GameObject.Find("HUD_Canvas");
            if (canvasGO == null) return;

            var go = new GameObject("Toast", typeof(RectTransform));
            go.transform.SetParent(canvasGO.transform, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.16f, 0.16f, 0.22f, 0.92f);
            img.sprite = HUDBuilder.RoundedSprite;
            img.type = Image.Type.Sliced;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0, 120);
            rt.sizeDelta = new Vector2(560, 58);

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            var text = textGO.AddComponent<Text>();
            text.font = HUDBuilder.UIFont;
            text.fontSize = 24;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            var trt = (RectTransform)textGO.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            text.text = message;

            UnityEngine.Object.Destroy(go, seconds);
        }
    }

    /// <summary>
    /// Two small retention loops, casual-genre standard (batch-35 requirement #5):
    ///
    ///  • Daily bonus — once per calendar day, a cash gift scaled by store level.
    ///  • Rotating mini-goals — "Serve N customers → $reward", tracked off
    ///    GameManager.CustomersServed, escalating each time one completes.
    ///
    /// Both persist via PlayerPrefs so they survive restarts independently of
    /// the main save.
    /// </summary>
    public class Retention : MonoBehaviour
    {
        private const string DailyKey = "MiniMart_LastDailyBonus";
        private const string GoalIndexKey = "MiniMart_GoalIndex";
        private const string GoalBaseKey = "MiniMart_GoalBaseline";

        private bool dailyChecked;
        private int goalIndex;
        private int goalBaseline;    // CustomersServed value when this goal started
        private Text goalChip;

        private int GoalTarget => 10 + goalIndex * 5;          // 10, 15, 20, ...
        private float GoalReward => 20f + goalIndex * 15f;     // $20, $35, $50, ...

        private void Start()
        {
            goalIndex = PlayerPrefs.GetInt(GoalIndexKey, 0);
            goalBaseline = PlayerPrefs.GetInt(GoalBaseKey, 0);
            BuildGoalChip();
        }

        private void BuildGoalChip()
        {
            var canvasGO = GameObject.Find("HUD_Canvas");
            if (canvasGO == null) return;

            var go = new GameObject("GoalChip", typeof(RectTransform));
            go.transform.SetParent(canvasGO.transform, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.20f, 0.55f, 0.85f, 0.92f);
            img.sprite = HUDBuilder.RoundedSprite;
            img.type = Image.Type.Sliced;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(20, -300);
            rt.sizeDelta = new Vector2(230, 46);

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            goalChip = textGO.AddComponent<Text>();
            goalChip.font = HUDBuilder.UIFont;
            goalChip.fontSize = 18;
            goalChip.fontStyle = FontStyle.Bold;
            goalChip.color = Color.white;
            goalChip.alignment = TextAnchor.MiddleCenter;
            var trt = (RectTransform)textGO.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Economy == null) return;

            // ── Daily bonus, granted once the game is actually running ──
            if (!dailyChecked)
            {
                dailyChecked = true;
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                if (PlayerPrefs.GetString(DailyKey, "") != today)
                {
                    PlayerPrefs.SetString(DailyKey, today);
                    PlayerPrefs.Save();
                    float bonus = 25f * gm.StoreLevel;
                    gm.Economy.Deposit(bonus);
                    AudioFx.Coin();
                    Toast.Show($"Daily bonus: +${bonus:F0}! See you tomorrow!");
                }
            }

            // ── Rotating mini-goal ──
            int progress = Mathf.Clamp(gm.CustomersServed - goalBaseline, 0, GoalTarget);
            if (goalChip != null)
                goalChip.text = $"Serve {GoalTarget} customers  {progress}/{GoalTarget}  →  ${GoalReward:F0}";

            if (progress >= GoalTarget)
            {
                gm.Economy.Deposit(GoalReward);
                AudioFx.Purchase();
                if (gm.Player != null) Vfx.Poof(gm.Player.transform.position);
                Toast.Show($"Goal complete! +${GoalReward:F0}");

                goalIndex++;
                goalBaseline = gm.CustomersServed;
                PlayerPrefs.SetInt(GoalIndexKey, goalIndex);
                PlayerPrefs.SetInt(GoalBaseKey, goalBaseline);
                PlayerPrefs.Save();
            }
        }
    }
}
