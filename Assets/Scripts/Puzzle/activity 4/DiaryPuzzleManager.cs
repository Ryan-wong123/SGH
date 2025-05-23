using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DiaryPuzzleManager : MonoBehaviour
{
    [SerializeField] TMP_InputField diary1input1;
    [SerializeField] TMP_InputField diary1input2;

    [SerializeField] TMP_InputField diary2input1;
    [SerializeField] TMP_InputField diary2input2;
    [SerializeField] TMP_InputField diary2input3;
    [SerializeField] TMP_InputField diary2input4;
    [SerializeField] TMP_InputField diary2input5;
    [SerializeField] TMP_InputField diary2input6;
    [SerializeField] TMP_InputField diary2input7;
    [SerializeField] TMP_InputField diary2input8;

    [SerializeField] TMP_InputField diary3input1;
    [SerializeField] TMP_InputField diary3input2;
    [SerializeField] TMP_InputField diary3input3;
    [SerializeField] TMP_InputField diary3input4;
    [SerializeField] TMP_InputField diary3input5;
    [SerializeField] TMP_InputField diary3input6;

    private int currentStage = 1;

    [SerializeField] GameObject diaryPanel1;
    [SerializeField] GameObject diaryPanel2;
    [SerializeField] GameObject diaryPanel3;

    public bool SubmitDiary()
    {
        switch (currentStage)
        {
            case 1:
                if (CheckDiary1Ans())
                {
                    Debug.Log("Diary 1 correct. Proceed to Diary 2.");
                    diaryPanel1.SetActive(false);
                    diaryPanel2.SetActive(true);
                    currentStage = 2;
                    return false;
                }
                else
                {
                    Debug.Log("Diary 1 incorrect. Please fix it.");
                    return false;
                }
 
            case 2:
                if (CheckDiary2Ans())
                {
                    Debug.Log("Diary 2 correct. Proceed to Diary 3.");
                    diaryPanel2.SetActive(false);
                    diaryPanel3.SetActive(true);
                    currentStage = 3;
                    return false;
                }
                else
                {
                    Debug.Log("Diary 2 incorrect. Please fix it.");
                    return false;
                }
             
            case 3:
                if (CheckDiary3Ans())
                {
                    Debug.Log("Diary 3 correct. All stages complete!");
                    diaryPanel3.SetActive(false);
                    return true;
                }
                else
                {
                    Debug.Log("Diary 3 incorrect. Please fix it.");
                    return false; 
                }
        }
        return false;
    }

    private bool CheckDiary1Ans()
    {
        string input1 = diary1input1.text.Trim().ToLower();
        string input2 = diary1input2.text.Trim().ToLower();

        bool correct1 = input1 == "32";
        bool correct2 = input2 == "32";

        diary1input1.image.color = correct1 ? Color.green : Color.red;
        diary1input2.image.color = correct2 ? Color.green : Color.red;

        return correct1 && correct2;
    }

    private bool CheckDiary2Ans()
    {
        string i1 = diary2input1.text.Trim().ToLower();
        string i2 = diary2input2.text.Trim().ToLower();
        string i3 = diary2input3.text.Trim().ToLower();
        string i4 = diary2input4.text.Trim().ToLower();
        string i5 = diary2input5.text.Trim().ToLower();
        string i6 = diary2input6.text.Trim().ToLower();
        string i7 = diary2input7.text.Trim().ToLower();
        string i8 = diary2input8.text.Trim().ToLower();

        bool c1 = i1 == "24";
        bool c2 = i2 == "8";
        bool c3 = i3 == "8";
        bool c4 = i4 == "8";
        bool c5 = i5 == "24";
        bool c6 = i6 == "8";
        bool c7 = i7 == "8";
        bool c8 = i8 == "8";

        diary2input1.image.color = c1 ? Color.green : Color.red;
        diary2input2.image.color = c2 ? Color.green : Color.red;
        diary2input3.image.color = c3 ? Color.green : Color.red;
        diary2input4.image.color = c4 ? Color.green : Color.red;
        diary2input5.image.color = c5 ? Color.green : Color.red;
        diary2input6.image.color = c6 ? Color.green : Color.red;

        return c1 && c2 && c3 && c4 && c5 && c6;
    }

    private bool CheckDiary3Ans()
    {
        string i1 = diary3input1.text.Trim().ToLower();
        string i2 = diary3input2.text.Trim().ToLower();
        string i3 = diary3input3.text.Trim().ToLower();
        string i4 = diary3input4.text.Trim().ToLower();
        string i5 = diary3input5.text.Trim().ToLower();
        string i6 = diary3input6.text.Trim().ToLower();

        bool c1 = i1 == "40";
        bool c2 = i2 == "40";
        bool c3 = i3 == "40";
        bool c4 = i4 == "40";
        bool c5 = i5 == "40";
        bool c6 = i6 == "75";

        diary3input1.image.color = c1 ? Color.green : Color.red;
        diary3input2.image.color = c2 ? Color.green : Color.red;
        diary3input3.image.color = c3 ? Color.green : Color.red;
        diary3input4.image.color = c4 ? Color.green : Color.red;
        diary3input5.image.color = c5 ? Color.green : Color.red;

        return c1 && c2 && c3 && c4 && c5;
    }
    public void ResetPuzzle()
    {
        // Reset Diary 1 Inputs
        diary1input1.text = "";
        diary1input2.text = "";
        diary1input1.image.color = Color.white;
        diary1input2.image.color = Color.white;

        // Reset Diary 2 Inputs
        diary2input1.text = "";
        diary2input2.text = "";
        diary2input3.text = "";
        diary2input4.text = "";
        diary2input5.text = "";
        diary2input6.text = "";

        diary2input1.image.color = Color.white;
        diary2input2.image.color = Color.white;
        diary2input3.image.color = Color.white;
        diary2input4.image.color = Color.white;
        diary2input5.image.color = Color.white;
        diary2input6.image.color = Color.white;

        // Reset Diary 3 Inputs
        diary3input1.text = "";
        diary3input2.text = "";
        diary3input3.text = "";
        diary3input4.text = "";
        diary3input5.text = "";

        diary3input1.image.color = Color.white;
        diary3input2.image.color = Color.white;
        diary3input3.image.color = Color.white;
        diary3input4.image.color = Color.white;
        diary3input5.image.color = Color.white;

        // Reset panels
        diaryPanel1.SetActive(true);
        diaryPanel2.SetActive(false);
        diaryPanel3.SetActive(false);

        // Reset stage tracker
        currentStage = 1;

        Debug.Log("Diary puzzle reset.");
    }


}
