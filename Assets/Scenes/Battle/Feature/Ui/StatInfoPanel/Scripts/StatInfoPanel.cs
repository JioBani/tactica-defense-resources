using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unit = Scenes.Battle.Feature.Units.Unit;

namespace Scenes.Battle.Feature.Ui.StatInfoPanel
{
    public class StatInfoPanel : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image[] starImages;
        [SerializeField] private Button closeButton;

        [Header("Body")]
        [SerializeField] private Image illustrationImage;

        private void Awake()
        {
            closeButton.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            closeButton.onClick.RemoveListener(Hide);
        }

        public void Show(Unit unit)
        {
            gameObject.SetActive(true);

            var def = unit.UnitLoadOutData.Unit;
            nameText.text = def.DisplayName;
            illustrationImage.sprite = def.Illustration;

            // TODO: 유닛에 성급(star) 정보가 추가되면 unit.Star 등으로 교체
            UpdateStars(1);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void UpdateStars(int count)
        {
            for (int i = 0; i < starImages.Length; i++)
                starImages[i].gameObject.SetActive(i < count);
        }
    }
}
