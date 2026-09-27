#!/usr/bin/env node
// UserPromptSubmit hook: 사용자 프롬프트 원문을 wiki/08-worklog/raw/YYYY-MM-DD.md 에 누적 기록한다.
// 실패해도 프롬프트 처리를 막지 않도록 항상 exit 0 으로 종료한다.
const fs = require('fs');
const path = require('path');

const pad = (n) => String(n).padStart(2, '0');

let input = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', (chunk) => (input += chunk));
process.stdin.on('end', () => {
  try {
    const payload = JSON.parse(input || '{}');
    const prompt = (payload.prompt || '').trim();
    if (!prompt) return;

    const root = process.env.CLAUDE_PROJECT_DIR || payload.cwd || process.cwd();
    const dir = path.join(root, 'wiki', '08-worklog', 'raw');
    fs.mkdirSync(dir, { recursive: true });

    const now = new Date();
    const date = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
    const time = `${pad(now.getHours())}:${pad(now.getMinutes())}:${pad(now.getSeconds())}`;
    const session = (payload.session_id || 'unknown').slice(0, 8);

    const file = path.join(dir, `${date}.md`);
    if (!fs.existsSync(file)) {
      fs.writeFileSync(file, `# 프롬프트 원문 로그 ${date}\n\n> hook(UserPromptSubmit)이 자동으로 기록합니다. 직접 수정하지 마세요.\n`);
    }

    // 프롬프트 안의 코드블록과 충돌하지 않도록 가장 긴 백틱 연속보다 긴 fence 를 사용한다.
    const longest = Math.max(0, ...(prompt.match(/`+/g) || []).map((s) => s.length));
    const fence = '`'.repeat(Math.max(3, longest + 1));

    fs.appendFileSync(file, `\n## ${time} · session \`${session}\`\n\n${fence}text\n${prompt}\n${fence}\n`);
  } catch (e) {
    process.stderr.write(`[log-prompt] ${e.message}\n`);
  }
});
