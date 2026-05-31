using UnityEngine;

public class RuntimePerformanceSettings : MonoBehaviour
{
    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

#if UNITY_ANDROID
        Application.backgroundLoadingPriority = ThreadPriority.BelowNormal;
#endif
    }
}
