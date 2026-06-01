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

        /// <summary>체력바 왼쪽 끝에 표시되는 역할군 아이콘 슬롯.</summary>
        [Header("역할군 아이콘")]
        [SerializeField] private SpriteRenderer roleGroupIconSprite;

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

        /// <summary>
        /// 역할군 아이콘을 체력바 왼쪽 끝에 표시한다. 스폰 시 1회 세팅되며 역할군은 불변이다.
        /// </summary>
        /// <param name="icon">표시할 역할군 아이콘. null이면 슬롯을 비운다(역할군 없는 유닛 또는 아트 미할당).</param>
        public void SetRoleGroupIcon(Sprite icon)
        {
            if (roleGroupIconSprite == null)
            {
                // 비정상 셋업: 프리팹에 역할군 아이콘 SpriteRenderer가 연결되지 않음. 단서를 남기고 중단한다.
                Debug.LogError($"[HealthBar] roleGroupIconSprite가 연결되지 않았습니다. 체력바 프리팹의 역할군 아이콘 SpriteRenderer 연결을 확인하세요. ({name})", this);
            }
            else
            {
                roleGroupIconSprite.sprite = icon;
                // 아이콘이 없으면 빈 슬롯으로 둔다.
                roleGroupIconSprite.enabled = icon != null;
            }
        }
    }
}
