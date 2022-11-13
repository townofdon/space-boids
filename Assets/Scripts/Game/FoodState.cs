

public class FoodLauncherState
{
    public FoodLauncherState(Food.FoodType type)
    {
        this.type = type;
    }

    public Food.FoodType type;
    public bool isAcquired = false;
}
