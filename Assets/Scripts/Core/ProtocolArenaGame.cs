using System;
using System.Collections.Generic;
using System.Text;
using ProtocolArena.Battle;
using ProtocolArena.Minigame;
using ProtocolArena.Run;
using ProtocolArena.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProtocolArena.Core
{
    public sealed class ProtocolArenaGame : MonoBehaviour
    {
        private enum GameState
        {
            Preparation,
            Minigame,
            Reward,
            Battle,
            Result
        }

        private const int PreparationRounds = 3;
        private static readonly EnemyKind[] EncounterOrder = { EnemyKind.RushUnit, EnemyKind.CrusherBoss };

        private readonly List<string> combatFeed = new List<string>();
        private readonly List<UpgradeOffer> rewardOffers = new List<UpgradeOffer>();
        private readonly Button[] rewardButtons = new Button[3];

        private System.Random random;
        private RunBuild runBuild;
        private CalibrationSession calibration;
        private AutoBattleSimulation simulation;
        private GameState state;

        private GameObject preparationPanel;
        private GameObject minigamePanel;
        private GameObject rewardPanel;
        private GameObject battlePanel;
        private GameObject resultPanel;

        private Text preparationRoundText;
        private Text buildStatsText;
        private Text acquiredModulesText;
        private Text preparationActionText;

        private RectTransform targetZone;
        private RectTransform calibrationCursor;
        private Image calibrationPulse;
        private Button calibrationLockButton;
        private Text calibrationAttemptText;
        private Text calibrationFeedbackText;
        private readonly Text[] calibrationResultTexts = new Text[3];
        private float calibrationCountdown;
        private float calibrationResultDelay;
        private float calibrationPulseAmount;
        private Color calibrationPulseColor;

        private Text rewardGradeText;
        private Text battleTimerText;
        private Text battleStageText;
        private Text battleBannerText;
        private Text battleFeedText;
        private Text battleBuildText;
        private Text resultTitleText;
        private Text resultSummaryText;
        private Text resultModulesText;
        private HealthBarView playerHealth;
        private HealthBarView enemyHealth;
        private FighterView playerFighter;
        private FighterView enemyFighter;
        private Text battleImpactText;
        private float battleImpactTimer;
        private float battleBannerTimer;
        private float battleTransitionDelay;
        private int encounterIndex;
        private float runBattleTime;
        private float runDamage;
        private float runHealing;
        private int runBurstCount;
        private int runSynergyCount;
        private float playerFlash;
        private float enemyFlash;
        private ProceduralAudio audioPlayer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<ProtocolArenaGame>() == null)
            {
                new GameObject("Protocol Arena").AddComponent<ProtocolArenaGame>();
            }
        }

        private void Awake()
        {
            random = new System.Random(Environment.TickCount);
            audioPlayer = new GameObject("Game Audio").AddComponent<ProceduralAudio>();
            audioPlayer.transform.SetParent(transform, false);
            BuildInterface();
            StartNewRun();
        }

        private void Update()
        {
            if (state == GameState.Minigame)
            {
                UpdateMinigame(Time.deltaTime);
                UpdateCalibrationView();
            }
            else if (state == GameState.Battle)
            {
                UpdateBattle(Time.deltaTime);
                playerFlash = Mathf.MoveTowards(playerFlash, 0f, Time.deltaTime * 5f);
                enemyFlash = Mathf.MoveTowards(enemyFlash, 0f, Time.deltaTime * 5f);
                playerFighter.Tick(Time.deltaTime, playerFlash);
                enemyFighter.Tick(Time.deltaTime, enemyFlash);
                UpdateBattleImpact(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            DetachSimulation();
        }

        private void BuildInterface()
        {
            EnsureEventSystem();
            var canvasObject = new GameObject("Protocol Arena UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            preparationPanel = BuildPreparationPanel(canvasObject.transform);
            minigamePanel = BuildMinigamePanel(canvasObject.transform);
            rewardPanel = BuildRewardPanel(canvasObject.transform);
            battlePanel = BuildBattlePanel(canvasObject.transform);
            resultPanel = BuildResultPanel(canvasObject.transform);
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetParent(transform, false);
            eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private GameObject BuildPreparationPanel(Transform parent)
        {
            GameObject panel = CreateScreen(parent, "Preparation");
            CreateHeader(panel.transform, "강철 투기장", "미니게임으로 기체를 강화하고 자동 전투에서 결과를 확인하세요");

            preparationRoundText = RuntimeUIFactory.CreateText(panel.transform, "Preparation Round", string.Empty, 29, TextAnchor.MiddleCenter, RuntimeUIFactory.Orange);
            preparationRoundText.fontStyle = FontStyle.Bold;
            RuntimeUIFactory.SetRect(preparationRoundText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 265f), new Vector2(700f, 55f));

            CreateStep(panel.transform, "01", "미니게임", -390f);
            CreateStep(panel.transform, "02", "보상 3개 중 1개", 0f);
            CreateStep(panel.transform, "03", "자동 전투", 390f);

            GameObject statsPanel = CreateBox(panel.transform, "Current Build", new Vector2(-345f, -40f), new Vector2(610f, 330f), RuntimeUIFactory.Cyan);
            Text statsTitle = RuntimeUIFactory.CreateText(statsPanel.transform, "Title", "현재 기체 능력치", 22, TextAnchor.MiddleLeft, RuntimeUIFactory.Cyan);
            RuntimeUIFactory.SetRect(statsTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(540f, 42f));
            buildStatsText = RuntimeUIFactory.CreateText(statsPanel.transform, "Build Stats", string.Empty, 24, TextAnchor.UpperLeft, Color.white);
            RuntimeUIFactory.SetRect(buildStatsText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(540f, 220f));

            GameObject modulesPanel = CreateBox(panel.transform, "Acquired Modules", new Vector2(345f, -40f), new Vector2(610f, 330f), RuntimeUIFactory.Orange);
            Text modulesTitle = RuntimeUIFactory.CreateText(modulesPanel.transform, "Title", "획득한 강화", 22, TextAnchor.MiddleLeft, RuntimeUIFactory.Orange);
            RuntimeUIFactory.SetRect(modulesTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(540f, 42f));
            acquiredModulesText = RuntimeUIFactory.CreateText(modulesPanel.transform, "Modules", string.Empty, 20, TextAnchor.UpperLeft, Color.white);
            RuntimeUIFactory.SetRect(acquiredModulesText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(540f, 220f));

            Button action = RuntimeUIFactory.CreateButton(panel.transform, "Preparation Action", "미니게임 시작", OnPreparationAction, RuntimeUIFactory.Cyan, 27);
            RuntimeUIFactory.SetRect(action.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -330f), new Vector2(440f, 76f));
            preparationActionText = action.GetComponentInChildren<Text>();
            return panel;
        }

        private GameObject BuildMinigamePanel(Transform parent)
        {
            GameObject panel = CreateScreen(parent, "Calibration");
            CreateHeader(panel.transform, "동력 맞추기", "움직이는 바늘을 초록색 구간에 멈추세요 · 총 3회");

            calibrationAttemptText = RuntimeUIFactory.CreateText(panel.transform, "Attempt", string.Empty, 28, TextAnchor.MiddleCenter, RuntimeUIFactory.Orange);
            calibrationAttemptText.fontStyle = FontStyle.Bold;
            RuntimeUIFactory.SetRect(calibrationAttemptText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 240f), new Vector2(650f, 55f));

            GameObject meter = CreateBox(panel.transform, "Calibration Meter", new Vector2(0f, 50f), new Vector2(1240f, 150f), RuntimeUIFactory.Cyan);
            Image track = RuntimeUIFactory.CreateImage(meter.transform, "Track", new Color(0.08f, 0.11f, 0.17f, 1f));
            RuntimeUIFactory.SetRect(track.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1120f, 58f));

            Image zone = RuntimeUIFactory.CreateImage(track.transform, "Target Zone", new Color(0.2f, 0.95f, 0.5f, 0.65f));
            targetZone = zone.rectTransform;
            targetZone.anchorMin = new Vector2(0.4f, 0f);
            targetZone.anchorMax = new Vector2(0.55f, 1f);
            targetZone.offsetMin = Vector2.zero;
            targetZone.offsetMax = Vector2.zero;

            Image perfectZone = RuntimeUIFactory.CreateImage(zone.transform, "Perfect Zone", new Color(0.9f, 1f, 0.55f, 0.92f));
            perfectZone.rectTransform.anchorMin = new Vector2(0.35f, 0f);
            perfectZone.rectTransform.anchorMax = new Vector2(0.65f, 1f);
            perfectZone.rectTransform.offsetMin = Vector2.zero;
            perfectZone.rectTransform.offsetMax = Vector2.zero;

            calibrationPulse = RuntimeUIFactory.CreateImage(track.transform, "Result Pulse", Color.clear);
            RuntimeUIFactory.Stretch(calibrationPulse.rectTransform);
            calibrationPulse.raycastTarget = false;

            Image cursor = RuntimeUIFactory.CreateImage(track.transform, "Cursor", RuntimeUIFactory.Orange);
            calibrationCursor = cursor.rectTransform;
            calibrationCursor.anchorMin = new Vector2(0.1f, 0f);
            calibrationCursor.anchorMax = new Vector2(0.1f, 1f);
            calibrationCursor.sizeDelta = new Vector2(10f, 18f);

            calibrationFeedbackText = RuntimeUIFactory.CreateText(panel.transform, "Feedback", "잠시 후 시작합니다", 31, TextAnchor.MiddleCenter, Color.white);
            calibrationFeedbackText.fontStyle = FontStyle.Bold;
            RuntimeUIFactory.SetRect(calibrationFeedbackText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -85f), new Vector2(900f, 70f));

            for (int index = 0; index < calibrationResultTexts.Length; index++)
            {
                calibrationResultTexts[index] = RuntimeUIFactory.CreateText(panel.transform, $"Attempt Result {index + 1}", $"{index + 1}차  -", 19, TextAnchor.MiddleCenter, new Color(0.56f, 0.64f, 0.75f, 1f));
                RuntimeUIFactory.SetRect(calibrationResultTexts[index].rectTransform, new Vector2(0.5f, 0.5f), new Vector2((index - 1) * 235f, -148f), new Vector2(210f, 42f));
            }

            calibrationLockButton = RuntimeUIFactory.CreateButton(panel.transform, "Lock In", "여기서 멈추기  [스페이스바]", LockCalibration, RuntimeUIFactory.Orange, 27);
            RuntimeUIFactory.SetRect(calibrationLockButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -255f), new Vector2(440f, 88f));
            return panel;
        }

        private GameObject BuildRewardPanel(Transform parent)
        {
            GameObject panel = CreateScreen(parent, "Reward");
            CreateHeader(panel.transform, "강화 선택", "아래 보상 3개 중 하나만 가져갈 수 있습니다");
            rewardGradeText = RuntimeUIFactory.CreateText(panel.transform, "Calibration Grade", string.Empty, 28, TextAnchor.MiddleCenter, RuntimeUIFactory.Orange);
            rewardGradeText.fontStyle = FontStyle.Bold;
            RuntimeUIFactory.SetRect(rewardGradeText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 245f), new Vector2(800f, 55f));

            float[] cardX = { -455f, 0f, 455f };
            for (int index = 0; index < rewardButtons.Length; index++)
            {
                int offerIndex = index;
                Button card = RuntimeUIFactory.CreateButton(panel.transform, $"Reward Card {index + 1}", string.Empty, () => SelectReward(offerIndex), RuntimeUIFactory.Cyan, 22);
                RuntimeUIFactory.SetRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(cardX[index], -20f), new Vector2(380f, 390f));
                card.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleCenter;
                rewardButtons[index] = card;
            }

            Text instruction = RuntimeUIFactory.CreateText(panel.transform, "Instruction", "선택하지 않은 보상 2개는 사라집니다", 18, TextAnchor.MiddleCenter, new Color(0.58f, 0.66f, 0.78f, 1f));
            RuntimeUIFactory.SetRect(instruction.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -305f), new Vector2(900f, 44f));
            return panel;
        }

        private GameObject BuildBattlePanel(Transform parent)
        {
            GameObject panel = CreateScreen(parent, "Battle");
            CreateHeader(panel.transform, "자동 전투", "지금부터는 완성한 기체가 스스로 싸웁니다");
            battleTimerText = RuntimeUIFactory.CreateText(panel.transform, "Timer", "00:00.0", 22, TextAnchor.MiddleCenter, Color.white);
            RuntimeUIFactory.SetRect(battleTimerText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 335f), new Vector2(300f, 40f));
            battleStageText = RuntimeUIFactory.CreateText(panel.transform, "Stage", string.Empty, 20, TextAnchor.MiddleCenter, RuntimeUIFactory.Orange);
            battleStageText.fontStyle = FontStyle.Bold;
            RuntimeUIFactory.SetRect(battleStageText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 302f), new Vector2(700f, 34f));
            playerHealth = new HealthBarView(panel.transform, "Player Health", RuntimeUIFactory.Cyan, new Vector2(-470f, 292f));
            enemyHealth = new HealthBarView(panel.transform, "Enemy Health", new Color(1f, 0.28f, 0.24f, 1f), new Vector2(470f, 292f));

            GameObject arena = CreateBox(panel.transform, "Arena", new Vector2(0f, 65f), new Vector2(1380f, 330f), RuntimeUIFactory.Cyan);
            Texture2D backgroundTexture = Resources.Load<Texture2D>("Art/ArenaBackground");
            if (backgroundTexture != null)
            {
                RawImage background = RuntimeUIFactory.CreateRawImage(arena.transform, "Arena Background", backgroundTexture, new Color(0.72f, 0.78f, 0.9f, 0.86f));
                RuntimeUIFactory.Stretch(background.rectTransform, 3f);
                background.uvRect = new Rect(0f, 0.25f, 1f, 0.5f);
                background.raycastTarget = false;
            }
            Image floor = RuntimeUIFactory.CreateImage(arena.transform, "Floor", new Color(0.13f, 0.18f, 0.25f, 0.72f));
            floor.rectTransform.anchorMin = Vector2.zero;
            floor.rectTransform.anchorMax = new Vector2(1f, 0f);
            floor.rectTransform.pivot = new Vector2(0.5f, 0f);
            floor.rectTransform.sizeDelta = new Vector2(0f, 22f);
            playerFighter = new FighterView(arena.transform, "Player Unit", RuntimeUIFactory.Cyan, true);
            enemyFighter = new FighterView(arena.transform, "Enemy Unit", new Color(1f, 0.28f, 0.24f, 1f), false);
            battleImpactText = RuntimeUIFactory.CreateText(arena.transform, "Damage Number", string.Empty, 38, TextAnchor.MiddleCenter, Color.white);
            battleImpactText.fontStyle = FontStyle.Bold;
            battleImpactText.raycastTarget = false;
            RuntimeUIFactory.SetRect(battleImpactText.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 70f));
            battleBannerText = RuntimeUIFactory.CreateText(arena.transform, "Battle Banner", string.Empty, 34, TextAnchor.MiddleCenter, Color.white);
            battleBannerText.fontStyle = FontStyle.Bold;
            battleBannerText.raycastTarget = false;
            RuntimeUIFactory.SetRect(battleBannerText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 95f), new Vector2(900f, 100f));

            GameObject feedPanel = CreateBox(panel.transform, "Combat Feed", new Vector2(-315f, -270f), new Vector2(750f, 220f), RuntimeUIFactory.Orange);
            Text feedTitle = RuntimeUIFactory.CreateText(feedPanel.transform, "Title", "전투 기록", 19, TextAnchor.MiddleLeft, RuntimeUIFactory.Orange);
            RuntimeUIFactory.SetRect(feedTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -27f), new Vector2(690f, 36f));
            battleFeedText = RuntimeUIFactory.CreateText(feedPanel.transform, "Entries", string.Empty, 17, TextAnchor.UpperLeft, Color.white);
            RuntimeUIFactory.SetRect(battleFeedText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -23f), new Vector2(690f, 145f));

            GameObject buildPanel = CreateBox(panel.transform, "Battle Build", new Vector2(505f, -270f), new Vector2(570f, 220f), RuntimeUIFactory.Cyan);
            Text buildTitle = RuntimeUIFactory.CreateText(buildPanel.transform, "Title", "적용 중인 강화", 19, TextAnchor.MiddleLeft, RuntimeUIFactory.Cyan);
            RuntimeUIFactory.SetRect(buildTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -27f), new Vector2(510f, 36f));
            battleBuildText = RuntimeUIFactory.CreateText(buildPanel.transform, "Modules", string.Empty, 16, TextAnchor.UpperLeft, Color.white);
            RuntimeUIFactory.SetRect(battleBuildText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -23f), new Vector2(510f, 145f));
            return panel;
        }

        private GameObject BuildResultPanel(Transform parent)
        {
            GameObject panel = CreateScreen(parent, "Result");
            resultTitleText = RuntimeUIFactory.CreateText(panel.transform, "Result Title", string.Empty, 64, TextAnchor.MiddleCenter, Color.white);
            resultTitleText.fontStyle = FontStyle.Bold;
            RuntimeUIFactory.SetRect(resultTitleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 340f), new Vector2(1000f, 95f));
            resultSummaryText = RuntimeUIFactory.CreateText(panel.transform, "Result Summary", string.Empty, 23, TextAnchor.MiddleCenter, Color.white);
            RuntimeUIFactory.SetRect(resultSummaryText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(1200f, 80f));

            GameObject modules = CreateBox(panel.transform, "Run Report", new Vector2(0f, 5f), new Vector2(1050f, 390f), RuntimeUIFactory.Orange);
            Text reportTitle = RuntimeUIFactory.CreateText(modules.transform, "Title", "이번 전투 결과", 23, TextAnchor.MiddleCenter, RuntimeUIFactory.Orange);
            RuntimeUIFactory.SetRect(reportTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(950f, 45f));
            resultModulesText = RuntimeUIFactory.CreateText(modules.transform, "Modules", string.Empty, 21, TextAnchor.UpperLeft, Color.white);
            RuntimeUIFactory.SetRect(resultModulesText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(880f, 270f));

            Button newRun = RuntimeUIFactory.CreateButton(panel.transform, "New Run", "처음부터 다시 도전", StartNewRun, RuntimeUIFactory.Cyan, 28);
            RuntimeUIFactory.SetRect(newRun.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -315f), new Vector2(430f, 78f));
            return panel;
        }

        private void StartNewRun()
        {
            DetachSimulation();
            runBuild = new RunBuild();
            calibration = null;
            rewardOffers.Clear();
            combatFeed.Clear();
            ShowPreparation();
        }

        private void ShowPreparation()
        {
            SetOnlyActive(preparationPanel);
            state = GameState.Preparation;
            int completed = runBuild.Upgrades.Count;
            preparationRoundText.text = completed >= PreparationRounds
                ? "강화 완료 · 전투 준비 끝"
                : $"강화 준비  {completed + 1} / {PreparationRounds}";
            preparationActionText.text = completed >= PreparationRounds ? "전투 시작" : "미니게임 시작";
            buildStatsText.text = UpgradePresentation.BuildStats(runBuild);
            acquiredModulesText.text = UpgradePresentation.BuildModules(runBuild, "아직 획득한 강화가 없습니다.\n미니게임을 끝내고 보상 하나를 선택하세요.");
        }

        private void OnPreparationAction()
        {
            if (runBuild.Upgrades.Count >= PreparationRounds) StartBattle();
            else StartCalibration();
        }

        private void StartCalibration()
        {
            audioPlayer.Play(GameSound.Confirm);
            calibration = new CalibrationSession(random);
            SetOnlyActive(minigamePanel);
            state = GameState.Minigame;
            calibrationCountdown = 2.4f;
            calibrationResultDelay = 0f;
            calibrationPulseAmount = 0f;
            calibrationLockButton.interactable = false;
            for (int index = 0; index < calibrationResultTexts.Length; index++)
            {
                calibrationResultTexts[index].text = $"{index + 1}차  -";
                calibrationResultTexts[index].color = new Color(0.56f, 0.64f, 0.75f, 1f);
            }
            calibrationFeedbackText.text = "준비";
            UpdateCalibrationView();
        }

        private void UpdateMinigame(float deltaTime)
        {
            calibrationPulseAmount = Mathf.MoveTowards(calibrationPulseAmount, 0f, deltaTime * 2.8f);
            calibrationPulse.color = new Color(calibrationPulseColor.r, calibrationPulseColor.g, calibrationPulseColor.b, calibrationPulseAmount * 0.42f);

            if (calibrationResultDelay > 0f)
            {
                calibrationResultDelay -= deltaTime;
                if (calibrationResultDelay <= 0f) ShowRewardSelection();
                return;
            }

            if (calibrationCountdown > 0f)
            {
                calibrationCountdown -= deltaTime;
                int count = Mathf.Max(1, Mathf.CeilToInt(calibrationCountdown / 0.8f));
                calibrationFeedbackText.text = calibrationCountdown > 0f ? $"{count}" : "지금!";
                if (calibrationCountdown <= 0f) calibrationLockButton.interactable = true;
                return;
            }

            calibration.Tick(deltaTime);
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) LockCalibration();
        }

        private void LockCalibration()
        {
            if (state != GameState.Minigame || calibration.IsComplete || calibrationCountdown > 0f) return;
            float score = calibration.LockIn();
            audioPlayer.Play(GameSound.Lock);
            calibrationFeedbackText.text = FormatAttemptFeedback(score);
            int resultIndex = calibration.AttemptsCompleted - 1;
            calibrationResultTexts[resultIndex].text = $"{resultIndex + 1}차  {score:0}점";
            calibrationResultTexts[resultIndex].color = GetScoreColor(score);
            calibrationPulseColor = GetScoreColor(score);
            calibrationPulseAmount = 1f;
            if (calibration.IsComplete)
            {
                calibrationLockButton.interactable = false;
                calibrationResultDelay = 0.8f;
            }
            else UpdateCalibrationView();
        }

        private void UpdateCalibrationView()
        {
            if (calibration == null) return;
            float min = calibration.TargetCenter - CalibrationSession.TargetHalfWidth;
            float max = calibration.TargetCenter + CalibrationSession.TargetHalfWidth;
            targetZone.anchorMin = new Vector2(min, 0f);
            targetZone.anchorMax = new Vector2(max, 1f);
            targetZone.offsetMin = Vector2.zero;
            targetZone.offsetMax = Vector2.zero;
            calibrationCursor.anchorMin = new Vector2(calibration.CursorPosition, 0f);
            calibrationCursor.anchorMax = new Vector2(calibration.CursorPosition, 1f);
            calibrationCursor.anchoredPosition = Vector2.zero;
            calibrationAttemptText.text = $"{Mathf.Min(calibration.AttemptsCompleted + 1, CalibrationSession.RequiredAttempts)}번째 시도 / {CalibrationSession.RequiredAttempts}";
        }

        private void ShowRewardSelection()
        {
            rewardOffers.Clear();
            rewardOffers.AddRange(UpgradeCatalog.DraftThree(runBuild, calibration.RewardQuality, random));
            SetOnlyActive(rewardPanel);
            state = GameState.Reward;
            rewardGradeText.text = $"평가 {GetGradeName(calibration.GetGrade())} · 평균 {calibration.FinalScore:0}점 · 보상 효과 x{calibration.RewardQuality:0.00}";
            for (int index = 0; index < rewardButtons.Length; index++)
            {
                UpgradeOffer offer = rewardOffers[index];
                RuntimeUIFactory.ApplyButtonAccent(rewardButtons[index], UpgradePresentation.GetColor(offer.Id));
                rewardButtons[index].GetComponentInChildren<Text>().text = $"{UpgradePresentation.GetCategory(offer.Id)}\n\n<size=28>{offer.Name}</size>\n\n{offer.Description}\n\n{UpgradePresentation.BuildBeforeAfter(runBuild, offer)}\n\n[ 이 강화 선택 ]";
            }
        }

        private void SelectReward(int index)
        {
            if (state != GameState.Reward || index < 0 || index >= rewardOffers.Count) return;
            runBuild.Apply(rewardOffers[index]);
            audioPlayer.Play(GameSound.Reward);
            ShowPreparation();
        }

        private void StartBattle()
        {
            combatFeed.Clear();
            battleFeedText.text = "기체 출격 중...";
            battleBuildText.text = UpgradePresentation.BuildModules(runBuild, string.Empty);
            battleImpactText.text = string.Empty;
            battleBannerText.text = string.Empty;
            battleImpactTimer = 0f;
            battleBannerTimer = 0f;
            battleTransitionDelay = 0f;
            encounterIndex = 0;
            runBattleTime = 0f;
            runDamage = 0f;
            runHealing = 0f;
            runBurstCount = 0;
            runSynergyCount = 0;
            playerFlash = 0f;
            enemyFlash = 0f;
            SetOnlyActive(battlePanel);
            state = GameState.Battle;
            audioPlayer.Play(GameSound.Confirm);
            BeginEncounter();
        }

        private void BeginEncounter()
        {
            DetachSimulation();
            EnemyProfile profile = EnemyCatalog.Get(EncounterOrder[encounterIndex]);
            simulation = new AutoBattleSimulation();
            simulation.EventRaised += OnCombatEvent;
            simulation.StateChanged += UpdateBattleView;
            simulation.BattleEnded += OnBattleEnded;
            combatFeed.Add($"― {encounterIndex + 1}전투: {profile.Name} ―");
            battleFeedText.text = BuildRecentFeed(6);
            battleStageText.text = $"{encounterIndex + 1} / {EncounterOrder.Length} · {profile.Name} · {profile.Description}";
            ShowBattleBanner(encounterIndex == EncounterOrder.Length - 1 ? "보스 등장" : "첫 번째 상대", profile.Name);
            simulation.Start(runBuild, profile);
            UpdateBattleView();
        }

        private void UpdateBattle(float deltaTime)
        {
            if (battleTransitionDelay > 0f)
            {
                battleTransitionDelay -= deltaTime;
                if (battleTransitionDelay <= 0f) BeginEncounter();
            }
            else
            {
                simulation?.Tick(deltaTime);
            }

            if (battleBannerTimer > 0f)
            {
                battleBannerTimer = Mathf.MoveTowards(battleBannerTimer, 0f, deltaTime);
                Color color = battleBannerText.color;
                color.a = Mathf.Clamp01(battleBannerTimer / 0.45f);
                battleBannerText.color = color;
                if (battleBannerTimer <= 0f) battleBannerText.text = string.Empty;
            }
        }

        private void ShowBattleBanner(string heading, string enemyName)
        {
            battleBannerText.text = $"<size=21>{heading}</size>\n{enemyName}";
            battleBannerText.color = Color.white;
            battleBannerTimer = 1.35f;
        }

        private void OnCombatEvent(CombatEvent combatEvent)
        {
            combatFeed.Add($"{FormatTime(combatEvent.Time)}  {combatEvent.Message}");
            if (combatFeed.Count > 30) combatFeed.RemoveAt(0);
            battleFeedText.text = BuildRecentFeed(6);
            switch (combatEvent.Type)
            {
                case CombatEventType.PlayerAttack:
                    playerFighter.PlayAttack();
                    enemyFighter.PlayHit();
                    enemyFlash = 1f;
                    ShowBattleImpact($"-{combatEvent.Value:0}", new Color(1f, 0.84f, 0.35f, 1f), new Vector2(380f, 35f));
                    audioPlayer.Play(GameSound.Hit);
                    break;
                case CombatEventType.PlayerBurst:
                    playerFighter.PlayAttack();
                    enemyFighter.PlayHit();
                    enemyFlash = 1f;
                    ShowBattleImpact($"연속 공격!  -{combatEvent.Value:0}", RuntimeUIFactory.Orange, new Vector2(340f, 45f));
                    audioPlayer.Play(GameSound.Special);
                    break;
                case CombatEventType.PlayerBarrage:
                case CombatEventType.PlayerCharge:
                    playerFighter.PlayAttack();
                    enemyFighter.PlayHit();
                    enemyFlash = 1f;
                    ShowBattleImpact($"연계 발동!  -{combatEvent.Value:0}", new Color(0.82f, 0.42f, 1f, 1f), new Vector2(330f, 45f));
                    audioPlayer.Play(GameSound.Special);
                    break;
                case CombatEventType.PlayerCounter:
                    enemyFighter.PlayHit();
                    enemyFlash = 0.8f;
                    ShowBattleImpact($"반격!  -{combatEvent.Value:0}", RuntimeUIFactory.Cyan, new Vector2(355f, 42f));
                    audioPlayer.Play(GameSound.Special);
                    break;
                case CombatEventType.PlayerRepair:
                    playerFighter.PlayHeal();
                    playerFlash = 0.7f;
                    ShowBattleImpact($"비상 수리  +{combatEvent.Value:0}", new Color(0.35f, 1f, 0.48f, 1f), new Vector2(-340f, 45f));
                    audioPlayer.Play(GameSound.Heal);
                    break;
                case CombatEventType.EnemyAttack:
                    enemyFighter.PlayAttack();
                    playerFighter.PlayHit();
                    playerFlash = 1f;
                    ShowBattleImpact($"-{combatEvent.Value:0}", new Color(1f, 0.35f, 0.3f, 1f), new Vector2(-380f, 35f));
                    audioPlayer.Play(GameSound.Hit);
                    break;
                case CombatEventType.EnemyHeavy:
                    enemyFighter.PlayAttack();
                    playerFighter.PlayHit();
                    playerFlash = 1f;
                    ShowBattleImpact($"강타!  -{combatEvent.Value:0}", new Color(1f, 0.25f, 0.2f, 1f), new Vector2(-350f, 45f));
                    audioPlayer.Play(GameSound.Special);
                    break;
            }
        }

        private void UpdateBattleView()
        {
            if (simulation?.Player == null) return;
            playerHealth.SetValue("내 기체", simulation.Player.CurrentHealth, simulation.Player.MaxHealth);
            enemyHealth.SetValue(simulation.EnemyProfile.Name, simulation.Enemy.CurrentHealth, simulation.Enemy.MaxHealth);
            playerFighter.SetPosition(simulation.PlayerX);
            enemyFighter.SetPosition(simulation.EnemyX);
            battleTimerText.text = FormatTime(simulation.ElapsedTime);
        }

        private void OnBattleEnded(BattleOutcome outcome)
        {
            runBattleTime += simulation.ElapsedTime;
            runDamage += simulation.TotalPlayerDamage;
            runHealing += simulation.TotalHealing;
            runBurstCount += simulation.BurstTriggers;
            runSynergyCount += simulation.SynergyTriggers;
            bool clearedEncounter = outcome == BattleOutcome.Victory;
            if (clearedEncounter && encounterIndex < EncounterOrder.Length - 1)
            {
                combatFeed.Add($"{simulation.EnemyProfile.Name} 격파!");
                battleFeedText.text = BuildRecentFeed(6);
                encounterIndex++;
                battleTransitionDelay = 1.6f;
                ShowBattleBanner("다음 상대", EnemyCatalog.Get(EncounterOrder[encounterIndex]).Name);
                audioPlayer.Play(GameSound.Reward);
                return;
            }

            SetOnlyActive(resultPanel);
            state = GameState.Result;
            bool victory = clearedEncounter && encounterIndex == EncounterOrder.Length - 1;
            resultTitleText.text = victory ? "승리" : "패배";
            resultTitleText.color = victory ? RuntimeUIFactory.Cyan : new Color(1f, 0.3f, 0.25f, 1f);
            int defeated = victory ? EncounterOrder.Length : encounterIndex;
            resultSummaryText.text = $"격파 {defeated} / {EncounterOrder.Length}   ·   전투 시간 {FormatTime(runBattleTime)}   ·   준 피해 {runDamage:0}   ·   회복 {runHealing:0}\n연속 공격 {runBurstCount}회   ·   조합 효과 {runSynergyCount}회";
            resultModulesText.text = UpgradePresentation.BuildModules(runBuild, string.Empty) + $"\n\n최종 능력치\n{UpgradePresentation.BuildStats(runBuild)}";
            audioPlayer.Play(victory ? GameSound.Victory : GameSound.Defeat);
        }

        private void DetachSimulation()
        {
            if (simulation == null) return;
            simulation.EventRaised -= OnCombatEvent;
            simulation.StateChanged -= UpdateBattleView;
            simulation.BattleEnded -= OnBattleEnded;
            simulation = null;
        }

        private void SetOnlyActive(GameObject activePanel)
        {
            preparationPanel.SetActive(activePanel == preparationPanel);
            minigamePanel.SetActive(activePanel == minigamePanel);
            rewardPanel.SetActive(activePanel == rewardPanel);
            battlePanel.SetActive(activePanel == battlePanel);
            resultPanel.SetActive(activePanel == resultPanel);
        }

        private string BuildRecentFeed(int count)
        {
            int start = Mathf.Max(0, combatFeed.Count - count);
            var builder = new StringBuilder();
            for (int index = start; index < combatFeed.Count; index++) builder.AppendLine(combatFeed[index]);
            return builder.ToString();
        }

        private void ShowBattleImpact(string value, Color color, Vector2 position)
        {
            battleImpactText.text = value;
            battleImpactText.color = color;
            battleImpactText.rectTransform.anchoredPosition = position;
            battleImpactText.rectTransform.localScale = Vector3.one * 1.18f;
            battleImpactTimer = 0.7f;
        }

        private void UpdateBattleImpact(float deltaTime)
        {
            if (battleImpactTimer <= 0f)
            {
                battleImpactText.text = string.Empty;
                return;
            }

            battleImpactTimer = Mathf.MoveTowards(battleImpactTimer, 0f, deltaTime);
            Color color = battleImpactText.color;
            color.a = Mathf.Clamp01(battleImpactTimer / 0.35f);
            battleImpactText.color = color;
            battleImpactText.rectTransform.anchoredPosition += Vector2.up * (42f * deltaTime);
            battleImpactText.rectTransform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.18f, battleImpactTimer / 0.7f);
        }

        private static Color GetScoreColor(float score)
        {
            if (score >= 90f) return new Color(1f, 0.88f, 0.28f, 1f);
            if (score >= 70f) return new Color(0.28f, 1f, 0.55f, 1f);
            if (score >= 45f) return RuntimeUIFactory.Cyan;
            return new Color(1f, 0.4f, 0.3f, 1f);
        }

        private static string GetGradeName(string grade)
        {
            switch (grade)
            {
                case "S": return "S (최고)";
                case "A": return "A (우수)";
                case "B": return "B (보통)";
                default: return "C (아쉬움)";
            }
        }

        private static string FormatAttemptFeedback(float score)
        {
            if (score >= 90f) return $"완벽해요!  {score:0}점";
            if (score >= 70f) return $"좋아요!  {score:0}점";
            if (score >= 45f) return $"괜찮아요  {score:0}점";
            return $"아쉬워요  {score:0}점";
        }

        private static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            return $"{minutes:00}:{seconds - minutes * 60f:00.0}";
        }

        private static GameObject CreateScreen(Transform parent, string name)
        {
            return RuntimeUIFactory.CreatePanel(parent, name, RuntimeUIFactory.Background);
        }

        private static void CreateHeader(Transform parent, string titleValue, string subtitleValue)
        {
            Text title = RuntimeUIFactory.CreateText(parent, "Title", titleValue, 47, TextAnchor.MiddleCenter, RuntimeUIFactory.Cyan);
            title.fontStyle = FontStyle.Bold;
            RuntimeUIFactory.SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 395f), new Vector2(1100f, 66f));
            Text subtitle = RuntimeUIFactory.CreateText(parent, "Subtitle", subtitleValue, 19, TextAnchor.MiddleCenter, new Color(0.62f, 0.7f, 0.82f, 1f));
            RuntimeUIFactory.SetRect(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 350f), new Vector2(1200f, 40f));
        }

        private static GameObject CreateBox(Transform parent, string name, Vector2 position, Vector2 size, Color accent)
        {
            GameObject box = RuntimeUIFactory.CreatePanel(parent, name, RuntimeUIFactory.Panel);
            RuntimeUIFactory.SetRect(box.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), position, size);
            var outline = box.AddComponent<Outline>();
            outline.effectColor = new Color(accent.r, accent.g, accent.b, 0.65f);
            outline.effectDistance = new Vector2(2f, -2f);
            return box;
        }

        private static void CreateStep(Transform parent, string number, string label, float x)
        {
            Text text = RuntimeUIFactory.CreateText(parent, $"Step {number}", $"{number}  {label}", 18, TextAnchor.MiddleCenter, new Color(0.66f, 0.73f, 0.84f, 1f));
            RuntimeUIFactory.SetRect(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, 205f), new Vector2(330f, 44f));
        }

    }
}
