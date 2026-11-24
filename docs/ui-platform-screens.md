# 플랫폼 화면 설계 — 동의·계정 탭·고객 문의 (PR #73 재작업)

PR #73의 첫 구현은 화면을 코드로 조립했다(`new GameObject`/`AddComponent`, 색·크기·문구 하드코딩, 경로 문자열 `Find`). 이 문서는 그것을 **프리팹 + MVP**로 다시 만드는 계약서다. 화면은 사람이 에디터에서 위지윅으로 고칠 수 있어야 한다.

## 0. 원칙
- 보이는 것은 전부 프리팹 안에 미리 놓여 있다. 스크립트는 `[SerializeField]`로 참조된 요소에 데이터를 채우고 켜고 끄기만 한다. 레이아웃·색·글자 크기·고정 문구를 코드에서 정하지 않는다.
- 상황에 따라 바뀌는 문구(상태·오류)는 View의 `[SerializeField, TextArea] string` 필드에 기본값으로 두어 인스펙터에서 고친다. 로직(Presenter·서비스)에는 한국어 문구가 없다.
- `GameObject.Find`, `transform.Find("경로")`, `FindFirstObjectByType`로 UI를 찾지 않는다. UnityEngine.Object에 `?.`/`??`를 쓰지 않는다.
- 플랫폼 무관: UI 스크립트는 플랫폼 `#if` 밖에 있다. 기능 노출은 `LandLinkerServices`의 기능 플래그로만 정한다(Android와 STOVE PC에서 켜짐).
- 시스템 문구는 `~습니다`체. 요청은 `~해 주십시오`. 개발 용어(서버, FPS, AdMob, 오류 코드, ID 원문) 금지.
- 글자: `BMJUA SDF`에 없는 기호(`·`, `…`, `•`)를 쓰지 않는다. 본문은 줄바꿈 허용(Wrap), 버튼·라벨만 한 줄.

## 1. 구조

| 화면 | 프리팹 위치 | View 스크립트 | 로직 |
|---|---|---|---|
| 동의 모달 | `GlobalUI.prefab` 안 `UI_ConsentModal` (기본 비활성) | `UI_ConsentModalView` | `LlConsentGate`(부팅 순서) → `GlobalUICanvasView.Instance.ConsentModal.Show(...)` |
| 고객 문의 모달 | `GlobalUI.prefab` 안 `UI_SupportModal` (기본 비활성) | `UI_SupportModalView` | `UI_SupportModalPresenter`(순수 C#) |
| 계정 탭 | `OptionsCanvas.prefab` 안 탭 버튼 `UI_AccountTabButton` + 패널 `UI_AccountTab` | `UI_AccountTabView` | `UI_AccountTabPresenter`(순수 C#) |
| 확인 팝업(삭제 2단계·충돌 선택) | 기존 `GlobalUI` `Popup` — 본문 길이에 맞게 높이가 늘도록 레이아웃만 보강 | 기존 `GlobalUICanvasPopupPresenter.ShowPopup` | 계정 탭 Presenter |

- `GlobalUICanvasView`에 `[SerializeField] UI_ConsentModalView _consentModal; [SerializeField] UI_SupportModalView _supportModal;`과 읽기 프로퍼티를 추가한다.
- `UI_OptionView`는 계정 탭을 다른 탭과 똑같이 직렬화 필드(`_accountTab` CanvasGroup)로 가진다. 런타임 등록(`RegisterAccountTab`)·복제 금지.
- 계정 탭 버튼은 `LandLinkerServices.AccountFeaturesAvailable`이 거짓이면 숨긴다(키 바인딩 탭을 터치에서 숨기는 방식과 같다).

## 2. 시각 토큰
기존 옵션 화면의 값을 **복사해서** 쓴다(새 색을 만들지 않는다).
- 카드/패널 배경: 옵션 화면 본문 패널(`UI_Body` 계열) Image 색·스프라이트 그대로.
- 행 배경: 옵션 화면 설정 행(예: "안티 에일리어싱" 행)의 둥근 어두운 Image 그대로. 행 높이 64, 왼쪽 라벨 영역 / 구분선 / 오른쪽 값·버튼 구조도 같다.
- 기본 버튼(주 행동): "완료" 버튼과 같은 크림색 둥근 버튼, 글자 어두운 색.
- 보조 버튼: 행 배경과 같은 어두운 둥근 버튼, 글자 흰색.
- 위험 버튼(계정 삭제): 크림 버튼 형태에 배경 `#E5534B`, 글자 흰색.
- 모달 뒤 배경막: 검정 알파 0.6, 화면 전체, 레이캐스트 차단.
- 글꼴: `BMJUA SDF`. 크기(기준 해상도 1920×1080): 모달 제목 46, 섹션 제목 34(옵션의 "디스플레이"와 같게), 본문 28, 행 라벨 28, 버튼 28, 보조 안내 24.

## 3. 동의 모달 `UI_ConsentModal`
```
UI_ConsentModal (RectTransform 전체 늘림, CanvasGroup)
├─ Backdrop (Image 검정 0.6, 전체)
└─ Card (패널 배경, 가운데, 폭 1080, VerticalLayoutGroup 패딩 56/56/48/48 간격 24, ContentSizeFitter 세로 Preferred)
   ├─ Title (TMP 46 흰색, 가운데)              "시작하기 전에"
   ├─ Intro (TMP 28, Wrap)                      "클라우드 저장, 고객 문의, 광고 제공을 위해 아래 정보를 처리합니다."
   ├─ Items (VerticalLayoutGroup 간격 12)
   │  └─ Item ×3 (행 배경, 높이 ≥72, HorizontalLayoutGroup 패딩 24, 간격 24)
   │     ├─ Label (TMP 28, 폭 200 고정, 한 줄)
   │     └─ Desc (TMP 26, Wrap, 유연 폭)
   ├─ Note (TMP 24, Wrap, 연한 회색 — 옵션 화면 보조 글자색)
   └─ Buttons (HorizontalLayoutGroup 간격 20, 오른쪽 정렬, 높이 88)
      ├─ PolicyButton (텍스트 버튼, 밑줄 없이 보조 버튼)  "개인정보 처리방침"
      ├─ DeclineButton (보조 버튼, 최소 폭 240)          "동의하지 않음"
      └─ AcceptButton (기본 버튼, 최소 폭 280)           "동의하고 시작"
```
항목 문구:
| Label | Desc |
|---|---|
| 게임 기록 | 게스트 계정 번호, 구글 계정 연동 정보, 스테이지 기록 |
| 기기 정보 | 앱 버전, 기기 모델, 운영체제, 게임 실행 성능 |
| 광고 | 광고 식별자(광고 표시와 효과 측정) |

광고 항목과 Intro의 "광고 제공"은 `LandLinkerServices.AdsAvailable`(Android만 참)일 때만 보인다. View 참조 `_adItem`(세 번째 Item), `_intro`, 문구 `_introWithAds` / `_introWithoutAds`("클라우드 저장과 고객 문의를 위해 아래 정보를 처리합니다."). `Show(onAccept, onDecline, showAdDisclosure)`.

Note: "동의하지 않아도 오프라인으로 플레이할 수 있습니다. 설정의 계정 탭에서 언제든 바꿀 수 있습니다."

View API: `Show(Action onAccept, Action onDecline, Action onPolicy)`, `Hide()`. 표시 중 뒤 화면 입력 차단.

## 4. 계정 탭 `UI_AccountTab`
옵션의 그래픽/사운드 탭과 같은 스크롤 구조(ScrollRect + Viewport + Content VerticalLayoutGroup)를 **에디터에서 복제해 저장**한다. 탭 버튼 라벨 "계정", 순서는 그래픽 · 사운드 · 계정.
```
섹션 "계정"
 행 GoogleRow     라벨 "구글 계정"     값 StatusText  버튼 GoogleButton
 행 AccountIdRow  라벨 "계정 번호"     값 "#ABCD"     버튼 "복사"
 행 DeleteRow     라벨 "계정 삭제"     값 (없음)      위험 버튼 "삭제"
섹션 "지원"
 행 SupportRow    라벨 "고객 문의"                    버튼 "문의하기"
 행 PrivacyRow    라벨 "개인정보 처리방침"            버튼 "보기"
 행 TermsRow      라벨 "이용약관"                     버튼 "보기"
 행 AdPrivacyRow  라벨 "광고 개인정보 설정"           버튼 "설정"   (필요할 때만 표시)
```
행 버튼은 오른쪽 끝 고정 폭 200, 높이 52. 라벨은 한 줄, 값 텍스트는 한 줄(넘치면 말줄임 대신 폭 안에서 자동 축소 최소 22).

상태별 표시(문구는 View 직렬화 필드 기본값):
| 상태 | StatusText | GoogleButton | 나머지 계정 행 |
|---|---|---|---|
| 오프라인(동의 안 함) | "오프라인 플레이 중" | "온라인 전환" → 동의 모달 | 계정 번호·삭제 숨김 |
| 게스트 | "게스트" | "연동하기" | 표시 |
| 연결 중 | "연결 중" | 비활성 | 표시 |
| 연동됨 | 표시 이름(없으면 "연동됨") | 숨김 | 표시 |

복사 후 팝업: 제목 "복사했습니다", 본문 "계정 번호를 클립보드에 복사했습니다.", 버튼 "확인".

계정 삭제 흐름(기존 GlobalUI 팝업):
1. "계정 삭제" / "클라우드와 이 기기에 저장된 모든 진행 기록이 삭제됩니다. 삭제한 기록은 복구할 수 없습니다." / [취소] [계속]
2. "정말 삭제하시겠습니까?" / "이 작업은 되돌릴 수 없습니다." / [취소] [영구 삭제]
3. 성공: "계정을 삭제했습니다" / "오프라인 상태로 처음부터 시작합니다." / [확인]
   실패: "계정을 삭제하지 못했습니다" / "네트워크 연결을 확인한 뒤 다시 시도해 주십시오." / [확인]

PC(STOVE): 구글 연동은 시스템 브라우저에서 진행된다. 연결 중에는 StatusText "브라우저에서 로그인 중", GoogleButton "취소"(활성, `LandLinkerServices.CancelGoogleSignIn`). 취소·시간 초과는 실패 팝업을 띄우지 않는다.

구글 연동 실패: "연동하지 못했습니다" / "잠시 후 다시 시도해 주십시오." / [확인]

## 5. 고객 문의 모달 `UI_SupportModal`
```
UI_SupportModal (전체, CanvasGroup)
├─ Backdrop
└─ Card (폭 1080, 세로 레이아웃 패딩 56/56/48/48 간격 20, ContentSizeFitter)
   ├─ Title "고객 문의"
   ├─ FormGroup
   │  ├─ CategoryLabel "문의 유형" (24 보조)
   │  ├─ Categories (ToggleGroup, HorizontalLayoutGroup 간격 12) — 토글 4개: 계정 / 오류 / 제재 / 기타  (선택 = 크림, 미선택 = 행 배경)
   │  ├─ EmailLabel "답변 받을 이메일 (선택)"
   │  ├─ EmailInput (TMP_InputField, 행 배경, 높이 72, 이메일 입력 타입, 플레이스홀더 "example@email.com")
   │  ├─ BodyInput (TMP_InputField 여러 줄, 높이 260, 플레이스홀더 "문의 내용을 5자 이상 입력해 주십시오.")
   │  ├─ Footer (가로) ─ Notice "앱 버전과 기기 정보가 함께 전송됩니다." (24 보조, 유연 폭)  ─ Counter "0 / 2000" (24, 오른쪽)
   │  ├─ ErrorText (24, `#E5534B`, Wrap, 기본 비활성)
   │  └─ Buttons [취소(보조)] [보내기(기본)]  — 전송 중 보내기 버튼 글자 "보내는 중", 둘 다 비활성
   └─ DoneGroup (기본 비활성, FormGroup 대신 표시)
      ├─ DoneTitle "문의를 접수했습니다"
      ├─ DoneBody "문의 번호 #{0}\n답변은 문의 내역에서 확인할 수 있습니다." (Wrap)
      └─ Buttons [닫기(보조)] [문의 내역 보기(기본, 스레드 URL 없으면 숨김)]
```
- 카테고리 값(서버로 보내는 키): `account`, `bug`, `sanction`, `other`. LAND LINKER에는 결제가 없으므로 결제 유형은 넣지 않는다.
- 오류 문구: 본문 길이 "문의 내용을 5자 이상 입력해 주십시오." / 이메일 "이메일 주소 형식이 올바르지 않습니다." / 요청 제한 "잠시 후 다시 시도해 주십시오." / 기타 "문의를 보내지 못했습니다. 네트워크 연결을 확인해 주십시오."
- 키보드가 올라와도 입력칸이 보이도록 Card는 화면 위쪽 1/3 기준으로 배치(앵커 상단 중앙, y -120). 모바일 TMP_InputField 기본 동작 사용.

## 6. 검수 기준 (화면 QA)
- 해상도 2400×1080, 1920×1080, 2048×1536, 1280×720에서 각 화면 캡처. 글자가 잘리거나 겹치거나 카드 밖으로 나가면 불합격. 본문은 단어 중간이 아니라 자연스럽게 줄바꿈(TMP Word Wrapping, 한국어 줄바꿈 규칙).
- 긴 값: 표시 이름 20자, 문의 본문 2000자, 이메일 60자로도 확인.
- 네모 박스(없는 글리프)가 보이면 불합격.
- 에디터 편집 모드에서 프리팹을 열었을 때 화면이 그대로 보여야 한다.

## 7. 업데이트 게이트 (기존 GlobalUI 팝업, `UI_PlatformPromptsView`)
- 필수 업데이트(`KoryUpdateGate.Required`): "업데이트가 필요합니다" / "새 버전이 나왔습니다. 스토어에서 최신 버전으로 업데이트한 뒤 다시 실행해 주십시오." / [업데이트] — 팝업을 닫지 않는다.
- 선택 업데이트(`Available`, 실행당 1회): "새 버전이 있습니다" / "최신 버전으로 업데이트할 수 있습니다." / [업데이트(주)] [나중에]
- 업데이트 버튼은 `KoryUpdateGate.OpenStore()`: Android는 Play 스토어, PC는 `KoryConfig.desktopStoreUrl`(STOVE 상점 https://store.onstove.com/ko/games/105336).
