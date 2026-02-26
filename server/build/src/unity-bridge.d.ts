import { UnityRequest } from './types.js';
export interface UnityBridgeOptions {
    port?: number;
}
export declare class UnityBridge {
    private ws;
    private pendingRequests;
    private port;
    private host;
    private requestTimeout;
    private reconnectTimer;
    private reconnectAttempt;
    private maxReconnectDelay;
    private maxReconnectAttempts;
    private isShuttingDown;
    private portFromArgs;
    constructor(options?: UnityBridgeOptions);
    start(): Promise<void>;
    private readConfig;
    private connect;
    private scheduleReconnect;
    private handleMessage;
    sendRequest(request: UnityRequest): Promise<any>;
    stop(): Promise<void>;
}
