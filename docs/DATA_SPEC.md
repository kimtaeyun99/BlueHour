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

## 저장 파일
`Application.persistentDataPath/SaveData/` 기준 제안. settings.json(그래픽/음향/입력/UI/언어/환경), encyclopedia.json(종별 관찰), journal.json(메모 및 사진 참조), album.json(사진 메타), experience.json(해역/위치/회전/감상 모드/환경 선택), Photos/ 및 Backup/. 각 JSON에 schemaVersion, savedAtUtc 포함. 사진 파일명은 생성 ID 기반, 상대 경로만 저장.

```json
{"schemaVersion":1,"savedAtUtc":"2026-01-01T00:00:00Z","observations":[{"speciesId":"example_placeholder","firstSeenUtc":"2026-01-01T00:00:00Z","regionId":"gbr","depthM":12.5,"localTimeOfDay":"day"}]}
```
위 예시는 형식 설명용이며 실제 종 데이터 아님. 메모는 noteId/speciesId/text/createdAt/updatedAt/photoIds, 사진은 photoId/path/capturedAt/regionId/depthM/speciesIds/favorite, 체험은 regionId/position/rotation/mode/environmentSettings. 파일 경로 탈출 방지, 유효 위치 검사, 중복 ID 제거 정책, 미래 schemaVersion은 무단 덮어쓰기 금지. 사용자 메모는 번역하지 않는다.

## 종 구성 및 검증
어류12/상어·가오리4/포유류2/파충류2/기타 무척추4/산호4/해초·조류2. 실제 종과 대표 5종은 미확정. 공생 후보(흰동가리-말미잘, 청소놀래기-대형 어류, 망둑어-딱총새우)는 실제 분포 및 에셋 검증 후 선정. 자연 이벤트 후보(고래 이동, 대형 군집, 가오리, 산호 산란, 발광, 거북 수면 이동)도 동일. 자료 출처 후보는 GBRMPA, AIMS, WoRMS, FishBase, IUCN 등이며 실제 자료 조사 및 라이선스 검증은 미수행.
