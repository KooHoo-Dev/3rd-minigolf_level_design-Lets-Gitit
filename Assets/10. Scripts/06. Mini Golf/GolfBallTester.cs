using System;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Study3D.MiniGolf
{
    public class GolfBallTester : MonoBehaviour
    {
        public GolfBall ball;
        public Vector3 startPosition;
        public SnowStageManager stageManager;

        private void Update()
        {
            if (SimpleInput.GetKeyDown(Key.Digit1))
            {
                if (stageManager == null) ball.ReturnToLasShotPosition();
                else stageManager.ReturnToLastShot();
            }
            if (SimpleInput.GetKeyDown(Key.Digit2))
            {
                if (stageManager == null) ball.PlaceAt(startPosition);
                else stageManager.RestartCurrentStage();
            }
        }
    }
}
