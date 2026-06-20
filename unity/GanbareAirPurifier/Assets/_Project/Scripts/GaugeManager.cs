using UnityEngine;

public class GaugeManager
{
    private const int MaxSuctionLevel = 15;
    private static readonly float[] RequiredGaugeByLevel =
    {
        0f,
        100f,
        300f,
        500f,
        800f,
        1200f,
        1700f,
        2300f,
        3000f,
        3800f,
        4800f,
        6000f,
        7500f,
        9500f,
        12000f
    };

    public float Gauge { get; private set; }
    public int SuctionLevel { get; private set; } = 1;
    public float RequiredGaugeForNextLevel => GetRequiredGauge(SuctionLevel);
    public float GaugeRate => RequiredGaugeForNextLevel > 0f ? Mathf.Clamp01(Gauge / RequiredGaugeForNextLevel) : 1f;

    public void Reset()
    {
        Gauge = 0f;
        SuctionLevel = 1;
    }

    public bool AddGauge(float amount)
    {
        if (SuctionLevel >= MaxSuctionLevel)
        {
            Gauge += amount;
            return false;
        }

        Gauge += amount;
        if (Gauge < RequiredGaugeForNextLevel)
        {
            return false;
        }

        var leveledUp = false;
        while (SuctionLevel < MaxSuctionLevel && Gauge >= RequiredGaugeForNextLevel)
        {
            Gauge -= RequiredGaugeForNextLevel;
            SuctionLevel += 1;
            leveledUp = true;
        }

        if (SuctionLevel >= MaxSuctionLevel)
        {
            Gauge = Mathf.Max(0f, Gauge);
        }

        return leveledUp;
    }

    private static float GetRequiredGauge(int level)
    {
        if (level <= 0)
        {
            return RequiredGaugeByLevel[1];
        }

        if (level >= MaxSuctionLevel)
        {
            return 0f;
        }

        return RequiredGaugeByLevel[level];
    }
}
