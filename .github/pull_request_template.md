<!--
토픽 PR(feature/prd-*)은 /prd에서 Draft로 만들고, /retro 후 Ready로 바꿔 Merge commit으로 병합합니다.
토픽 밖 PR(feature/*, bugfix/*)은 "토픽" 절을 지우고 씁니다.
-->

## 개요

<!-- 무엇을, 왜 바꾸는지 한두 문장으로 적습니다. -->

## 토픽

- PRD: <!-- wiki/10-delivery/prd/PRD-NNN-....md -->
- 스프린트: <!-- S01, S02 (sprint/SNN 태그) -->
- 회고: <!-- wiki/10-delivery/retros/RETRO-PRD-NNN.md -->

### FR 충족 요약

| 요구사항 | 결과 |
|---|---|
| FR-01 | 충족 / 부분 충족 / 이관(BL-NNN) |

## 변경 사항

-

## 관련 문서

<!-- ADR, worklog, 갱신한 위키 문서 링크 -->

-

## 체크리스트

- [ ] 브랜치와 대상이 Git Flow 규칙에 맞다 (토픽 · `feature/*` · `bugfix/*` → `develop` / `release/*` · `hotfix/*` → `main`)
- [ ] PR 제목이 Conventional Commits 형식이다
- [ ] 빌드와 모든 테스트가 통과한다 (ADR-0006)
- [ ] 관련 위키 문서(frontmatter `updated`, 변경 이력)를 갱신했다
- [ ] 백로그 / 기술부채에 `new` 항목이 남아 있지 않다
- [ ] (토픽 PR) 모든 스프린트가 `done`이고 회고를 마쳤다
