using UnityEngine;

public class TimerManager
{
    public float TimeLeft { get; private set; }
    public bool IsFinished => TimeLeft <= 0f;

    public void Reset(float seconds)
    {
        TimeLeft = seconds;
    }

    public void Tick(float deltaTime)
    {
        TimeLeft = Mathf.Max(0f, TimeLeft - deltaTime);
    }

    public void ApplyPenalty(float seconds)
    {
        TimeLeft = Mathf.Max(0f, TimeLeft - seconds);
    }
}
