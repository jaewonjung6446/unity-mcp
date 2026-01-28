export interface UnityRequest {
    id?: string;
    method: string;
    params: Record<string, unknown>;
}
export interface UnityResponse {
    id: string;
    result?: {
        success: boolean;
        type: string;
        message: string;
        [key: string]: unknown;
    };
    error?: {
        type: string;
        message: string;
    };
}
