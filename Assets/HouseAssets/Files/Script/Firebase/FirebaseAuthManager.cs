﻿using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
//using System.Collections.Generic;
using System;
using System.Text;
using System.Globalization;

public class FirebaseAuthManager : MonoBehaviour
{
    // Firebase variable
    [Header("Firebase")]
    public DependencyStatus dependencyStatus;
    public FirebaseAuth auth;
    public FirebaseUser user;
    //public FirebaseFirestore db;
    //Dictionary<string, object> data;
    [SerializeField] private string _PlayerPath = "player_sheets/one_cool_dude";

    [FirestoreData]
    public struct PlayerData
    {
        [FirestoreProperty]
        public string Name { get; set; }

        [FirestoreProperty]
        public string Email { get; set; }

        [FirestoreProperty]
        public int Score { get; set; }
    }

    // Login Variables
    [Space]
    [Header("Login")]
    public InputField emailLoginField;
    public InputField passwordLoginField;

    // Registration Variables
    [Space]
    [Header("Registration")]
    public InputField nameRegisterField;
    public InputField emailRegisterField;
    public InputField passwordRegisterField;
    public InputField confirmPasswordRegisterField;

    private void Awake()
    {
        // Check that all of the necessary dependencies for firebase are present on the system
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            dependencyStatus = task.Result;

            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebase();
            }
            else
            {
                Debug.LogError("Could not resolve all firebase dependencies: " + dependencyStatus);
            }
        });
    }
    
      //  private void Start()
      //  {
        //      StartCoroutine(CheckAndFixDependenciesAsync());
        //db = FirebaseFirestore.DefaultInstance;
      //  }
    /*
        private IEnumerator CheckAndFixDependenciesAsync()
        {
            var depencencyTask = FirebaseApp.CheckAndFixDependenciesAsync();

            yield return new WaitUntil(() => depencencyTask.IsCompleted);

            dependencyStatus = depencencyTask.Result;

            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebase();
                yield return new WaitForEndOfFrame();
                StartCoroutine(CheckForAutoLogin());

            }
            else
            {
                Debug.LogError("Could not resolve all firebase dependencies: " + dependencyStatus);
            }
        }
    */
    void InitializeFirebase()
    {
        //Set the default instance object
        auth = FirebaseAuth.DefaultInstance;

        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
    }

    private IEnumerator CheckForAutoLogin()
    {
        if(user != null)
        {
            var reloadUserTask = user.ReloadAsync();

            yield return new WaitUntil(() => reloadUserTask.IsCompleted);
            AutoLogin();
        }
        else
        {
            UIManager.Instance.OpenLoginPanel();
        }
    }

    private void AutoLogin()
    {
        if(user != null)
        {
            References.userName = user.DisplayName;
            UnityEngine.SceneManagement.SceneManager.LoadScene("SceneTest");
        }
        else
        {
            UIManager.Instance.OpenLoginPanel();
        }
    }

    // Track state changes of the auth object.
    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;

            if (!signedIn && user != null)
            {
                Debug.Log("Signed out " + user.UserId);
            }

            user = auth.CurrentUser;

            if (signedIn)
            {
                Debug.Log("Signed in " + user.UserId);
            }
        }
    }

    public void Login()
    {
        StartCoroutine(LoginAsync(emailLoginField.text, passwordLoginField.text));
    }

    private IEnumerator LoginAsync(string email, string password)
    {
        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);

        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            Debug.LogError(loginTask.Exception);

            FirebaseException firebaseException = loginTask.Exception.GetBaseException() as FirebaseException;
            AuthError authError = (AuthError)firebaseException.ErrorCode;


            string failedMessage = "Login Failed! Because ";

            switch (authError)
            {
                case AuthError.InvalidEmail:
                    failedMessage += "Email is invalid";
                    break;
                case AuthError.WrongPassword:
                    failedMessage += "Wrong Password";
                    break;
                case AuthError.MissingEmail:
                    failedMessage += "Email is missing";
                    break;
                case AuthError.MissingPassword:
                    failedMessage += "Password is missing";
                    break;
                default:
                    failedMessage = "Login Failed";
                    break;
            }

            Debug.Log(failedMessage);
        }
        else
        {
            user = loginTask.Result;

            Debug.LogFormat("{0} You Are Successfully Logged In", user.DisplayName);

            References.userName = user.DisplayName;
            UIManager.Instance.OpenMainMenu();
        }
    }

    public void ChangetoGameScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("SceneTest");
    }

    public void Register()
    {
        StartCoroutine(RegisterAsync(nameRegisterField.text, emailRegisterField.text, passwordRegisterField.text, confirmPasswordRegisterField.text));
    }

    private IEnumerator RegisterAsync(string name, string email, string password, string confirmPassword)
    {
        if (name == "")
        {
            Debug.LogError("User Name is empty");
        }
        else if (email == "")
        {
            Debug.LogError("email field is empty");
        }
        else if (passwordRegisterField.text != confirmPasswordRegisterField.text)
        {
            Debug.LogError("Password does not match");
        }
        else
        {
            var registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);

            yield return new WaitUntil(() => registerTask.IsCompleted);

            if (registerTask.Exception != null)
            {
                Debug.LogError(registerTask.Exception);

                FirebaseException firebaseException = registerTask.Exception.GetBaseException() as FirebaseException;
                AuthError authError = (AuthError)firebaseException.ErrorCode;

                string failedMessage = "Registration Failed! Becuase ";
                switch (authError)
                {
                    case AuthError.InvalidEmail:
                        failedMessage += "Email is invalid";
                        break;
                    case AuthError.WrongPassword:
                        failedMessage += "Wrong Password";
                        break;
                    case AuthError.MissingEmail:
                        failedMessage += "Email is missing";
                        break;
                    case AuthError.MissingPassword:
                        failedMessage += "Password is missing";
                        break;
                    default:
                        failedMessage = "Registration Failed";
                        break;
                }

                Debug.Log(failedMessage);
            }
            else
            {
                // Get The User After Registration Success
                user = registerTask.Result;

                UserProfile userProfile = new UserProfile { DisplayName = name };

                var updateProfileTask = user.UpdateUserProfileAsync(userProfile);

                yield return new WaitUntil(() => updateProfileTask.IsCompleted);

                if (updateProfileTask.Exception != null)
                {
                    // Delete the user if user update failed
                    user.DeleteAsync();

                    Debug.LogError(updateProfileTask.Exception);

                    FirebaseException firebaseException = updateProfileTask.Exception.GetBaseException() as FirebaseException;
                    AuthError authError = (AuthError)firebaseException.ErrorCode;


                    string failedMessage = "Profile update Failed! Becuase ";
                    switch (authError)
                    {
                        case AuthError.InvalidEmail:
                            failedMessage += "Email is invalid";
                            break;
                        case AuthError.WrongPassword:
                            failedMessage += "Wrong Password";
                            break;
                        case AuthError.MissingEmail:
                            failedMessage += "Email is missing";
                            break;
                        case AuthError.MissingPassword:
                            failedMessage += "Password is missing";
                            break;
                        default:
                            failedMessage = "Profile update Failed";
                            break;
                    }

                    Debug.Log(failedMessage);
                }
                else
                {
                    Debug.Log("Registration Sucessful Welcome " + user.DisplayName);

                    /*                   data = new Dictionary<string, object>
                                       {
                                           {"Player Name", nameRegisterField.text},{"Email",emailRegisterField.text}, {"score", "0"}
                                       };
                                       db.Collection("Users").Document(emailRegisterField.text).SetAsync(data).ContinueWith(task =>
                                       {
                                           if (task.IsCompleted)
                                           {
                                               Debug.Log("Sucessfully added data to the firestore");
                                           }
                                           else
                                           {
                                               Debug.Log("Error in uploading data to firestore");
                                           }
                                       });
                    */
                    var PlayerData = new PlayerData
                    {
                        Name = nameRegisterField.text,
                        Email = emailRegisterField.text,
                        Score = int.Parse("0")
                };

                    var firestore = FirebaseFirestore.DefaultInstance;
                    firestore.Collection("users").Document(user.UserId).SetAsync(PlayerData);
                    UIManager.Instance.OpenLoginPanel();
                }
            }
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
