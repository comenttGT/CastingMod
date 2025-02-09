using System;
using UnityEngine;

namespace Utilities
{
    public class Timer : MonoBehaviour
    {
        public static Timer instance;
        public string ReturnedTime { get; internal set; }
        public bool shouldStopTimer { get; internal set; }
        public bool timerActive { get; internal set; }
        public float temptime { get; internal set; }
        private bool timerPaused = false;


        public void Start()
        {
            instance = this;
            temptime = -10f;
            timerActive = false;
        }

        public void StartOrStopTimer()
        {
            timerActive = !timerActive;
            if (!timerActive)
            {
                temptime = -10f;
                ReturnedTime = "";
                shouldStopTimer = false;
            }

        }

        public void UnpauseOrPauseTime() => timerPaused = !timerPaused;

        public void ImediateStopTime()
        {
            timerActive = false;
            temptime = -10f;
            ReturnedTime = "";
            shouldStopTimer = false;
        }
        public void Update()
        {
            if (timerActive)
            {
                if (!timerPaused)
                    temptime += Time.deltaTime;

                int min = ((int)temptime / 60);
                int sec = ((int)temptime % 60);
                float ms = temptime * 1000;
                ms = ms % 1000;
                if (min >= 3)
                {
                    shouldStopTimer = true;
                    ReturnedTime = "03:00:000";
                }
                else
                    ReturnedTime = String.Format("{0:00}:{1:00}:{2:000}", min, sec, ms);

            }
        }
    }
}
