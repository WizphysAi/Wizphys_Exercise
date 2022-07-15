
using UnityEngine;
using UnityEngine.UI;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine.Assertions;

public class GetUserData : MonoBehaviour
{
    [SerializeField] private string _userDataPath = "user_sheets/BetaUser";

    [SerializeField] private Text _nameText;
    [SerializeField] private Text _phoneNumber;
    [SerializeField] private Text _ageText;
    [SerializeField] private Text _height;
    [SerializeField] private Text _weight;

    private ListenerRegistration _listenerRegistration;



    //private void Start()
    //{
    //    var firestore = FirebaseFirestore.DefaultInstance;

    //    _listenerRegistration = firestore.Document(_userDataPath).Listen(snapshot => {

    //        var UserData = snapshot.ConvertTo<UserData>();

    //        _nameText.text = $"Name: {UserData.Name}";
    //        _phoneNumber.text = $"MobileNumber: {UserData.PhoneNumbert}";
    //        _ageText.text = $"Age: {UserData.Age}";
    //        _height.text = $"Height: {UserData.Height}";
    //        _weight.text = $"Weight: {UserData.Weight}";

    //    });

    //}

    //private void OnDestroy()
    //{
    //    _listenerRegistration.Stop();
    //}

    public void LoadProfileData()
    {
        var firestore = FirebaseFirestore.DefaultInstance;

        firestore.Document(_userDataPath).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            Assert.IsNull(task.Exception);
            var UserData = task.Result.ConvertTo<UserData>();

            _nameText.text = $"{UserData.Name}";
            _phoneNumber.text = $" {UserData.PhoneNumbert}";
            _ageText.text = $" {UserData.Age}";
            _height.text = $" {UserData.Height}";
            _weight.text = $" {UserData.Weight}";


        });
    }
}
