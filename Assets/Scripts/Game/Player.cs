using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
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

    [SerializeField] int lightLOD = 5;
    [SerializeField] Light2D spotlight;
    [SerializeField] Light2D circlelight;
    [SerializeField] SpriteRenderer spotlightFast;
    [SerializeField] SpriteRenderer circlelightFast;
    [SerializeField] Material unlitMaterial;
    [SerializeField] SpriteRenderer shipSprite;

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
    [SerializeField] Canvas tutorialCanvas;
    [SerializeField] CanvasGroup movementInstructions;
    [SerializeField] CanvasGroup settingsInstructions;
    [SerializeField] CanvasGroup photoModeInstructions;
    [SerializeField] CanvasGroup weaponsInstructions;

    [Space]
    [Space]

    [SerializeField] StudioEventEmitter shotSound;
    [SerializeField] StudioEventEmitter switchSound;
    [SerializeField] StudioEventEmitter errorSound;

    ParticleSystem.MainModule leftFXModule;
    ParticleSystem.MainModule rightFXModule;
    ParticleSystem.EmissionModule leftEmission;
    ParticleSystem.EmissionModule rightEmission;

    const int BLUE = 0;
    const int YELLOW = 1;
    const int RED = 2;

    Rigidbody2D rb;
    SettingsMenu settings;
    ScreenshotManager screenshotManager;
    PlayerInput input;
    InputActionMap inputActionMap;

    bool isInputDisabled;
    bool isPaused;

    Vector2 move;
    Vector2 look;
    Quaternion desiredRotation;
    float moveAngle;

    float timeElapsedSinceLastShot = float.MaxValue;
    bool hasReceivedWeaponsInstructions = false;

    FoodLauncherState[] launchers = new FoodLauncherState[3];
    int currentLauncherIndex = (int)Food.FoodType.Pod;
    Coroutine coTutorial;
    Tween tutorial;

    void ResetInput()
    {
        move = Vector2.zero;
        look = Vector2.zero;
    }

    // PlayerInput message
    void OnMove(InputValue value)
    {
        if (isInputDisabled) return;
        if (isPaused) return;
        move = value.Get<Vector2>();
    }

    // PlayerInput message
    void OnLook(InputValue value)
    {
        if (isInputDisabled) return;
        if (isPaused) return;
        look = value.Get<Vector2>();
    }

    void OnSwitchWeapon(InputValue value)
    {
        if (isInputDisabled) return;
        if (isPaused) return;
        if (!value.isPressed) return;
        TryToSwitchWeapon();
    }

    // PlayerInput message
    void OnFire(InputValue value)
    {
        if (isInputDisabled) return;
        if (isPaused) return;
        if (!value.isPressed) return;
        if (screenshotManager.isScreenshotModeEnabled)
        {
            screenshotManager.TryToTakeScreenshot();
        }
        else
        {
            TryToFire();
        }
    }

    // PlayerInput message
    void OnSettings(InputValue value)
    {
        if (isPaused) return;
        if (screenshotManager.isScreenshotModeEnabled) return;
        if (!value.isPressed) return;
        settings.ToggleMenu();
    }

    // PlayerInput message
    void OnToggleScreenshotMode(InputValue value)
    {
        if (isPaused) return;
        if (isInputDisabled) return;
        if (!value.isPressed) return;
        screenshotManager.ToggleScreenshotMode();
    }

    // PlayerInput message
    void OnTakeScreenshot(InputValue value)
    {
        if (isPaused) return;
        if (isInputDisabled) return;
        if (!value.isPressed) return;
        screenshotManager.TryToTakeScreenshot();
    }

    // PlayerInput message
    void OnPause(InputValue value)
    {
        if (isInputDisabled) return;
        if (!value.isPressed) return;
        if (!isPaused)
        {
            isPaused = true;
            GlobalEvent.Invoke(GlobalEvent.type.PAUSE);
        }
        else
        {
            isPaused = false;
            GlobalEvent.Invoke(GlobalEvent.type.UNPAUSE);
        }
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
                input.ActivateInput();
                coTutorial = StartCoroutine(InitialTutorial());
                break;
            case GlobalEvent.type.OPEN_MENU:
                isInputDisabled = true;
                ResetInput();
                break;
            case GlobalEvent.type.CLOSE_MENU:
                isInputDisabled = false;
                ResetInput();
                break;
            case GlobalEvent.type.DEGRADE_LOD:
                CheckLOD();
                break;
            case GlobalEvent.type.UNPAUSE:
                isPaused = false;
                break;
        }
    }

    void CheckLOD()
    {
        if (lightLOD > Perf.LOD) TurnOffLights();
    }

    void TurnOffLights()
    {
        spotlight.enabled = false;
        circlelight.enabled = false;
        spotlightFast.SetActiveAndEnable(true);
        circlelightFast.SetActiveAndEnable(true);
        shipSprite.material = unlitMaterial;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<PlayerInput>();
        settings = FindObjectOfType<SettingsMenu>();
        screenshotManager = FindObjectOfType<ScreenshotManager>();
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
        input.DeactivateInput();
        CheckLOD();
    }

    void Update()
    {
        CalcMovement();
        Move();
        Rotate();
        ManageTrails();
        timeElapsedSinceLastShot += Time.deltaTime * Simulation.speed;
        tutorialCanvas.transform.position = transform.position;
    }

    void CalcMovement() {
        moveAngle = Vector2.SignedAngle(Vector2.up, move);
        // moveAngle = moveAngle - moveAngle % cardinality;
        moveAngle = Mathf.Round(moveAngle / cardinality) * cardinality;
        desiredRotation = Quaternion.Euler(0f, 0f, moveAngle);
    }

    void Move()
    {
        if (Simulation.speed <= 0.1f + Mathf.Epsilon) rb.velocity = Vector2.zero;
        rb.velocity = desiredRotation * Vector2.up * move.magnitude * moveSpeed * Simulation.speed;
    }

    void Rotate()
    {
        if (rb.velocity.magnitude <= Mathf.Epsilon) return;
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
        // yield return tutorial.WaitForCompletion();
        // yield return new WaitForSeconds(timeBetweenInstructions);
        // tutorial = photoModeInstructions.DOFade(1f, instructionFadeTime);
        // yield return tutorial.WaitForCompletion();
        // yield return new WaitForSeconds(timeBetweenInstructions);
        // tutorial = photoModeInstructions.DOFade(0f, instructionFadeTime);
    }

    IEnumerator WeaponsTutorial()
    {
        yield return StopInitialTutorial();
        tutorial = weaponsInstructions.DOFade(1f, instructionFadeTime);
        yield return tutorial.WaitForCompletion();
        yield return new WaitForSeconds(timeShowWeaponsInstructions);
        tutorial = weaponsInstructions.DOFade(0f, instructionFadeTime);
        yield return tutorial.WaitForCompletion();
    }

    IEnumerator StopInitialTutorial()
    {
        if (coTutorial != null) StopCoroutine(coTutorial);
        if (tutorial != null) tutorial.Kill();
        if (movementInstructions.alpha > 0f)
        {
            tutorial = movementInstructions.DOFade(0f, instructionFadeTime);
            yield return tutorial.WaitForCompletion();
        }
        if (settingsInstructions.alpha > 0f)
        {
            tutorial = settingsInstructions.DOFade(0f, instructionFadeTime);
            yield return tutorial.WaitForCompletion();
        }
        yield return null;
    }
}
