import { SubmissionResponseDto } from './submission.model';

export interface SubmissionExtended extends SubmissionResponseDto {
  codePreview?: string;
  executionTime?: number;
  memoryUsage?: number;
  language?: string;
  evaluatedBy?: 'AI' | 'Manual';
}
