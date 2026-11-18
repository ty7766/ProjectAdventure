# LAND LINKER 콘솔 ID 정리 (게임 키 `ll`)

작성: 2026-11-19. 비밀 값(client_secret 등)은 포함하지 않는다.

## Google Play Console
- 개발자 계정: KORYGAMES (`8317802228951941180`)
- 앱 이름: LAND LINKER / 무료 / 게임 / 기본 언어 한국어
- **패키지명: `com.korygames.landlinker`**
- Play 앱 ID(콘솔 URL): `4972940461857906530`
- 상태: 앱 생성만 완료 (AAB 업로드·스토어 등록정보·콘텐츠 설정 미진행)

## Play Games Services
- **Play Games App ID(프로젝트 ID): `796817250748`**
- 연결된 Cloud 프로젝트: `LAND LINKER` / ID `idyllic-silo-515115-b1` (프로젝트 번호 `796817250748`)
- OAuth 동의 화면: 외부, **프로덕션 게시 완료**. 지원 이메일은 계정 이메일(helpdesk@codingbot.kr은 지원 이메일 목록에 없어 불가), 개발자 연락처는 `helpdesk@codingbot.kr`. 홈 `https://games.codingbot.kr`, 개인정보처리방침 `https://games.codingbot.kr/land-linker/privacy/`, 승인 도메인 `codingbot.kr`. 범위: `games`, `games_lite`, `drive.appdata`.
- Play Games 상태: 속성 "임시"(미게시). 게임 게시·테스터 설정·업적/리더보드 없음.

### games-ids XML (`res/values/games-ids.xml`)
```xml
<?xml version="1.0" encoding="utf-8"?>
<!--Google Play game services IDs. Save this file as res/values/games-ids.xml in your project.-->
<resources>
  <!--app_id-->
  <string name="app_id" translatable="false">796817250748</string>
  <!--package_name-->
  <string name="package_name" translatable="false">com.korygames.landlinker</string>
</resources>
```

### OAuth 클라이언트
| 종류 | 이름 | 클라이언트 ID |
|---|---|---|
| Android (Play Games 사용자 인증 정보로 연결됨) | LAND LINKER Android (debug) | `796817250748-ugbsr48ui7j03pitm7n3omp4pn5kmm0i.apps.googleusercontent.com` |
| 웹 애플리케이션 (서버 인증 코드용) | LAND LINKER Game Server (web) | `796817250748-85krvqf6sbc95itfnrei7nvtdskiq7t6.apps.googleusercontent.com` |

- Android 클라이언트에 등록된 SHA-1: **디버그 키스토어** `%USERPROFILE%\.android\debug.keystore` → `4B:28:00:D2:0C:AC:4E:CC:B6:B8:A5:61:AE:1C:29:1B:4E:4C:2A:08` (프로젝트에 커스텀 키스토어 없음, `androidUseCustomKeystore: 0`).
- 웹 클라이언트 JSON(client_secret 포함): `C:\Users\Art\Downloads\client_secret_landlinker_web.json` (저장소 밖, 서버 `Games:ll:Google:OAuthClientJson`에 사용. 커밋 금지.) Chrome이 `7ca80335-cbc9-46f1-98c6-b7c7cb0a25b4.tmp`로 남긴 동일 내용 복사본이 같은 폴더에 있으니 확인 후 삭제할 것.

## AdMob
- 게시자 ID: `pub-4520592084396814` (LAST CALL·리듬 비트 디펜스와 동일 계정)
- **AdMob 앱 ID: `ca-app-pub-4520592084396814~7394577073`** (Android, 스토어 미연결 → 광고 게재 제한 상태)
- **전면(Interstitial) 광고 단위** `LandLinker_Interstitial`: `ca-app-pub-4520592084396814/7203005389`
- **보상형(Rewarded) 광고 단위** `LandLinker_Rewarded`: `ca-app-pub-4520592084396814/2195442472` (보상 1 / "Reward")
- 개발 중에는 Google 테스트 광고 단위 ID를 쓸 것.

## 남은 일 / 주의
1. **Play 앱 서명(또는 업로드) 키 SHA-1을 Android OAuth 클라이언트에 추가**: Play에 첫 AAB를 올려 앱 서명이 생성된 뒤 Play Console > 앱 무결성 > 앱 서명에서 SHA-1을 확인해 Cloud 콘솔에 Android 클라이언트를 추가로 만들고 Play Games 사용자 인증 정보로 연결. 현재는 디버그 빌드에서만 로그인 가능.
2. **게임 서버(웹) 사용자 인증 정보 미연결**: Play Games > 설정 > 사용자 인증 정보 > "게임 서버"에서 웹 클라이언트를 고르려 했으나 드롭다운에 후보가 나타나지 않음(생성 직후 전파 지연 추정). 잠시 후 다시 시도.
3. Play Games 게시(검토 및 게시), 테스터 추가는 미진행.
4. AdMob 앱을 Play 스토어 앱과 연결해야 광고 게재 한도가 해제됨(앱 게시 후).
