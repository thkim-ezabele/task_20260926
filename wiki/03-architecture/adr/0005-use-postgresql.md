# ADR-0005: PostgreSQL 채택

- 상태: 승인
- 날짜: 2026-09-27
- 결정자:

## 배경 (Context)

> TODO: 결정이 필요했던 배경

## 검토한 대안 (Options)

> TODO: 검토한 대안과 장단점

## 결정 (Decision)

서비스 데이터 저장소로 **PostgreSQL** 을 사용합니다.

> TODO: 선정 사유 상세

## 결과 (Consequences)

- EF Core와 Npgsql 프로바이더를 사용합니다.
- 서비스별로 별도 Database(또는 Schema)를 사용합니다.
- 로컬 개발과 통합 테스트는 컨테이너 기반 PostgreSQL을 사용합니다.
