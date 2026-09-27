---
title: "S04: 테스트 보강 · 문서 · 인수 증빙"
type: sprint
sprint: "S04"
status: planned
prd: [PRD-001]
started:
finished:
adrs: []
worklogs: []
aliases: [S04]
tags: [delivery, sprint]
created: 2026-09-27
updated: 2026-09-27
---

# S04: 테스트 보강 · 문서 · 인수 증빙

- PRD: [PRD-001](../prd/PRD-001-foundation.md)
- 토픽 브랜치: `feature/prd-001-foundation` · 스프린트 종료 태그: `sprint/S04`

## 목표

> 모든 FR / NFR에 증빙(테스트, CI 실행, 문서, 재현 기록)이 붙고, 새 환경에서 문서만 보고 clone → 명령 1개로 실행할 수 있다.

## 작업

| ID | 작업 | 요구사항 | 완료 조건 | 의존 | 상태 | 커밋 |
|---|---|---|---|---|---|---|
| S04-T01 | 커버리지 점검 · 보강과 (선택) Aspire 스모크 테스트 | NFR-03, NFR-07, FR-03, FR-09 | CI 커버리지 보고에 BuildingBlocks · Employee Domain / Application 라인 커버리지가 기록되고, 80% 미만 영역은 보강하거나 사유와 함께 기술부채로 남는다. Aspire.Hosting.Testing 스모크 테스트는 CI 소요 시간(10분)을 측정한 뒤 도입 여부를 정하고 기록 | S03-T05 | todo | |
| S04-T02 | 기준 문서 갱신: clean-architecture, database, logging-observability, error-codes, coding-conventions | FR-11 | clean-architecture(`src/Aspire/*`, `tests/BuildingBlocks/*`, Gateway · `deploy/` 제거, Outbox 보류), database(UUID v7, DB 이름 · 계정, Aspire 연결 매핑, 리셋 정책, Employee 코드 표, ERD), logging-observability(Aspire 연동, Serilog → OTLP), error-codes(공통 · Employee), coding-conventions(SaveChanges 책임 예시)가 코드와 일치하고 `draft` 이상 | S03-T05 | todo | |
| S04-T03 | `todo` 문서 작성: local-setup, configuration, environments, ci-cd, troubleshooting | FR-11, NFR-04 | 5개 문서가 템플릿으로 `draft` 이상 작성된다. local-setup에 사전 준비, user-secrets 비밀번호, 볼륨 · 비밀번호 초기화, `dotnet ef --context` 사용법, ci-cd에 워크플로 단계와 커버리지 보고 위치 | S03-T04, S01-T07 | todo | |
| S04-T04 | 인수 증빙: 새 환경 재현, CI 실패 표시 확인, 대시보드 증빙 | FR-03, FR-10, FR-11, NFR-04, NFR-07 | 새 clone(스크래치 디렉터리, 볼륨 초기화)에서 local-setup만 따라 AppHost 명령 1개로 전체 실행 · 등록 → 조회 과정이 기록된다(worklog 재현 기록). 실패 테스트를 넣은 임시 브랜치에서 CI 실패 표시를 확인하고 실행 링크를 남긴 뒤 브랜치를 삭제한다. 토픽 PR 워크플로 통과와 소요 시간 10분 이내 기록 | T01, T02, T03 | todo | |

상태: `todo` · `doing` · `done` · `blocked`(반려 3회) · `moved:BL-NNN`(백로그로 이관)

### 작업별 파이프라인

| 작업 | dba | developer | reviewer | tester |
|---|---|---|---|---|
| S04-T01 | 해당 없음 | 부족분 단위 테스트 | 표준 진입 점검 | 커버리지 수치, CI 소요 시간 기록 |
| S04-T02 | database.md와 ERD 작성 / 검토 | 나머지 문서 수정 | 문서 ↔ 코드 대조(경로, 이름, 코드 번호), 링크 · frontmatter | 점검표: 문서 경로가 실제 디렉터리와 일치, error-codes 번호 ↔ 코드 상수 grep 대조, 링크 유효 |
| S04-T03 | local-setup의 DB 초기화 · 마이그레이션 명령 검토 | 작성 | 템플릿 · 링크 · 명령 정확성 | local-setup 명령을 그대로 실행해 동작 확인(재현은 T04) |
| S04-T04 | 해당 없음 | 증빙 절차 준비(실패 테스트는 병합하지 않음) | 증빙 항목 ↔ PRD 인수 조건 대조 | 재현 실행 · 기록. 임시 브랜치 push는 사용자 확인 필요(메모 N2) |

## 완료 기준 (DoD)

- [ ] 모든 작업이 `done`이거나 백로그로 이관되었다
- [ ] 빌드와 모든 테스트(단위 · 통합 · 아키텍처)가 통과했다
- [ ] 관련 위키 문서(API, 이벤트, DB)를 갱신했다
- [ ] 백로그 / 기술부채를 정리했다 (`new` 항목 없음)
- [ ] 토픽 브랜치를 push하고 `sprint/S04` 태그를 붙였다

## 진행 기록

> 단계 판정(PASS / REJECT / BLOCKED), 반려와 재작업, 계획 변경을 시간 순서로 적습니다.

| 날짜 | 작업 | 단계 | 판정 | 내용 (반려 시 되돌린 단계와 사유) |
|---|---|---|---|---|
| | | | | |

## 계획 리뷰

> `/sprint` 시작 시 에이전트별 계획 리뷰와 orchestrator 통합 결과, 사용자 승인 내용을 요약합니다.

분할 시 남긴 메모 (계획 리뷰에서 확정):

- **N2 CI 실패 확인 push**: FR-10의 실패 표시 확인은 임시 브랜치 push가 필요해 "push는 스프린트 종료 때만" 규칙과 부딪친다. 사용자 확인 후 예외로 허용할지, 스프린트 종료 push 직후 DoD 단계에서 할지 정한다.

## 결과 리뷰

> 스프린트 종료 시 orchestrator의 결과 리뷰(계획 대비 실제, 완료 조건 · FR 충족, 반려 분석)를 요약합니다.

-

## 생긴 백로그 / 기술부채

| ID | 제목 | 발생 작업 | 정리 결과 |
|---|---|---|---|
| | | | open / planned:SNN / dropped |

## 회고

### 잘된 점

-

### 문제

-

### 다음에 바꿀 것

-

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 스프린트 계획 (`/prd` PRD-001 분할) |
