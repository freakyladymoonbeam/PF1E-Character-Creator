namespace Rules.Core.Data;

// Starting wealth in gp: roll (Dice)d(Sides), then multiply.
public record StartingWealth(int Dice, int Sides, int Multiplier)
{
    public int Minimum => Dice * Multiplier;
    public int Maximum => Dice * Sides * Multiplier;
    public decimal Average => Dice * (Sides + 1) / 2m * Multiplier;

    public int Roll(Random random)
    {
        int total = 0;
        for (int i = 0; i < Dice; i++)
            total += random.Next(1, Sides + 1);
        return total * Multiplier;
    }
}