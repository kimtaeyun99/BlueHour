# Blue Hour
몰입형 해양 체험 콘텐츠 | 기획 기준 v1.0

Blue Hour는 실제 해역을 바탕으로 바닷속을 자유롭게 탐험하고 생물을 관찰하며 휴식하는 비게임 콘텐츠다. Windows PC 우선 출시 후 VR로 확장한다.

## 기준 환경
Unity 6.3 LTS **6000.3.7f1**, C#, URP, Git/GitHub. FHD 60 FPS 목표(기준 하드웨어 미정). 외부 웹 API 및 계정 서버 없음. 한국어/영어, 무료 출시. 초기 해역은 그레이트 배리어 리프를 재구성한 500×500m, 수심 0~150m 월드, 생물 30종 목표.

## 문서 참조 순서
1. [AGENTS.md](AGENTS.md): 모든 작업의 규칙.
2. [GDD](docs/GDD.md): 제품 범위와 사용자 경험.
3. [기능 명세](docs/FUNCTIONAL_SPEC.md), [기술 명세](docs/TECHNICAL_SPEC.md), [데이터 명세](docs/DATA_SPEC.md): 구현 계약.
4. [UI/UX](docs/UI_UX_SPEC.md), [아트 가이드](docs/ART_GUIDE.md): 표현과 조작.
5. [개발 계획](docs/DEVELOPMENT_PLAN.md), [테스트 계획](docs/TEST_PLAN.md): 작업 및 승인 기준.

## 시작 방법
이 문서 세트를 Unity 프로젝트 루트에 배치한다. 기존 프로젝트가 있으면 ProjectVersion.txt와 Packages/manifest.json을 먼저 확인한다. 없다면 사용자가 지정 버전의 URP 프로젝트를 생성하거나 승인된 방식으로 생성한다. Codex는 M0부터 시작하며 다음 단계는 사용자 승인 전 진행하지 않는다.

## 문서 상태
확정 = 사용자 결정. 제안 = 구현 기본안이며 검증 후 변경 가능. TBD = 미결정, 임의 확정 금지. 모든 문서는 설계 산출물이며 실제 코드, 에셋, 빌드, 과학 자료 검증을 완료했다는 의미가 아니다. 프로젝트 명칭의 상표/스토어 중복 여부도 미검증이다.
