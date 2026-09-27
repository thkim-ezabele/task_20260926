---
title: "CI/CD (GitHub Actions)"
type: doc
status: draft
tags: [deployment]
created: 2026-09-27
updated: 2026-09-28
---

# CI/CD (GitHub Actions)

> GitHub Actions PR 워크플로(빌드 · 형식 검사 · 테스트 · 커버리지)의 트리거, 단계, 산출물과 실패 때 동작입니다. 배포(CD)는 아직 없습니다.
>
> [위키 홈](../README.md)

## 파이프라인 개요

| 항목 | 값 |
|---|---|
| 워크플로 | [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml)(이름 `ci`), 잡 1개 `build-test`(표시 이름 `build · format · test · coverage`) |
| 트리거 | `pull_request`, 대상 브랜치 `develop` · `main`. 기본 활동 유형(opened · synchronize · reopened)이라 **Draft PR에서도 실행**된다. push 트리거는 없다 |
| 러너 | `ubuntu-24.04`, `timeout-minutes: 20`(NFR-07 목표는 10분) |
| 권한 · 비밀 | `permissions: contents: read`, GitHub Secrets를 쓰지 않는다([설정 & 시크릿 관리](configuration.md#시크릿-관리-user-secrets--github-secrets)) |
| 동시성 | `ci-<PR 번호>` 그룹, 새 push가 오면 진행 중인 실행을 취소(`cancel-in-progress: true`) |
| SDK · 액션 | SDK는 `global.json`으로 설치(`actions/setup-dotnet`). 액션은 커밋 SHA로 고정하고 버전 주석을 단다([패키지 버전 · GitHub Actions](../03-architecture/package-versions.md#github-actions)) |
| 구성 | `Release`, `DOTNET_NOLOGO` · `DOTNET_CLI_TELEMETRY_OPTOUT` · `DOTNET_SKIP_FIRST_TIME_EXPERIENCE` = `true` |

## CI (빌드 / 테스트 / 정적 분석)

단계(워크플로 순서):

| # | 단계 | 명령 · 동작 | 실패 때 |
|---|---|---|---|
| 1 | Start step timer | 잡 시작 시각 기록 | - |
| 2 | Checkout | `persist-credentials: false` | - |
| 3 | Setup .NET (global.json) | `global-json-file: global.json` | - |
| 4 | .NET info | `dotnet --info` | - |
| 5 | Restore tools | `dotnet tool restore`(`dotnet-ef`, `reportgenerator`) | - |
| 6 | Restore | `dotnet restore EmergencyHub.sln` | - |
| 7 | Build | `dotnet build EmergencyHub.sln --no-restore --configuration Release`. 경고 = 오류(`Directory.Build.props` `TreatWarningsAsErrors`) | 경고 하나로 실패 |
| 8 | Format check | `dotnet format EmergencyHub.sln --verify-no-changes --no-restore` | 형식 차이가 있으면 실패 |
| 9 | Pull PostgreSQL image | `dotnet msbuild <통합 테스트 csproj> -getProperty:EmergencyHubPostgresImageTag`로 태그를 읽어 `docker pull postgres:<태그>`. 워크플로에 태그 리터럴 없음 | 태그를 못 읽으면 `::error::` 뒤 실패 |
| 10 | Test (coverage) | `dotnet test EmergencyHub.sln --no-build --configuration Release --collect "XPlat Code Coverage" --settings coverlet.runsettings --logger trx --results-directory TestResults`. 단위 · 통합(Testcontainers) · 아키텍처 테스트를 함께 실행. `EMERGENCYHUB_CONTAINER_LOG_DIRECTORY` = `${{ runner.temp }}/container-logs` | 테스트 1건이라도 실패하면 실패 |
| 11 | Coverage report | `dotnet tool run reportgenerator "-reports:TestResults/*/coverage.cobertura.xml" "-targetdir:coveragereport" "-reporttypes:Html;TextSummary;MarkdownSummaryGithub"`, `Summary.txt`를 로그에, `SummaryGithub.md`를 잡 요약에 | **앞 단계가 실패하면 건너뜀**(아래) |
| 12 | Upload test results (trx) | 아티팩트 `test-results`(`TestResults/**/*.trx`) | `if: always()`: 실패해도 올림 |
| 13 | Upload coverage report | 아티팩트 `coverage-report`(`coveragereport/`) | `if: always()`. 11단계를 건너뛰었으면 파일이 없어 올리지 않음(`if-no-files-found: ignore`) |
| 14 | Upload container logs | 아티팩트 `container-logs`(통합 테스트 fixture가 남긴 컨테이너 로그) | `if: failure()`: **실패한 실행만** |
| 15 | Step times summary | 단계별 소요 시간(초) 표를 잡 요약에 | `if: always()` |

- 아티팩트 보관 기간은 모두 14일입니다.
- **테스트가 실패하면 Coverage report 단계는 건너뜁니다**(BL-062). 이 단계에는 `if:` 조건이 없어 기본값(앞 단계가 모두 성공일 때만 실행)을 따릅니다. 실패한 실행에서는 커버리지 요약이 잡 요약에 없고, trx(`test-results`) · 컨테이너 로그(`container-logs`) 아티팩트와 단계별 시간 표로 원인을 봅니다. 커버리지는 통과한 실행에서만 보고합니다.
- 단계별 시간: 6 ~ 11단계가 `trap ... EXIT`로 `$RUNNER_TEMP/step-times.md`에 한 줄씩 적고(실패한 단계도 기록) 15단계가 잡 요약 표로 싣습니다(NFR-07).
- 로컬에서 같은 명령을 같은 순서로 실행할 수 있습니다(Git Bash 명령 목록은 [테스트 전략 · CI](../04-development/testing-strategy.md#ci)). 로컬 Docker Engine API가 1.43 이하면 Test 명령 앞에 `DOCKER_API_VERSION=1.43`이 필요합니다([트러블슈팅 · Docker 관련 문제](../01-getting-started/troubleshooting.md#docker-관련-문제)).

### 커버리지 보고

- **대상**: `coverlet.runsettings`의 `Include` 6개 어셈블리(BuildingBlocks Domain · Application · Infrastructure · Api, Employee Domain · Application). 대상 · 제외 규칙과 80% 목표(보고만, 임계값 실패 없음)의 원본은 [테스트 전략 · 커버리지 기준](../04-development/testing-strategy.md#커버리지-기준)입니다.
- **보는 곳**:

| 위치 | 내용 |
|---|---|
| 실행 로그 Coverage report 단계 | `Summary.txt`(TextSummary): 어셈블리별 · 합계 라인 · 분기 · 메서드 커버리지 |
| 실행 잡 요약(Summary 탭) | `SummaryGithub.md`(MarkdownSummaryGithub) 표, 단계별 시간 표 |
| 아티팩트 `coverage-report` | HTML 보고서(`index.html`) 전체 |

- 메서드 커버리지: ReportGenerator 무료판의 MarkdownSummaryGithub(잡 요약)는 메서드 커버리지 칸에 `Feature is only available for sponsors`를 표시합니다(BL-060, 실행 36340575571 아티팩트에서 확인). 라인 · 분기는 정상이고, 메서드 수치는 로그의 `Summary.txt`(`Method coverage: ...`)에 있습니다. NFR-03 판정은 라인 커버리지입니다.
- **ArchitectureTests 수집기 없음 메시지**(TD-024): `EmergencyHub.ArchitectureTests`에는 `coverlet.collector`를 넣지 않으므로 커버리지 실행 때 아래 메시지만 내고 테스트는 통과합니다. 오류가 아닙니다.
  - CI(영어): `Data collection : Unable to find a datacollector with friendly name 'XPlat code coverage'.`와 `Data collection : Could not find data collector 'XPlat code coverage'`(실행 36340575571 로그)
  - 로컬 한국어 SDK: `데이터 수집: 이름이 'XPlat code coverage'인 datacollector를 찾을 수 없습니다.`(S04-T01 실측)
- 합산되는 cobertura 파일 수 = 수집기가 있는 테스트 프로젝트 수(S04-T01 실측 12 = 전체 테스트 프로젝트 13 − ArchitectureTests)입니다. 보고 경로는 한 단계 패턴(`TestResults/*/`)이라 trx가 복사한 첨부(`TestResults/<trx 이름>/In/**`)가 두 번 합산되지 않습니다.

## 서비스별 빌드 트리거 (경로 필터)

없습니다. 워크플로는 경로 필터 없이 PR마다 솔루션 전체(`EmergencyHub.sln`)를 빌드 · 테스트합니다. 경로 필터 도입 여부는 정하지 않았습니다.

## Docker 이미지 빌드 & 레지스트리 (GHCR)

없습니다. 서비스 컨테이너 이미지 · 레지스트리는 이후 토픽(Phase 4)입니다([기술 스택 · 남은 항목 처리 방식](../03-architecture/tech-stack.md#남은-항목-처리-방식)). CI가 쓰는 이미지는 통합 테스트용 `postgres:<태그>` pull뿐입니다.

## CD (배포 절차)

없습니다. 배포 환경이 없습니다([환경 구성](environments.md#환경-목록-local--dev--staging--prod)). 릴리스는 Git Flow 태그(`vX.Y.Z`)까지입니다([Git 워크플로우](../04-development/git-workflow.md)).

## 브랜치 보호 규칙

GitHub Free private 저장소라 브랜치 보호 · 필수 체크를 설정할 수 없습니다. 규칙으로 지킵니다: `main` · `develop`에 직접 push하지 않고, PR은 Merge commit으로 병합하며, 스프린트 태그는 토픽 PR CI 통과 뒤에 답니다([Git 워크플로우](../04-development/git-workflow.md), [에이전트 워크플로우](../10-delivery/agents.md)).

## 롤백 절차

배포가 없어 해당 없습니다. 배포(CD)를 도입할 때 정합니다.

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-28 | developer | `draft`로 작성: 트리거(`pull_request` develop · main, Draft 포함) · 러너 · 권한 · 동시성, 워크플로 15단계와 실패 때 동작(테스트 실패 시 Coverage report 건너뜀 BL-062, 아티팩트 3종), 커버리지 보고 위치 · 대상, 메서드 커버리지 sponsors 표시(BL-060), ArchitectureTests 수집기 없음 메시지 원문(TD-024), 경로 필터 · 이미지 · CD · 롤백 없음, 브랜치 보호 불가와 규칙 (S04-T03) |
