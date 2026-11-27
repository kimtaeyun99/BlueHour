# 데이터 명세서
## 공통 계약 (제안)
안정적인 문자열 ID 사용(표시명 변경과 분리), SI 단위, 날짜 ISO 8601 UTC 및 해역 시간대 별도, schemaVersion 정수. 필수 필드 누락/중복 ID/범위 역전/참조 오류는 검증 실패. 정의 데이터와 사용자 저장 분리.

| 데이터 | 필드 후보 |
|---|---|
| SpeciesData | speciesId, nameKey, scientificName, taxonomy, descriptionKey, sizeMinM/MaxM, habitatIds, regionIds, depthRangeM, temperatureRangeC, salinityRange, activityProfile, movementProfile, dietIds, predatorIds, symbiosisIds, seasonalProfile, approachResponse, prefab, animationProfile, audioProfile, image, sourceIds |
| RegionDefinition | regionId, nameKey, timezone, boundsM, depthRangeM, habitatIds, spawnProfiles, environmentProfile, mapAsset |
| HabitatZone | habitatId, type, geometry, depthRangeM, substrate, speciesWeights |
| EnvironmentSnapshot | timestamp, season, weather, temperatureC, salinity, pressurePa, currentVectorMps, lightFactor |
| CreatureRuntime | instanceId, speciesId, position, velocity, state, variationSeed, sizeScale, speedScale, alertness; 비저장 |
| SpawnProfile | speciesId, habitatId, conditions, weight, min/maxGroup, activeCap, cooldown; 값 TBD |
| SourceRecord | sourceId, title, publisher, url, accessDate, supportedClaims, verificationStatus, license |

염분 단위는 자료에 따라 실용염분(무차원) 또는 g/kg를 명시하고 혼용하지 않는다. 압력은 절대압/게이지압 구분. 실제 종별 수치와 확률은 검증 전 placeholder 표시. 출현 가중치는 실제 빈도로 주장하지 않는다.

## M3 해역 및 환경 값

- 기준은 그레이트 배리어 리프 재구성 해역 `gbr`이다. 수면 높이 0m, 유영 한계 수심 150m, 수평 폭 500×500m를 사용한다. 여섯 서식지는 수평 구역으로 구분하고, 공유 경계는 동쪽 또는 북쪽 구역에 속한다.
- 현지 시각과 계절은 기기 UTC에서 퀸즐랜드 UTC+10을 적용해 구한다. 가속 모드는 단조 증가하는 세션 경과 시간을 60배 적용하며, 고정 모드는 연속해서 같은 시각을 유지한다.
- 날씨는 시드와 날짜에서 구름량을 계산하고 연속적으로 보간한다. 온도·염분·빛·해류는 프로토타입의 근사 모델이며 실측·예보 값으로 쓰지 않는다. 실용염분은 무차원이며, 절대압은 해수 밀도 1,025kg/m³, 표준중력 9.80665m/s², 수면 기압 101,325Pa를 가정한다. 해류는 월드 X/Z 방향 m/s로 제공한다.

## 저장 파일
`Application.persistentDataPath/SaveData/` 기준 제안. settings.json(그래픽/음향/입력/UI/언어/환경), encyclopedia.json(종별 관찰), journal.json(메모 및 사진 참조), album.json(사진 메타), experience.json(해역/위치/회전/감상 모드/환경 선택), Photos/ 및 Backup/. 각 JSON에 schemaVersion, savedAtUtc 포함. 사진 파일명은 생성 ID 기반, 상대 경로만 저장.

```json
{"schemaVersion":1,"savedAtUtc":"2026-01-01T00:00:00Z","observations":[{"speciesId":"example_placeholder","firstSeenUtc":"2026-01-01T00:00:00Z","regionId":"gbr","depthM":12.5,"localTimeOfDay":"day"}]}
```
위 예시는 형식 설명용이며 실제 종 데이터 아님. 메모는 noteId/speciesId/text/createdAt/updatedAt/photoIds, 사진은 photoId/path/capturedAt/regionId/depthM/speciesIds/favorite, 체험은 regionId/position/rotation/mode/environmentSettings. 파일 경로 탈출 방지, 유효 위치 검사, 중복 ID 제거 정책, 미래 schemaVersion은 무단 덮어쓰기 금지. 사용자 메모는 번역하지 않는다.

## 종 구성 및 검증
어류12/상어·가오리4/포유류2/파충류2/기타 무척추4/산호4/해초·조류2. M2 대표 5종과 한글 표시명은 아래 표에 확정했다. 전체 30종 목록은 미확정이다. 공생 후보(흰동가리-말미잘, 청소놀래기-대형 어류, 망둑어-딱총새우)는 실제 분포 및 에셋 검증 후 선정. 자연 이벤트 후보(고래 이동, 대형 군집, 가오리, 산호 산란, 발광, 거북 수면 이동)도 동일. 자료 출처 후보는 GBRMPA, AIMS, WoRMS, FishBase, IUCN 등이며 M2 대상의 확인 항목은 아래에 기록한다. 외부 아트·음향 에셋과 라이선스는 도입 시 별도 검증한다.

### M2 대표 5종

한글 표기는 프로젝트 표시명이다. 검증 상태는 그레이트 배리어 리프 분포와 아래 행동의 문헌 근거에 한한다. 접근 거리·속도·무리 크기는 테스트 씬용 설정값이다. 프리팹은 Unity 기본 메시와 프로젝트에서 제작한 재질을 사용한 임시 모델이며, 외부 사진·모델·음원은 포함하지 않았다.

| ID | 한글 표시명 · 학명 | 확인한 근거와 구현 범위 |
|---|---|---|
| amphiprion_melanopus | 시나몬 흰동가리 · *Amphiprion melanopus* | 해역 관찰과 숙주 관계 확인. 숙주 주변 유영 및 재접근을 검증한다. [현장 조사](https://researchonline.jcu.edu.au/41716/) · [숙주 선택 연구](https://researchonline.jcu.edu.au/27376/) |
| entacmaea_quadricolor | 버블팁 말미잘 · *Entacmaea quadricolor* | 해역 관찰과 숙주 관계 확인. 시나몬 흰동가리의 고정형 숙주로 배치한다. [현장 조사](https://researchonline.jcu.edu.au/41716/) |
| chromis_viridis | 그린 크로미스 · *Chromis viridis* | 산호 연관 군집 유영 확인. 분리·정렬·응집을 검증한다. [리저드섬 군집 유영 연구](https://pmc.ncbi.nlm.nih.gov/articles/PMC6215405/) |
| taeniura_lymma | 파란점가오리 · *Taeniura lymma* | 리저드섬 기록과 모래·암초 연관 서식 확인. 해저 인접 유영·휴식과 접근 회피를 검증한다. [호주박물관 현장 기록](https://lifg.australian.museum/Group.html?groupId=prUGMQjZ&hierarchyId=qBHH2Q3E) · [해양공원청 자료](https://elibrary.gbrmpa.gov.au/bitstreams/a470cd33-3085-4cde-abca-d5c0612ab40d/download) |
| acropora_millepora | 밀레포라 가지산호 · *Acropora millepora* | 케펠 제도 분포와 가지형 돌산호 확인. 고정형 표시를 검증한다. [AIMS 현장 연구](https://www.aims.gov.au/information-centre/news-and-stories/bleaching-can-hamper-reproduction-and-recovery-corals) |
