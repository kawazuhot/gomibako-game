using UnityEngine;

public class ScoreManager
{
    public int Score { get; private set; }

    public void Reset()
    {
        Score = 0;
    }

    public int AddSuccessScore(int baseScore, int combo)
    {
        var gained = Mathf.RoundToInt(baseScore * GetComboMultiplier(combo));
        Score += gained;
        return gained;
    }

    private static float GetComboMultiplier(int combo)
    {
        if (combo >= 100)
        {
            return 2.0f;
        }

        if (combo >= 70)
        {
            return 1.8f;
        }

        if (combo >= 50)
        {
            return 1.5f;
        }

        if (combo >= 40)
        {
            return 1.4f;
        }

        if (combo >= 30)
        {
            return 1.3f;
        }

        if (combo >= 20)
        {
            return 1.2f;
        }

        if (combo >= 5)
        {
            return 1.1f;
        }

        return 1.0f;
    }
}
