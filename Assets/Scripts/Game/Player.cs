using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


// NOTE - I combined all of the player functionality into a single script for sheer convenience.
// In an actual game I would have definitely split out various functions into separate script,
// e.g. control, movement, FX, etc.

public class Player : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float cardinality = 45f;

    [Space]
    [Space]

    [SerializeField] ParticleSystem leftTrail;
    [SerializeField] ParticleSystem rightTrail;
    [SerializeField] float exhaustRate = 20f;
    [SerializeField] AnimationCurve velocityExhaustMod = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Space]
    [Space]

    [SerializeField] float foodLaunchSpeed = 10f;
    [SerializeField] float timeBetweenShots = .2f;
    [SerializeField] float launchVariance = .5f;
    [SerializeField] GameObject blueFood;
    [SerializeField] GameObject yellowFood;
    [SerializeField] GameObject redFood;

    ParticleSystem.MainModule leftFXModule;
    ParticleSystem.MainModule rightFXModule;
    ParticleSystem.EmissionModule leftEmission;
    ParticleSystem.EmissionModule rightEmission;

    Rigidbody2D rb;
    SettingsMenu settings;

    Vector2 move;
    Vector2 look;
    Quaternion desiredRotation;

    float timeElapsedSinceLastShot = float.MaxValue;

    FoodLauncherState[] launchers = new FoodLauncherState[3];
    int currentLauncherIndex = (int)Food.FoodType.Pod;

    // PlayerInput message
    void OnMove(InputValue value)
    {
        move = value.Get<Vector2>();
    }

    // PlayerInput message
    void OnLook(InputValue value)
    {
        look = value.Get<Vector2>();
    }

    void OnSwitchWeapon(InputValue value)
    {
        if (!value.isPressed) return;
        TryToSwitchWeapon();
    }

    // PlayerInput message
    void OnFire(InputValue value)
    {
        if (!value.isPressed) return;
        TryToFire();
    }

    // PlayerInput message
    void OnSettings(InputValue value)
    {
        settings.ToggleMenu();
    }

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
                AcquireLauncher(Food.FoodType.Pod);
                break;
            case GlobalEvent.type.ACQUIRE_YELLOW_POWER_TANK:
                AcquireLauncher(Food.FoodType.Freighter);
                break;
            case GlobalEvent.type.ACQUIRE_RED_POWER_TANK:
                AcquireLauncher(Food.FoodType.Xenon);
                break;
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        settings = FindObjectOfType<SettingsMenu>();
        leftEmission = leftTrail.emission;
        rightEmission = rightTrail.emission;
        leftEmission.rateOverTime = 0f;
        rightEmission.rateOverTime = 0f;
        launchers[(int)Food.FoodType.Pod] = new FoodLauncherState(Food.FoodType.Pod);
        launchers[(int)Food.FoodType.Xenon] = new FoodLauncherState(Food.FoodType.Xenon);
        launchers[(int)Food.FoodType.Freighter] = new FoodLauncherState(Food.FoodType.Freighter);
    }

    void Start()
    {
        desiredRotation = transform.rotation;
        StartCoroutine(UpdateScreenStats());
        GlobalEvent.Invoke(GlobalEvent.type.SELECT_BLUE_POWER_TANK);
    }

    void Update()
    {
        Move();
        Rotate();
        ManageTrails();
        timeElapsedSinceLastShot += Time.deltaTime * Simulation.speed;
    }

    void Move()
    {
        if (Simulation.speed <= 0.1f + Mathf.Epsilon) rb.velocity = Vector2.zero;
        rb.velocity = move * moveSpeed * Simulation.speed;
    }

    void Rotate()
    {
        if (rb.velocity.magnitude <= Mathf.Epsilon) return;
        float angle = Vector2.SignedAngle(Vector2.up, rb.velocity);
        angle = angle - angle % cardinality;
        desiredRotation = Quaternion.Euler(0f, 0f, angle);
        transform.rotation = desiredRotation;
    }

    void ManageTrails()
    {
        float exhaust = exhaustRate * velocityExhaustMod.Evaluate(Mathf.Clamp01(rb.velocity.magnitude / moveSpeed));
        leftEmission.rateOverTime = exhaust;
        rightEmission.rateOverTime = exhaust;
    }

    void AcquireLauncher(Food.FoodType foodType)
    {
        currentLauncherIndex = (int)foodType;
        launchers[currentLauncherIndex].isAcquired = true;
    }

    void TryToSwitchWeapon()
    {
        int nextIndex = GetNextWeaponIndex();
        if (nextIndex == currentLauncherIndex) return;

        // TODO: PLAY SOUND

        currentLauncherIndex = nextIndex;
        switch (launchers[currentLauncherIndex].type)
        {
            case Food.FoodType.Pod:
                GlobalEvent.Invoke(GlobalEvent.type.SELECT_BLUE_POWER_TANK);
                break;
            case Food.FoodType.Xenon:
                GlobalEvent.Invoke(GlobalEvent.type.SELECT_YELLOW_POWER_TANK);
                break;
            case Food.FoodType.Freighter:
                GlobalEvent.Invoke(GlobalEvent.type.SELECT_RED_POWER_TANK);
                break;
        }
    }

    void TryToFire()
    {
        if (timeElapsedSinceLastShot < timeBetweenShots) return;
        if (!launchers[currentLauncherIndex].isAcquired) return;
        GameObject foodPrefab = GetFoodPrefab(launchers[currentLauncherIndex].type);
        if (foodPrefab == null) return;

        // TODO: PLAY SOUND

        timeElapsedSinceLastShot = 0f;
        GameObject food = Instantiate(foodPrefab, transform.position, Quaternion.identity);
        Rigidbody2D foodPhysics = food.GetComponent<Rigidbody2D>();
        Vector2 heading = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-launchVariance, launchVariance)) * transform.up;
        foodPhysics.velocity = rb.velocity * 0.7f + heading * foodLaunchSpeed;
        foodPhysics.AddTorque(foodLaunchSpeed * UnityEngine.Random.Range(-1f, 1f));
    }

    int GetNextWeaponIndex()
    {
        int nextIndex = currentLauncherIndex;
        for (int i = currentLauncherIndex; i < currentLauncherIndex + launchers.Length; i++)
        {
            nextIndex = (nextIndex + 1) % launchers.Length;
            if (launchers[nextIndex].isAcquired) return nextIndex;
        }
        return nextIndex;
    }

    GameObject GetFoodPrefab(Food.FoodType foodType)
    {
        switch (foodType)
        {
            case Food.FoodType.Pod:
                return blueFood;
            case Food.FoodType.Xenon:
                return yellowFood;
            case Food.FoodType.Freighter:
                return redFood;
        }
        return null;
    }

    IEnumerator UpdateScreenStats()
    {
        while (true)
        {
            Utils.InvalidateScreenStats();
            yield return new WaitForSeconds(1f);
        }
    }
}
