# 기술 설계서
## 기준 및 의존성
Unity 6000.3.7f1, URP, C#. Input System, Localization, Test Framework, Shader Graph/HLSL, Audio Mixer, Cinemachine은 후보/제안이며 설치 버전은 M0 호환 검증 및 승인 후 고정. OpenXR/XR Interaction Toolkit은 VR 단계. Git으로 ProjectSettings, Packages, Assets 및 .meta 관리. Library/Temp/Logs/빌드 산출물 제외.

## 모듈 구조 (제안)
`Assets/BlueHour/{Core,World,Environment,Creatures,Observation,Photography,Experience,UI,Audio,Persistence,Localization,Tests,Art}`. asmdef는 실제 의존성 경계에 맞게 구성. Core는 Unity 표현에 의존하지 않는 계약/공통 타입 중심. 조립은 Bootstrap/Composition Root에서 수행하며 전역 싱글톤 남용 금지.

| 모듈 | 책임 / 후보 타입 |
|---|---|
| World | RegionDefinition, HabitatZone, WorldLoader, BoundaryService |
| Environment | EnvironmentClock, WeatherService, EnvironmentSnapshot, EnvironmentSampler |
| Creatures | SpeciesData(SO), CreatureRuntime(C#), CreatureView(Mono), CreatureStateMachine, SpawnService, SchoolingService |
| Experience | SwimController, InputAdapter, CameraModeController, TourPath, FollowTargetService |
| Observation | ObservationService, EncyclopediaRepository, JournalService |
| Photography | PhotoCaptureService, AlbumRepository |
| Persistence | SaveCoordinator, JsonRepository, MigrationRegistry |
| UI/Audio | Presenter, SettingsService, AudioDirector |

## 데이터 흐름
환경 시계/날씨 → 환경 스냅샷 → 구역별 샘플 → 스폰 및 AI → CreatureView/음향. 관찰/사진 이벤트 → 기록 서비스 → 저장 요청 → UI 갱신. SO 정의는 읽기 전용, 런타임 개체 상태 별도. 이벤트는 변경 시 발행하고 구독 생명주기 관리. 저장 DTO는 Unity 오브젝트 참조를 갖지 않는다.

## 생태 행동
FSM은 종별 허용 상태와 전이 조건을 데이터로 구성. Idle/Swim/Rest/School/Forage/Chase/Flee/Hide/Symbiosis/Migrate 후보. 우선순위 예: 경계 회피 > 위험 회피 > 조건 행동 > 일반 유영. 개체별 난수는 테스트 시 seed 주입 가능. 군집은 separation/alignment/cohesion 및 장애물 회피를 합성하고 속도/회전 제한. 공간 해시 또는 구역 인덱스로 이웃 검색 비용 제한. 수중 3D 이동은 지상 NavMesh에 의존하지 않으며 충돌 검사 및 구역 경계 적용. 고정형 산호/해초는 이동 FSM 생략. 추격 종료는 거리/시간/조건 기반이며 실제 피해 없음.

## 환경 및 스폰
환경 값은 위치/수심/시각/계절 기반 데이터 보간. 압력은 교육용 근사식(절대압 p=p0+ρgh) 사용 가능하나 단위 및 가정 명시. 빛 감쇠는 파장별 근사 또는 시각용 곡선. 날씨 전이는 내부 상태 머신. 스폰은 서식 조건 필터 → 가중치 선택 → 유효 위치 검사 → 풀 활성화. 카메라 가시성 및 거리 히스테리시스로 팝인 방지. 생태 이벤트는 조건/쿨다운/확률 및 동시 발생 제한. 정확한 수치는 TBD.

## 저장 안정성
데이터별 직렬화 → 동일 볼륨 tmp 작성/flush → 검증 → 원자적 교체 가능한 플랫폼 API 사용 → 이전 정상 파일 bak 유지. 실패 시 기존 정상 파일 유지. 로드 primary→bak→검증된 tmp 후보, 전부 실패 시 해당 데이터 기본값 및 오류 안내. 버전 마이그레이션과 교차 파일 참조 정합성 검사. 사진은 파일 저장 성공 후 메타데이터 커밋; 중단 후 고아 파일/누락 참조 복구 정책 필요. 저장 요청 debounce 및 단일 writer. 종료 저장만 의존하지 않음.

## 성능
FHD 60 FPS 목표, 프레임 예산 16.67ms. CPU/GPU 병목 분리 측정. 풀링, LOD, 컬링, GPU Instancing, 거리별 AI tick, 구역 활성화, NonAlloc 및 이벤트 기반 UI. NonAlloc 버퍼 포화 시 누락 방지 정책 필요. Jobs/Burst는 측정 후 승인된 부분만 적용. 기준 PC, 생물 상한, 품질 프리셋 수치는 TBD. VR은 별도 프레임 예산 및 기기 테스트.
