# Arkesia Blue Marble — Unity 버전

현재 개발 대상은 **네이티브 Unity 6 LTS 프로젝트**입니다.

- [Unity 프로젝트 실행 방법](UnityProject/README.md)
- Unity Hub에서 추가할 폴더: `UnityProject`
- 씬: `Assets/Arkesia/Scenes/Main.unity`
- Play 후 ‘화면 목록’에서 11개 화면을 이동합니다.
- F8 또는 `Arkesia → Capture All 11 Screens`로 실제 Unity 실행 화면을 PNG로 저장합니다.

**이 작업 환경에 Unity Editor가 없어 Unity 컴파일·실행·빌드·캡처는 검증하지 못했습니다.** C# 5개 파일의 구문 분석과 프로젝트 파일 정합성만 확인했습니다. Unity에서 첫 실행 시 추가 수정이 필요할 수 있습니다.

`previews/`는 아래 웹 프로토타입의 과거 캡처입니다. **Unity 실행 화면이 아닙니다.** Unity 캡처는 사용자의 실행 후 `UnityProject/Captures/`에 생성됩니다.

## 이전 웹 프로토타입

첨부된 모바일 웹 기능·화면 정의서를 참고한 프런트엔드 시안입니다. 서버, 인증, 실시간 멀티플레이 및 영구 보상은 연결되지 않았습니다. 영웅 이미지와 시너지 효과는 검증되지 않은 개발용 예시입니다.

## 실행

의존성 설치 없이 `index.html`을 브라우저에서 열거나 다음 명령으로 실행합니다.

```sh
cd /workspace/Lost-Ark-Dice
python3 -m http.server 4173 --bind 0.0.0.0
```

390×844 모바일 화면 기준이며 데스크톱에서도 모바일 폭으로 표시합니다. 상단 화면 메뉴에서 M-001~M-011을 확인할 수 있습니다. 주사위, 구매, 편성, 검색, 준비, 카드 뒤집기 및 전투는 로컬 시연입니다. 새로고침하면 초기화됩니다.
