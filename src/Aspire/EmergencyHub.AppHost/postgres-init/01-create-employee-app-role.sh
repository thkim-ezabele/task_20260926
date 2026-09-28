#!/usr/bin/env bash
# employee_app 롤 초기화 스크립트 (postgres 공식 이미지 /docker-entrypoint-initdb.d, 빈 데이터 볼륨에서 한 번만 실행)
#
# 원본: wiki/04-development/database.md "로컬 DB 구성 (AppHost)", ADR-0011 "롤 생성"
# - AppHost는 WithInitFiles("postgres-init")로, 통합 테스트 fixture(S03-T06)는 Testcontainers
#   WithResourceMapping으로 이 파일을 /docker-entrypoint-initdb.d/ 에 넣는다(두 곳이 같은 파일을 쓴다).
# - 롤만 만든다. Database는 AppHost WithCreationScript
#   "CREATE DATABASE emergency_hub_employee OWNER employee_app" 한 문장이 만든다(재시작 때 42P04 무시).
# - CREATE SCHEMA 금지: 롤 이름과 같은 스키마가 있으면 search_path("$user", public) 때문에
#   테이블 · __EFMigrationsHistory가 public이 아닌 곳에 생긴다(BL-036).
# - 비밀번호는 환경 변수 EMPLOYEE_APP_PASSWORD(AppHost 매개변수 employee-app-password)로 받아
#   psql 변수 :'employee_app_password'로 인용한다. 값을 출력하지 않는다.
# - 실행 비트가 없으면 엔트리포인트가 source로 실행하므로 exit · set 변경을 쓰지 않는다.
# - 줄바꿈은 LF여야 한다(.gitattributes eol=lf).

: "${EMPLOYEE_APP_PASSWORD:?EMPLOYEE_APP_PASSWORD 환경 변수가 비어 있습니다}"

psql -v ON_ERROR_STOP=1 \
     -v employee_app_password="$EMPLOYEE_APP_PASSWORD" \
     --no-psqlrc \
     --username "$POSTGRES_USER" \
     --dbname "$POSTGRES_DB" <<-'EOSQL'
	-- 이 세션에서 오류가 나도 서버 로그에 문장(비밀번호 리터럴)이 남지 않게 한다.
	SET log_statement = 'none';
	SET log_min_error_statement = 'panic';

	CREATE ROLE employee_app
	    LOGIN
	    NOSUPERUSER
	    NOCREATEDB
	    NOCREATEROLE
	    NOREPLICATION
	    NOBYPASSRLS
	    PASSWORD :'employee_app_password';
EOSQL
