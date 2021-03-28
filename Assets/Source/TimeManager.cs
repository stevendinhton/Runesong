using UnityEngine;
using System.Collections;

public class TimeManager : MonoBehaviour { 

    public static ulong DateInTicks { private set; get; }
    public static TimeManager instance;

    private int secondsPerTick = 1;
    private float lastUpdateTime = 0; 

    public void Awake() {
        instance = this;
    }

    public void Update() {
        float currentTime = Time.time;

        if (currentTime - lastUpdateTime > secondsPerTick) {
            DateInTicks += (ulong) ((currentTime - lastUpdateTime) / secondsPerTick);
            lastUpdateTime = currentTime;
        }
    }
}