---
title: "아키텍처 결정 기록 (ADR)"
type: index
status: stable
tags: [adr, architecture]
created: 2026-09-27
updated: 2026-09-27
---

# 아키텍처 결정 기록 (ADR)

> 주요 기술/아키텍처 의사결정의 배경과 결과를 기록합니다.
>
> [위키 홈](../../README.md)

## 작성 방법

1. [ADR 템플릿](../../_templates/adr.md)으로 새 문서를 만듭니다 (Obsidian: Templates 플러그인).
2. 파일명은 `NNNN-kebab-case-title.md` 형식으로 하고, frontmatter의 `adr`, `aliases`, 제목의 번호를 맞춥니다.
3. 작성한 ADR을 아래 목록에 추가합니다.
4. 결정이 바뀌면 기존 ADR의 본문은 수정하지 않습니다. 새 ADR에 `supersedes`를 적고, 기존 ADR의 frontmatter만 `status: superseded`, `superseded_by`로 바꿉니다.

## ADR 목록

| 번호 | 제목 | 상태 | 날짜 |
|---|---|---|---|
| [0001](0001-use-dotnet8.md) | .NET 8 채택 | 승인 | 2026-09-27 |
| [0002](0002-adopt-msa.md) | 마이크로서비스 아키텍처(MSA) 채택 | 승인 | 2026-09-27 |
| [0003](0003-clean-architecture-and-ddd.md) | Clean Architecture + DDD 적용 | 승인 | 2026-09-27 |
| [0004](0004-adopt-event-driven-architecture.md) | 이벤트 기반 아키텍처(EDA) 채택 | 승인 | 2026-09-27 |
| [0005](0005-use-postgresql.md) | PostgreSQL 채택 | 승인 | 2026-09-27 |
| [0006](0006-adopt-tdd.md) | TDD 개발 방법론 채택 | 승인 | 2026-09-27 |
| [0007](0007-adopt-cqrs.md) | CQRS 적용 | 승인 | 2026-09-27 |
| [0008](0008-integer-codes-and-bitmask.md) | 코드값 정수화와 비트 마스킹 | 승인 | 2026-09-27 |
| [0009](0009-separate-read-write-db-context.md) | 읽기 / 쓰기 DB 연결 분리와 Repository 규칙 | 승인 | 2026-09-27 |
| [0010](0010-convention-based-di-registration.md) | 규칙 기반 DI 자동 등록 | 승인 | 2026-09-27 |
