---
title: "ADR-0005: PostgreSQL 채택"
type: adr
adr: "0005"
status: accepted
date: 2026-09-27
deciders: []
aliases: [ADR-0005]
tags: [adr, architecture]
created: 2026-09-27
updated: 2026-09-27
---

# ADR-0005: PostgreSQL 채택

## 배경 (Context)

- 서비스마다 자기 Database를 갖는 구조(ADR-0002)에서, 모든 서비스가 공통으로 쓸 관계형 데이터 저장소가 필요하다.
- 직원 · 조직 · 연락망 · 긴급 상황 · 응답 데이터는 관계가 분명하고 무결성(외래 키, 체크 제약)이 중요하다.
- Outbox payload, 외부 발송 응답처럼 반정형 데이터도 함께 다룬다.
- 로컬 개발과 통합 테스트를 컨테이너로 쉽게 띄울 수 있어야 하고, 라이선스 비용이 없어야 한다.

## 검토한 대안 (Options)

1. **PostgreSQL**: 장점: 오픈소스, 강한 무결성 기능(체크 제약, 부분 인덱스), `jsonb`, `timestamptz`, EF Core Npgsql 프로바이더가 성숙. 단점: 대규모 수평 확장은 별도 설계(복제본, 파티셔닝)가 필요하다.
2. **MySQL**: 장점: 널리 쓰이고 운영 자료가 많다. 단점: 체크 제약 · 부분 인덱스 · JSON 처리 등 무결성 · 표현 기능이 PostgreSQL보다 약하다.
3. **SQL Server**: 장점: .NET과 통합이 가장 좋다. 단점: 상용 라이선스 비용, 컨테이너 이미지 크기와 리소스 부담.
4. **MongoDB(문서 DB)**: 장점: 스키마가 유연하다. 단점: 관계 · 무결성이 중요한 이 도메인에 맞지 않고, 트랜잭션 · Outbox 구현이 번거롭다.

## 결정 (Decision)

서비스 데이터 저장소로 **PostgreSQL** 을 사용합니다.

선정 사유:

- 체크 제약과 정수 코드 · 비트 마스킹(ADR-0008)으로 **데이터 무결성을 DB에서도 보장**할 수 있다.
- `jsonb`로 Outbox payload 등 반정형 데이터를 같은 DB, **같은 트랜잭션**에서 다룬다(ADR-0004).
- **EF Core + Npgsql** 프로바이더와 `xmin` 기반 낙관적 잠금을 쓸 수 있다.
- Testcontainers로 통합 테스트에서 실제 PostgreSQL을 쓸 수 있다.
- 읽기 복제본으로 조회 부하를 나눌 수 있어, 읽기 / 쓰기 연결 분리(ADR-0009) 구조와 맞는다.

## 결과 (Consequences)

- EF Core와 Npgsql 프로바이더를 사용합니다.
- 서비스별로 별도 Database(또는 Schema)를 사용합니다.
- 로컬 개발과 통합 테스트는 컨테이너 기반 PostgreSQL을 사용합니다.
- 후속: 연결 분리와 Repository 규칙은 [ADR-0009](0009-separate-read-write-db-context.md), 설계 규칙은 [데이터베이스](../../04-development/database.md)를 따른다.

> 2026-09-27: 작성 당시 비워 둔 배경 · 대안 · 선정 사유를 보완했습니다(결정 변경 없음).
