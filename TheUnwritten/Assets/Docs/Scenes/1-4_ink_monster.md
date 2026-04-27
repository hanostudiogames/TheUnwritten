# Scene 1-4 — 잉크 괴물 전투 (옵션 C: 디에제시스적 그림자 카드 획득)

## 테마

"쓰는 것이 곧 얻는 것이다" — 괴물이 쏟아낸 어둠이 플레이어의 손에서 *카드가 되는* 순간을
씬 자체에서 연출해, 이후 슬롯 선택지에 그림자 카드가 등장하는 개연성을 세운다.

## 필요한 데이터 / 런타임 확장

| 구분 | 항목 | 비고 |
|---|---|---|
| Enum | `SceneEventType.CardGrant = 2` | 씬 중간 카드 런타임 지급 |
| EventRecord | `int[] CardIds` | 지급할 카드 ID 목록 |
| SlotRecord | `SlotResult[] SlotResults` | `{CardId, ResultLocalKey}` — 슬롯에 채워질 문구를 카드별로 분기 |
| CardInventory | 플레이어 소유 카드 집합 | `ShowCardAsync` 가 AllowedCardIds 를 소유 기준으로 필터링 |
| CardGrantEventHandler | `ISceneEventHandler` | EventRecord.CardIds 를 인벤토리에 추가 |

## 초기 덱 주의사항

`CardInventory` 는 빈 상태로 시작한다. 따라서 **Scene 1-4 이전에 `CardGrant` 이벤트로 불꽃/봉인
카드를 미리 지급**해야 Scene 1-4 전투 단계 2~3 이 정상 동작한다 (권장 위치: Scene 1-1 또는 1-2
의 튜토리얼 나레이션에 `CardGrant` 이벤트 삽입).

임시 시드가 필요하면 `MainPresenter` 의 `CardInventory` 생성 직후 `AddCard(1); AddCard(3);` 를
호출하는 방법이 있지만, 디에제시스 일관성을 위해 씬 데이터 측 지급을 권장한다.

---

## 씬 구조 (DialogueRecord 시퀀스)

아래의 `LocalKey` 들은 **새로 추가해야 하는** Localization 키다 (Dialogue 테이블).

| # | 레코드 타입 | LocalKey | 내용 |
|---|---|---|---|
| 1 | `NarrationRecord` | `scene_1_4_opening_00` | "아무도 없었다. 이 세계에는 당신과 문자들만 있었다." |
| 2 | `NarrationRecord` | `scene_1_4_opening_01` | "그리고 — 땅이 검어지기 시작했다. 글자들이 녹아내렸다. 흘러내리고, 모이고, 형태를 만들어냈다." |
| 3 | `NarrationRecord` | `scene_1_4_opening_02_grant` | "검은 흐름 한 자락이 — 당신의 손끝으로 튀었다. 차가웠다. 그리고, 익숙했다. 손바닥 위에서 그것은 *한 장의 카드가 되었다.*" |
| 3 | `EventRecord` (3 에 연결) | `EventId=CardGrant`, `CardIds=[2]` | 그림자 카드(Id=2) 지급 |
| 4 | `NarrationRecord` (내적 독백) | `scene_1_4_monologue_grant` | "— 이 세계에선… 쓰는 것이 얻는 것인가. 아직 읽지도 못한 이름이 내 손에 있다." |
| 5 | `NarrationRecord` (슬롯) | `scene_1_4_slot_01` | "잉크 괴물이 당신을 향해 [ `<slot_1>` ] 밀려온다." — `SlotId=1` |

### 슬롯 1 분기 (SlotRecord Id=1)

**AllowedCardIds**: `[1, 2]`  (flame, shadow) — 봉인은 이 슬롯 부적합 (그림자/불꽃 테마 매칭)

**SlotResults**:

| CardId | ResultLocalKey | 결과 문구 |
|---|---|---|
| 2 (shadow) | `scene_1_4_slot_01_shadow` | "자기 자신의 어둠을 뚫고" — *회피 (최선, 테마 회수)* |
| 1 (flame)  | `scene_1_4_slot_01_flame`  | "불꽃을 뚫고" — *데미지 50% 감소 (분기)* |
| (미입력)     | — | "거세게" — 일반 전투 (후속 이벤트 필요, 현재 Typer 는 자동 진행 아님) |

> ⚠️ "미입력" 분기는 현재 타이머/스킵 UI 가 없어 자동 진행되지 않는다. 별도 작업 필요.

### 선택 후 독백

| 선택 카드 | LocalKey | 독백 |
|---|---|---|
| Shadow | `scene_1_4_monologue_shadow_after` | "— 네가 준 걸로 네가 막혔다." |
| Flame | `scene_1_4_monologue_flame_after` | "— 방금 그 검은 카드는… 나중에 읽자." (그림자는 덱에 잔존) |
| 미입력 | `scene_1_4_monologue_none_after` | "— 손이 떨렸다. 두 장 다 쓸 수 없었다." |

---

## 전투 단계 (2~5단계)

시나리오 원본 그대로. 추가 시스템 TODO:

- 카드 **효과 스키마** (데미지, 해독도, n턴 정지) — 현재 `CardRecord` 미지원
- **턴 관리** (불꽃 → 봉인 → 불꽃+고함 콤보) — `BattleSceneMode` 리팩토링 필요
- **카드 해금 🎁** (고함 카드) — `CardGrant` 이벤트 재활용 가능 (전투 종료 콜백에서 `AddCard(4)`)
- **거울 씨앗 3** — 신규 `SceneEventType` (`MirrorSeed`) 도입 후보, 회차 카운터 + 상호해석 분기

## 현재 커밋에서 지원되는 범위

✅ 1단계 (실시간 서술 개입 + 카드별 결과 문구 분기)
✅ 도입부 카드 지급 연출 (런타임 인벤토리 반영)
⚠️ 2~5단계 — 후속 PR 필요
