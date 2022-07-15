using Firebase.Firestore;

[FirestoreData]
public struct UserData
{
    [FirestoreProperty]
    public string Name { get; set; }

    [FirestoreProperty]
    public string PhoneNumbert { get; set; }

    [FirestoreProperty]
    public bool Gender { get; set; }

    [FirestoreProperty]
    public string Age { get; set; }

    [FirestoreProperty]
    public bool Diabetes { get; set; }

    [FirestoreProperty]
    public bool Surgery { get; set; }

    [FirestoreProperty]
    public string Height { get; set; }

    [FirestoreProperty]
    public string Weight { get; set; }
}
