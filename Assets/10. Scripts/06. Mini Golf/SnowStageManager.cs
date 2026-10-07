using System.Collections;
using UnityEngine;

namespace Study3D.MiniGolf
{
    public enum SnowStageState
    {
        Playing,
        Rescuing,
        StageCleared,
        Transitioning,
        GameCompleted
    }

    [System.Serializable]
    public class SnowGolfStage
    {
        public string title;
        public GameObject stageRoot;
        public Transform startPoint;
        public SnowGoalTree goal;
        public SnowBoss boss;
        public SnowBreakable[] obstacles = System.Array.Empty<SnowBreakable>();
        public SnowPickup[] pickups = System.Array.Empty<SnowPickup>();
        public SnowHeatAttack[] heatAttacks = System.Array.Empty<SnowHeatAttack>();
        public SnowSupply[] snowSupplies = System.Array.Empty<SnowSupply>();
    }

    public class SnowStageManager : MonoBehaviour
    {
        public SnowballPlayer player;
        public GolfPlayer golfPlayer;
        public GolfPlainCamera golfCamera;
        public int startingStage = 1;
        public SnowGolfStage[] stages =
        {
            new SnowGolfStage { title = "1. 눈덩이를 굴려라" },
            new SnowGolfStage { title = "2. 크게 키워라" },
            new SnowGolfStage { title = "3. 펭귄 보스" }
        };

        private int currentStageIndex;
        private int currentShotCount;
        private int[] completedStageShotCounts;
        private SnowStageState state;
        private float heatWarningTime;
        private bool showHeatWarning;
        private GUIStyle uiBoxStyle;
        private GUIStyle uiLabelStyle;
        private GUIStyle uiButtonStyle;
        private GUIStyle uiScoreTitleStyle;
        private GUIStyle uiScoreCellStyle;
        private GUIStyle uiScoreTotalStyle;
        private Texture2D uiPanelTexture;
        private static readonly Color uiTextColor = new Color(0.09f, 0.17f, 0.25f);

        public bool CanShoot => state == SnowStageState.Playing;
        public int CurrentShotCount => currentShotCount;
        public int CurrentStageNumber => currentStageIndex + 1;
        public SnowStageState State => state;

        private void Start()
        {
            if (player == null) player = FindFirstObjectByType<SnowballPlayer>();
            if (golfPlayer == null) golfPlayer = FindFirstObjectByType<GolfPlayer>();
            if (golfCamera == null) golfCamera = FindFirstObjectByType<GolfPlainCamera>();
            golfPlayer.stageManager = this;
            player.stageManager = this;
            golfCamera.snowball = player;
            GolfBallTester tester = FindFirstObjectByType<GolfBallTester>();
            if (tester != null) tester.stageManager = this;
            completedStageShotCounts = new int[stages.Length];
            StartStage(startingStage - 1);
        }

        private void Update()
        {
            if (showHeatWarning)
            {
                heatWarningTime -= Time.deltaTime;
                if (heatWarningTime <= 0f) showHeatWarning = false;
            }
        }

        public bool TryRegisterShot()
        {
            if (!CanShoot || !player.GetComponent<GolfBall>().IsStopped) return false;
            currentShotCount++;
            player.CaptureLastShot();
            return true;
        }

        public void SetHeatCue(SnowHeatAttack attack, float remainingTime)
        {
            showHeatWarning = remainingTime > 0f;
            heatWarningTime = remainingTime;
        }

        public void TriggerBossHitHeatAttack()
        {
            if (!CanShoot) return;
            foreach (SnowHeatAttack heatAttack in stages[currentStageIndex].heatAttacks)
                heatAttack.TriggerExtraAttack();
        }

        public void HitByHeat(SnowballPlayer target)
        {
            if (!CanShoot) return;
            target.ChangeSize(target.InitialSize - target.Size);
        }

        public void ReturnToLastShot()
        {
            if (!CanShoot) return;
            currentShotCount++;
            player.RestoreLastShot();
            if (golfCamera != null) golfCamera.SnapToTarget();
        }

        public void RescueGoal(SnowGoalTree goal)
        {
            if (!CanShoot) return;
            state = SnowStageState.Rescuing;
            golfPlayer.ClearCharge();
            StartCoroutine(CompleteRescue(goal));
        }

        private IEnumerator CompleteRescue(SnowGoalTree goal)
        {
            yield return goal.Rescue();
            completedStageShotCounts[currentStageIndex] = currentShotCount;
            state = SnowStageState.StageCleared;
        }

        public void DefeatBoss(SnowBoss boss, Vector3 launchDirection)
        {
            if (!CanShoot || stages[currentStageIndex].boss != boss) return;
            state = SnowStageState.Transitioning;
            golfPlayer.ClearCharge();
            StopStageEvents(stages[currentStageIndex]);
            boss.Defeat(launchDirection);
            StartCoroutine(CompleteGame());
        }

        private IEnumerator CompleteGame()
        {
            SnowGoalTree goal = stages[currentStageIndex].goal;
            if (goal != null) yield return goal.Rescue();
            else yield return new WaitForSeconds(1.5f);
            completedStageShotCounts[currentStageIndex] = currentShotCount;
            state = SnowStageState.GameCompleted;
        }

        public void NextStage()
        {
            if (state != SnowStageState.StageCleared) return;
            StartStage(currentStageIndex + 1);
        }

        public void RestartCurrentStage()
        {
            StopAllCoroutines();
            StartStage(currentStageIndex);
        }

        public void RestartGame()
        {
            StopAllCoroutines();
            System.Array.Clear(completedStageShotCounts, 0, completedStageShotCounts.Length);
            StartStage(0);
        }

        private void StartStage(int index)
        {
            if (index >= stages.Length)
            {
                state = SnowStageState.GameCompleted;
                return;
            }

            if (currentStageIndex < stages.Length)
                StopStageEvents(stages[currentStageIndex]);

            currentStageIndex = index;
            SnowGolfStage stage = stages[index];

            for (int i = 0; i < stages.Length; i++)
            {
                GameObject root = stages[i].stageRoot;
                if (root != null && root.activeSelf != (i == index))
                    root.SetActive(i == index);
            }

            currentShotCount = 0;
            state = SnowStageState.Playing;
            showHeatWarning = false;
            golfPlayer.ClearCharge();

            foreach (SnowBreakable obstacle in stage.stageRoot.GetComponentsInChildren<SnowBreakable>(true))
                obstacle.ResetObstacle();
            foreach (SnowPickup pickup in stage.pickups)
                pickup.ResetPickup();
            foreach (SnowHeatAttack heat in stage.heatAttacks)
            {
                heat.stageManager = this;
                heat.boss = stage.boss;
                heat.ResetPattern();
                heat.StartPattern();
            }
            foreach (SnowSupply supply in stage.snowSupplies)
            {
                supply.stageManager = this;
                supply.ResetSupply();
                supply.Begin();
            }

            if (stage.goal != null)
            {
                stage.goal.stageManager = this;
                stage.goal.ResetGoal();
            }

            if (stage.boss != null)
            {
                stage.boss.stageManager = this;
                stage.boss.ResetBoss();
                if (index == stages.Length - 1) stage.boss.BeginBattle();
            }

            Vector3 start = stage.startPoint != null ? stage.startPoint.position : player.transform.position;
            player.ResetAt(start);
            if (golfCamera != null) golfCamera.SnapToTarget();
        }

        private void StopStageEvents(SnowGolfStage stage)
        {
            if (stage == null) return;
            foreach (SnowHeatAttack heat in stage.heatAttacks)
                heat.StopPattern();
            foreach (SnowSupply supply in stage.snowSupplies)
                supply.ResetSupply();
        }

        private void OnGUI()
        {
            if (stages.Length == 0 || currentStageIndex >= stages.Length) return;
            InitializeUIStyles();

            SnowGolfStage stage = stages[currentStageIndex];
            GUI.Box(new Rect(18, 18, 300, 142), "눈덩이 골프", uiBoxStyle);
            GUI.Label(new Rect(34, 49, 270, 24), stage.title, uiLabelStyle);
            GUI.Label(new Rect(34, 74, 270, 24), $"스테이지 {CurrentStageNumber} / {stages.Length}     샷 {currentShotCount}", uiLabelStyle);
            GUI.Label(new Rect(34, 99, 270, 22), $"눈덩이 크기  {player.Size:F1} / {player.MaximumSize:F1}", uiLabelStyle);
            GUI.Box(new Rect(34, 124, 260, 18), "", uiBoxStyle);
            GUI.color = new Color(0.45f, 0.82f, 1f);
            GUI.DrawTexture(new Rect(36, 126, 256 * player.Size / player.MaximumSize, 14), Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (stage.boss != null && currentStageIndex == stages.Length - 1)
            {
                GUI.Box(new Rect(18, 168, 300, 55), $"펭귄 보스  {stage.boss.Health:F0} / {stage.boss.maximumHealth:F0}", uiBoxStyle);
                GUI.Box(new Rect(34, 199, 260, 16), "", uiBoxStyle);
                GUI.color = new Color(0.7f, 0.25f, 0.2f);
                GUI.DrawTexture(new Rect(36, 201, 256 * stage.boss.HealthRatio, 12), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            if (showHeatWarning)
                GUI.Box(new Rect(Screen.width * 0.5f - 150, 28, 300, 42), $"열기 공격 예고  {heatWarningTime:F1}초", uiBoxStyle);

            if (state == SnowStageState.GameCompleted)
            {
                DrawFinalScorecard();
            }
            else if (state != SnowStageState.Playing)
                GUI.Box(new Rect(Screen.width * 0.5f - 180, Screen.height * 0.5f - 82, 360, 150), StateMessage(), uiBoxStyle);

            if (state == SnowStageState.StageCleared && currentStageIndex < stages.Length - 1)
            {
                if (DrawOutlinedButton(new Rect(Screen.width * 0.5f - 100, Screen.height * 0.5f + 5, 200, 36), "다음 스테이지"))
                    NextStage();
            }
            else if (state == SnowStageState.GameCompleted)
            {
                if (DrawOutlinedButton(new Rect(Screen.width * 0.5f - 100, Screen.height * 0.5f + 104, 200, 36), "처음부터 다시"))
                    RestartGame();
            }

            if (state != SnowStageState.Transitioning && GUI.Button(new Rect(Screen.width - 178, 18, 160, 32), "스테이지 재시작", uiButtonStyle))
                RestartCurrentStage();
        }

        private bool DrawOutlinedButton(Rect rect, string label)
        {
            bool clicked = GUI.Button(rect, label, uiButtonStyle);
            Color originalColor = GUI.color;
            GUI.color = uiTextColor;
            const float thickness = 2f;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.color = originalColor;
            return clicked;
        }

        private void DrawFinalScorecard()
        {
            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;
            GUI.Box(new Rect(centerX - 225, centerY - 150, 450, 300), "", uiBoxStyle);
            GUI.Label(new Rect(centerX - 210, centerY - 135, 420, 32), StateMessage(), uiScoreTitleStyle);

            const float cellWidth = 130f;
            const float cellGap = 10f;
            float firstCellX = centerX - (cellWidth * 3f + cellGap * 2f) * 0.5f;
            for (int i = 0; i < completedStageShotCounts.Length; i++)
            {
                Rect cell = new Rect(firstCellX + i * (cellWidth + cellGap), centerY - 82, cellWidth, 76);
                GUI.Box(cell, $"{i + 1}라운드\n{completedStageShotCounts[i]}타", uiScoreCellStyle);
            }

            int totalShots = 0;
            foreach (int shots in completedStageShotCounts)
                totalShots += shots;
            GUI.Label(new Rect(centerX - 210, centerY + 12, 420, 32), $"총 {totalShots}타", uiScoreTotalStyle);
        }

        private void InitializeUIStyles()
        {
            if (uiBoxStyle != null) return;

            uiPanelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            uiPanelTexture.SetPixel(0, 0, new Color(0.91f, 0.95f, 0.98f, 0.96f));
            uiPanelTexture.Apply();

            uiBoxStyle = new GUIStyle(GUI.skin.box);
            uiBoxStyle.normal.background = uiPanelTexture;
            uiBoxStyle.normal.textColor = uiTextColor;
            uiBoxStyle.fontStyle = FontStyle.Bold;

            uiLabelStyle = new GUIStyle(GUI.skin.label);
            uiLabelStyle.normal.textColor = uiTextColor;

            uiButtonStyle = new GUIStyle(GUI.skin.button);
            uiButtonStyle.normal.background = uiPanelTexture;
            uiButtonStyle.hover.background = uiPanelTexture;
            uiButtonStyle.active.background = uiPanelTexture;
            uiButtonStyle.focused.background = uiPanelTexture;
            uiButtonStyle.normal.textColor = uiTextColor;
            uiButtonStyle.hover.textColor = uiTextColor;
            uiButtonStyle.active.textColor = uiTextColor;
            uiButtonStyle.focused.textColor = uiTextColor;
            uiButtonStyle.fontStyle = FontStyle.Bold;

            uiScoreTitleStyle = new GUIStyle(GUI.skin.label);
            uiScoreTitleStyle.normal.textColor = uiTextColor;
            uiScoreTitleStyle.alignment = TextAnchor.MiddleCenter;
            uiScoreTitleStyle.fontSize = 20;
            uiScoreTitleStyle.fontStyle = FontStyle.Bold;

            uiScoreCellStyle = new GUIStyle(GUI.skin.box);
            uiScoreCellStyle.normal.background = uiPanelTexture;
            uiScoreCellStyle.normal.textColor = uiTextColor;
            uiScoreCellStyle.alignment = TextAnchor.MiddleCenter;
            uiScoreCellStyle.fontSize = 17;
            uiScoreCellStyle.fontStyle = FontStyle.Bold;

            uiScoreTotalStyle = new GUIStyle(uiScoreTitleStyle);
            uiScoreTotalStyle.fontSize = 18;
        }

        private string StateMessage()
        {
            switch (state)
            {
                case SnowStageState.Rescuing: return "동료 눈덩이를 구출하는 중…";
                case SnowStageState.StageCleared: return "스테이지 완료!";
                case SnowStageState.Transitioning: return "펭귄 보스 격파!";
                case SnowStageState.GameCompleted: return "눈덩이 마을을 구했습니다!";
                default: return "";
            }
        }
    }
}
