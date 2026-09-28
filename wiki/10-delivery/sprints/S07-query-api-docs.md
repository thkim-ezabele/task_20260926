---
title: "S07: 조회 API, 조회 성능, 문서 마무리"
type: sprint
sprint: "S07"
status: planned
prd: [PRD-002]
started:
finished:
adrs: []
worklogs: []
aliases: [S07]
tags: [delivery, sprint]
created: 2026-09-27
updated: 2026-09-28
---

# S07: 조회 API, 조회 성능, 문서 마무리

- PRD: [PRD-002](../prd/PRD-002-employee-contacts.md)
- 토픽 브랜치: `feature/prd-002-employee-contacts` · 스프린트 종료 태그: `sprint/S07`
- 스프린트 번호는 S05-T01에서 확정했다(PRD-001 S01~S04 다음 번호, PRD `sprints` · 파일 이름 · 작업 ID 일치).

## 목표

> POST로 등록한 직원을 `GET /api/employee`(페이징)와 `GET /api/employee/{name}`(동명이인이면 입사일이 빠른 1명)으로 조회한다. 10,000건에서 두 조회가 200ms 안에 끝나고, Aspire로 띄운 상태에서 문서의 curl 예시가 모두 동작한다.

## 작업

| ID | 작업 | 요구사항 | 완료 조건 | 의존 | 상태 | 커밋 |
|---|---|---|---|---|---|---|
| S07-T01 | 목록 조회 Query와 `GET /api/employee` | FR-07, FR-10 | `page` 기본 1 · 상한 100,000, `pageSize` 기본 20 · 1~100. 숫자 아님 1001, 범위 밖 1003. 목록 · 개수를 따로 조회해 Handler가 합침. 응답 `{ items, totalCount, page, pageSize }`. 단위 테스트(Validator, Handler) 통과 | S06-T06 | todo | |
| S07-T02 | 이름 조회 Query와 `GET /api/employee/{name}` | FR-08, FR-10 | trim + NFC 뒤 정확 일치(대소문자 구분), 동명이인이면 `joined_on`, `id` 순 첫 1명. 없으면 404(Employee 대상 없음), 공백 제거 뒤 빈 이름 400. 단위 테스트 통과, 로그에 이름 값 없음 | S06-T06 | todo | |
| S07-T03 | 조회 통합 테스트와 10,000건 조회 성능 측정 | FR-07, FR-08, FR-10, NFR-03, NFR-04 | ① 페이징 경계: 25건에서 `page=2&pageSize=10` → 11~20번째, 마지막 페이지 초과 → 빈 `items` · 200, `page=0` · `pageSize=101` → 1003, `page=abc` → 1001 ② 한글 URL 200, NFD 조회, 없는 이름 404, 동명이인 3명 중 가장 빠른 1명 ③ 10,000건 fixture 시더(마이그레이션 시드 아님)로 두 조회 측정값을 진행 기록에 남김(200ms 이내, CI 임계값 2배) ④ 404 본문과 로그에 이름 값 없음 ⑤ 대응표 나머지(1001 HTTP)를 닫아 "이전 대기" 0건 | T01, T02 | todo | |
| S07-T04 | 문서 마무리와 Aspire curl 실행 기록 (문서 작업) | FR-11 | API 명세(3개 엔드포인트, 행 오류 경로 규칙, `curl -F file=@` · `-F data=` · `--data-binary` + Content-Type 예시), error-codes · database(일괄 트랜잭션 예외 링크, 리셋 이력) · local-setup 등록 · 조회 예시가 draft 이상. Aspire로 띄운 상태에서 curl 예시를 모두 실행하고, **BL-024** 대시보드 추적 화면에서 `db.connection_string`에 비밀번호가 없고 span 태그에 등록 값이 없음을 확인해(마스킹 캡처를 `evidence/S07-T04/`에) 진행 기록에 명령 · 출력을 남기고, worklog 반영은 사용자에게 안내(`/worklog`) | T03 | todo | |

상태: `todo` · `doing` · `done` · `blocked`(반려 3회) · `moved:BL-NNN`(백로그로 이관)

### 작업별 파이프라인

| 작업 | dba | developer | reviewer | tester |
|---|---|---|---|---|
| S07-T01 | 해당 없음(S05-T06 인덱스 사용 확인만) | TDD | ADR ① 경로 예외, CQRS 읽기 경로 | 인수 조건 대조(HTTP는 T03) |
| S07-T02 | 해당 없음 | TDD | 개인정보(경로 매개변수 로깅, 요청 로깅의 path 기록 여부) | 인수 조건 대조(HTTP는 T03) |
| S07-T03 | 시더 구성, EXPLAIN으로 인덱스 사용 확인 | 시더 · fixture 준비 | 표준 진입 점검 | 주 작성자 |
| S07-T04 | database 문서 검토 | 문서 작업 | 문서 규칙, 문서의 코드 번호와 error-codes 일치 | 명령 점검표: curl 예시 실행 출력(상태 코드 · `code`) |

## 완료 기준 (DoD)

- [ ] 모든 작업이 `done`이거나 백로그로 이관되었다
- [ ] 빌드와 모든 테스트(단위 · 통합 · 아키텍처)가 통과했다 (경고 0, 커버리지 보고 — NFR-06)
- [ ] 관련 위키 문서(API, 이벤트, DB)를 갱신했다
- [ ] 백로그 / 기술부채를 정리했다 (`new` 항목 없음)
- [ ] 토픽 브랜치를 push하고 `sprint/S07` 태그를 붙였다

## 진행 기록

> 단계 판정(PASS / REJECT / BLOCKED), 반려와 재작업, 계획 변경을 시간 순서로 적습니다.

| 날짜 | 작업 | 단계 | 판정 | 내용 (반려 시 되돌린 단계와 사유) |
|---|---|---|---|---|
| | | | | |

## 계획 리뷰

> `/sprint` 시작 시 에이전트별 계획 리뷰와 orchestrator 통합 결과, 사용자 승인 내용을 요약합니다.

분할 시 남긴 메모:

- POST(S06)를 먼저 끝냈으므로, POST로 넣은 데이터로 curl 조회를 시연할 수 있다.
- T03에서 PRD-001 증빙 대응표의 "이전 대기"를 모두 닫는다.
- S07-T02: ADR-0025 A — RequestPath · instance(ErrorProblemDetails.cs:55) · url.path 세 곳 라우트 템플릿화, 기존 instance 계약 테스트 수정 포함([ADR-0025](../../03-architecture/adr/0025-api-rule-exceptions-for-assignment-endpoints.md#이름-경로-매개변수와-개인정보), S05-T02 대리 확인).

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
| 2026-09-27 | - | 스프린트 계획 (`/prd` PRD-002 분할, 가번호) |
| 2026-09-28 | - | BL-024 편입: T04에 대시보드 추적 수동 확인 추가 |
| 2026-09-28 | developer | S05-T01: 스프린트 번호 확정, T01 dba 참조 `S05-T05` → `S05-T06`(S05 작업 재구성) |
| 2026-09-28 | developer | S05-T02: 계획 메모에 S07-T02 라우트 템플릿 세 곳(ADR-0025) 항목 추가 |
