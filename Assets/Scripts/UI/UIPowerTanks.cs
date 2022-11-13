using UnityEngine;

public class UIPowerTanks : MonoBehaviour
{
    [SerializeField] UIPowerTank blueTank;
    [SerializeField] UIPowerTank yellowTank;
    [SerializeField] UIPowerTank redTank;

    void OnEnable()
    {
        GlobalEvent.Subscribe(OnGlobalEvent);
    }

    void OnDisable()
    {
        GlobalEvent.Unsubscribe(OnGlobalEvent);
    }

    void OnGlobalEvent(GlobalEvent.type eventType)
    {
        switch (eventType)
        {
            case GlobalEvent.type.ACQUIRE_BLUE_POWER_TANK:
                blueTank.FlagAcquired();
                break;
            case GlobalEvent.type.ACQUIRE_YELLOW_POWER_TANK:
                yellowTank.FlagAcquired();
                break;
            case GlobalEvent.type.ACQUIRE_RED_POWER_TANK:
                redTank.FlagAcquired();
                break;
            case GlobalEvent.type.SELECT_BLUE_POWER_TANK:
                blueTank.SetActive(true);
                yellowTank.SetActive(false);
                redTank.SetActive(false);
                break;
            case GlobalEvent.type.SELECT_YELLOW_POWER_TANK:
                blueTank.SetActive(false);
                yellowTank.SetActive(true);
                redTank.SetActive(false);
                break;
            case GlobalEvent.type.SELECT_RED_POWER_TANK:
                blueTank.SetActive(false);
                yellowTank.SetActive(false);
                redTank.SetActive(true);
                break;
        }
    }
}
