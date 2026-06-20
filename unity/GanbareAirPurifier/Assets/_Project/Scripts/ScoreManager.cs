public class ScoreManager
{
    public int Score { get; private set; }

    public void Reset()
    {
        Score = 0;
    }

    public int AddSuccessScore(int requiredLevel, int combo)
    {
        var gained = requiredLevel * 100 + combo * 15;
        Score += gained;
        return gained;
    }
}
