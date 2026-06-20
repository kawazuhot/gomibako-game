public class ScoreManager
{
    public int Score { get; private set; }

    public void Reset()
    {
        Score = 0;
    }

    public int AddSuccessScore(int baseScore, int combo)
    {
        var gained = baseScore + combo * 15;
        Score += gained;
        return gained;
    }
}
