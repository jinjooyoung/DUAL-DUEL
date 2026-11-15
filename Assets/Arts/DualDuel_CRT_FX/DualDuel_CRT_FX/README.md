# DUAL DUEL — CRT 카드 이펙트 10종

## 설치
`DualDuel_CRT_FX` 폴더(+ `.meta`)를 `Assets/` 아래에 그대로 넣으세요. 텍스처 설정(Point / 압축 없음 / 밉맵 없음)은 에디터 스크립트가 자동으로 맞춥니다.
머티리얼이 분홍색이면: **Tools > DUAL DUEL > CRT FX 머티리얼·텍스처 재설정**.

## 크기 기준
- FX 캔버스 96×96 원본 px (초상화 ≈78px + 타격이 프레임 밖으로 튈 여유)
- 카드와 같은 200% export → 프레임당 192×192px, PPU 100 → 파티클 크기 **1.92 유닛**
- FX 1px = 카드 1px. `CardFXSpawner.fitToTarget` 이 초상화 RectTransform 실제 크기에 맞춰 스케일 보정

## 프리팹 구조 (VFX_Arcade_01과 동일 방식)
```
VFX_CRT_xxx      ParticleSystem  Color 시트 (Alpha Blend)   SortingOrder 150, Stop Action = Destroy
 └ Glow          ParticleSystem  Glow 시트  (Additive x1.6) SortingOrder 151 → HDR 블룸
```
각 레이어: 버스트 1개, Billboard, Texture Sheet Animation(Whole Sheet, 4열), 수명 = 애니 길이, Scaling Mode = Hierarchy.

정렬: CRT 세팅 기준 HUD 캔버스 ≥100, 스캔라인 200, 툴팁 300 → **150/151** 이라 FX 위에 스캔라인이 덮입니다. 초상화 캔버스 Sorting Layer가 Default가 아니면 프리팹 렌더러의 Sorting Layer도 맞춰주세요.

## 목록
| 프리팹 | 길이 | 상황 |
|---|---|---|
| VFX_CRT_Attack | 0.60s | 피격 (초상화 위) — 흔들림 자동 |
| VFX_CRT_Defense_Gain | 0.80s | 방어력 획득 |
| VFX_CRT_Defense_Block | 0.60s | 방어 성공 — 흔들림 자동 |
| VFX_CRT_Heal | 0.80s | 회복 |
| VFX_CRT_Buff_Regeneration | 0.80s | 재생 획득 |
| VFX_CRT_Buff_Force | 0.80s | 힘 획득 |
| VFX_CRT_Buff_Lifesteal | 0.80s | 흡혈 획득 |
| VFX_CRT_Debuff_Poison | 0.80s | 중독 |
| VFX_CRT_Debuff_Bleed | 0.80s | 출혈 |
| VFX_CRT_Debuff_Weaken | 0.80s | 약화 |

20fps, 시트 4×3(12f) 또는 4×4(16f).

## 사용
씬에 빈 오브젝트 + `CardFXSpawner`, 프리팹 10개 연결 후:
```csharp
CardFXSpawner.Instance.Play(CardFX.Attack, monsterPortraitRect);
CardFXSpawner.Instance.Play(CardFX.BuffForce, playerPortraitRect);
```
히트스톱(`useHitStop`)은 Time.timeScale을 건드리므로 기본 꺼둠. 타격감을 더 원하면 켜보세요 (0.04~0.06초 권장).

## 연출 규칙
채널 믹서가 모든 색을 밝기 기반 초록으로 바꾸므로 빨강/초록 구분은 사라집니다. 그래서 **모양과 움직임**으로 구분했습니다.
- 긍정: 스캔라인 인쇄, 상승, 외곽선 확장 → **CRT 전원 꺼짐**(선으로 접혔다 점으로) 퇴장
- 부정: 찢어진 행, 떨림, 깜빡임, 하강, 경고 브래킷 → **신호 찢김** 퇴장

## 수정 / 재생성
`pixelfx_source/` 는 pixelfx 레포에 넣는 원본 코드입니다.
```bash
git clone https://github.com/patch-gg/pixelfx && cd pixelfx
cp -r <pixelfx_source>/src/fx/dualduel src/fx/ && cp <pixelfx_source>/tools/dd-export.mjs tools/
node tools/render.mjs dualduel/attack-hit --all --gif   # 보기
node tools/dd-export.mjs ./out                          # Color/Glow 시트 재생성 (같은 파일명으로 덮어쓰기)
```
