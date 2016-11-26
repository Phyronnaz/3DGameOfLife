using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class DebugUI : MonoBehaviour
    {
        public Text ComputationTimeText;
        public Text TrianglesTimeText;
        public Text WarningText;
        public Text ErrorText;

        public static DebugUI UI { get; private set; }

        public float ClearTime = 5f;

        void Start()
        {
            UI = this;
            Clear();
            Log.ComputationTimeLog += ComputationTimeLog;
            Log.TrianglesTimesLog += TrianglesTimeLog;
            Log.WarningsLog += WarningLog;
            Log.ErrorsLog += ErrorLog;
#if UNITY_EDITOR
            Log.ComputationTimeLog += Debug.Log;
            Log.TrianglesTimesLog += Debug.Log;
            Log.WarningsLog += Debug.LogWarning;
            Log.ErrorsLog += Debug.LogError;
#endif
        }

        public void SetActive(bool active)
        {
            ComputationTimeText.enabled = active;
            TrianglesTimeText.enabled = active;
            WarningText.enabled = active;
            ErrorText.enabled = active;
        }

        void ComputationTimeLog(string s)
        {
            ComputationTimeText.text = s;
            CancelInvoke("ClearComputationTime");
            Invoke("ClearComputationTime", ClearTime);
        }

        void TrianglesTimeLog(string s)
        {
            TrianglesTimeText.text = s;
            CancelInvoke("ClearTrianglesTime");
            Invoke("ClearTrianglesTime", ClearTime);
        }

        void WarningLog(string s)
        {
            WarningText.text += s + "\n";
            CancelInvoke("ClearWarning");
            Invoke("ClearWarning", ClearTime);
        }

        void ErrorLog(string s)
        {
            ErrorText.text += s + "\n";
            CancelInvoke("ClearError");
            Invoke("ClearError", ClearTime);
        }

        void Clear()
        {
            ClearComputationTime();
            ClearTrianglesTime();
            ClearWarning();
            ClearError();
        }

        void ClearComputationTime()
        {
            ComputationTimeText.text = "";
        }
        void ClearTrianglesTime()
        {
            TrianglesTimeText.text = "";
        }
        void ClearWarning()
        {
            WarningText.text = "";
        }
        void ClearError()
        {
            ErrorText.text = "";
        }
    }
}
