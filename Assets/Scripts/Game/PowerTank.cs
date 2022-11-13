using UnityEngine;

public class PowerTank : MonoBehaviour
{
    [SerializeField] Food.FoodType foodType;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        switch (foodType)
        {
            case Food.FoodType.Pod:
                GlobalEvent.Invoke(GlobalEvent.type.ACQUIRE_BLUE_POWER_TANK);
                break;
            case Food.FoodType.Xenon:
                GlobalEvent.Invoke(GlobalEvent.type.ACQUIRE_RED_POWER_TANK);
                break;
            case Food.FoodType.Freighter:
                GlobalEvent.Invoke(GlobalEvent.type.ACQUIRE_YELLOW_POWER_TANK);
                break;
        }

        // TODO: PLAY SOUND

        GetComponent<SpriteRenderer>().enabled = false;
    }
}
