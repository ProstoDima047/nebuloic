using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Player : MonoBehaviour
{   
    //��������: 
    //gear - ��������
    //speed - ��������
    [Header("Shifts")]

    [SerializeField] [Range(0, 30)] List<float> cumulativeShiftSpeeds; //�������� �� ���������� ���������

    [SerializeField] [Range(1, 10)] public List<int> shiftHps; //�� �� �� ����� ������ ��������

    [SerializeField] public List<Color> shiftColors; //������� ��� ��������� �������

    List<int> cumulativeShiftHPs = new List<int>(); //������� �� ��� ����� ��������

    int hp; // �������� ��������
    public int HP { get => hp; set {
            hp = Mathf.Clamp(value, 0, cumulativeShiftHPs[shiftNumber - 1]);
            HPChangeEvent.Invoke();
        } }
    // Is invoked when HP changes
    public event Action HPChangeEvent;
    
    // Maximum shift depending on HP
    public int MaxShift {
        get {
            for (int i = 0; i < shiftNumber; i++) {
                if (cumulativeShiftHPs[i] >= HP) {
                    return i;
                }   
            }
            return 0;
        }
    }

    [SerializeField] float minimumSpeed; //��������� ����������� ��������

    [SerializeField] public int shiftNumber = 1;

    int currentShift = 0;

    // Is called when the shift is changed

    public event Action ShiftChangeEvent;
    public int CurrentShift { get => currentShift; set {
            currentShift = Mathf.Clamp(value, 0, MaxShift);
            ShiftChangeEvent.Invoke();

        } }


    // Calculates the correct shift depending on the current speed. The correct shift will have the fastest acceleration
    public int CorrectShift { get {
            if (currentSpeed < cumulativeShiftSpeeds[0]) return 0;
            for (int i = 0; i < cumulativeShiftSpeeds.Count - 2; i++) 
            {
                if (cumulativeShiftSpeeds[i] < currentSpeed && cumulativeShiftSpeeds[i + 1] > currentSpeed) {
                    return i + 1;
                }
            }
            return CurrentShift;
        } 
    }
    
    float currentSpeed;
    
    public float CurrentSpeed { get => currentSpeed; set {
            currentSpeed = Mathf.Clamp(value, minimumSpeed, MaxSpeed);
        } }

    // Returns the maximum speed of the current shift
    public float MaxSpeed { get { return cumulativeShiftSpeeds[CurrentShift]; } }

    [SerializeField] [Range(0,10)] public float accelerationModifier;

    // Calculates acceleration depending on the modifier and the difference between the current shift and the correct one
    public float Acceleration { get {
            if (Mathf.Abs(CurrentSpeed - cumulativeShiftSpeeds[CurrentShift]) < 0.05f) return 0;
            return (CurrentShift == CorrectShift) ? accelerationModifier : accelerationModifier / (1 + Mathf.Abs(CurrentShift - CorrectShift)); } }

    public float CurrentAcceleration; 

    [Header("Physics")]

    [SerializeField] float horizontalSpeed; //������������� �������� ��� ����������

    float horizontalMovement = 0; //������������� ���������� ��� ����� �����

    [SerializeField] float horizontalLimits = 1; //������������� ���� ������

    [SerializeField] [Range(0, 1)] float limitLerpSpeed; //�������� ���������� �� "������" ��� �������������� ����

    [Header("Roll")]
    [SerializeField] float horizontalRollLimits = 1.5f; //���� ������ �� ��� �����
    [SerializeField] float rollDurationInSeconds = 1; //��������� �����

    public bool isRolling = false; //������ �����

    [SerializeField] SimpleAnimator myAnimator;

    [SerializeField] GameObject bullet;

    [SerializeField] float holdDownTimeInSeconds;

    [SerializeField] float repeatShootingInSeconds;

    Coroutine holdDownCoroutine;
    Coroutine repeatShot;

    bool perfectSwitch;

    [SerializeField] float perfectSwitchTiming;
    [SerializeField] float perfectSwitchBoost;

    public float speedTValue {
        get
        {
            float speedDifference = CorrectShift > 0 ? cumulativeShiftSpeeds[CorrectShift - 1] : minimumSpeed;
            return (CurrentSpeed - speedDifference) / (cumulativeShiftSpeeds[CurrentShift] - speedDifference);
        }
    }


    void Start()
    {
        int sum = 0; 
        foreach (int _hp in shiftHps) { //�������� ����� �������� ��
            sum += _hp;
            cumulativeShiftHPs.Add(sum);
        }
        HP = sum;
        currentSpeed = minimumSpeed;
        CurrentShift = 0;
        CurrentAcceleration = 0; 
    }

    public void TakeDamage(int hpDamage, int shiftDamage) {
        HP -= hpDamage;
        if (hpDamage > 0) myAnimator.ShakeCamera();
        if (CurrentShift > 0 && shiftDamage > 0) myAnimator.ShowDeceleration();
        CurrentShift -= shiftDamage;
        
     } 

    void Update()
    {
        GetInputs();
    }

    

    /// <summary>
    /// Gets all the needed inputs from the Input class and updates the variables
    /// </summary>
    private void GetInputs()
    {
        horizontalMovement = Input.GetAxis("Horizontal") * horizontalSpeed;
        if (Input.GetButtonDown("Roll") & !isRolling)
        {
            isRolling = true;
            StartCoroutine(ResetRoll());
        }
        if (Input.GetButtonDown("ShiftDown"))
        {
            holdDownCoroutine = StartCoroutine(StopHoldDown());
            repeatShot = StartCoroutine(RepeatedShooting());
        }
        if (Input.GetButtonUp("ShiftDown"))
        {
            if (holdDownCoroutine != null) StopCoroutine(holdDownCoroutine);
            if (repeatShot != null) StopCoroutine(repeatShot);
        }
        if (Input.GetButtonDown("ShiftUp"))
        {
            if (perfectSwitch) {
                CurrentAcceleration += perfectSwitchBoost;
                perfectSwitch = false;
            }
            CurrentShift += 1;
        }  
    }

    IEnumerator RepeatedShooting()
    {
        if (CurrentSpeed - minimumSpeed < bullet.GetComponent<Projectile>().speedReduction) yield break;
        CurrentSpeed -= bullet.GetComponent<Projectile>().speedReduction;
        Shoot();
        float speed = CurrentShift > 0 ? cumulativeShiftSpeeds[CurrentShift - 1] : minimumSpeed;
        if (CurrentSpeed < speed)
        {
            CurrentShift -= 1;
            StopCoroutine(holdDownCoroutine);
            holdDownCoroutine = StartCoroutine(StopHoldDown());
            yield break;
        }
        yield return new WaitForSeconds(repeatShootingInSeconds);
        repeatShot = StartCoroutine(RepeatedShooting());
    }


    IEnumerator StopHoldDown() {
        yield return new WaitForSeconds(holdDownTimeInSeconds);
        CurrentShift -= 1;
        if (repeatShot != null) StopCoroutine(repeatShot);
        holdDownCoroutine = StartCoroutine(StopHoldDown());
    }

    

    void Shoot() {
        var b = Instantiate(bullet, transform.position, Quaternion.identity);
        b.GetComponent<Rigidbody2D>().AddForce(Vector2.up * 0.1f);
    }

    /// <summary>
    /// Resets the roll after a period of time
    /// </summary>
    IEnumerator ResetRoll() {
        yield return new WaitForSeconds(rollDurationInSeconds);
        isRolling = false;
        transform.rotation = Quaternion.identity;
    }


    /// <summary>
    /// Moves the player and updates the thruster, also updates the speed
    /// </summary>
    void FixedUpdate()
    {
        if (isRolling) horizontalMovement *= 0.5f;
        transform.Translate(new Vector2(horizontalMovement * Time.fixedDeltaTime, 0), Space.World);
        
        float diff = Mathf.Abs(transform.position.x) - horizontalLimits;
        if (diff > 0) {
            if (!isRolling)
            {
                transform.Translate(new Vector2(-diff * limitLerpSpeed * Mathf.Sign(transform.position.x), 0), Space.World);
            }
            else if (Mathf.Abs(transform.position.x) > horizontalRollLimits)
            {
                transform.position = new Vector2(-(horizontalRollLimits - 0.1f) * Mathf.Sign(transform.position.x), transform.position.y);
            }
        }

        CurrentAcceleration = Mathf.Lerp(CurrentAcceleration, Acceleration, Time.fixedDeltaTime);
        CurrentSpeed += CurrentAcceleration * Time.fixedDeltaTime;
        if (CurrentSpeed > MaxSpeed * 0.95f && !perfectSwitch) {
            perfectSwitch = true;
            Utility.ExecuteAfterTime(ResetPerfectSwitch, perfectSwitchTiming);
        }


    }

    void ResetPerfectSwitch() {
        perfectSwitch = false;
    }
}
