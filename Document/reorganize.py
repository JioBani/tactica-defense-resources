# -*- coding: utf-8 -*-
import re

# 파일 읽기
with open('기획서.md', 'r', encoding='utf-8') as f:
    content = f.read()

# 0번 섹션 전체를 찾아서 저장 (# 0. 세계관부터 # 1. 게임 개요 전까지)
worldview_match = re.search(r'(<a id="worldview"></a>\s*# 0\. 세계관 및 배경 설정.*?)(?=<a id="game-overview"></a>)', content, re.DOTALL)
worldview_section = worldview_match.group(1) if worldview_match else ""

# 목차에서 0번 섹션 제거하고 1.2에 세계관 개요 추가
toc_pattern = r'(## 목차\n)(0\. \[세계관 및 배경 설정\].*?(?=1\. \[게임 개요\]))(1\. \[게임 개요\].*?\n   - \[1\.1 한 줄 소개\(피치\)\].*?\n)(   - \[1\.2 핵심 재미.*?\n)'
toc_replacement = r'\g<1>\g<3>   - [1.2 세계관 개요](#worldview-summary)\n\g<4>'
content = re.sub(toc_pattern, toc_replacement, content, flags=re.DOTALL)

# 목차에서 1.3부터 1.6으로 번호 조정
content = re.sub(r'   - \[1\.2 핵심 재미', '   - [1.3 핵심 재미', content)
content = re.sub(r'   - \[1\.3 레퍼런스', '   - [1.4 레퍼런스', content)
content = re.sub(r'   - \[1\.4 플레이어 목표', '   - [1.5 플레이어 목표', content)
content = re.sub(r'   - \[1\.5 핵심 용어', '   - [1.6 핵심 용어', content)

# 목차에 16번 부록 추가
toc_append = """16. [부록: 세계관 상세 설정](#worldview-detail)
   - [16.1 시대 및 장르](#era-genre)
   - [16.2 스토리 개요](#story-overview)
   - [16.3 실체화 기술의 기원: 외계체의 하이브](#materialization-origin)
   - [16.4 인류의 대응: "실체화 무기"와 소환술사](#human-response)
   - [16.5 소환술사의 핵심 자질: 상상력(공상력)](#summoner-qualities)
   - [16.6 바스티온 네트워크(학원도시): 분산형 거점 도시 체계](#bastion-network)
      - [16.6.1 학교 생활: 심상 강화의 핵심 장치](#school-life)
   - [16.7 에너지원: 페이즈 입자(마나)](#energy-source)
   - [16.8 장비: PLGU(완드)](#equipment-plgu)
   - [16.9 전투 참여 방식](#combat-participation)
"""

content = re.sub(r'(15\. \[가정/확인 필요.*?   - \[15\.2 의사결정 우선순위\].*?\n)', r'\g<1>' + toc_append, content)

# 0번 세계관 섹션 삭제
content = re.sub(r'<a id="worldview"></a>\s*# 0\. 세계관 및 배경 설정.*?(?=<a id="game-overview"></a>)', '', content, flags=re.DOTALL)

# 1.1 다음에 1.2 세계관 개요 추가
worldview_summary = """
<a id="worldview-summary"></a>
## 1.2 세계관 개요
**배경**: 205X년, 외계체의 침략으로 인류가 붕괴 직전까지 몰린 근미래

**핵심 설정**:
- 인류는 외계체의 "실체화 기술"을 역설계하여, 인간의 **심상(인지/상상력)**을 무기로 바꾸는 기술을 개발
- 이를 다루는 **소환술사(Summoner)**들이 **바스티온(거점 도시)**에서 외계체와 전투
- 소환술사는 자신의 상상력을 바탕으로 다양한 형태의 무기/개체를 실체화하여 전투에 참여
- 게임 내 **유닛의 다양성**은 술사마다 다른 "심상(테마)"을 가지고 있다는 설정으로 설명됨

> 자세한 세계관 설정은 [16. 부록: 세계관 상세 설정](#worldview-detail) 참조
"""

content = re.sub(r'(<a id="pitch"></a>\n## 1\.1 한 줄 소개\(피치\)\n)', r'\g<1>' + worldview_summary + '\n', content)

# 1.2 핵심 재미를 1.3으로 변경
content = re.sub(r'<a id="core-fun"></a>\n## 1\.2 핵심 재미', '<a id="core-fun"></a>\n## 1.3 핵심 재미', content)

# 1.3 레퍼런스를 1.4로 변경
content = re.sub(r'<a id="references"></a>\n## 1\.3 레퍼런스', '<a id="references"></a>\n## 1.4 레퍼런스', content)

# 1.4 플레이어 목표를 1.5로 변경
content = re.sub(r'<a id="win-lose"></a>\n## 1\.4 플레이어 목표', '<a id="win-lose"></a>\n## 1.5 플레이어 목표', content)

# 1.5 핵심 용어를 1.6으로 변경
content = re.sub(r'<a id="glossary"></a>\n## 1\.5 핵심 용어', '<a id="glossary"></a>\n## 1.6 핵심 용어', content)

# 15.2 의사결정 우선순위 다음에 16번 부록 추가
worldview_detail_section = worldview_section.replace('# 0. 세계관 및 배경 설정', '# 16. 부록: 세계관 상세 설정')
worldview_detail_section = worldview_detail_section.replace('## 0.1 세계관 요약', '## 16.1 시대 및 장르')
worldview_detail_section = worldview_detail_section.replace('<a id="worldview-summary"></a>', '<a id="era-genre"></a>')
worldview_detail_section = worldview_detail_section.replace('<a id="worldview"></a>', '<a id="worldview-detail"></a>')

# 세부 섹션 번호 변경
worldview_detail_section = re.sub(r'## 0\.2 시대 및 장르.*?(?=<a id="story-overview">)', '', worldview_detail_section, flags=re.DOTALL)
worldview_detail_section = worldview_detail_section.replace('## 0.3 스토리 개요', '## 16.2 스토리 개요')
worldview_detail_section = worldview_detail_section.replace('## 0.4 실체화 기술', '## 16.3 실체화 기술')
worldview_detail_section = worldview_detail_section.replace('## 0.5 인류의 대응', '## 16.4 인류의 대응')
worldview_detail_section = worldview_detail_section.replace('## 0.6 소환술사', '## 16.5 소환술사')
worldview_detail_section = worldview_detail_section.replace('## 0.7 바스티온', '## 16.6 바스티온')
worldview_detail_section = worldview_detail_section.replace('### 0.7.1 학교', '### 16.6.1 학교')
worldview_detail_section = worldview_detail_section.replace('## 0.8 에너지원', '## 16.7 에너지원')
worldview_detail_section = worldview_detail_section.replace('## 0.9 장비', '## 16.8 장비')
worldview_detail_section = worldview_detail_section.replace('## 0.10 전투 참여', '## 16.9 전투 참여')

content = re.sub(r'(<a id="decision-priority"></a>\n## 15\.2 의사결정 우선순위)\n', r'\g<1>\n\n---\n\n' + worldview_detail_section, content)

# 파일 저장
with open('기획서.md', 'w', encoding='utf-8') as f:
    f.write(content)

print("File reorganized successfully!")
