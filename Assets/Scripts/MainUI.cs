using System;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class MainUI : MonoBehaviour
    {
        public Button SaveButton;
        public Button LoadButton;

        public Text DensityText;
        public Slider RandomizeSlider;
        public Button SingleButton;
        public Button RandomizeButton;

        public InputField WField;
        public InputField XField;
        public InputField YField;
        public InputField ZField;

        public InputField ChunkSizeField;
        public InputField ThreadSizeField;

        public InputField SizeField;
        public Button ChangeSizeButton;

        public Toggle DebugToggle;
        public Toggle Use3DToggle;

        public Toggle ConstantUpdateToggle;
        public Toggle EditModeToggle;

        GameOfLife GOL { get { return GameOfLife.GOL; } }

        void Start()
        {
            SaveButton.onClick.AddListener(IO.Save);
            LoadButton.onClick.AddListener(IO.Load);

            RandomizeSlider.onValueChanged.AddListener(f => DensityText.text = string.Format("Density: {0}", Mathf.Pow(RandomizeSlider.value, 2) + 0.0001f).Substring(0, 13));
            RandomizeButton.onClick.AddListener(() => { GOL.Randomize(Mathf.Pow(RandomizeSlider.value, 2)); GameOfLife.GOL.UpdateCubes(); });
            SingleButton.onClick.AddListener(() =>
            {
                GOL.Reset();
                if (GameOfLife.Use3D)
                {
                    GOL.SetBlock(GOL.XSize / 2, GOL.YSize / 2, GOL.ZSize / 2, true);
                }
                else
                {
                    GOL.SetBlock(GOL.XSize / 2, GOL.YSize - 1, GOL.ZSize / 2, true);
                }
                GOL.UpdateCubes();
            });


            WField.onEndEdit.AddListener(s => { if (s != "") GameOfLife.W = int.Parse(s); else WField.text = GameOfLife.W.ToString(); });
            XField.onEndEdit.AddListener(s => { if (s != "") GameOfLife.X = int.Parse(s); else XField.text = GameOfLife.X.ToString(); });
            YField.onEndEdit.AddListener(s => { if (s != "") GameOfLife.Y = int.Parse(s); else YField.text = GameOfLife.Y.ToString(); });
            ZField.onEndEdit.AddListener(s => { if (s != "") GameOfLife.Z = int.Parse(s); else ZField.text = GameOfLife.Z.ToString(); });

            WField.onEndEdit.Invoke("");
            XField.onEndEdit.Invoke("");
            YField.onEndEdit.Invoke("");
            ZField.onEndEdit.Invoke("");

            ChunkSizeField.onEndEdit.AddListener(s =>
            {
                if (s != "")
                {
                    GameOfLife.ChunkSize = int.Parse(s);
                    Log.LogWarning("Restart needed");
                    ChunkSizeField.text = GameOfLife.ChunkSize.ToString();
                }
                else
                {
                    ChunkSizeField.text = GameOfLife.ChunkSize.ToString();
                }
            });
            ThreadSizeField.onEndEdit.AddListener(s => { if (s != "") GameOfLife.ThreadSize = int.Parse(s); else ThreadSizeField.text = GameOfLife.ThreadSize.ToString(); });

            ChunkSizeField.onEndEdit.Invoke("");
            ThreadSizeField.onEndEdit.Invoke("");

            SizeField.text = GOL.XSize.ToString();
            ChangeSizeButton.onClick.AddListener(() => { GOL.SetSize(int.Parse(SizeField.text)); GOL.UpdateCubes(); });

            DebugToggle.onValueChanged.AddListener(b => DebugUI.UI.SetActive(b));
            Use3DToggle.onValueChanged.AddListener(b => GameOfLife.Use3D = b);
            ConstantUpdateToggle.onValueChanged.AddListener(b => GOL.ConstantUpdate = b);
            EditModeToggle.onValueChanged.AddListener(b => { GameOfLife.EditMode = b; if (b) { GOL.MarkAllForUpdate(); GOL.UpdateCubes(); } });
        }
    }
}
