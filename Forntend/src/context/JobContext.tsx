import React, { createContext, useContext, useEffect, useState } from "react";
import { Job, JobProgressUpdate } from "../modals/Job";
import { fetchJobs } from "../services/jobApi";
import signalRService from "../services/signalRService";

interface JobContextType {
  jobs: Job[];
  loading: boolean;
  error: string | null;
  refreshJobs: () => Promise<void>;
}

const JobContext = createContext<JobContextType | undefined>(undefined);

export const JobProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [jobs, setJobs] = useState<Job[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refreshJobs = async () => {
    setLoading(true);
    try {
      const jobsData = await fetchJobs();

      setJobs(jobsData);
      setError(null);
    } catch (err) {
      setError("Failed to fetch jobs");
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    refreshJobs();

    // Start SignalR connection
    signalRService.startConnection();

    // Subscribe to job status updates
    const unsubscribe = signalRService.onJobProgressUpdate(
      (update: JobProgressUpdate) => {
        setJobs((prevJobs) =>
          prevJobs.map((job) =>
            job.jobID === update.jobID
              ? { ...job, status: update.status, progress: update.progress }
              : job
          )
        );
      }
    );

    return () => {
      unsubscribe();
      signalRService.stopConnection();
    };
  }, []);

  return (
    <JobContext.Provider value={{ jobs, loading, error, refreshJobs }}>
      {children}
    </JobContext.Provider>
  );
};

export const useJobs = () => {
  const context = useContext(JobContext);
  if (context === undefined) {
    throw new Error("useJobs must be used within a JobProvider");
  }
  return context;
};
