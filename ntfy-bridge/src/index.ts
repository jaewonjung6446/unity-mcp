import { spawn, execSync } from "child_process";
import EventSource from "eventsource";

// 환경변수
const NTFY_TOPIC = process.env.NTFY_TOPIC || "jaewon-claude-cmd";
const NTFY_RESULT_TOPIC = process.env.NTFY_RESULT_TOPIC || "jaewon-claude-done";
const WORKSPACE = process.env.WORKSPACE || "/opt/render/project/src/workspace";
const GITHUB_TOKEN = process.env.GITHUB_TOKEN || "";

// 프로젝트 설정 (레포지토리 이름)
const PROJECTS: Record<string, string> = {
  "potion": "jaewonjung6446/potion",
  "unity-mcp": "jaewonjung6446/unity-mcp"
};

// GitHub URL 생성 (토큰 포함)
function getRepoUrl(repo: string): string {
  if (GITHUB_TOKEN) {
    return `https://${GITHUB_TOKEN}@github.com/${repo}.git`;
  }
  return `https://github.com/${repo}.git`;
}

// ntfy로 결과 전송
async function sendNotification(title: string, message: string, tags: string = "robot") {
  try {
    const body = `[${title}]\n${message}`.slice(0, 4000);
    const encoder = new TextEncoder();
    const bodyBytes = encoder.encode(body);

    await fetch(`https://ntfy.sh/${NTFY_RESULT_TOPIC}`, {
      method: "POST",
      headers: {
        "Title": "Claude",
        "Tags": tags
      },
      body: bodyBytes
    });
    console.log(`알림 전송: ${title}`);
  } catch (error) {
    console.error("알림 전송 실패:", error);
  }
}

// 레포지토리 클론 또는 풀
function setupRepo(name: string, repo: string): string {
  const repoPath = `${WORKSPACE}/${name}`;
  const url = getRepoUrl(repo);
  try {
    // 이미 있으면 pull
    execSync(`git -C ${repoPath} pull`, { stdio: "pipe" });
    console.log(`${name} 업데이트 완료`);
  } catch {
    // 없으면 clone
    try {
      execSync(`mkdir -p ${WORKSPACE}`, { stdio: "pipe" });
      execSync(`git clone ${url} ${repoPath}`, { stdio: "pipe" });
      console.log(`${name} 클론 완료`);
    } catch (e) {
      console.error(`${name} 설정 실패:`, e);
    }
  }
  return repoPath;
}

// Claude Code 실행
async function runClaudeCode(prompt: string, workDir: string): Promise<string> {
  return new Promise((resolve) => {
    let output = "";
    let error = "";

    const claude = spawn("npx", [
      "@anthropic-ai/claude-code",
      "-p", prompt,
      "--dangerously-skip-permissions",
      "--output-format", "text"
    ], {
      cwd: workDir,
      env: { ...process.env }
    });

    claude.stdout.on("data", (data) => {
      output += data.toString();
    });

    claude.stderr.on("data", (data) => {
      error += data.toString();
    });

    claude.on("close", (code) => {
      if (code === 0) {
        resolve(output || "작업 완료");
      } else {
        resolve(`오류 (code ${code}): ${error || output}`);
      }
    });

    claude.on("error", (err) => {
      resolve(`실행 오류: ${err.message}`);
    });

    // 10분 타임아웃
    setTimeout(() => {
      claude.kill();
      resolve("타임아웃: 10분 초과");
    }, 10 * 60 * 1000);
  });
}

// 메시지 파싱: "프로젝트: 명령" 형식
function parseMessage(message: string): { project: string | null; command: string } {
  const match = message.match(/^(\w+[-\w]*):\s*(.+)$/s);
  if (match) {
    return { project: match[1].toLowerCase(), command: match[2].trim() };
  }
  return { project: null, command: message };
}

// 메시지 처리
async function handleMessage(message: string) {
  console.log(`메시지 수신: ${message}`);

  const { project, command } = parseMessage(message);

  // 프로젝트 지정 없으면 안내
  if (!project) {
    await sendNotification("안내", `프로젝트를 지정해주세요.\n예: potion: ${command}\n\n사용 가능: ${Object.keys(PROJECTS).join(", ")}`, "warning");
    return;
  }

  // 프로젝트 확인
  const repoUrl = PROJECTS[project];
  if (!repoUrl) {
    await sendNotification("오류", `알 수 없는 프로젝트: ${project}\n사용 가능: ${Object.keys(PROJECTS).join(", ")}`, "x");
    return;
  }

  // 시작 알림
  await sendNotification(`${project} 시작`, command, "hourglass");

  // 레포 준비
  const workDir = setupRepo(project, repoUrl);

  // Claude Code 실행
  const response = await runClaudeCode(command, workDir);

  // 결과 알림
  await sendNotification(`${project} 완료`, response, "white_check_mark");
}

// 초기 설정
function initWorkspace() {
  try {
    execSync(`mkdir -p ${WORKSPACE}`, { stdio: "pipe" });
  } catch {
    // Windows에서는 실패할 수 있음
  }
  console.log(`워크스페이스: ${WORKSPACE}`);
  console.log(`등록된 프로젝트: ${Object.keys(PROJECTS).join(", ")}`);
}

// ntfy 구독 시작
function subscribeToNtfy() {
  const url = `https://ntfy.sh/${NTFY_TOPIC}/sse`;
  console.log(`ntfy 구독: ${NTFY_TOPIC}`);
  console.log(`결과 토픽: ${NTFY_RESULT_TOPIC}`);

  const eventSource = new EventSource(url);

  eventSource.onopen = () => {
    console.log("ntfy 연결됨");
    sendNotification("서버 시작", `프로젝트: ${Object.keys(PROJECTS).join(", ")}\n\n사용법: 프로젝트명: 명령`, "rocket");
  };

  eventSource.onmessage = async (event) => {
    try {
      const data = JSON.parse(event.data);
      if (data.event === "message" && data.message) {
        await handleMessage(data.message);
      }
    } catch {
      // JSON 파싱 실패는 무시
    }
  };

  eventSource.onerror = (error) => {
    console.error("ntfy 연결 오류:", error);
  };
}

// 시작
console.log("ntfy-claude-bridge 시작");
initWorkspace();
subscribeToNtfy();

process.on("SIGINT", () => {
  console.log("종료 중...");
  process.exit(0);
});
