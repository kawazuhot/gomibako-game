using UnityEngine;

public class GaugeManager
{
    private const float MaxGauge = 100f;
    private const int MaxSuctionLevel = 14;

    public float Gauge { get; private set; }
    public int SuctionLevel { get; private set; } = 1;
    public float GaugeRate => Mathf.Clamp01(Gauge / MaxGauge);

    public void Reset()
    {
        Gauge = 0f;
        SuctionLevel = 1;
    }

    public bool AddGauge(float amount)
    {
        if (SuctionLevel >= MaxSuctionLevel)
        {
            Gauge = Mathf.Min(MaxGauge, Gauge + amount);
            return false;
        }

        Gauge += amount;
        if (Gauge < MaxGauge)
        {
            return false;
        }

        Gauge -= MaxGauge;
        SuctionLevel += 1;
        return true;
    }
}
