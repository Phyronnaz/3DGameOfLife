using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class UIHandler : MonoBehaviour
    {
        public Text ComputationTimeText;
        public Text TrianglesTimeText;
        public Text WarningText;
        public Text ErrorText;
        void Start()
        {
            ComputationTimeText.text = "";
            TrianglesTimeText.text = "";
            WarningText.text = "";
            ErrorText.text = "";
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

        void ComputationTimeLog(string s)
        {
            ComputationTimeText.text = s;
        }

        void TrianglesTimeLog(string s)
        {
            TrianglesTimeText.text = s;
        }

        void WarningLog(string s)
        {
            WarningText.text += s;
        }

        void ErrorLog(string s)
        {
            ErrorText.text += s;
        }
    }
}
