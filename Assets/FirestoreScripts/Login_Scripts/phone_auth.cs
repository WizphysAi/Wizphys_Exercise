using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Auth;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class phone_auth : MonoBehaviour
{
    [SerializeField] InputField phoneNumber;
    [SerializeField] InputField CountryCode;
    FirebaseAuth firebaseAuth;
    private uint phoneAuthTimeoutMs = 60*1000;
    PhoneAuthProvider provider;
    private string VerificationId;[SerializeField] Text debug;
    [SerializeField] InputField otp;


    void Start()
    {
        firebaseAuth = FirebaseAuth.DefaultInstance;
    }

    public void login()
    {
         provider = PhoneAuthProvider.GetInstance(firebaseAuth);
        provider.VerifyPhoneNumber(CountryCode.text+ phoneNumber.text, phoneAuthTimeoutMs, null,
          verificationCompleted: (credential) => {
      // Auto-sms-retrieval or instant validation has succeeded (Android only).
      // There is no need to input the verification code.
      // `credential` can be used instead of calling GetCredential().
  },
          verificationFailed: (error) => {
      // The verification code was not sent.
      // `error` contains a human readable explanation of the problem.
  },
          codeSent: (id, token) => {
              VerificationId = id;
              debug.text = "code sent";
      // Verification code was successfully sent via SMS.
      // `id` contains the verification id that will need to passed in with
      // the code from the user when calling GetCredential().
      // `token` can be used if the user requests the code be sent again, to
      // tie the two requests together.
  },
          codeAutoRetrievalTimeOut: (id) => {
      // Called when the auto-sms-retrieval has timed out, based on the given
      // timeout parameter.
      // `id` contains the verification id of the request that timed out.
  });
    }
    public void verify_otp()
    {
        Credential credential =
    provider.GetCredential(VerificationId, otp.text);
        firebaseAuth.SignInWithCredentialAsync(credential).ContinueWith(task => {
            if (task.IsFaulted)
            {
                debug.text = ("SignInWithCredentialAsync encountered an error: " +
                               task.Exception);
                return;
            }

            FirebaseUser newUser = task.Result;
            debug.text=("User signed in successfully");
            // This should display the phone number.
            debug.text = ("Phone number: " + newUser.PhoneNumber);
            // The phone number providerID is 'phone'.
            debug.text = ("Phone provider ID: " + newUser.ProviderId);
            SceneManager.LoadScene("Profile");
        });
    }
}


