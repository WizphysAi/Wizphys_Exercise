using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

[RequireComponent(typeof(Dropdown))]
public class Userprefs : MonoBehaviour
{
    //save user data in playerprefs
    public InputField userName_Text;
    public InputField phoneNumber_Text;
    public InputField age_Text;
    public InputField weight_Text;
    public InputField Height_Text;
    public Dropdown gender_Bool;
    public Dropdown diabetes_Bool;
    public Dropdown surgery_Bool;
    public Dropdown language_pref;

    private void Awake()
    {
        gender_Bool = GetComponent<Dropdown>();
        diabetes_Bool = GetComponent<Dropdown>();
        surgery_Bool = GetComponent<Dropdown>();
        language_pref = GetComponent<Dropdown>();
    }

    public void SaveData()
    {
        gender_Bool.onValueChanged.AddListener(new UnityAction<int>(index =>
        {
            PlayerPrefs.SetInt("Gender", gender_Bool.value);
            PlayerPrefs.Save();
        }));

        diabetes_Bool.onValueChanged.AddListener(new UnityAction<int>(index =>
        {
            PlayerPrefs.SetInt("Diabetes", diabetes_Bool.value);
            PlayerPrefs.Save();
        }));

        gender_Bool.onValueChanged.AddListener(new UnityAction<int>(index =>
        {
            PlayerPrefs.SetInt("Surgery", surgery_Bool.value);
            PlayerPrefs.Save();
        }));

        language_pref.onValueChanged.AddListener(new UnityAction<int>(index =>
        {
            PlayerPrefs.SetInt("Language", language_pref.value);
            PlayerPrefs.Save();
        }));

        PlayerPrefs.SetString("Name", userName_Text.text);
        PlayerPrefs.SetString("PhoneNumber", phoneNumber_Text.text);
        PlayerPrefs.SetString("Age", age_Text.text);
        PlayerPrefs.SetString("Weight", weight_Text.text);
        PlayerPrefs.SetString("Height", Height_Text.text);
        PlayerPrefs.Save();

        gender_Bool.value = PlayerPrefs.GetInt("Gender", 0);
        diabetes_Bool.value = PlayerPrefs.GetInt("Diabetes", 0);
        surgery_Bool.value = PlayerPrefs.GetInt("Surgery", 0);
        language_pref.value = PlayerPrefs.GetInt("language", 0);
    }

    public void LoadDashboard()
    {
        SceneManager.LoadScene("Dashboard");
    }
}
