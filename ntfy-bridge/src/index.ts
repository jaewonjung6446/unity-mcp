import { spawn } from "child_process";
import EventSource from "eventsource";

// 환경변수
const NTFY_TOPIC = process.env.NTFY_TOPIC || "jaewon-claude-cmd";
const NTFY_RESULT_TOPIC = process.env.NTFY_RESULT_TOPIC || "jaewon-claude-done";
const WORK_DIR = process.env.WORK_DIR || "/app/workspace";

// ntfy로 결과 전송
async function sendNotification(title: string, message: string, tags: string = "robot") {
  try {
    await fetch(`https://ntfy.sh/${NTFY_RESULT_TOPIC}`, {
      method: "POST",
      headers: {
        "Title": title,
        "Tags": tags,
        "Content-Type": "text/plain; charset=utf-8"
      },
      body: message.slice(0, 4000) // ntfy 메시지 길이 제한
    });
    console.log(`알림 전송: ${title}`);
  } catch (error) {
    console.error("알림 전송 실패:", error);
  }
}

// Claude Code 실행
async function runClaudeCode(prompt: string): Promise<string> {
  return new Promise((resolve) => {
    let output = "";
    let error = "";

    const claude = spawn("claude", [
      "-p", prompt,
      "--dangerously-skip-permissions",
      "--output-format", "text"
    ], {
      cwd: WORK_DIR,
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
        resolve(output || "작업 완료 (출력 없음)");
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

// 메시지 처리
async function handleMessage(message: string) {
  console.log(`메시지 수신: ${message}`);

  // 시작 알림
  await sendNotification("작업 시작", message, "hourglass");

  // Claude Code 실행
  const response = await runClaudeCode(message);

  // 결과 알림
  await sendNotification("작업 완료", response, "white_check_mark");
}

// ntfy 구독 시작
function subscribeToNtfy() {
  const url = `https://ntfy.sh/${NTFY_TOPIC}/sse`;
  console.log(`ntfy 구독 시작: ${NTFY_TOPIC}`);
  console.log(`결과 토픽: ${NTFY_RESULT_TOPIC}`);
  console.log(`작업 디렉토리: ${WORK_DIR}`);

  const eventSource = new EventSource(url);

  eventSource.onopen = () => {
    console.log("ntfy 연결됨");
    sendNotification("서버 시작", "ntfy-claude-bridge 준비 완료", "rocket");
  };

  eventSource.onmessage = async (event) => {
    try {
      const data = JSON.parse(event.data);
      if (data.event === "message" && data.message) {
        await handleMessage(data.message);
      }
    } catch (error) {
      // JSON 파싱 실패는 무시 (keepalive 등)
    }
  };

  eventSource.onerror = (error) => {
    console.error("ntfy 연결 오류:", error);
  };
}

// 서버 시작
console.log("ntfy-claude-bridge 시작");
subscribeToNtfy();

process.on("SIGINT", () => {
  console.log("종료 중...");
  process.exit(0);
});
