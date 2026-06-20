public class ComboManager
{
    public int Combo { get; private set; }

    public void Reset()
    {
        Combo = 0;
    }

    public int AddCombo()
    {
        Combo += 1;
        return Combo;
    }
}
