using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using FMODUnity;
using DG.Tweening;


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

    [Space]
    [Space]

    [SerializeField] float timeDelayTutorial = 2f;
    [SerializeField] float timeShowMovementInstructions = 1.5f;
    [SerializeField] float timeShowSettingsInstructions = 2f;
    [SerializeField] float timeShowWeaponsInstructions = 3f;
    [SerializeField] float timeBetweenInstructions = 1f;
    [SerializeField] float instructionFadeTime = 0.5f;
    [SerializeField] GameObject tutorialCanvas;
    [SerializeField] CanvasGroup movementInstructions;
    [SerializeField] CanvasGroup settingsInstructions;
    [SerializeField] CanvasGroup weaponsInstructions;

    [Space]
    [Space]

    [SerializeField] StudioEventEmitter shotSound;
    [SerializeField] StudioEventEmitter switchSound;

    ParticleSystem.MainModule leftFXModule;
    ParticleSystem.MainModule rightFXModule;
    ParticleSystem.EmissionModule leftEmission;
    ParticleSystem.EmissionModule rightEmission;

    const int BLUE = 0;
    const int YELLOW = 1;
    const int RED = 2;

    Rigidbody2D rb;
    SettingsMenu settings;

    Vector2 move;
    Vector2 look;
    Quaternion desiredRotation;

    float timeElapsedSinceLastShot = float.MaxValue;
    bool hasReceivedWeaponsInstructions = false;

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
                AcquireLauncher(BLUE);
                break;
            case GlobalEvent.type.ACQUIRE_YELLOW_POWER_TANK:
                AcquireLauncher(YELLOW);
                break;
            case GlobalEvent.type.ACQUIRE_RED_POWER_TANK:
                AcquireLauncher(RED);
                break;
            case GlobalEvent.type.SIMULATION_START:
                StartCoroutine(InitialTutorial());
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
        launchers[BLUE] = new FoodLauncherState(Food.FoodType.Pod);
        launchers[YELLOW] = new FoodLauncherState(Food.FoodType.Freighter);
        launchers[RED] = new FoodLauncherState(Food.FoodType.Xenon);
    }

    void Start()
    {
        desiredRotation = transform.rotation;
        StartCoroutine(UpdateScreenStats());
        GlobalEvent.Invoke(GlobalEvent.type.SELECT_BLUE_POWER_TANK);
        tutorialCanvas.transform.SetParent(null);
    }

    void Update()
    {
        Move();
        Rotate();
        ManageTrails();
        timeElapsedSinceLastShot += Time.deltaTime * Simulation.speed;
        tutorialCanvas.transform.position = transform.position;
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

    void AcquireLauncher(int index)
    {
        currentLauncherIndex = index;
        launchers[currentLauncherIndex].isAcquired = true;
        if (!hasReceivedWeaponsInstructions)
        {
            hasReceivedWeaponsInstructions = true;
            StartCoroutine(WeaponsTutorial());
        }
    }

    void TryToSwitchWeapon()
    {
        int nextIndex = GetNextWeaponIndex();
        if (nextIndex == currentLauncherIndex) return;

        switchSound.Play();
        currentLauncherIndex = nextIndex;
        switch (currentLauncherIndex)
        {
            case BLUE:
                GlobalEvent.Invoke(GlobalEvent.type.SELECT_BLUE_POWER_TANK);
                break;
            case YELLOW:
                GlobalEvent.Invoke(GlobalEvent.type.SELECT_YELLOW_POWER_TANK);
                break;
            case RED:
                GlobalEvent.Invoke(GlobalEvent.type.SELECT_RED_POWER_TANK);
                break;
        }
    }

    void TryToFire()
    {
        if (timeElapsedSinceLastShot < timeBetweenShots) return;
        if (!launchers[currentLauncherIndex].isAcquired) return;
        GameObject foodPrefab = GetFoodPrefab(currentLauncherIndex);
        if (foodPrefab == null) return;

        shotSound.Play();
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

    GameObject GetFoodPrefab(int index)
    {
        switch (index)
        {
            case BLUE:
                return blueFood;
            case YELLOW:
                return yellowFood;
            case RED:
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

    IEnumerator InitialTutorial()
    {
        Tween tutorial;
        yield return new WaitForSeconds(timeDelayTutorial);
        tutorial = movementInstructions.DOFade(1f, instructionFadeTime);
        yield return tutorial.WaitForCompletion();
        yield return new WaitForSeconds(timeShowMovementInstructions);
        tutorial = movementInstructions.DOFade(0f, instructionFadeTime);
        yield return tutorial.WaitForCompletion();
        yield return new WaitForSeconds(timeBetweenInstructions);
        tutorial = settingsInstructions.DOFade(1f, instructionFadeTime);
        yield return tutorial.WaitForCompletion();
        yield return new WaitForSeconds(timeShowSettingsInstructions);
        tutorial = settingsInstructions.DOFade(0f, instructionFadeTime);
    }

    IEnumerator WeaponsTutorial()
    {
        Tween tutorial;
        tutorial = weaponsInstructions.DOFade(1f, instructionFadeTime);
        yield return tutorial.WaitForCompletion();
        yield return new WaitForSeconds(timeShowWeaponsInstructions);
        tutorial = weaponsInstructions.DOFade(0f, instructionFadeTime);
        yield return tutorial.WaitForCompletion();
    }
}
