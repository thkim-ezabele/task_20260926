---
title: "API 레퍼런스"
type: doc
status: draft
tags: [api]
created: 2026-09-27
updated: 2026-09-28
---

# API 레퍼런스

> 서비스별 API 목록과 Swagger 문서 위치를 안내합니다.
>
> [위키 홈](../README.md)

## Swagger 접속 정보

Development 환경에서만 노출합니다([ADR-0019](../03-architecture/adr/0019-use-swashbuckle-openapi.md)). 문서 이름은 경로의 주 버전과 같은 `v1`입니다.

| 서비스 | OpenAPI JSON | Swagger UI | 로컬 주소(launchSettings `http`) |
|---|---|---|---|
| Employee | `/swagger/v1/swagger.json` | `/swagger` | `http://localhost:5180` (AppHost 실행 시 주소는 대시보드 기준, S03-T05) |

## 인증 방법

> TODO:

## 서비스별 API 목록

| 서비스 | 명세 | 엔드포인트 |
|---|---|---|
| Employee | [직원 API](employee-api.md) | 없음(헬스 경로만). PRD-001 `POST /api/v1/employees` · `GET /api/v1/employees/{id}`는 S05-T04에서 제거, PRD-002 API는 S06-T05 · S07 |

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-28 | developer | Swagger 접속 정보, 서비스별 API 목록(Employee → [직원 API](employee-api.md)) (S03-T04) |
| 2026-09-28 | developer | Employee 엔드포인트 목록을 샘플 API 제거 뒤 상태로 (S05-T04) |
