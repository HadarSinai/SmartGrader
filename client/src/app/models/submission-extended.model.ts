import { SubmissionResponseDto } from './submission.model';

export interface SubmissionExtended extends SubmissionResponseDto {
  testResults?: {
    passed: number;
    failed: number;
    total: number;
  };
  codePreview?: string;
  executionTime?: number;
  memoryUsage?: number;
  language?: string;
  evaluatedBy?: 'AI' | 'Manual';
}
