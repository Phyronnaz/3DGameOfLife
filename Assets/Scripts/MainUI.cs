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

        public Toggle DebugToggle;

        public InputField WField;
        public InputField XField;
        public InputField YField;
        public InputField ZField;

        GameOfLife GOL { get { return GameOfLife.GOL; } }

        void Start()
        {
            SaveButton.onClick.AddListener(IO.Save);
            LoadButton.onClick.AddListener(IO.Load);

            RandomizeSlider.onValueChanged.AddListener(f => DensityText.text = string.Format("Density: {0}", Mathf.Pow(RandomizeSlider.value, 2) + 0.0001f).Substring(0, 13));
            RandomizeButton.onClick.AddListener(() => { GOL.Randomize(Mathf.Pow(RandomizeSlider.value, 2)); GameOfLife.GOL.UpdateCubes(); });
            SingleButton.onClick.AddListener(() => { GOL.Reset(); GOL.SetBlock(GOL.XSize / 2, GOL.YSize / 2, GOL.ZSize / 2, true); GOL.UpdateCubes(); });

            DebugToggle.onValueChanged.AddListener(b => DebugUI.UI.SetActive(b));

            WField.text = GameOfLife.W.ToString();
            XField.text = GameOfLife.X.ToString();
            YField.text = GameOfLife.Y.ToString();
            ZField.text = GameOfLife.Z.ToString();

            WField.onEndEdit.AddListener(s => { if (s != "") GameOfLife.W = int.Parse(s); else WField.text = GameOfLife.W.ToString(); });
            XField.onEndEdit.AddListener(s => { if (s != "") GameOfLife.X = int.Parse(s); else XField.text = GameOfLife.X.ToString(); });
            YField.onEndEdit.AddListener(s => { if (s != "") GameOfLife.Y = int.Parse(s); else YField.text = GameOfLife.Y.ToString(); });
            ZField.onEndEdit.AddListener(s => { if (s != "") GameOfLife.Z = int.Parse(s); else ZField.text = GameOfLife.Z.ToString(); });
        }
    }
}
