using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class QuestionnaireManager : MonoBehaviour
{
    public Button[] nasaTlxButtonsArray; // Assign in Inspector
    public Button[] ipqButtonsArray; // Assign in Inspector
    public Button[] costumButtonsArray; // Assign in Inspector

    public Button[][] nasaTlxButtons;
    public Button[][] ipqButtons;
    public Button[][] customButtons;

    // Panels for each questionnaire page
    public GameObject[] nasaTlxPanels;
    public GameObject[] ipqPanels;
    public GameObject[] customQuestionPanels;

    // Current state
    public int currentQuestionnaireCounter = 0; // 0 for NASA TLX, 1 for IPQ, 2 for Custom
    public int currentPage = 0;

    // Start and end time tracking for page 2
    private DateTime startTime, endTime;
    private bool isTimerActive = false;
    TimeSpan duration;

    public Button[] selectedButtons; // Array to hold the currently selected button for each question

    public string loggingPath = @"Assets/Results/Logs/";
    public string questionnairePath = @"Assets/Results/Questionnaires/";

    public Main_K main_k;
    private string currentQuestionnaireName;

    void Start()
    {
        GameObject[] panels = GetCurrentQuestionnairePanels();
        Debug.Log("panels.Length: " + panels.Length);

        selectedButtons = new Button[nasaTlxButtonsArray.Length];
        int questionsCount = 6; // Total number of questions
        int buttonsPerQuestion = 20; // Buttons per question

        nasaTlxButtons = new Button[questionsCount][];

        for (int i = 0; i < questionsCount; i++)
        {
            nasaTlxButtons[i] = new Button[buttonsPerQuestion];
            Array.Copy(nasaTlxButtonsArray, i * buttonsPerQuestion, nasaTlxButtons[i], 0, buttonsPerQuestion);
        }

        // Adding listeners to each button
        for (int questionIndex = 0; questionIndex < nasaTlxButtons.Length; questionIndex++)
        {
            for (int buttonIndex = 0; buttonIndex < nasaTlxButtons[questionIndex].Length; buttonIndex++)
            {
                Button button = nasaTlxButtons[questionIndex][buttonIndex];
                if (button != null)
                {
                    int captureQuestionIndex = questionIndex;
                    int captureButtonIndex = buttonIndex;

                    // Use the captured values in the lambda expression
                    button.onClick.AddListener(() => OnButtonClicked(captureQuestionIndex, captureButtonIndex));
                }
            }
        }

        questionsCount = 14;
        buttonsPerQuestion = 7;
        ipqButtons = new Button[questionsCount][];

        for (int i = 0; i < questionsCount; i++)
        {
            ipqButtons[i] = new Button[buttonsPerQuestion];
            Array.Copy(ipqButtonsArray, i * buttonsPerQuestion, ipqButtons[i], 0, buttonsPerQuestion);
        }

        // Adding listeners to each button
        for (int questionIndex = 0; questionIndex < ipqButtons.Length; questionIndex++)
        {
            for (int buttonIndex = 0; buttonIndex < ipqButtons[questionIndex].Length; buttonIndex++)
            {
                Button button = ipqButtons[questionIndex][buttonIndex];
                if (button != null)
                {
                    int captureQuestionIndex = questionIndex;
                    int captureButtonIndex = buttonIndex;

                    // Use the captured values in the lambda expression
                    button.onClick.AddListener(() => OnButtonClicked(captureQuestionIndex, captureButtonIndex));
                }
            }
        }

        questionsCount = 6;
        buttonsPerQuestion = 7;
        customButtons = new Button[questionsCount][];

        for (int i = 0; i < questionsCount; i++)
        {
            customButtons[i] = new Button[buttonsPerQuestion];
            Array.Copy(costumButtonsArray, i * buttonsPerQuestion, customButtons[i], 0, buttonsPerQuestion);
        }

        // Adding listeners to each button
        for (int questionIndex = 0; questionIndex < customButtons.Length; questionIndex++)
        {
            for (int buttonIndex = 0; buttonIndex < customButtons[questionIndex].Length; buttonIndex++)
            {
                Button button = customButtons[questionIndex][buttonIndex];
                if (button != null)
                {
                    int captureQuestionIndex = questionIndex;
                    int captureButtonIndex = buttonIndex;

                    // Use the captured values in the lambda expression
                    button.onClick.AddListener(() => OnButtonClicked(captureQuestionIndex, captureButtonIndex));
                }
            }
        }
        // Initialization logic
        //ShowQuestionnaire(0); // Start with the first questionnaire
    }

    void Update()
    {
        if (isTimerActive)
        {
            // Update UI or perform other actions while the timer is active
            // Example: Update a timer display on the UI
        }
    }

    public void ShowQuestionnaire(int questionnaireIndex)
    {
        // Deactivate all panels of the current questionnaire
        DeactivateCurrentQuestionnairePanels();

        if (currentQuestionnaireCounter > 2)
        {
            // End of questionnaire phase
            // Project is going back to the next condition
            main_k.initiateNextCondition();
        }

        // Update current questionnaire
        currentQuestionnaireCounter = questionnaireIndex;
        if (currentQuestionnaireCounter == 0) currentQuestionnaireName = "NasaTLX";
        if (currentQuestionnaireCounter == 1) currentQuestionnaireName = "IPQ";
        if (currentQuestionnaireCounter == 2) currentQuestionnaireName = "CUSTOM";
        // Reset current page
        currentPage = 0;

        // Activate the first panel of the new questionnaire
        GameObject[] newPanels = GetCurrentQuestionnairePanels();

        if (newPanels != null && newPanels.Length > 0)
        {
            newPanels[0].SetActive(true);
        }
    }

    private void DeactivateCurrentQuestionnairePanels()
    {
        GameObject[] currentPanels = GetCurrentQuestionnairePanels();
        if (currentPanels != null)
        {
            foreach (GameObject panel in currentPanels)
            {
                if (panel != null)
                {
                    panel.SetActive(false);
                }
            }
        }
    }


    public void NextPage()
    {
        // Logic to navigate to the next page of the current questionnaire
        // Includes error checking for unanswered questions
        // Tracking start and end time for page 2
        GameObject[] panels = GetCurrentQuestionnairePanels();
        if (panels != null && currentPage < panels.Length)
        {
            panels[currentPage].SetActive(false);
        }

        if (currentPage == 0)
        {
            panels[1].SetActive(true);
            startTime = DateTime.Now;
            isTimerActive = true;
        }

        else if (currentPage == 1)
        {
            if (!CheckAllQuestionsAnswered())
            {
                // Show error message for unanswered questions
                HighlightUnansweredQuestions();
                panels[1].SetActive(true);
                return; // Do not proceed to the next page
            }
            else
            {
                // Stop the timer when leaving the second page
                endTime = DateTime.Now;
                isTimerActive = false;

                duration = endTime - startTime;
                Debug.Log("Time spent on page: " + duration);
                List<int> selectedAnswers = CollectSelectedAnswers();
                string results = string.Join(",", selectedAnswers);
                Debug.Log("Saved Answers: " + results);
                SaveQuestionnaireResults(selectedAnswers);
                // Show the third page
                if (panels.Length > 2)
                {
                    panels[2].SetActive(true);
                }
            }
        }
        else if (currentPage == 2)
        {
            currentQuestionnaireCounter++;
            currentPage = 0;
            ShowQuestionnaire(currentQuestionnaireCounter);
            return;
        }
        // Logic to navigate to the next page
        currentPage++;        
    }
    private List<int> CollectSelectedAnswers()
    {
        Button[][] currentQuestionnaireButtons = GetCurrentQuestionnaireButtons();
        List<int> selectedAnswers = new List<int>();

        for (int questionIndex = 0; questionIndex < currentQuestionnaireButtons.Length; questionIndex++)
        {
            bool questionAnswered = false;

            for (int buttonIndex = 0; buttonIndex < currentQuestionnaireButtons[questionIndex].Length; buttonIndex++)
            {
                Button btn = currentQuestionnaireButtons[questionIndex][buttonIndex];
                if (btn != null && btn.GetComponent<Image>().color == Color.green)
                {
                    questionAnswered = true;
                    selectedAnswers.Add(buttonIndex);
                    break;
                }
            }

            if (!questionAnswered)
            {
                selectedAnswers.Add(-1); // Indicate an unanswered question
            }
        }

        return selectedAnswers;
    }

    private bool CheckAllQuestionsAnswered()
    {
        Button[][] currentQuestionnaireButtons = GetCurrentQuestionnaireButtons();

        // Iterate over each set of buttons (each question)
        for (int questionIndex = 0; questionIndex < currentQuestionnaireButtons.Length; questionIndex++)
        {
            bool questionAnswered = false;

            // Iterate over each button for the current question
            for (int buttonIndex = 0; buttonIndex < currentQuestionnaireButtons[questionIndex].Length; buttonIndex++)
            {
                Button btn = currentQuestionnaireButtons[questionIndex][buttonIndex];
                if (btn != null && btn.GetComponent<Image>().color == Color.green) // Assuming green indicates selected
                {
                    questionAnswered = true;
                    break; // A button has been selected, no need to check further
                }
            }

            if (!questionAnswered)
            {
                return false; // Found an unanswered question
            }
        }

        return true; // All questions have been answered
    }

    private void SaveQuestionnaireResults(List<int> answers)
    {
        // Constructing the file path and name with questionnaire name
        string fileName = "QuestionnaireLog_" + currentQuestionnaireName + "_" + System.DateTime.Now.Ticks + ".csv";
        string filePath = questionnairePath + fileName;

        // Creating the StreamWriter object
        using (StreamWriter fileWriter = new StreamWriter(filePath))
        {
            // Writing headers
            fileWriter.Write("SubjectID,Age,Gender,Recruitment,condition,");
            for (int i = 0; i < answers.Count; i++)
            {
                fileWriter.Write("Q" + (i + 1) + ",");
            }
            fileWriter.WriteLine();

            // Writing participant info and answers
            fileWriter.Write(main_k.SubjectID + ",");
            fileWriter.Write(main_k.Age + ",");
            fileWriter.Write(main_k.Gender + ",");
            fileWriter.Write(main_k.Recruitment + ",");
            fileWriter.Write(main_k.conditionInfo + ",");
            foreach (var answer in answers)
            {
                fileWriter.Write((answer+1) + ",");
            }
            fileWriter.WriteLine();
        }

        Debug.Log("Questionnaire answers saved to: " + filePath);
    }

    private void HighlightUnansweredQuestions()
    {
        // Logic to highlight unanswered questions
        // For example, show red-colored error messages
    }

    public void OnButtonClicked(int questionIndex, int buttonIndex)
    {
        // Logic for handling button clicks
        // Change the color of the clicked button
        // Reset the color of other buttons in the same question
        // Record the selected response
        Button[][] currentQuestionnaireButtons = GetCurrentQuestionnaireButtons();

        if (currentQuestionnaireButtons == null) return;

        Button clickedButton = currentQuestionnaireButtons[questionIndex][buttonIndex];

        // If there is already a selected button for this question, reset its color
        if (selectedButtons[questionIndex] != null)
        {
            selectedButtons[questionIndex].GetComponent<Image>().color = Color.white; // Reset color
        }

        // Update the selected button for this question
        selectedButtons[questionIndex] = clickedButton;

        // Change the color of the newly selected button
        clickedButton.GetComponent<Image>().color = Color.green;

        Debug.Log("Button clicked: Question Index = " + questionIndex + ", Button Index = " + buttonIndex);

    }

    private void GenerateCSVFile()
    {
        StringBuilder csvContent = new StringBuilder();
        csvContent.AppendLine("header"); // Replace with actual header
        // Add response data

        string fileName = questionnairePath + "eventLog_" + System.DateTime.Now.Ticks + ".csv";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllText(filePath, csvContent.ToString());
    }

    private GameObject[] GetCurrentQuestionnairePanels()
    {
        // Return the button array for the current questionnaire
        switch (currentQuestionnaireCounter)
        {
            case 0: return nasaTlxPanels;
            case 1: return ipqPanels;
            case 2: return customQuestionPanels;
            default: return null;
        }
    }

    private Button[][] GetCurrentQuestionnaireButtons()
    {
        // Return the button array for the current questionnaire
        switch (currentQuestionnaireCounter)
        {
            case 0: return nasaTlxButtons;
            case 1: return ipqButtons;
            case 2: return customButtons;
            default: return null;
        }
    }
}
