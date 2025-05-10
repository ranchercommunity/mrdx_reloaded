namespace MRDX.Base.BattleSimulator;

public record struct BattleResult
{
    public int Timer;
}

public class BattleSimulation
{
    public async Task<BattleResult> Battle(Random rng)
    {
        return await Task.FromResult(new BattleResult());
    }
}