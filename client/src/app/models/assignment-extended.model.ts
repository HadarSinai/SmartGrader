import { AssignmentResponseDto } from './assignment.model';

export interface AssignmentExtended extends AssignmentResponseDto {
  difficulty?: 'Easy' | 'Medium' | 'Hard';
  completionRate?: number;
  averageScore?: number;
  dueDate?: string;
  maxScore?: number;
  status?: 'Active' | 'Closed' | 'Draft';
}
