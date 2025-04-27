export enum JobStatus {
  Pending = 0,
  InQueue = 1,
  Running = 2,
  Completed = 3,
  Failed = 4,
  Stopped = 5,
}

export enum JobPriority {
  Regular = 0,
  High = 1,
}

export interface Job {
  jobID: string;
  status: JobStatus;
  createdAt: number;
  startedAt: number;
  completedAt: number;
  progress: number;
  name: string;
  priority: JobPriority;
}

export interface JobProgressUpdate {
  jobID: string;
  status: JobStatus;
  progress: number;
}

export interface CreateJobRequest {
  name: string;
  priority: JobPriority;
}
