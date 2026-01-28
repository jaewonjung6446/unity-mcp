import WebSocket from 'ws';
import { v4 as uuidv4 } from 'uuid';
import { UnityRequest, UnityResponse } from './types.js';
import { promises as fs } from 'fs';
import path from 'path';

interface PendingRequest {
  resolve: (value: any) => void;
  reject: (reason: any) => void;
  timeout: NodeJS.Timeout;
}

export class UnityBridge {
  private ws: WebSocket | null = null;
  private pendingRequests = new Map<string, PendingRequest>();
  private port = 8090;
  private host = '127.0.0.1';
  private requestTimeout = 10000;
  private reconnectTimer: NodeJS.Timeout | null = null;
  private reconnectAttempt = 0;
  private maxReconnectDelay = 30000;
  private isShuttingDown = false;

  async start(): Promise<void> {
    await this.readConfig();
    try {
      await this.connect();
    } catch {
      console.error(`[Unity Bridge] Could not connect to Unity on port ${this.port}, will retry on next request`);
    }
  }

  private async readConfig(): Promise<void> {
    const configPath = path.resolve(process.cwd(), '../ProjectSettings/McpUnitySettings.json');
    try {
      const content = await fs.readFile(configPath, 'utf-8');
      const json = JSON.parse(content);
      if (json.Port) this.port = parseInt(json.Port, 10);
      if (json.RequestTimeoutSeconds) this.requestTimeout = parseInt(json.RequestTimeoutSeconds, 10) * 1000;
    } catch { }
  }

  private connect(): Promise<void> {
    return new Promise((resolve, reject) => {
      const url = `ws://${this.host}:${this.port}/`;
      this.ws = new WebSocket(url);

      const timeout = setTimeout(() => {
        this.ws?.terminate();
        reject(new Error('Connection timeout'));
      }, this.requestTimeout);

      this.ws.onopen = () => {
        clearTimeout(timeout);
        this.reconnectAttempt = 0;
        console.error('[Unity Bridge] Connected to Unity');
        resolve();
      };

      this.ws.onerror = (err) => {
        clearTimeout(timeout);
        reject(new Error(`WebSocket error: ${err.message}`));
      };

      this.ws.onmessage = (event) => {
        this.handleMessage(event.data.toString());
      };

      this.ws.onclose = () => {
        this.ws = null;
        if (!this.isShuttingDown) {
          this.scheduleReconnect();
        }
      };
    });
  }

  private scheduleReconnect(): void {
    if (this.reconnectTimer) return;
    const delay = Math.min(1000 * Math.pow(2, this.reconnectAttempt), this.maxReconnectDelay);
    this.reconnectAttempt++;
    this.reconnectTimer = setTimeout(async () => {
      this.reconnectTimer = null;
      try {
        await this.connect();
      } catch { }
    }, delay);
  }

  private handleMessage(data: string): void {
    try {
      const response = JSON.parse(data) as UnityResponse;
      if (response.id && this.pendingRequests.has(response.id)) {
        const req = this.pendingRequests.get(response.id)!;
        clearTimeout(req.timeout);
        this.pendingRequests.delete(response.id);
        if (response.error) {
          req.reject(new Error(response.error.message));
        } else {
          req.resolve(response.result);
        }
      }
    } catch (e) {
      console.error('[Unity Bridge] Failed to parse message:', e);
    }
  }

  async sendRequest(request: UnityRequest): Promise<any> {
    if (!this.ws || this.ws.readyState !== WebSocket.OPEN) {
      try {
        await this.connect();
      } catch {
        throw new Error('Not connected to Unity Editor');
      }
    }

    const id = request.id || uuidv4();
    const message = { ...request, id };

    return new Promise((resolve, reject) => {
      const timeout = setTimeout(() => {
        this.pendingRequests.delete(id);
        reject(new Error('Request timed out'));
      }, this.requestTimeout);

      this.pendingRequests.set(id, { resolve, reject, timeout });

      try {
        this.ws!.send(JSON.stringify(message));
      } catch (err) {
        clearTimeout(timeout);
        this.pendingRequests.delete(id);
        reject(err);
      }
    });
  }

  async stop(): Promise<void> {
    this.isShuttingDown = true;
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }
    for (const [id, req] of this.pendingRequests) {
      clearTimeout(req.timeout);
      req.reject(new Error('Shutting down'));
    }
    this.pendingRequests.clear();
    if (this.ws) {
      this.ws.close();
      this.ws = null;
    }
  }
}
