using UnityEngine;
using UnityEngine.UI;
using MiniMart.Characters;

namespace MiniMart.Engine
{
    /// <summary>
    /// First-run guided onboarding: one big bouncing yellow arrow in the world
    /// plus a friendly banner at the top of the screen, walking a brand-new
    /// player through the core loop one step at a time:
    ///
    ///   1. Go to the tomato plants (harvest)
    ///   2. Carry them to the tomato stand in the store (stock)
    ///   3. Collect the money by the till (earn)
    ///   4. Stand on a glowing pad (build/expand)
    ///
    /// Designed so a player of any age can follow it without reading much:
    /// the arrow does the guiding, the short text is reinforcement. Destroys
    /// itself when the loop is complete; never shown to returning players
    /// (SceneBootstrapper only spawns it when no save exists).
    /// </summary>
    public class TutorialGuide : MonoBehaviour
    {
        private enum Step { GoHarvest, GoStock, CollectCash, BuyPad, Done }
        private Step step = Step.GoHarvest;

        private Transform worldArrow;   // bouncing yellow arrow above the current target
        private Text bannerText;
        private GameObject bannerGO;
        private float bobT;
        private float startCash = -1f;
        private int startPadCount = -1;

        private void Start()
        {
            BuildWorldArrow();
            BuildBanner();
        }

        private void BuildWorldArrow()
        {
            worldArrow = new GameObject("TutorialArrow").transform;
            var yellow = new Color(1f, 0.85f, 0.10f);
            // Bigger than a pad arrow so it reads as "THE objective", not "a pad".
            var stem = PrimitiveFactory.Part(PrimitiveType.Cube, worldArrow,
                new Vector3(0, 0.55f, 0), new Vector3(0.5f, 1.0f, 0.5f), yellow);
            stem.name = "Stem";
            var tip = PrimitiveFactory.Part(PrimitiveType.Cube, worldArrow,
                new Vector3(0, -0.25f, 0), new Vector3(1.0f, 1.0f, 0.5f), yellow);
            tip.name = "Tip";
            tip.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        private void BuildBanner()
        {
            var canvasGO = GameObject.Find("HUD_Canvas");
            if (canvasGO == null) return;

            bannerGO = new GameObject("TutorialBanner", typeof(RectTransform));
            bannerGO.transform.SetParent(canvasGO.transform, false);
            var img = bannerGO.AddComponent<Image>();
            img.color = new Color(0.20f, 0.65f, 0.28f, 0.95f); // friendly green
            img.sprite = HUDBuilder.RoundedSprite;
            img.type = Image.Type.Sliced;
            var rt = (RectTransform)bannerGO.transform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -14);
            rt.sizeDelta = new Vector2(560, 62);

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(bannerGO.transform, false);
            bannerText = textGO.AddComponent<Text>();
            bannerText.font = HUDBuilder.UIFont;
            bannerText.fontSize = 28;
            bannerText.fontStyle = FontStyle.Bold;
            bannerText.color = Color.white;
            bannerText.alignment = TextAnchor.MiddleCenter;
            var trt = (RectTransform)textGO.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Player == null) return;

            // Advance the state machine on real progress, not just proximity —
            // "did the thing" beats "walked near the thing".
            switch (step)
            {
                case Step.GoHarvest:
                    Point(gm.TomatoFarm != null ? gm.TomatoFarm.transform : null,
                        "Walk to the tomatoes to pick them!");
                    if (gm.Player.CarryCount > 0) step = Step.GoStock;
                    break;

                case Step.GoStock:
                {
                    var shelf = FindShelf(Core.ItemType.Tomato);
                    Point(shelf != null ? shelf.transform : null,
                        "Great! Now put them on the tomato stand!");
                    if (shelf != null && shelf.Count > 0) { step = Step.CollectCash; startCash = gm.Economy.PlayerCash; }
                    break;
                }

                case Step.CollectCash:
                {
                    // Point at the live money stack if one exists, else the till.
                    var stack = FindAnyObjectByType<MoneyStack>();
                    Transform target = stack != null ? stack.transform
                        : (gm.Counters != null && gm.Counters.Count > 0 ? gm.Counters[0].transform : null);
                    Point(target, "Customers pay by the till — walk over the cash!");
                    if (startCash >= 0f && gm.Economy.PlayerCash > startCash + 0.5f)
                    {
                        step = Step.BuyPad;
                        startPadCount = FindObjectsByType<PurchasePad>(FindObjectsSortMode.None).Length;
                    }
                    break;
                }

                case Step.BuyPad:
                {
                    var pads = FindObjectsByType<PurchasePad>(FindObjectsSortMode.None);
                    PurchasePad cheapest = null;
                    foreach (var p in pads)
                        if (p.isActiveAndEnabled && (cheapest == null || p.Cost < cheapest.Cost)) cheapest = p;
                    Point(cheapest != null ? cheapest.transform : null,
                        "Stand on a glowing pad to build something new!");
                    if (startPadCount > 0 && pads.Length < startPadCount) step = Step.Done;
                    break;
                }

                case Step.Done:
                    if (bannerText != null) bannerText.text = "You're all set — grow your mart!";
                    Vfx.Stars(gm.Player.transform.position); // gentle falling stars send-off
                    // Linger 4 seconds, then clean up everything.
                    Destroy(worldArrow != null ? worldArrow.gameObject : null, 0.1f);
                    Destroy(bannerGO, 4f);
                    Destroy(gameObject, 4.2f);
                    enabled = false;
                    break;
            }

            // Bounce the arrow above the current target.
            if (worldArrow != null && worldArrow.gameObject.activeSelf)
            {
                bobT += Time.deltaTime * 4f;
                var pos = worldArrow.position;
                worldArrow.position = new Vector3(pos.x, 2.6f + Mathf.Abs(Mathf.Sin(bobT)) * 0.45f, pos.z);
            }
        }

        /// <summary>Move the arrow over a target and set the banner line. A null
        /// target hides the arrow but keeps the text so the player still has words.</summary>
        private void Point(Transform target, string message)
        {
            if (bannerText != null) bannerText.text = message;
            if (worldArrow == null) return;
            if (target == null) { worldArrow.gameObject.SetActive(false); return; }
            worldArrow.gameObject.SetActive(true);
            var p = target.position;
            worldArrow.position = new Vector3(p.x, worldArrow.position.y, p.z);
        }

        private ShopShelf FindShelf(Core.ItemType item)
        {
            foreach (var s in FindObjectsByType<ShopShelf>(FindObjectsSortMode.None))
                if (s.Item == item) return s;
            return null;
        }
    }
}
