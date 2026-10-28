# 검증 상태

## 이 환경에서 확인한 것

- C# 파일 5개를 C# 문법 파서로 분석: 문법 오류 노드 없음.
- UGUI 패키지와 사용한 오디오·물리·스크린 캡처·이미지 변환 모듈의 manifest JSON 파싱.
- 32칸 이름·가격과 11개 화면 정의, 빌드 씬 경로·GUID, 한국어 TrueType 폰트·OFL 파일 존재.
- Git 공백 오류 검사.

## 실행하지 못한 것

- Unity API에 대한 C# 컴파일과 UPM 패키지 가져오기
- 씬 로딩과 Play Mode, 렌더링·레이캐스트·UI 배치
- 11개 실제 화면 캡처와 화면 전환
- Windows/macOS/Android/iOS 빌드, 실기기 터치·성능

이 환경에는 Unity Editor 실행 파일, 활성화된 Unity 라이선스, 연결된 Unity 실행 도구가 없다. 따라서 코드 작성·문법 확인을 Unity 실행 성공으로 해석하면 안 된다.

Unity를 실행할 수 있는 PC에서는 `README.md`의 실행·캡처 절차를 수행하고 Console 오류와 `Captures/M-001.png`~`M-011.png`를 확인한다. 해당 결과가 오면 컴파일/API 호환성과 화면 문제를 이어서 수정할 수 있다.
