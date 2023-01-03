using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Player : MonoBehaviour
{   
    //��������: 
    //shift - ��������
    //speed - ��������
    [Header("Shifts")]

    [SerializeField] [Range(0, 30)] List<float> cumulativeShiftSpeeds; //�������� �� ���������� ���������

    [SerializeField] [Range(1, 10)] public List<int> shiftHps; //�� �� �� ����� ������ ��������

    [SerializeField] public List<Color> shiftColors; //������� ��� ��������� �������

    List<int> cumulativeShiftHPs = new List<int>(); //������� �� ��� ����� ��������

    int hp; // ������� ��������
    public int HP { get => hp; set {
            hp = Mathf.Clamp(value, 0, cumulativeShiftHPs[shiftNumber - 1]);
            HPChangeEvent.Invoke();
        } }

    public delegate void EmptyEventListener();
    
    // Is invoked when HP changes
    public event EmptyEventListener HPChangeEvent;
    
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

    [SerializeField] float minimumSpeed; //�������� ����������� ��������

    [SerializeField] int shiftNumber = 1;

    int currentShift = 0;

    // Is called when the shift is changed

    public event EmptyEventListener ShiftChangeEvent;
    public int CurrentShift { get => currentShift; set {
            currentShift = Mathf.Clamp(value, 0, MaxShift);
            ShiftChangeEvent.Invoke();
            thruster.material.SetColor("_Color", shiftColors[currentShift]);
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

    [SerializeField] [Range(0,10)] float accelerationModifier;

    // Calculates acceleration depending on the modifier and the difference between the current shift and the correct one
    public float Acceleration { get { return (CurrentShift == CorrectShift) ? accelerationModifier : accelerationModifier / (1 + Mathf.Abs(CurrentShift - CorrectShift)); } }


    [Header("Physics")]

    [SerializeField] float horizontalSpeed; //������������� �������� ��� ����������

    float horizontalMovement = 0; //������������� ���������� ��� ����� �����

    [SerializeField] float horizontalLimits = 1; //������������ ���� ������

    [SerializeField] [Range(0, 1)] float limitLerpSpeed; //�������� ���������� �� "������" ��� �������������� ����

    [Header("Roll")]
    [SerializeField] float horizontalRollLimits = 1.5f; //���� ������ �� ��� �����
    [SerializeField] float rollDurationInSeconds = 1; //��������� �����

    bool isRolling = false; //������ �����

    [Space(20)] [SerializeField] SpriteRenderer thruster;

    void Start()
    {
        int sum = 0; 
        foreach (int _hp in shiftHps) { //�������� ����� �������� ��
            sum += _hp;
            cumulativeShiftHPs.Add(sum);
        }
        HP = sum;
        currentSpeed = minimumSpeed;
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
            CurrentShift -= 1;
            CurrentSpeed = Mathf.Min(CurrentSpeed, MaxSpeed);
        }
        if (Input.GetButtonDown("ShiftUp"))
        {
            CurrentShift += 1;
        }
    }

    /// <summary>
    /// Resets the roll after a period of time
    /// </summary>
    IEnumerator ResetRoll() {
        yield return new WaitForSeconds(rollDurationInSeconds);
        isRolling = false;
    }


    /// <summary>
    /// Moves the player and updates the thruster, also updates the speed
    /// </summary>
    void FixedUpdate()
    {
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

        
        CurrentSpeed += Acceleration * Time.fixedDeltaTime;



        float speedDifference = CorrectShift > 0 ? cumulativeShiftSpeeds[CorrectShift - 1] : minimumSpeed;
        thruster.material.SetFloat("_tValue", Mathf.Lerp(thruster.material.GetFloat("_tValue"), (CurrentSpeed - speedDifference) / (cumulativeShiftSpeeds[CurrentShift] - speedDifference), 0.5f));        
        

    }
}
