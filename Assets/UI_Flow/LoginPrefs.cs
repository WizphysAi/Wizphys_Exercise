using UnityEngine;
using UnityEngine.UI;

public class LoginPrefs : MonoBehaviour
{
    public InputField userName_Text;
    public InputField Email_Text;
    public InputField Password_Text;
    public InputField VerifyPassword_Text;

    public InputField LoginEmail_Text;
    public InputField LoginPassword_Text;

    public void SaveData()
    {
        PlayerPrefs.SetString("Username", userName_Text.text);
        PlayerPrefs.SetString("Email", Email_Text.text);
        PlayerPrefs.SetString("Password", Password_Text.text);
        PlayerPrefs.SetString("VerifyPassword", VerifyPassword_Text.text);

        PlayerPrefs.SetString("LoginEmail", LoginEmail_Text.text);
        PlayerPrefs.SetString("LoginPassword", LoginPassword_Text.text);

        PlayerPrefs.Save();
    }
}
