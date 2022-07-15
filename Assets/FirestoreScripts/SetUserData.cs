
using UnityEngine;
using UnityEngine.UI;
using Firebase.Firestore;

public class SetUserData : MonoBehaviour
{
    [SerializeField] private string _userDataPath = "user_sheets/BetaUser";

    [SerializeField] private InputField _nameField;
    [SerializeField] private InputField _phoneNumbert;
    [SerializeField] private Dropdown _gender;
    [SerializeField] private InputField _ageField;
    [SerializeField] private Dropdown _diabetes;
    [SerializeField] private Dropdown _Surgery;
    [SerializeField] private InputField _heightField;
    [SerializeField] private InputField _weightField;
    [SerializeField] private Button _createProfileButton;

    void Start()
    {
        _createProfileButton.onClick.AddListener(() =>
        {
            var UserData = new UserData
            {
                Name = _nameField.text,
                PhoneNumbert = _phoneNumbert.text,
                Gender = _gender,
                Age = (_ageField.text),
                Diabetes = _diabetes,
                Surgery = _Surgery,
                Height = (_heightField.text),
                Weight = (_weightField.text),
            };

            var firestore = FirebaseFirestore.DefaultInstance;
            firestore.Document(_userDataPath).SetAsync(UserData);
        });
    }
}
