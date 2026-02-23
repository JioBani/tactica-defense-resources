using UnityEngine;

namespace Scenes.Battle.Feature.Units.HealthBars
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer outlineSprite;
        [SerializeField] private SpriteRenderer insideSprite;

        [Header("성급별 테두리 색")]
        [SerializeField] private Color star1Color = Color.gray;
        [SerializeField] private Color star2Color = Color.green;
        [SerializeField] private Color star3Color = new Color(1f, 0.84f, 0f);

        public void Display(float rate)
        {
            if (rate <= 0)
            {
                rate = 0;
            }

            float sizeX = outlineSprite.size.x * rate;
            insideSprite.size = new Vector2(sizeX, insideSprite.size.y);
        }

        /// <summary>
        /// 성급에 따라 HP 바 테두리 색을 변경한다.
        /// 1성: 회색, 2성: 금색, 3성 이상: 주황색.
        /// </summary>
        public void SetStarGrade(int star)
        {
            outlineSprite.color = star switch
            {
                <= 1 => star1Color,
                2    => star2Color,
                _    => star3Color,
            };
        }
    }
}
