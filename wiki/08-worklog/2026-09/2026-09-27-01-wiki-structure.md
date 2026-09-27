# WL-2026-09-27-01: 위키 기본 구조 및 초기 ADR 작성

- 날짜: 2026-09-27
- 도구 / 모델: Claude Code
- 세션 ID: 알 수 없음 (하드 드라이브 고장으로 세션 기록 유실)
- 관련: ADR-0001 ~ ADR-0006

> ⚠️ 이 로그는 **사후에 복원한 기록**입니다. 원래 세션 기록과 프롬프트 원문은 드라이브 고장으로 사라졌고, 남은 산출물(`wiki/`)과 다음 세션의 설명을 바탕으로 작성했습니다.

## 목표

- Emergency Hub(직원 긴급연락망 서비스) 백엔드 위키의 기본 구조를 잡는다.
- 핵심 기술 스택과 아키텍처 결정을 ADR로 남긴다.

## 주요 프롬프트

> 원문 유실. 요약만 남깁니다.

1. 위키 기본 구조 구성 요청 (.NET 8, MSA, Clean Architecture, DDD / EDA / TDD, PostgreSQL, GitHub Actions)
2. 과제형 프로젝트이므로 ADR과 별개로 세션별 작업 로그를 남기는 방안 논의 (→ WL-2026-09-27-02에서 이어서 진행)

## 작업 내용

- `wiki/` 아래 00~07 섹션 구조와 문서 뼈대 생성 (대부분 `> TODO:` 상태)
- 문서 작성 규칙, 상태 범례 정의 (`wiki/README.md`)
- ADR 템플릿과 초기 ADR 6건 작성

### 변경 파일

| 파일 | 변경 | 설명 |
|---|---|---|
| `wiki/README.md` | 추가 | 위키 홈, 목차, 작성 규칙 |
| `wiki/00-overview/**` ~ `wiki/07-operations/**` | 추가 | 섹션별 문서 뼈대 |
| `wiki/03-architecture/adr/0000~0006` | 추가 | ADR 템플릿과 초기 ADR |

## 결정 사항

| 결정 | 이유 | ADR |
|---|---|---|
| .NET 8 채택 | - | [0001](../../03-architecture/adr/0001-use-dotnet8.md) |
| MSA 채택 | - | [0002](../../03-architecture/adr/0002-adopt-msa.md) |
| Clean Architecture + DDD | - | [0003](../../03-architecture/adr/0003-clean-architecture-and-ddd.md) |
| EDA 채택 | - | [0004](../../03-architecture/adr/0004-adopt-event-driven-architecture.md) |
| PostgreSQL 채택 | - | [0005](../../03-architecture/adr/0005-use-postgresql.md) |
| TDD 채택 | - | [0006](../../03-architecture/adr/0006-adopt-tdd.md) |

## 이슈 / 배운 점

- 로컬 폴더가 git 저장소가 아니었고 백업도 없어서, 드라이브 고장으로 세션 기록을 잃었다. → 다음 세션에서 git 저장소를 구성했다.

## 다음 할 일

- [x] Worklog 체계 확정 (→ WL-2026-09-27-02)
