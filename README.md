# unity-ai-avatar-camera-system

Unity cross-platform camera system with AWS S3 integration for AI avatar generation

## 🛠 Tech Stack

- Unity 2021.3+
- C#
- AWS SDK (S3, Cognito)
- UniTask
- DOTween
- Unity WebCamTexture
- Cross-Platform Development

## ⭐ Key Features

- 크로스 플랫폼 카메라 시스템
- 실시간 권한 관리
- AWS S3 자동 업로드
- 플랫폼별 이미지 처리
- AI 분석 결과 처리
- 파츠 조합 알고리즘
- 앱 생명주기 관리
- 진행률 기반 UI 연동
- 메모리 최적화

## 🎮 How It Works

1. 카메라 권한 확인 및 요청
2. 실시간 카메라 프리뷰 표시
3. 사진 촬영 또는 갤러리 이미지 선택
4. AWS S3에 이미지 업로드
5. AI 서버로 아바타 생성 요청
6. 생성된 아바타 적용

## 🎯 System Flow

1. **권한 관리**: 앱 실행/포커스 시 카메라 권한 자동 체크
2. **카메라 초기화**: iOS/Android 각각 최적화된 설정 적용
3. **이미지 처리**: 플랫폼별 회전/크기 조정 자동 처리
4. **클라우드 업로드**: AWS S3 비동기 업로드 및 URL 생성
5. **AI 분석 처리**: 서버 응답 데이터 파싱 및 검증
6. **파츠 조합**: 필수/선택 파츠 분류 및 랜덤 조합
7. **진행률 관리**: 각 단계별 로딩 상태 실시간 업데이트

## 🎨 Avatar Generation Logic

- **카테고리 분류**: 필수/선택 파츠 자동 분류
- **색상 동기화**: AI 분석 색상을 다른 파츠에 적용
- **확률 시스템**: 선택적 파츠의 랜덤 장착/비장착
- **데이터 조합**: AI 결과 + 무료 아바타 파츠 합성

## 🔧 Platform Optimization

- **iOS**: 전면/후면 카메라 회전 및 확대 처리
- **Android**: 플랫폼별 카메라 방향 자동 보정
- **권한 시스템**: 실시간 권한 상태 모니터링
- **메모리 관리**: 텍스처 자동 해제 및 리소스 정리

> **참고**: 보안상 AWS 설정값들이 마스킹 처리되어 있습니다.
