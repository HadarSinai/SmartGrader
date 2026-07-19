export interface LogResponseDto {
  id: number;
  timestamp: string; // ISO 8601 (UTC)
  userId: number | null;
  lessonId: number | null;
  assignmentId: number | null;
  actionType: string;
  message: string;
  status: string;
  systemSource: string;
}

export interface DeleteOldLogsResultDto {
  deleted: number;
}
