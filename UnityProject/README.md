# Arkesia Blue Marble · 네이티브 Unity 프로토타입

기준: Unity **6 LTS / 6000.0.23f1**, Built-in Render Pipeline, uGUI. 브라우저나 WebView를 사용하지 않습니다. 다른 Unity 6 LTS 패치로 여는 경우 Hub의 버전 변경 안내를 확인하세요.

## 현재 상태

- 11개 화면의 C# 소스와 시작 씬을 작성했습니다.
- 길 위의 3D 캐릭터, 높이가 다른 타일, 나무·유적·건물·다리 기둥, 조명·그림자를 Unity 프리미티브로 구성했습니다. 완성 캐릭터 원화나 리깅 에셋은 아닙니다.
- **작성 환경에 Unity Editor와 라이선스가 없어 Unity 컴파일·Play Mode·빌드·캡처를 실행하지 못했습니다. 실행 가능 여부는 사용자 PC에서 확인이 필요합니다.** 문법 검사 결과와 Unity 실행 검증은 다릅니다.
- `../previews`의 이미지는 이전 **웹 버전** 캡처입니다. Unity 실행 화면이 아닙니다.

## PC에서 열기

1. Unity Hub에서 Unity 6 LTS를 설치·활성화합니다. 우선 Editor로 확인할 때 Android/iOS 빌드 모듈은 필요하지 않습니다.
2. **Add project from disk**에서 저장소 전체가 아닌 이 `UnityProject` 폴더를 선택합니다.
3. 최초 패키지 가져오기와 스크립트 컴파일이 끝날 때까지 기다립니다. uGUI 패키지 다운로드에는 `packages.unity.com` / `download.packages.unity.com` 접근이 필요할 수 있습니다.
4. **Arkesia → 1. Prepare Project**, **Arkesia → 2. Open Main Scene**을 선택합니다. Built-in Standard 재질과 빌드 씬, 구 입력 시스템을 설정합니다. 입력 시스템 변경 시 Editor 재시작 안내가 나오면 재시작합니다.
5. Game View를 **390×844** 또는 **844×390**으로 설정하고 **Play**를 누릅니다. 초기 화면은 홈입니다.
6. **화면 목록** 버튼에서 M-001~M-011을 이동합니다. 보드에서는 드래그로 회전하고 휠/두 손가락/± 버튼으로 확대합니다. 타일을 클릭하면 상세로 이동합니다.

추가 CLI 검사(실행·라이선스가 가능한 PC 전용):

```sh
Unity -batchmode -quit -projectPath /absolute/path/to/UnityProject -executeMethod Arkesia.Editor.ArkesiaEditor.ValidateProject -logFile /absolute/path/to/unity-validation.log
```

위 명령은 스크립트 컴파일·기초 데이터 검증이며 그래픽 실행 테스트가 아닙니다. 실제 화면은 Game View에서 별도로 확인합니다.

## 11개 실제 화면 캡처

Play Mode에서 **Game View를 표시한 채** 아래 중 하나를 실행합니다.

- **Arkesia → 4. Capture All 11 Screens (Play Mode)**
- 화면 목록의 **11개 실제 화면 캡처**
- **F8**

현재 Game View 해상도로 `Captures/M-001.png` ~ `Captures/M-011.png`를 저장합니다. **Arkesia → 5. Open Captures Folder**로 폴더를 엽니다. 캡처는 실제 프레임 버퍼를 PNG로 저장하며, 파일이 만들어지기 전에는 실행 캡처가 존재한다고 간주하지 않습니다. 플레이어 빌드에서는 `Application.persistentDataPath/Captures`에 저장합니다. 에디터의 배치 모드에서는 `WaitForEndOfFrame` 캡처를 사용하지 마세요.

## 화면과 연결된 동작

| ID | 화면 | 로컬 프로토타입 동작 |
|---|---|---|
| M-001 | 홈 | Unity가 렌더링한 보드 프리뷰, 로비·보드 이동 |
| M-002 | 방 로비 | 코드 복사, 준비 토글, 준비 후 시연 시작 |
| M-003 | 영웅 도감 | 이름·속성·보유 필터, 구매, 중복 구매 차단 |
| M-004 | 3인 편성 | 슬롯 교체, 중복 방지, 대표 영웅 선택, 기기 저장 |
| M-005 | 시너지 도감 | 예시 세트 검색·구성원·보유 현황 |
| M-006 | 3D 보드 | 32칸, 입체 SD 말 4개, 이동, 회전·확대, 영지 건물 |
| M-007 | 영지 | 구매, 3단계 건설, 잔액 부족 시 비활성 |
| M-008 | 운명 카드 | 1회 로컬 보상, 중복 확인 차단 |
| M-009 | 3대3 전투 | 입체 6인 무대, 주사위, 스킬 조건, 대상 선택, 예시 피해 |
| M-010 | 결과 | 예시 순위·보상 표시, 실제 보상 지급 없음 |
| M-011 | 설정·채팅 | 실제 그림자·모션 축소 설정, 로컬 채팅, 전체 화면 캡처 |

계정 골드·보유 영웅·편성·설정은 `PlayerPrefs`로 기기에 저장합니다. 매치 상태는 재시작 시 초기화됩니다. 온라인 방, 로그인, 서버 권한 RNG, 통행료·파산·PvP 전체 규칙, 재접속, 실제 보상 정산, 음원, 검증된 카드 세트 데이터는 구현되지 않았습니다. 영웅·전투 수치와 모델은 개발용입니다.

## 점검할 항목

- Console에 컴파일 오류가 없는지, 11개 화면이 열리는지
- 모바일 세로/가로 Game View와 safe area에서 UI가 가려지지 않는지
- 길 표면 위의 말과 그림자, 실제 클릭 가능한 32칸, 이동 중 중복 주사위 차단
- 영지 구매 잔액·건물, 영웅 구매·편성 저장, 카드 중복 지급 방지
- 전투 스킬 조건과 대상 지정, 프레임·메모리 및 실제 iOS/Android 성능

원작과 동일한 에셋을 포함하지 않습니다. 한국어 폰트는 NanumGothic이며 `Assets/Arkesia/Resources/NanumGothic-OFL.txt` 라이선스를 포함합니다.
