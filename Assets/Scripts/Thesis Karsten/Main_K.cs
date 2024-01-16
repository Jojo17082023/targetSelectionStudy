using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Tobii.XR;
using Tobii.XR.Examples.GettingStarted;
using System.IO;
using System;
using System.Globalization;
using System.Linq;
using QuestionnaireToolkit.Scripts;


public class Main_K : MonoBehaviour
{

    public enum GenderOptions { Male, Female, Other, DoNotWantToSpecify };
    public enum RecruitmentOption { Student, Staff, FamilyAndFriends, InvitedExternal, Other };

    [Header("Demographics")]
    public int SubjectID;
    public int Age;
    public GenderOptions Gender;
    public RecruitmentOption Recruitment;
    private StreamWriter fileWriter;

    [Header("Logging Paths")]
    public string loggingPath = @"Assets/Results/Logs/";
    public string questionnairePath = @"Assets/Results/Questionnaires/";
    private readonly long applicationStartTimestamp;



    [SerializeField]
    public bool hasConditionFinishedFlag;
    public GameObject[] gameObjectsArray;
    [SerializeField] private TextMeshProUGUI countdownText; // Assign this in the Inspector
    public GameObject endGameObject;
    private float countdownTime = 5.0f; // Starting countdown time
    public GameObject[] targets; // Array of target GameObjects
    public int currentTargetIndex = 0; // Index of the current target
    private int sizeAdjustmentState = 0; // Neue Variable zur Speicherung des Zustandes für AdjustTargetSizes()
    private const float minTargetSize = 0.5f; // Minimale Größe der Targets
    private const float maxTargetSize = 3.0f; // Maximale Größe der Targets
    private float currentRoundTargetSize; // Aktuelle Größe der Targets für die Runde
    private Vector3 startPoint;
    private float movementStartTime; // Variable für die Startzeit der Bewegung
    private float hitTime;

    [Header("Conditions")]
    public int[] conditionOrder;
    public int currentConditionOrderIndex = 0;
    public int totalConditions = 16; // Anzahl der verschiedenen Conditions
    public int conditionInfo = 0;

    public int roundNumber = 0; // Rundennummber d.h. bin ich bei Konditon1 oder Kondition 2 (Runde)
    private const int totalRounds = 1; // Die 9 Targets durchlaufen 7 mal mit der bestimmten Kondition
    private const int totalTargets = 9; // Anzahl der Ziele
    private int currentConditionIndex = 0;
    private bool isCurrentlyVibrating = false;
    private bool isCurrentlyEmittingEMS = false;
    private bool isCurrentlyEMS_Vibration = false;



    public GameObject m_Dot;
    public float m_RaycastDefaultLength = 500.0f;
    private string _colorChangeableTag = "ColorChangeable"; // Der Tag für Objekte, die eingefärbt werden sollen
    private GameObject lastHitObject = null;
    Dictionary<GameObject, bool> targetsHitStatus = new Dictionary<GameObject, bool>();

    [Header("Condition States")]
    public bool isActiveEMS;
    public bool isActiveVibration;
    public bool isActiveVisuals;
    public bool isActiveEyeGaze;
    public bool isActiveHeadRaycast;

    [Header("Skripte")]
    public ExperimentController experimentController;
    public PluxUnityInterface pluxUnityInterface;
    public SerialController serialController;
    public QuestionnaireManager questionnaireManager;


    // Start is called before the first frame update
    void Start()
    {
        conditionOrder = CalculateLatinSquareOrder(SubjectID, totalConditions);
        InitializeConditions();
        // logEvent(targets[currentTargetIndex]);
        fileWriter = new StreamWriter(loggingPath + "eventLog_" + System.DateTime.Now.Ticks + ".csv");
        fileWriter.AutoFlush = true;
        fileWriter.WriteLine("TimeStamp;SubjectID;Gender;Age;Recruitment;Condition;hitObject.name" +
            "HitTarget;MaxEMG;MinEMG;Target_X_Position;Target_Y_Position;Target_Z_Position;Target_size;Fitts_Law_indexDifficulty;MT;movementTime");

        conditionOrder = CalculateLatinSquareOrder(SubjectID, totalConditions: 16);

        serialController = GameObject.Find("SerialController").GetComponent<SerialController>();
        hasConditionFinishedFlag = true;


        // ActivateCondition(currentConditionIndex); // Aktiviert "Condition_1" beim Start

    }

    // Update is called once per frame
    void Update()
    {
        //AddLog();

        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("Sending EMS");
            serialController.SendSerialMessage("A");
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            Debug.Log("Sending Vibration");
            serialController.SendSerialMessage("Z");
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log("Sending EMS & Vibration");
            serialController.SendSerialMessage("B");
        }

        if (hasConditionFinishedFlag)
        {
            Debug.Log("Vor GetNextCondition: " + gameObjectsArray[0].activeSelf);
            //GetNextCondition();
            SetFeedbackOptionsBasedOnCondition(conditionOrder[currentConditionIndex]);
        }

        if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            Debug.Log("START!");
            StartCoroutine(Countdown());
        }

        ToggleRaycast(isActiveHeadRaycast);
        ToggleEyeTracking(isActiveEyeGaze);

    }
    private int[] CalculateLatinSquareOrder(int participantId, int totalConditions)
    {
        int[] order = new int[totalConditions];

        // Erzeugen Sie eine Liste von Konditionen in zufälliger Reihenfolge für jeden Teilnehmer
        List<int> conditionList = Enumerable.Range(1, totalConditions).ToList();
        System.Random random = new System.Random(participantId); // Verwenden Sie die Teilnehmer-ID als Seed für die Zufallszahlengenerierung
        conditionList = conditionList.OrderBy(x => random.Next()).ToList();

        for (int i = 0; i < totalConditions; i++)
        {
            order[i] = conditionList[i];
        }

        return order;
    }


    // Diese Methode initialisiert die Conditions basierend auf der berechneten Reihenfolge
    private void InitializeConditions()
    {
        foreach (GameObject condition in gameObjectsArray)
        {
            condition.SetActive(false);
        }
        gameObjectsArray[conditionOrder[currentConditionOrderIndex] - 1].SetActive(true);
    }

    private void SetFeedbackOptionsBasedOnCondition(string name)
    {
        throw new NotImplementedException();
    }

    void SetTargetColor(GameObject target, Color color)
    {
        Renderer targetRenderer = target.GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            targetRenderer.material.color = color; // Setze die Farbe des Materials
        }
    }
    // Setzt die Feedback-Optionen basierend auf der Condition-Nummer
    private int SetFeedbackOptionsBasedOnCondition(int conditionNumber)
    {


        switch (conditionNumber)
        {

            case 1:
                SetCondition(false, false, false, true);
                Debug.Log("Case 1");
                conditionInfo = 1;

                break;
            case 2:
                SetCondition(true, false, false, false);
                Debug.Log("Case 2");
                conditionInfo = 2;

                break;
            case 3:
                SetCondition(false, true, false, true);
                Debug.Log("Case 3");
                conditionInfo = 3;

                break;
            case 4:
                SetCondition(true, true, false, false);
                Debug.Log("Case 4");
                conditionInfo = 4;

                break;
            case 5:
                SetCondition(false, false, true, true);
                Debug.Log("Case 5");
                conditionInfo = 5;

                break;
            case 6:
                SetCondition(true, false, true, false);
                Debug.Log("Case 6");
                conditionInfo = 6;

                break;
            case 7:
                SetCondition(false, true, true, true);
                Debug.Log("Case 7");
                conditionInfo = 7;

                break;
            case 8:
                SetCondition(true, true, true, false);
                Debug.Log("Case 8");
                conditionInfo = 8;

                break;
            case 9:
                SetCondition(false, false, false, false);
                Debug.Log("Case 9");
                conditionInfo = 9;

                break;
            case 10:
                SetCondition(true, false, false, true);
                Debug.Log("Case 10");
                conditionInfo = 10;

                break;
            case 11:
                SetCondition(false, true, false, false);
                Debug.Log("Case 11");
                conditionInfo = 11;

                break;
            case 12:
                SetCondition(true, true, false, true);
                Debug.Log("Case 12");
                conditionInfo = 12;

                break;
            case 13:
                SetCondition(false, false, true, false);
                Debug.Log("Case 13");
                conditionInfo = 13;

                break;
            case 14:
                SetCondition(true, false, true, true);
                Debug.Log("Case 14");
                conditionInfo = 14;

                break;
            case 15:
                SetCondition(false, true, true, false);
                Debug.Log("Case 15");
                conditionInfo = 15;

                break;
            case 16:
                SetCondition(true, true, true, true);
                Debug.Log("Case 16");
                conditionInfo = 16;

                break;
            default:
                Debug.Log("Invalid condition number: " + conditionNumber);
                break;
        }
        Debug.Log("Aktuelle Condition" + conditionInfo);
        return conditionInfo;

    }
    private IEnumerator Countdown()
    {
        // Ensure countdownTime is reset to 5 at the start
        countdownTime = 5.0f;

        while (countdownTime > 0)
        {
            countdownText.text = countdownTime.ToString("F0"); // Display the countdown as a whole number
            yield return new WaitForSeconds(1.0f);
            countdownTime -= 1.0f;
        }

        countdownText.text = "";
        ActivateNextTarget();
    }

    private void ResetTargetColors()
    {
        foreach (var target in targets)
        {
            SetTargetColor(target, Color.white);
        }
    }

    public void TargetSelected(GameObject selectedTarget)
    {
        //Hinzugefügt für die Fitts Law berechnung
        if (currentTargetIndex > 0)
        {
            startPoint = targets[currentTargetIndex - 1].transform.position;
        }
        else
        {
            startPoint = Camera.main.transform.position;
        }
        //-----------------------------------------

        if (selectedTarget == targets[currentTargetIndex])
        {
            hitTime = Time.time; // Zeitpunkt des Treffens des Ziels
            Debug.Log("hitTime: " +hitTime);
            float movementTime = hitTime - movementStartTime; // Bewegungszeit berechnen
            logEvent(selectedTarget, movementTime);
            selectedTarget.SetActive(false); // Deaktiviere das aktuelle Ziel
            currentTargetIndex++;

            if (currentTargetIndex < targets.Length)
            {
                ActivateNextTarget();
            }
            else
            {
                ResetTargetColors(); // Setze die Farben aller Ziele zurück

                currentTargetIndex = 0; // Setze den Zielindex zurück
                roundNumber++;

                if (roundNumber < totalRounds)
                {
                    StartNewRound(); // Starte eine neue Runde
                }
                else
                {
                    // Alle Runden in der aktuellen Condition wurden abgeschlossen
                    currentConditionIndex++; // Wechsle zur nächsten Condition
                    if (currentConditionIndex < gameObjectsArray.Length)
                    {
                        questionnaireManager.ShowQuestionnaire(0);
                       
                    }
                    else
                    {
                        
                        // Logik für das Ende des Experiments

                        Debug.Log("Questionnaire");
                    }

                }
            }
        }
    }
    public void initiateNextCondition()
    {
        roundNumber = 0; // Setze die Rundenzahl für die nächste Condition zurück
        ActivateCondition(currentConditionIndex); // Aktiviere die nächste Condition
        StartNewRound(); // Starte die erste Runde der neuen Condition
    }
    /*private void InitializeConditions()
    {
        // Deaktiviert alle Conditions zu Beginn außer "Condition_1"
        foreach (GameObject condition in gameObjectsArray)
        {
            condition.SetActive(false);
        }
        gameObjectsArray[0].SetActive(true); // Stellt sicher, dass "Condition_1" aktiviert ist
    }*/

    private void ActivateCondition(int index)
    {
        for (int i = 0; i < gameObjectsArray.Length; i++)
        {
            gameObjectsArray[i].SetActive(i == index);
        }

        currentConditionIndex = index;
        roundNumber = 0; // Setzen Sie die Rundenzahl für die neue Condition zurück
        AdjustTargetSizes(); // Passt die Größe der Targets für die neue Condition an
        currentConditionIndex = index;
        // GetNextCondition();
        SetFeedbackOptionsBasedOnCondition(conditionOrder[currentConditionIndex]);
    }
    private void AdjustTargetSizes()
    {
        foreach (var target in targets)
        {
            target.transform.localScale = new Vector3(currentRoundTargetSize, currentRoundTargetSize, currentRoundTargetSize);
        }
    }
    private void StartNewRound()
    {
        currentRoundTargetSize = UnityEngine.Random.Range(minTargetSize, maxTargetSize); // Bestimme eine zufällige Größe für diese Runde
        AdjustTargetSizes(); // Setze die Größe aller Targets für die neue Runde
        InitializeTargets(); // Initialisiere die Targets für die neue Runde
    }


    private float CalculateScaleFactor(int round)
    {
        float startValue = 2.0f; // Twice the size
        float endValue = 0.5f; // Half the size
        int totalRounds = 7;

        // Linear interpolation from startValue to endValue over totalRounds
        return startValue + ((float)round / (totalRounds - 1)) * (endValue - startValue);
    }

    private void ActivateNextTarget()
    {
        if (currentTargetIndex < targets.Length)
        {
            targets[currentTargetIndex].SetActive(true);
            movementStartTime = Time.time;// Startzeit der Bewegung zum Ziel
            Debug.Log("movmentStartTime: "+ movementStartTime);
        }

    }
    // Hier werden die Targets vorbereitet
    private void InitializeTargets()
    {
        foreach (var target in targets)
        {
            target.SetActive(false);
        }
        targets[0].SetActive(true); // Activate the first target
    }

    /*  public void GetNextCondition()
      {
          Debug.Log("GetNextCondition aufgerufen.");
          foreach (GameObject obj in gameObjectsArray)
          {
              Debug.Log("Überprüfe GameObject im Array: " + obj.name + ", aktiv: " + obj.activeSelf);
              if (obj.activeSelf)
              {
                  string name = obj.name;
                  int conditionNumber;
                  if (int.TryParse(name.Substring(name.IndexOf('_') + 1), out conditionNumber))
                  {
                      switch (conditionNumber)
                      {
                          case 1:
                              SetCondition(false, false, false, true);
                              Debug.Log("Case 1: false, false, false, true");
                              break;
                          case 2:
                              SetCondition(true, false, false, false);
                              Debug.Log("Case 2: true, false, false, false");
                              break;
                          case 3:
                              Debug.Log("Case 3: false, true, false, true");
                              SetCondition(false, true, false, true);
                              break;
                          case 4:
                              SetCondition(true, true, false, false);
                              break;
                          case 5:
                              SetCondition(false, false, true, true);
                              break;
                          case 6:
                              SetCondition(true, false, true, false);
                              break;
                          case 7:
                              SetCondition(false, true, true, true);
                              break;
                          case 8:
                              SetCondition(true, true, true, false);
                              break;
                          case 9:
                              SetCondition(false, false, false, false);
                              break;
                          case 10:
                              SetCondition(true, false, false, true);
                              break;
                          case 11:
                              SetCondition(false, true, false, false);
                              break;
                          case 12:
                              SetCondition(true, true, false, true);
                              break;
                          case 13:
                              SetCondition(false, false, true, false);
                              break;
                          case 14:
                              SetCondition(true, false, true, true);
                              break;
                          case 15:
                              SetCondition(false, true, true, false);
                              break;
                          case 16:
                              SetCondition(true, true, true, true);
                              break;
                          default:
                              Debug.Log("Invalid condition number: " + conditionNumber);
                              break;
                      }
                  }
                  break;
              }
          }
      }*/

    private void SetCondition(bool ems, bool vibration, bool visuals, bool eyeGaze)
    {
        hasConditionFinishedFlag = false;
        isActiveEMS = ems;
        isActiveVibration = vibration;
        isActiveVisuals = visuals;
        isActiveEyeGaze = eyeGaze;
        isActiveHeadRaycast = !eyeGaze; // HeadRaycast is the inverse of EyeGaze

    }

    private RaycastHit CreateRaycast(float length)
    {
        RaycastHit hit;

        var pos = Camera.main.gameObject.transform.position;
        var forward = Camera.main.gameObject.transform.forward;
        Ray ray = new Ray(pos, forward);
        Physics.Raycast(ray, out hit, m_RaycastDefaultLength);

        return hit;
    }

    private void ToggleEyeTracking(bool isActiveEyeTracking)
    {
        if (isActiveEyeTracking)
        {
            // Nehmen wir an, dass Sie eine Weise haben, das derzeit vom Eye-Tracking anvisierte Objekt zu bestimmen
            GameObject currentLookedAtObject = null;
            foreach (GameObject target in targets)
            {
                if (target.GetComponent<TargetEyegazeScript>() && target.GetComponent<TargetEyegazeScript>().isLookedAt)
                {
                    currentLookedAtObject = target;
                    break;
                }
            }

            // Überprüfe, ob das letzte anvisierte Objekt nicht mehr anvisiert wird
            if (lastHitObject != null && lastHitObject != currentLookedAtObject && isActiveVisuals && System.Array.IndexOf(targets, lastHitObject) >= 0)
            {
                SetTargetColor(lastHitObject, Color.white); // Setze die Farbe des letzten Objekts auf Weiß, wenn es nicht mehr anvisiert wird
            }

            // Wenn das aktuelle Objekt ein Ziel ist
            if (currentLookedAtObject != null && System.Array.IndexOf(targets, currentLookedAtObject) >= 0)
            {
                // Überprüfen, ob das EMG ausgelöst wird
                if (pluxUnityInterface.isTriggering())
                {
                    TargetSelected(currentLookedAtObject); // Rufe die TargetSelected-Methode auf, wenn das EMG ausgelöst wird
                }

                // Färbe das anvisierte Objekt rot, wenn visuals aktiv sind
                if (isActiveVisuals)
                {
                    SetTargetColor(currentLookedAtObject, Color.red); // Färbe das anvisierte Objekt rot
                }

                // Starte oder stoppe Vibration und EMS basierend auf Anvisierung
                if (isActiveVibration && !isActiveEMS)
                {
                    Debug.Log("Vibration Only");
                    ToggleVibration(true);
                }

                if (isActiveEMS && !isActiveVibration)
                {
                    Debug.Log("EMS Only");
                    ToggleEMS(true);
                }
                if (isActiveVibration && isActiveEMS)
                {
                    Debug.Log("Both EMS and Vibration");
                    ToggleEMS_Vibration(true, true);
                }
            }
            else
            {
                // Stoppe die Vibration und EMS, wenn kein Zielobjekt mehr anvisiert wird und sie vorher aktiv waren
                if (isCurrentlyVibrating && isActiveVibration)
                {
                    ToggleVibration(false);
                }
                if (isCurrentlyEmittingEMS && isActiveEMS)
                {
                    ToggleEMS(false);
                }
                if (isCurrentlyEMS_Vibration && isActiveVibration && isActiveEMS)
                {
                    ToggleEMS_Vibration(false, false);
                }
            }

            // Aktualisiere das zuletzt anvisierte Objekt am Ende der Methode
            lastHitObject = currentLookedAtObject;
        }
    }


    private void ToggleRaycast(bool isActiveRaycast)
    {
        if (isActiveRaycast)
        {
            RaycastHit hit = CreateRaycast(m_RaycastDefaultLength);
            GameObject hitObject = null;

            // Erhalte das aktuelle anvisierte Objekt
            if (hit.collider != null)
            {
                hitObject = hit.collider.gameObject; // Das vom Raycast getroffene Objekt
                m_Dot.transform.position = hit.point;
                m_Dot.SetActive(true);
            }
            else
            {
                m_Dot.SetActive(false);
            }

            // Überprüfe, ob das letzte anvisierte Objekt nicht mehr anvisiert wird
            if (lastHitObject != null && lastHitObject != hitObject && isActiveVisuals && System.Array.IndexOf(targets, lastHitObject) >= 0)
            {
                SetTargetColor(lastHitObject, Color.white); // Setze die Farbe des letzten Objekts auf Weiß, wenn es nicht mehr anvisiert wird
            }

            // Wenn das aktuelle Objekt ein Ziel ist
            if (hitObject != null && System.Array.IndexOf(targets, hitObject) >= 0)
            {
                // Überprüfen, ob das EMG ausgelöst wird
                if (pluxUnityInterface.isTriggering())
                {
                    TargetSelected(hitObject); // Rufe die TargetSelected-Methode auf, wenn das EMG ausgelöst wird
                }

                // Färbe das anvisierte Objekt rot, wenn visuals aktiv sind
                if (isActiveVisuals)
                {
                    SetTargetColor(hitObject, Color.red); // Färbe das anvisierte Objekt rot
                }

                // Starte oder stoppe Vibration und EMS basierend auf Anvisierung, wenn Vibration aktiviert ist
                if (isActiveVibration && !isActiveEMS)
                {
                    Debug.Log("ZZZZZZZZZZZZ");
                    ToggleVibration(true);
                }

                if (isActiveEMS && !isActiveVibration)
                {
                    Debug.Log("AAAAAAAAAAAAAA");
                    ToggleEMS(true);
                }
                if (isActiveVibration && isActiveEMS)
                {
                    Debug.Log("BBBBBBBBBBBBBBB");
                    ToggleEMS_Vibration(true, true);
                }
            }
            else if (isCurrentlyVibrating && isActiveVibration)
            {
                // Stoppe die Vibration und EMS, wenn kein Zielobjekt mehr anvisiert wird und sie vorher aktiv waren
                ToggleVibration(false);
            }
            else if (isCurrentlyEmittingEMS && isActiveEMS)
            {
                ToggleEMS(false);
            }
            else if (isCurrentlyEMS_Vibration && isActiveVibration && isActiveEMS)
            {
                ToggleEMS_Vibration(false, false);
            }

            // Aktualisiere das zuletzt anvisierte Objekt am Ende der Methode
            lastHitObject = hitObject;
        }
        else
        {
            m_Dot.SetActive(false);
        }
    }

    private void ToggleVibration(bool shouldVibrate)
    {
        if (shouldVibrate && !isCurrentlyVibrating)
        {
            Debug.Log("Starting Vibration");
            serialController.SendSerialMessage("Z"); // Starte die Vibration
            isCurrentlyVibrating = true;
        }
        else if (!shouldVibrate && isCurrentlyVibrating)
        {
            Debug.Log("Stopping Vibration");
            // Hier könnte ein Befehl gesendet werden, um die Vibration zu stoppen
            isCurrentlyVibrating = false;
        }
    }
    private void ToggleEMS(bool shouldEmitEMS)
    {
        if (shouldEmitEMS && !isCurrentlyEmittingEMS)
        {
            Debug.Log("Starting EMS");
            serialController.SendSerialMessage("A"); // Starte EMS
            isCurrentlyEmittingEMS = true;
        }
        else if (!shouldEmitEMS && isCurrentlyEmittingEMS)
        {
            Debug.Log("Stopping EMS");
            // Hier könnte ein Befehl gesendet werden, um EMS zu stoppen
            isCurrentlyEmittingEMS = false;
        }
    }

    private void ToggleEMS_Vibration(bool shouldEmitEMS, bool shouldVibrate)
    {
        if (shouldEmitEMS && shouldVibrate && !isCurrentlyEmittingEMS)
        {
            Debug.Log("Starting EMS & Vibraiton");
            serialController.SendSerialMessage("B"); // Starte EMS
            isCurrentlyEmittingEMS = true;
        }
        else if (!shouldEmitEMS && !shouldVibrate && isCurrentlyEmittingEMS)
        {
            Debug.Log("Stopping EMS & Vibration");
            // Hier könnte ein Befehl gesendet werden, um EMS zu stoppen
            isCurrentlyEmittingEMS = false;
        }
    }


    public void logEvent(GameObject hitObject, float movementTime)
    {
        Vector3 targetPosition = hitObject.transform.position;
        float distanceToTarget = Vector3.Distance(startPoint, targetPosition);
        float targetSize = hitObject.transform.localScale.x;

        double a = 0.225; // Beispielwert für 'a'
        double b = 0.1175; // Beispielwert für 'b'

        double maxvalue = pluxUnityInterface.maxvalue;
        double threshold_l = pluxUnityInterface.threshold_l;
        double threshold_h = pluxUnityInterface.threshold_h;
        
        // Berechnung der Entfernung zum Ziel
        

        // Bewegungszeit: Zeit vom Start der Bewegung bis zum Treffen des Ziels
        // Annahme: Die Startzeit wird irgendwo erfasst, z.B. beim Beginn der Runde
        

        // Fitts' Law Berechnung
        double indexDifficulty = Mathf.Log((distanceToTarget / targetSize) + 1) / Mathf.Log(2); // Logarithmus Basis 2
        double MT = a + b * indexDifficulty; // Berechnung von MT

        Debug.Log("Größe Objekt: " + targetSize);
        //Debug.Log("Getroffenes Objekt: " + hitObject.name + ", Position: " + targetPosition);

        string fileOutput = System.DateTime.Now.Ticks + ";"
            + SubjectID + ";"
            + Gender + ";"
            + Age + ";"
            + Recruitment + ";"
            + conditionInfo + ";"
            + hitObject.name + ";"
            + maxvalue + ";"
            + threshold_h + ";"
            + threshold_l + ";"
            + targetPosition.x + ";"
            + targetPosition.y + ";"
            + targetPosition.z + ";"
            + targetSize + ";"
            + indexDifficulty + ";"
            + MT + ";"
            + movementTime
            ;
       // print(fileOutput);
        fileWriter.WriteLine(fileOutput);
    }
}