import { CreateJobRequest, Job, JobStatus } from "../modals/Job";

const BASE_URL = "http://localhost:5000";

export const fetchJobs = async (): Promise<Job[]> => {
  const response = await fetch(`${BASE_URL}/Jobs`);
  if (!response.ok) {
    throw new Error("Failed to fetch jobs");
  }
  return response.json();
};

export const createJob = async (job: CreateJobRequest): Promise<Job> => {
  const response = await fetch(`${BASE_URL}/Jobs`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(job),
  });

  if (!response.ok) {
    throw new Error("Failed to create job");
  }

  return response.json();
};

export const deleteJob = async (jobId: string): Promise<void> => {
  const response = await fetch(`${BASE_URL}/Jobs/${jobId}`, {
    method: "DELETE",
  });

  if (!response.ok) {
    throw new Error("Failed to delete job");
  }
};

export const deleteJobsByStatus = async (status: JobStatus): Promise<void> => {
  const response = await fetch(`${BASE_URL}/Jobs/${status}`, {
    method: "DELETE",
  });

  if (!response.ok) {
    throw new Error("Failed to delete jobs");
  }
};

export const stopJob = async (jobId: string): Promise<void> => {
  const response = await fetch(`${BASE_URL}/Jobs/${jobId}/stop`, {
    method: "POST",
  });

  if (!response.ok) {
    throw new Error("Failed to stop job");
  }
};

export const restartJob = async (jobId: string): Promise<void> => {
  const response = await fetch(`${BASE_URL}/Jobs/${jobId}/restart`, {
    method: "POST",
  });

  if (!response.ok) {
    throw new Error("Failed to restart job");
  }
};
