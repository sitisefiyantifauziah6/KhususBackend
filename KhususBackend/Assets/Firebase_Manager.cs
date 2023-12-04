using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System.Threading.Tasks;
using System.Linq;

public class Firebase_Manager : MonoBehaviour
{
    [Header("Untuk UI")] //UI [Header("teks di sini")]
    public GameObject MainMenuUI;
    public GameObject LoginUI;
    public GameObject RegisterUI;
    public GameObject UserUI;
    public GameObject LeaderBoardUI;

    [Header("Untuk Login")] //Login stuff
    public TMP_InputField emailLogin;
    public TMP_InputField passwordLogin;
    public Toggle passwordLoginToggle;
    public Text passwordLoginToggleText;

    [SerializeField] private string Username;
    [SerializeField] private string UserID;

    [Header("Untuk Registrasi")] // reg stuff
    public TMP_InputField emailRegister;
    public TMP_InputField usernameRegister;
    public TMP_InputField passwordRegister;
    public TMP_InputField confirmPasswordRegister;
    public Toggle passwordRegisterToggle;
    public Text passwordRegisterToggleText;

    [Header("Untuk User Menu")] //usermenu stuff
    public TMP_Text UserOnUI;
    public TMP_InputField stats1;
    public TMP_InputField stats2;
    public TMP_InputField stats3;
    public Transform LeaderBoardContent;
    public GameObject LeaderBoardElement;

    [Header("Untuk Firebase")] // firebase stuff
    public DependencyStatus depStatus;
    public FirebaseUser FbUser;
    public FirebaseAuth FbAuth;
    public DatabaseReference FbDatabase;

    private void Awake()
    {
        passwordLoginToggleText = passwordLoginToggle.GetComponentInChildren<Text>(); // ambil komponen teks toggle di children
        passwordRegisterToggleText = passwordRegisterToggle.GetComponentInChildren<Text>(); // ambil komponen teks toggle di children

        StartCoroutine(CheckFirebase()); // start coroutine (nama fungsi) adalah syntax untuk memanggil fungsi bertipe IENumerator
    }

    void Start()
    {
        MainMenuUI.SetActive(true);
        LoginUI.SetActive(false);
        UserUI.SetActive(false);
        RegisterUI.SetActive(false);
        LeaderBoardUI.SetActive(false);

        passwordLoginToggle.isOn = false; // toggle ga dicentang
        passwordLoginToggleText.text = "Show password"; // ngubah komponen teks toggle
        passwordLogin.contentType = TMP_InputField.ContentType.Password; // konten input password hidden (bintang2)

        passwordRegisterToggle.isOn = false; // toggle ga dicentang
        passwordRegisterToggleText.text = "Show password"; // ngubah komponen teks toggle
        passwordRegister.contentType = TMP_InputField.ContentType.Password; // konten input password hidden (bintang2)
        confirmPasswordRegister.contentType = TMP_InputField.ContentType.Password; // konten input password hidden (bintang2)
    }

    private IEnumerator CheckFirebase()
    {
        Task<DependencyStatus> depTask = FirebaseApp.CheckAndFixDependenciesAsync(); // untuk mengecek apakah firebase online
        yield return new WaitUntil(() => depTask.IsCompleted); // menunggu sampai task dependency selesai

        depStatus = depTask.Result;

        if (depStatus == DependencyStatus.Available) // kalau firebase online
        {
            Debug.Log("Firebase online dan bisa digunakan");
            InitializeFirebase();
            yield return new WaitForEndOfFrame();

            StartCoroutine(AutoLoginCheck());
        }

        else
        {
            Debug.Log("Firebase offline, turu");
        }
    }

    public void InitializeFirebase()
    {
        FbAuth = FirebaseAuth.DefaultInstance;
        FbAuth.StateChanged += AuthStateChanged;
        FbDatabase = FirebaseDatabase.DefaultInstance.RootReference;
        AuthStateChanged(this, null);
    }

    public void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (FbAuth.CurrentUser != FbUser)
        {
            bool signedin = FbUser != FbAuth.CurrentUser && FbAuth.CurrentUser != null;

            if (!signedin && FbUser != null)
            {
                Debug.Log("Signed out " + FbUser.UserId);
            }

            FbUser = FbAuth.CurrentUser;

            if (signedin)
            {
                Debug.Log("Signed in " + FbUser.UserId);
            }
        }
    }

    private IEnumerator AutoLoginCheck()
    {
        if (FbUser != null)
        {
            Task reloadUserTask = FbUser.ReloadAsync(); // untuk memastikan usernya ada
            yield return new WaitUntil(() => reloadUserTask.IsCompleted); // membaca ulang data user dari console firebase

            AutoLogin();
        }
    }

    public void AutoLogin()
    {
        if (FbUser != null) // ekstra pengecekan untuk lebih memastikan user
        {
            UserID = FbUser.UserId;
            StartCoroutine(AutoLoginTransition());
            Debug.Log("Auto Login Success!!");
        }

        else
        {
            MainMenuUI.SetActive(false);
            LoginUI.SetActive(true);
            //dibawa ke loginUI jika autologin gagal
        }
    }

    private IEnumerator AutoLoginTransition()
    {
        yield return new WaitForSeconds(0.8f);

        Username = FbUser.DisplayName;
        Debug.Log(Username);
        UserOnUI.text = Username;

        MainMenuUI.SetActive(false);
        UserUI.SetActive(true);

        LoadUserData();
    }

    public void LoadUserData()
    {
        StartCoroutine(LoadUserDataFirebase());
    }

    private IEnumerator LoadUserDataFirebase()
    {
        Task<DataSnapshot> dbTask = FbDatabase.Child("users").Child(UserID).GetValueAsync();
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null) // kalau pembacaan database ada error
        {
            Debug.Log("gagal membaca database");
        }

        else if (dbTask == null) // kalau database nya kosong
        {
            Debug.Log("belum ada data tercatat");
            stats1.text = "0";
            stats2.text = "0";
            stats3.text = "0";
        }

        else // kemungkinan lain yaitu ada nilai di database
        {
            Debug.Log("database berhasil dimuat");
            DataSnapshot snapshot = dbTask.Result;

            stats1.text = snapshot.Child("victory").Value.ToString();
            if (string.IsNullOrEmpty(stats1.text))
            {
                stats1.text = "0";
            }

            stats2.text = snapshot.Child("defeat").Value.ToString();
            if (string.IsNullOrEmpty(stats2.text))
            {
                stats2.text = "0";
            }

            stats3.text = snapshot.Child("experience").Value.ToString();
            if (string.IsNullOrEmpty(stats3.text))
            {
                stats3.text = "0";
            }
        }
    }

    private IEnumerator UpdateUserAuth()
    {
        UserProfile profile = new UserProfile { DisplayName = Username };

        Task ProfileTask = FbUser.UpdateUserProfileAsync(profile); // untuk memastikan user sesuai atau tidak
        yield return new WaitUntil(predicate: () => ProfileTask.IsCompleted);

        if (ProfileTask.Exception != null) // kalau ada error
        {
            Debug.Log("Pengecekan user gagal :(");
        }

        else
        {
            Debug.Log("Pengecekan user berhasil");
        }
    }

    private IEnumerator UpdateUserDatabase()
    {
        Task dbTask = FbDatabase.Child("users").Child(UserID).Child("username").SetValueAsync(Username); // untuk memastikan data di database
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null)
        {
            Debug.Log("Pengecekan database user gagal :(");
        }
        else
        {
            Debug.Log("Pengecekan database user berhasil!");
        }
    }

    private IEnumerator UpdateStat1(int s1)
    {
        Task dbTask = FbDatabase.Child("users").Child(UserID).Child("victory").SetValueAsync(s1);
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null) // kalau pengiriman data gagal
        {
            Debug.Log("updata database gagal :(");
        }

        else
        {
            Debug.Log("update database berhasil!");
        }
    }

    private IEnumerator UpdateStat2(int s2)
    {
        Task dbTask = FbDatabase.Child("users").Child(UserID).Child("defeat").SetValueAsync(s2);
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null) // kalau pengiriman data gagal
        {
            Debug.Log("updata database gagal :(");
        }

        else
        {
            Debug.Log("update database berhasil!");
        }
    }

    private IEnumerator UpdateStat3(int s3)
    {
        Task dbTask = FbDatabase.Child("users").Child(UserID).Child("experience").SetValueAsync(s3);
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null) // kalau pengiriman data gagal
        {
            Debug.Log("updata database gagal :(");
        }

        else
        {
            Debug.Log("update database berhasil!");
        }
    }

    public void SaveUserData()
    {
        Debug.Log("user yang bersangkutan: " + Username);
        Debug.Log("nilai victory yang akan dikirim: " + stats1.text);
        Debug.Log("nilai defeat yang akan dikirim: " + stats2.text);
        Debug.Log("nilai experience yang akan dikirim: " + stats3.text);

        StartCoroutine(UpdateUserAuth());
        StartCoroutine(UpdateUserDatabase());

        StartCoroutine(UpdateStat1(int.Parse(stats1.text)));
        StartCoroutine(UpdateStat2(int.Parse(stats2.text)));
        StartCoroutine(UpdateStat3(int.Parse(stats3.text)));
    }

    public void ResetUserDataButton()
    {
        StartCoroutine(UpdateStat1(0));
        StartCoroutine(UpdateStat2(0));
        StartCoroutine(UpdateStat3(0));

        LoadUserData();
    }

    public void ShowLeaderBoardButton()
    {
        StartCoroutine(LoadLeaderBoardFirebase());
    }

    private IEnumerator LoadLeaderBoardFirebase()
    {
        Task<DataSnapshot> LBTask = FbDatabase.Child("users").OrderByChild("experience").GetValueAsync(); // ambil data user, urutkan pakai experience
        yield return new WaitUntil(predicate: () => LBTask.IsCompleted); // tunggu sampai selesai

        if (LBTask.Exception != null) // kalau ada error
        {
            Debug.Log("gagal menampilkan leaderboard :(");
        }

        else //  kalau tidak ada error
        {
            UserUI.SetActive(false);
            LeaderBoardUI.SetActive(true);

            Debug.Log("berhasil memuat leaderboard");
            DataSnapshot snapshot = LBTask.Result;

            foreach (Transform tableContent in LeaderBoardContent.transform)
            {
                Destroy(tableContent.gameObject); // ngosongin konten leaderboard
            }

            foreach (DataSnapshot tableSnaphot in snapshot.Children.Reverse<DataSnapshot>())
            {
                string username = tableSnaphot.Child("username").Value.ToString();
                int stat1 = int.Parse(tableSnaphot.Child("victory").Value.ToString());
                int stat2 = int.Parse(tableSnaphot.Child("defeat").Value.ToString());
                int stat3 = int.Parse(tableSnaphot.Child("experience").Value.ToString());

                GameObject LBE = Instantiate(LeaderBoardElement, LeaderBoardContent);
                LBE.GetComponent<LeaderBoardElement>().New_LBElement(username, stat1, stat2, stat3);
            }
        }
    }
    public void RegisterButton()
    {
        StartCoroutine(RegisterFirebase(emailRegister.text, usernameRegister.text, passwordRegister.text, confirmPasswordRegister.text));
    }

    private IEnumerator RegisterFirebase(string email, string username, string password, string confirmPass)
    {
        if (string.IsNullOrEmpty(email)) // cek apakah input teks kosong, true kalau kosong
        {
            Debug.Log("emailnya kosong bos");
        }

        else if (string.IsNullOrEmpty(username)) // cek apakah input teks kosong, true kalau kosong
        {
            Debug.Log("username kosong bos");
        }

        else if (password != confirmPass) // cek apakah konfirmasi password sudah cocok
        {
            Debug.Log("pastikan password cocok");
        }

        else
        {
            Task<AuthResult> registerTask = FbAuth.CreateUserWithEmailAndPasswordAsync(email, password); // daftar menggunakan email, password
            yield return new WaitUntil(() => registerTask.IsCompleted); // tunggu register sampai selesai

            if (registerTask.Exception != null) // kalau register task ada error
            {
                Debug.Log(registerTask.Exception);

                FirebaseException firebaseException = registerTask.Exception.GetBaseException() as FirebaseException; // mendeklarasi errornya apa
                AuthError authError = (AuthError)firebaseException.ErrorCode; // mengambil error code

                string failMessage = "Registration failed! Because ";

                switch (authError) // switch case itu mirip dgn if else atau if else if
                {
                    case AuthError.InvalidEmail:
                        failMessage += "Email is Invalid";
                        break;
                    case AuthError.WrongPassword:
                        failMessage += "Wrong Password";
                        break;
                    case AuthError.MissingEmail:
                        failMessage += "Email is missing, please provide email";
                        break;
                    case AuthError.MissingPassword:
                        failMessage += "Password is missing, please provide password";
                        break;
                    default:
                        failMessage = "Registration failed :(";
                        break;
                }
                Debug.Log(failMessage);
            }

            else // kalau register task tidak ada error
            {
                FbUser = registerTask.Result.User; // mengambil user dari hasil register task
                UserProfile uProfile = new UserProfile { DisplayName = username };

                Task ProfileTask = FbUser.UpdateUserProfileAsync(uProfile); // update user profile
                yield return new WaitUntil(() => ProfileTask.IsCompleted); // tunggu sampai update profile selesai

                Debug.Log(FbUser.DisplayName);
                if (ProfileTask.Exception != null) // kalau update profil user ada error
                {
                    FbUser.DeleteAsync();
                    Debug.Log(ProfileTask.Exception);
                    FirebaseException firebaseException = ProfileTask.Exception.GetBaseException() as FirebaseException;
                    AuthError authError = (AuthError)firebaseException.ErrorCode;

                    string failMessage = "Profile update failed! Because ";
                    switch (authError)
                    {
                        case AuthError.InvalidEmail:
                            failMessage += "Email is Invalid";
                            break;
                        case AuthError.WrongPassword:
                            failMessage += "Wrong Password";
                            break;
                        case AuthError.MissingEmail:
                            failMessage += "Email is missing, please provide email";
                            break;
                        case AuthError.MissingPassword:
                            failMessage += "Password is missing, please provide password";
                            break;
                        default:
                            failMessage = "Profile update failed :(";
                            break;
                    }
                    Debug.Log(failMessage);
                }

                else // kalau update user profile tidak ada error
                {
                    Debug.Log(FbUser.DisplayName);
                    Debug.Log("Registrasi berhasil bos"); // registrasi berhasil
                    //pindah ke LoginUI pakai coroutine

                    StartCoroutine(RegisterSuccess());
                }
            }
        }
    }

    private IEnumerator RegisterSuccess()
    {
        yield return new WaitForSeconds(0.8f);

        RegisterUI.SetActive(false);
        LoginUI.SetActive(true);
    }
    public void LoginButton()
    {
        StartCoroutine(LoginFirebase(emailLogin.text, passwordLogin.text));
    }

    private IEnumerator LoginFirebase(string email, string password)
    {
        if (string.IsNullOrEmpty(email)) // kalau kosong (true), error
        {
            Debug.Log("email nya kosong bos");
        }

        else if (string.IsNullOrEmpty(password)) // kalau kosong (true), error
        {
            Debug.Log("password nya kosong");
        }

        else
        {
            Task<AuthResult> loginTask = FbAuth.SignInWithEmailAndPasswordAsync(email, password);
            yield return new WaitUntil(() => loginTask.IsCompleted);

            if (loginTask.Exception != null) // kalau ada error
            {
                Debug.Log(loginTask.Exception);

                FirebaseException firebaseException = loginTask.Exception.GetBaseException() as FirebaseException;
                AuthError authError = (AuthError)firebaseException.ErrorCode;

                string failMessage = "Login failed! Because ";

                switch (authError) // switch case - mirip if else if
                {
                    case AuthError.InvalidEmail:
                        failMessage += "Email is Invalid";
                        break;
                    case AuthError.WrongPassword:
                        failMessage += "Wrong Password";
                        break;
                    case AuthError.MissingEmail:
                        failMessage += "Email is missing, please provide email";
                        break;
                    case AuthError.MissingPassword:
                        failMessage += "Password is missing, please provide password";
                        break;
                    default:
                        failMessage = "Login failed, try again :(";
                        break;
                }
                Debug.Log(failMessage);
            }

            else
            {
                FbUser = loginTask.Result.User;
                Debug.Log("Login Success! " + FbUser.DisplayName);

                Username = FbUser.DisplayName;
                Debug.Log("Your username: " + Username);
                PlayerPrefs.SetString("playername", Username);
                UserID = FbUser.UserId;
                Debug.Log("Your userid: " + UserID);
                PlayerPrefs.SetString("myUserid", UserID);

                StartCoroutine(LoginTransition());//pindah scene ke scene main maneu (scene multiplayer photon)
            }
        }
    }

    private IEnumerator LoginTransition()
    {
        yield return new WaitForSeconds(0.8f);

        LoginUI.SetActive(false);
        UserUI.SetActive(true);
        emailLogin.text = ""; // kosongin inputfield
        passwordLogin.text = ""; // kosongin inputfield

        Username = FbUser.DisplayName;
        Debug.Log(Username);
        UserOnUI.text = Username;

        LoadUserData();
    }

    public void LogoutButton()
    {
        FbAuth.SignOut();

        StartCoroutine(LogoutFirebase());
    }

    private IEnumerator LogoutFirebase()
    {
        yield return new WaitForSeconds(1.2f);

        UserUI.SetActive(false);
        MainMenuUI.SetActive(true);
    }
    void Update()
    {
        if (passwordLoginToggle.isOn) // kalau toggle dicentang
        {
            passwordLogin.contentType = TMP_InputField.ContentType.Standard; // konten input password biasa
            passwordLoginToggleText.text = "Hide password"; // ngubah komponen teks toggle
        }

        else if (!passwordLoginToggle.isOn) // kalau toggle ga dicentang
        {
            passwordLogin.contentType = TMP_InputField.ContentType.Password; // konten input password hidden
            passwordLoginToggleText.text = "Show password"; // ngubah komponen teks toggle
        }

        passwordLogin.ForceLabelUpdate(); // mengupdate inputfield

        if (passwordRegisterToggle.isOn)
        {
            passwordRegister.contentType = TMP_InputField.ContentType.Standard; // konten input password biasa
            confirmPasswordRegister.contentType = TMP_InputField.ContentType.Standard;
            passwordRegisterToggleText.text = "Hide password"; // ngubah komponen teks toggle;
        }

        else if (!passwordRegisterToggle.isOn) // kalau toggle ga dicentang
        {
            passwordRegister.contentType = TMP_InputField.ContentType.Password; // konten input password biasa
            confirmPasswordRegister.contentType = TMP_InputField.ContentType.Password;
            passwordRegisterToggleText.text = "Show password"; // ngubah komponen teks toggle;
        }

        passwordRegister.ForceLabelUpdate(); // mengupdate inputfield
        confirmPasswordRegister.ForceLabelUpdate(); // mengupdate inputfield
    }
}