using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class UIHandler : MonoBehaviour
    {
        public Text ComputationTimeText;
        public Text TrianglesTimeText;
        public Text CacheText;
        public Text WarningText;
        public Text ErrorText;

        public float ClearTime = 5f;

        void Start()
        {
            Clear();
            Log.ComputationTimeLog += ComputationTimeLog;
            Log.TrianglesTimesLog += TrianglesTimeLog;
            Log.CacheLog += CacheLog;
            Log.WarningsLog += WarningLog;
            Log.ErrorsLog += ErrorLog;
#if UNITY_EDITOR
            Log.ComputationTimeLog += Debug.Log;
            Log.TrianglesTimesLog += Debug.Log;
            Log.CacheLog += Debug.Log;
            Log.WarningsLog += Debug.LogWarning;
            Log.ErrorsLog += Debug.LogError;
#endif
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

        void CacheLog(string s)
        {
            CacheText.text = s;
            CancelInvoke("ClearCache");
            Invoke("ClearCache", ClearTime);
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
            ClearCache();
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
        void ClearCache()
        {
            CacheText.text = "";
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
