namespace PixelBoard.MainServer.Configuration;

public class PadukOptions
{
    public int StartBudget { get; set; }
    public int MaxBudget { get; set; }
    public bool StartWithCheckerboard { get; set; }
    /// <summary>
    /// Increase the paint budget of every team after N ticks.
    /// Do not increase frequently if set to 0.
    /// </summary>
    public int BudgetIncreaseDelay { get; set; }
    public int BudgetIncreaseSize { get; set; }
    public int TickDelayMs { get; set; }

    public PadukOptions()
    {
        StartBudget = 10;
        MaxBudget = 10;
        BudgetIncreaseDelay = 10;
        BudgetIncreaseSize = 1;
        StartWithCheckerboard = false;
        TickDelayMs = 1000;
    }
}