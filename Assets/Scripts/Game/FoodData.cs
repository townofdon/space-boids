using UnityEngine;

public struct FoodData
{
    public Vector2 position { get; private set; }
    public bool isAvailable { get; private set; }
    public Food.FoodType foodType { get; private set; }
    public Food foodRef { get; private set; }

    bool isInitialized;

    public void Init(Food incoming)
    {
        isInitialized = true;
        foodRef = incoming;
        foodType = foodRef.foodType;
    }

    public void Hydrate()
    {
        if (!isInitialized) throw new UnityException("FoodData has not been initialized!");

        if (foodRef == null)
        {
            isAvailable = false;
            return;
        }

        position = foodRef.transform.position;
        isAvailable = !foodRef.isEaten && foodRef.isActiveAndEnabled;
    }
}
