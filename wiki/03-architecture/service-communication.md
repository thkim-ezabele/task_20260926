---
title: "서비스 간 통신"
type: doc
status: draft
tags: [architecture]
created: 2026-09-27
updated: 2026-09-28
---

# 서비스 간 통신

> 동기 / 비동기 통신 방식의 선택 기준과 규칙입니다. 비동기 이벤트의 상세 규칙은 [이벤트 기반 아키텍처](event-driven-architecture.md)를 따릅니다.
>
> [위키 홈](../README.md)

## 동기 vs 비동기 선택 기준

| 상황 | 방식 | 예 |
|---|---|---|
| 클라이언트가 **즉시 결과**가 필요한 조회 · 명령 | 동기 HTTP (API Gateway 도입 전에는 서비스 Api 직접 호출, [ADR-0023](adr/0023-deferred-adoptions.md)) | 직원 조회, 긴급 상황 발령 요청 |
| 다른 서비스에 **상태 변화를 알림** | **비동기 통합 이벤트** | 직원 등록 → 연락망에 반영 |
| 다른 서비스의 데이터가 **조회에 필요** | 이벤트로 **자기 DB에 복제**한 데이터 사용 | 연락망 서비스가 직원 이름 · 채널을 복제 보유 |
| 서비스 간 즉시 응답이 꼭 필요하고 복제로 해결할 수 없음 | 동기 HTTP (예외, 최소화) | 🟡 해당 사례가 생기면 작업 문서에 사유 기록 |

원칙:

- **서비스 간 공유 DB는 없다.** 다른 서비스의 Database에 직접 접근하지 않는다([ADR-0002](adr/0002-adopt-msa.md), [데이터베이스](../04-development/database.md#database-per-service-원칙)).
- 기본은 **비동기**다. 서비스 간 동기 호출은 장애를 전파하고 결합도를 높이므로 최소화한다.
- 동기 호출 체인은 **한 단계까지만** 허용한다(A → B → C 연쇄 호출 금지).

## 동기 통신 (REST / gRPC)

- 외부(클라이언트) API는 **REST(HTTP/JSON)**다. 규칙은 [API 설계 가이드](../04-development/api-guidelines.md)를 따른다.
  - 코드값은 정수로 직렬화한다([ADR-0008](adr/0008-integer-codes-and-bitmask.md)).
  - 실패 응답은 RFC 9457 `ProblemDetails`(정수 에러 코드 포함)다.
- 서비스 간 동기 호출이 필요하면 REST를 기본으로 한다. gRPC는 🟡 필요성이 확인되면 ADR로 도입한다.
- 호출은 `IHttpClientFactory`의 타입 지정 클라이언트로 하고, Application에 정의한 포트(`IService` 상속)의 Infrastructure 구현으로 감싼다.
- 모든 HTTP 호출은 `traceparent`를 전파한다(OpenTelemetry 자동 계측).

## 비동기 통신 (이벤트)

- 상태 전파는 통합 이벤트(`*IntegrationEvent`, 과거형)로 한다.
- 발행은 Transactional Outbox, 소비는 Inbox로 멱등 처리한다.
- 메시지 헤더에 W3C `traceparent`를 넣어 추적을 잇는다.
- 브로커(RabbitMQ / Kafka) · 추상화(MassTransit 8.x 등) · Outbox / Inbox 구현은 보류다([ADR-0023](adr/0023-deferred-adoptions.md)).

상세: [이벤트 기반 아키텍처](event-driven-architecture.md), 이벤트 목록: [이벤트 카탈로그](../05-api/event-catalog.md)

### 데이터 복제

- 구독 서비스는 필요한 필드만 자기 DB에 복제한다(전체 복제 금지). 개인정보는 필요한 최소한만 복제한다.
- 복제 데이터는 **읽기 전용 사본**이다. 원본 소유 서비스만 변경하고, 사본은 이벤트로만 갱신한다.
- 복제 지연(최종 일관성)을 전제로 설계한다.

## API Gateway

> **보류**([ADR-0023](adr/0023-deferred-adoptions.md)): Gateway 프로젝트를 두지 않고 클라이언트가 서비스 Api를 직접 호출합니다. 재검토 때 YARP(추천안) / Ocelot 중 선택합니다. 아래 표는 도입 때의 책임 설계안입니다.

| 책임 | 내용 |
|---|---|
| 라우팅 | 외부 경로 → 서비스 (`/api/employees/**` → Employee 등) |
| 인증 | 토큰 검증 후 서비스로 전달 (🟡 Identity 설계에 따름) |
| 공통 처리 | 요청 제한(rate limit), CORS, `traceparent` 전파 |
| 하지 않는 것 | 업무 로직, 여러 서비스 응답 조합(BFF 필요 시 별도 검토) |

### 인증 정보 전파

- 🟡 Identity 서비스 설계(토큰 형식, 발급 · 검증 방식)는 해당 PRD에서 정합니다.
- 기본 방향: Gateway가 토큰을 검증하고, 서비스는 토큰의 클레임(사용자 ID, 권한 비트 마스크)을 사용한다. 서비스 간 동기 호출은 원래 요청의 토큰 또는 서비스 계정 토큰을 전달한다.
- 이벤트에는 토큰을 넣지 않는다. 필요한 경우 행위자 ID만 페이로드에 포함한다.

## 서비스 디스커버리

- 로컬 / 컨테이너 환경: docker compose 서비스 이름(DNS)과 설정 파일의 주소를 쓴다.
- 🟡 Kubernetes를 도입하면 Service DNS를 쓴다. 별도 디스커버리 서버(Consul 등)는 두지 않는 것을 기본으로 한다.
- 서비스 주소는 코드에 하드코딩하지 않고 설정으로 주입한다([설정 & 시크릿 관리](../06-deployment/configuration.md)).

## 장애 격리 (Timeout / Retry / Circuit Breaker)

동기 호출에는 반드시 회복성 정책을 적용합니다(🟡 Polly / `Microsoft.Extensions.Http.Resilience`).

| 정책 | 기본값 (🟡 조정) | 규칙 |
|---|---|---|
| Timeout | 호출당 짧게 (예: 2~5초) | 타임아웃 없는 호출 금지 |
| Retry | 일시 오류(5xx, 네트워크)만, 지수 백오프 + 지터, 소수 회 | 멱등하지 않은 요청(POST 등)은 멱등 키 없이 재시도 금지 |
| Circuit Breaker | 연속 실패 시 일정 시간 차단 | 차단 중에는 즉시 실패 → `Result` 오류로 변환 |
| Fallback | 기능별로 정의 | 긴급 전파 경로는 동기 의존을 두지 않는 것을 우선 |

- 외부 발송 사업자 호출(SMS / 푸시 / 메일)도 같은 정책을 적용한다.
- 재시도 · 회로 차단 발생은 `Warning` 로그로 남긴다([로깅 & 관측성](../04-development/logging-observability.md)).

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-27 | - | 초안 작성: 동기 / 비동기 선택 기준, REST 규칙, 이벤트 · 데이터 복제, API Gateway(미정), 인증 전파, 디스커버리, 장애 격리 |
| 2026-09-28 | developer | 브로커 · 메시징 추상화 · API Gateway 미정 표시를 보류([ADR-0023](adr/0023-deferred-adoptions.md) 링크)로 교체, Gateway 도입 전 직접 호출 명시 (S04-T02, BL-038) |
