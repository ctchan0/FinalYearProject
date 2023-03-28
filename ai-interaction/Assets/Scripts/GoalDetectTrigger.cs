using UnityEngine;
using UnityEngine.Events;

public class GoalDetectTrigger : MonoBehaviour
{

    [Header("Trigger Collider Tag To Detect")]
    public string tagToDetect = "Goal"; //collider tag to detect
    public GameObject goal;

    [Header("Goal Value")]
    public float GoalValue = 1;

    [Header("Color")]
    public int color;

    public GameObject crate;

    public bool toGoal = false;
    public float distance;

    private Collider m_col;
    private EnvController m_EnvController;

    [System.Serializable]
    public class TriggerEvent : UnityEvent<GoalDetectTrigger, float>
    {
    }

    [Header("Trigger Callbacks")]
    public TriggerEvent onTriggerEnterEvent = new TriggerEvent();
    public TriggerEvent onTriggerStayEvent = new TriggerEvent();
    public TriggerEvent onTriggerExitEvent = new TriggerEvent();

    // Start is called before the first frame update
    void Awake()
    {
        m_EnvController = GetComponentInParent<EnvController>();
        m_col = GetComponent<Collider>();
        m_col.enabled = false;
        toGoal = false;
    }

    void FixedUpdate()
    {
        // Hurry Up Penalty
        if (!toGoal)
            m_EnvController.AddGroupReward(0, -0.25f / m_EnvController.MaxEnvironmentSteps);
    }

    /* Detect Goal */
    private void OnTriggerEnter(Collider col)
    {
        if (col.CompareTag(tagToDetect))
        {
            toGoal = true;
            onTriggerEnterEvent.Invoke(this, GoalValue);
        }
    }

    private void OnTriggerStay(Collider col)
    {
        
    }

    private void OnTriggerExit(Collider col)
    {
        if (col.CompareTag(tagToDetect))
        {
            toGoal = false;
            onTriggerExitEvent.Invoke(this, GoalValue);
        }
    }


    /* Detect Pushing */
    private void OnCollisionEnter(Collision other) 
    {
        if (other.gameObject.CompareTag("Adventurer"))
        {
            // print($"{other.gameObject.gameObject.name} hit the block");
            var adventurer = other.gameObject.GetComponent<AdventurerAgent>();
            adventurer.pushing = true;
            adventurer.pushingBlock = this;
        }
    }

    private void OnCollisionExit(Collision other)
    {
        if (other.gameObject.CompareTag("Adventurer"))
        {
            // print($"{other.gameObject.gameObject.name} leave the block");
            var adventurer = other.gameObject.GetComponent<AdventurerAgent>();
            adventurer.pushing = false;
            adventurer.pushingBlock = null;
        }
    }

    public void Destroy()
    {
        this.gameObject.SetActive(false);
        m_col.enabled = false;
    }

    public void Reset(int color)
    {
        this.gameObject.SetActive(true);
        m_col.enabled = true;
        toGoal = false;
        distance = Vector3.Distance(this.goal.transform.localPosition, 
                                    this.transform.localPosition);

        var renderer = crate.GetComponent<MeshRenderer>();
        this.color = color;
        switch (color)
        {
            case 0:
                renderer.materials[1].color = Color.blue;
                break;
            case 1:
                renderer.materials[1].color = Color.red; 
                break;
            case 2:
                renderer.materials[1].color = Color.yellow;
                break;
            default:
                Debug.Log("Invalid random color");
                break;
        }
    }

    // distance from goal
    public float GetDistance()
    {
        return Vector3.Distance(this.goal.transform.localPosition, 
                                    this.transform.localPosition);
    }

    // Update is called once per frame
    void Update()
    {

    }
}